using System.Globalization;

namespace PracticeCalculator;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new CalculatorForm());
    }
}

internal sealed class CalculatorForm : Form
{
    private readonly TextBox _display = new()
    {
        ReadOnly = true,
        Text = "0",
        TextAlign = HorizontalAlignment.Right,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 24, FontStyle.Bold)
    };

    private decimal _accumulator;
    private string? _pendingOperation;
    private bool _startNewNumber = true;
    private int _fontIndex;
    private readonly float[] _fontSizes = [24, 32, 40];
    private readonly string _saveFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PracticeCalculator",
        "saved-result.txt");

    public CalculatorForm()
    {
        Text = "Калькулятор";
        ClientSize = new Size(390, 520);
        MinimumSize = new Size(360, 480);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(_display, 0, 0);

        var keys = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6 };
        for (var i = 0; i < 4; i++) keys.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        for (var i = 0; i < 6; i++) keys.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));

        Add(keys, "7", 0, 0, (_, _) => Digit("7"));
        Add(keys, "8", 1, 0, (_, _) => Digit("8"));
        Add(keys, "9", 2, 0, (_, _) => Digit("9"));
        Add(keys, "÷", 3, 0, (_, _) => ChooseOperation("/"));
        Add(keys, "4", 0, 1, (_, _) => Digit("4"));
        Add(keys, "5", 1, 1, (_, _) => Digit("5"));
        Add(keys, "6", 2, 1, (_, _) => Digit("6"));
        Add(keys, "×", 3, 1, (_, _) => ChooseOperation("*"));
        Add(keys, "1", 0, 2, (_, _) => Digit("1"));
        Add(keys, "2", 1, 2, (_, _) => Digit("2"));
        Add(keys, "3", 2, 2, (_, _) => Digit("3"));
        Add(keys, "−", 3, 2, (_, _) => ChooseOperation("-"));
        Add(keys, "0", 0, 3, (_, _) => Digit("0"));
        Add(keys, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, 1, 3, (_, _) => DecimalPoint());
        Add(keys, "=", 2, 3, (_, _) => Calculate());
        Add(keys, "+", 3, 3, (_, _) => ChooseOperation("+"));
        Add(keys, "C", 0, 4, (_, _) => Clear());
        Add(keys, "⌫", 1, 4, (_, _) => Backspace());
        Add(keys, "Сохранить", 2, 4, (_, _) => SaveResult(), columnSpan: 2);
        Add(keys, "Загрузить", 0, 5, (_, _) => LoadResult(), columnSpan: 2);
        Add(keys, "Шрифт", 2, 5, (_, _) => ChangeFontSize(), columnSpan: 2);

        root.Controls.Add(keys, 0, 1);
        Controls.Add(root);
    }

    private static void Add(
        TableLayoutPanel panel,
        string text,
        int column,
        int row,
        EventHandler onClick,
        int columnSpan = 1)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            Font = new Font("Segoe UI", text.Length > 2 ? 11 : 16, FontStyle.Bold)
        };
        button.Click += onClick;
        panel.Controls.Add(button, column, row);
        if (columnSpan > 1) panel.SetColumnSpan(button, columnSpan);
    }

    private void Digit(string digit)
    {
        if (_startNewNumber || _display.Text == "0" || _display.Text.StartsWith("Ошибка"))
        {
            _display.Text = digit;
            _startNewNumber = false;
            return;
        }
        if (_display.Text.Length < 28) _display.Text += digit;
    }

    private void DecimalPoint()
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        if (_startNewNumber)
        {
            _display.Text = "0" + separator;
            _startNewNumber = false;
        }
        else if (!_display.Text.Contains(separator))
        {
            _display.Text += separator;
        }
    }

    private bool TryReadDisplay(out decimal value) =>
        decimal.TryParse(_display.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out value);

    private void ChooseOperation(string operation)
    {
        if (!TryReadDisplay(out var current)) return;
        if (_pendingOperation is not null && !_startNewNumber)
        {
            if (!TryApply(_accumulator, current, _pendingOperation, out _accumulator)) return;
            Show(_accumulator);
        }
        else
        {
            _accumulator = current;
        }
        _pendingOperation = operation;
        _startNewNumber = true;
    }

    private void Calculate()
    {
        if (_pendingOperation is null || !TryReadDisplay(out var right)) return;
        if (!TryApply(_accumulator, right, _pendingOperation, out _accumulator)) return;
        Show(_accumulator);
        _pendingOperation = null;
        _startNewNumber = true;
    }

    private bool TryApply(decimal left, decimal right, string operation, out decimal result)
    {
        try
        {
            result = operation switch
            {
                "+" => left + right,
                "-" => left - right,
                "*" => left * right,
                "/" when right == 0 => throw new DivideByZeroException(),
                "/" => left / right,
                _ => right
            };
            return true;
        }
        catch (DivideByZeroException)
        {
            result = 0;
            _display.Text = "Ошибка: деление на ноль";
        }
        catch (OverflowException)
        {
            result = 0;
            _display.Text = "Ошибка: слишком большое число";
        }
        _pendingOperation = null;
        _startNewNumber = true;
        return false;
    }

    private void Show(decimal value) =>
        _display.Text = value.ToString("0.############################", CultureInfo.CurrentCulture);

    private void Clear()
    {
        _display.Text = "0";
        _accumulator = 0;
        _pendingOperation = null;
        _startNewNumber = true;
    }

    private void Backspace()
    {
        if (_startNewNumber || _display.Text.StartsWith("Ошибка")) return;
        _display.Text = _display.Text.Length > 1 ? _display.Text[..^1] : "0";
    }

    private void SaveResult()
    {
        if (!TryReadDisplay(out _)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(_saveFile)!);
        File.WriteAllText(_saveFile, _display.Text);
        MessageBox.Show("Результат сохранён.", "Калькулятор", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LoadResult()
    {
        if (!File.Exists(_saveFile))
        {
            MessageBox.Show("Сохранённый результат не найден.", "Калькулятор", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var value = File.ReadAllText(_saveFile).Trim();
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out _))
        {
            MessageBox.Show("Файл результата повреждён.", "Калькулятор", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        _display.Text = value;
        _pendingOperation = null;
        _startNewNumber = true;
    }

    private void ChangeFontSize()
    {
        _fontIndex = (_fontIndex + 1) % _fontSizes.Length;
        _display.Font = new Font(_display.Font.FontFamily, _fontSizes[_fontIndex], FontStyle.Bold);
    }
}

