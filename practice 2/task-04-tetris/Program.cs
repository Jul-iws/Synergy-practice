using System.Diagnostics;
using System.Text;

namespace ConsoleTetris;

internal static class Program
{
    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        do
        {
            new TetrisGame().Run();
            Console.SetCursorPosition(0, TetrisGame.Height + 4);
            Console.Write("Игра окончена. Нажмите Y для новой игры: ");
        } while (Console.ReadKey(true).Key == ConsoleKey.Y);
        Console.CursorVisible = true;
    }
}

internal sealed class TetrisGame
{
    public const int Width = 10;
    public const int Height = 20;
    private readonly int[,] _field = new int[Height, Width];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private Piece _piece = Piece.Random();
    private long _lastFall;
    private int _score;
    private bool _gameOver;

    public void Run()
    {
        Console.Clear();
        if (!Fits(_piece, _piece.X, _piece.Y, _piece.Rotation)) _gameOver = true;
        while (!_gameOver)
        {
            ReadInput();
            if (_clock.ElapsedMilliseconds - _lastFall >= 500)
            {
                MoveDown();
                _lastFall = _clock.ElapsedMilliseconds;
            }
            Draw();
            Thread.Sleep(16);
        }
        Draw();
    }

    private void ReadInput()
    {
        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.LeftArrow: TryMove(-1, 0); break;
                case ConsoleKey.RightArrow: TryMove(1, 0); break;
                case ConsoleKey.DownArrow: MoveDown(); _score++; break;
                case ConsoleKey.UpArrow:
                case ConsoleKey.Spacebar: TryRotate(); break;
                case ConsoleKey.Escape: _gameOver = true; break;
            }
        }
    }

    private void TryMove(int dx, int dy)
    {
        if (Fits(_piece, _piece.X + dx, _piece.Y + dy, _piece.Rotation))
        {
            _piece.X += dx;
            _piece.Y += dy;
        }
    }

    private void MoveDown()
    {
        if (Fits(_piece, _piece.X, _piece.Y + 1, _piece.Rotation))
        {
            _piece.Y++;
            return;
        }
        LockPiece();
        ClearLines();
        _piece = Piece.Random();
        if (!Fits(_piece, _piece.X, _piece.Y, _piece.Rotation)) _gameOver = true;
    }

    private void TryRotate()
    {
        var nextRotation = (_piece.Rotation + 1) % _piece.Shapes.Length;
        foreach (var kick in new[] { 0, -1, 1, -2, 2 })
        {
            if (!Fits(_piece, _piece.X + kick, _piece.Y, nextRotation)) continue;
            _piece.X += kick;
            _piece.Rotation = nextRotation;
            return;
        }
    }

    private bool Fits(Piece piece, int newX, int newY, int rotation)
    {
        var shape = piece.Shapes[rotation];
        for (var y = 0; y < shape.GetLength(0); y++)
        for (var x = 0; x < shape.GetLength(1); x++)
        {
            if (shape[y, x] == 0) continue;
            var px = newX + x;
            var py = newY + y;
            if (px < 0 || px >= Width || py < 0 || py >= Height || _field[py, px] != 0) return false;
        }
        return true;
    }

    private void LockPiece()
    {
        var shape = _piece.Shapes[_piece.Rotation];
        for (var y = 0; y < shape.GetLength(0); y++)
        for (var x = 0; x < shape.GetLength(1); x++)
            if (shape[y, x] != 0) _field[_piece.Y + y, _piece.X + x] = _piece.Color;
    }

    private void ClearLines()
    {
        var cleared = 0;
        for (var y = Height - 1; y >= 0; y--)
        {
            var full = true;
            for (var x = 0; x < Width; x++) full &= _field[y, x] != 0;
            if (!full) continue;
            cleared++;
            for (var pull = y; pull > 0; pull--)
                for (var x = 0; x < Width; x++)
                    _field[pull, x] = _field[pull - 1, x];
            for (var x = 0; x < Width; x++) _field[0, x] = 0;
            y++;
        }
        _score += cleared switch { 1 => 100, 2 => 300, 3 => 500, 4 => 800, _ => 0 };
    }

    private void Draw()
    {
        var frame = (int[,])_field.Clone();
        var shape = _piece.Shapes[_piece.Rotation];
        for (var y = 0; y < shape.GetLength(0); y++)
        for (var x = 0; x < shape.GetLength(1); x++)
            if (shape[y, x] != 0 && _piece.Y + y is >= 0 and < Height)
                frame[_piece.Y + y, _piece.X + x] = _piece.Color;

        var sb = new StringBuilder();
        sb.AppendLine($"Счёт: {_score}     Управление: ← → ↓, поворот: ↑/Space, выход: Esc");
        sb.AppendLine("┌" + new string('─', Width * 2) + "┐");
        for (var y = 0; y < Height; y++)
        {
            sb.Append('│');
            for (var x = 0; x < Width; x++) sb.Append(frame[y, x] == 0 ? "  " : "██");
            sb.AppendLine("│");
        }
        sb.AppendLine("└" + new string('─', Width * 2) + "┘");
        Console.SetCursorPosition(0, 0);
        Console.Write(sb.ToString());
    }
}

internal sealed class Piece
{
    private static readonly int[][,] I =
    [
        new int[,] { { 1, 1, 1, 1 } },
        new int[,] { { 1 }, { 1 }, { 1 }, { 1 } }
    ];
    private static readonly int[][,] O = [new int[,] { { 1, 1 }, { 1, 1 } }];
    private static readonly int[][,] T =
    [
        new int[,] { { 0, 1, 0 }, { 1, 1, 1 } },
        new int[,] { { 1, 0 }, { 1, 1 }, { 1, 0 } },
        new int[,] { { 1, 1, 1 }, { 0, 1, 0 } },
        new int[,] { { 0, 1 }, { 1, 1 }, { 0, 1 } }
    ];
    private static readonly int[][,] L =
    [
        new int[,] { { 1, 0 }, { 1, 0 }, { 1, 1 } },
        new int[,] { { 1, 1, 1 }, { 1, 0, 0 } },
        new int[,] { { 1, 1 }, { 0, 1 }, { 0, 1 } },
        new int[,] { { 0, 0, 1 }, { 1, 1, 1 } }
    ];
    private static readonly int[][,] J =
    [
        new int[,] { { 0, 1 }, { 0, 1 }, { 1, 1 } },
        new int[,] { { 1, 0, 0 }, { 1, 1, 1 } },
        new int[,] { { 1, 1 }, { 1, 0 }, { 1, 0 } },
        new int[,] { { 1, 1, 1 }, { 0, 0, 1 } }
    ];
    private static readonly int[][,] S =
    [
        new int[,] { { 0, 1, 1 }, { 1, 1, 0 } },
        new int[,] { { 1, 0 }, { 1, 1 }, { 0, 1 } }
    ];
    private static readonly int[][,] Z =
    [
        new int[,] { { 1, 1, 0 }, { 0, 1, 1 } },
        new int[,] { { 0, 1 }, { 1, 1 }, { 1, 0 } }
    ];
    private static readonly int[][][,] All = [I, O, T, L, J, S, Z];

    public required int[][,] Shapes { get; init; }
    public required int Color { get; init; }
    public int X { get; set; } = 3;
    public int Y { get; set; }
    public int Rotation { get; set; }

    public static Piece Random()
    {
        var index = System.Random.Shared.Next(All.Length);
        return new Piece { Shapes = All[index], Color = index + 1 };
    }
}

