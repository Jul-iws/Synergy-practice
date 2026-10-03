using System;
using System.IO;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PracticeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PracticeScene.unity";
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    private const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    public static void BuildScene()
    {
        ImportTextMeshProResources();
        EnsureFolders();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var blue = CreateMaterial("PlayerBlue", new Color(0.08f, 0.38f, 0.88f));
        var groundMaterial = CreateMaterial("Ground", new Color(0.12f, 0.18f, 0.22f));
        var platformMaterial = CreateMaterial("Platform", new Color(0.28f, 0.36f, 0.42f));
        var gold = CreateMaterial("CoinGold", new Color(1f, 0.62f, 0.05f), true);
        var green = CreateMaterial("BonusGreen", new Color(0.08f, 0.82f, 0.35f), true);
        var red = CreateMaterial("BarrelRed", new Color(0.72f, 0.08f, 0.06f));
        var orange = CreateMaterial("ObstacleOrange", new Color(1f, 0.28f, 0.04f), true);
        var white = CreateMaterial("White", new Color(0.85f, 0.9f, 0.95f));
        var particleMaterial = CreateParticleMaterial();

        CreateLighting();
        CreateEnvironment(groundMaterial, platformMaterial, white);

        var ui = CreateInterface();
        var player = CreatePlayer(blue, particleMaterial, ui.coinLabel, ui.healthLabel);
        CreateCinemachineCamera(player.transform);
        CreateCoins(gold);
        CreateBonusZone(green);

        var explosionPrefab = CreateExplosionPrefab(particleMaterial);
        CreateBarrels(red, explosionPrefab);
        CreateMovingObstacles(orange);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeGameObject = player;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("PRACTICE_SCENE_BUILT: " + ScenePath);
    }

    public static void ImportTmpResources()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null)
        {
            Debug.Log("TMP_RESOURCES_ALREADY_IMPORTED");
            return;
        }

        var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
        if (package == null) throw new InvalidOperationException("Unable to locate the UGUI package.");

        var packagePath = Path.Combine(package.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
        if (!File.Exists(packagePath)) throw new FileNotFoundException("TMP Essential Resources package is missing.", packagePath);

        AssetDatabase.ImportPackage(packagePath, false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        Debug.Log("TMP_RESOURCES_IMPORT_REQUESTED");
    }

    public static void ValidateScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new InvalidOperationException("PracticeScene.unity was not created.");

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RequireOne<SimplePlayerController>("player controller");
        RequireOne<PlayerHealth>("player health");
        RequireOne<Wallet>("wallet");
        RequireOne<CinemachineCamera>("Cinemachine camera");
        RequireOne<CinemachineBrain>("Cinemachine brain");
        RequireCount<Coin>(5, "coins");
        RequireOne<BonusZone>("bonus zone");
        RequireCount<ExplosiveBarrel>(2, "explosive barrels");
        RequireCount<MovingObstacle>(2, "moving obstacles");
        RequireOne<PlayerMovementParticles>("movement particles controller");

        if (EditorBuildSettings.scenes.Length != 1 || EditorBuildSettings.scenes[0].path != ScenePath)
            throw new InvalidOperationException("Practice scene is not the only enabled build scene.");

        Debug.Log("PRACTICE_PROJECT_VALIDATION_OK");
    }

    private static void ImportTextMeshProResources()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) == null)
        {
            ImportTmpResources();
        }
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Animations");
        EnsureFolder("Assets/Scenes");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static Material CreateMaterial(string name, Color color, bool emission = false)
    {
        var path = $"Assets/Materials/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateParticleMaterial()
    {
        const string path = "Assets/Materials/Particles.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(shader) { name = "Particles" };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateLighting()
    {
        var lightObject = new GameObject("Sun", typeof(Light));
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        var light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.94f, 0.82f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.22f, 0.35f, 0.52f);
        RenderSettings.ambientEquatorColor = new Color(0.15f, 0.2f, 0.26f);
        RenderSettings.ambientGroundColor = new Color(0.07f, 0.08f, 0.1f);
    }

    private static void CreateEnvironment(Material ground, Material platform, Material accent)
    {
        CreatePrimitive("Ground", PrimitiveType.Cube, new Vector3(0f, -0.5f, 2f), new Vector3(24f, 1f, 28f), ground);
        CreatePrimitive("StartPlatform", PrimitiveType.Cube, new Vector3(0f, 0.15f, -7f), new Vector3(7f, 0.3f, 5f), platform);
        CreatePrimitive("CenterPlatform", PrimitiveType.Cube, new Vector3(0f, 0.4f, 2f), new Vector3(9f, 0.8f, 5f), platform);
        CreatePrimitive("BonusPlatform", PrimitiveType.Cube, new Vector3(6f, 0.15f, 4f), new Vector3(5f, 0.3f, 6f), platform);

        CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-11.5f, 1f, 2f), new Vector3(1f, 2f, 28f), accent);
        CreatePrimitive("RightWall", PrimitiveType.Cube, new Vector3(11.5f, 1f, 2f), new Vector3(1f, 2f, 28f), accent);
        CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0f, 1f, 15.5f), new Vector3(24f, 2f, 1f), accent);

        var rampLeft = CreatePrimitive("RampLeft", PrimitiveType.Cube, new Vector3(-5.5f, 0.7f, 7f), new Vector3(4f, 0.5f, 7f), platform);
        rampLeft.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        var rampRight = CreatePrimitive("RampRight", PrimitiveType.Cube, new Vector3(5.5f, 0.7f, 9f), new Vector3(4f, 0.5f, 7f), platform);
        rampRight.transform.rotation = Quaternion.Euler(-10f, 0f, 0f);
    }

    private static GameObject CreatePlayer(
        Material material,
        Material particleMaterial,
        TMP_Text coinLabel,
        TMP_Text healthLabel)
    {
        var player = CreatePrimitive("Player", PrimitiveType.Capsule, new Vector3(0f, 1.2f, -7f), Vector3.one, material);
        UnityEngine.Object.DestroyImmediate(player.GetComponent<Collider>());

        var characterController = player.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.stepOffset = 0.35f;

        var movement = player.AddComponent<SimplePlayerController>();
        var health = player.AddComponent<PlayerHealth>();
        var wallet = player.AddComponent<Wallet>();
        var animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = CreateAnimatorController();

        SetObject(wallet, "counter", coinLabel);

        var particleObject = new GameObject("MovementParticles", typeof(ParticleSystem));
        particleObject.transform.SetParent(player.transform, false);
        particleObject.transform.localPosition = new Vector3(0f, -0.9f, 0f);
        particleObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var particles = particleObject.GetComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = 0.45f;
        main.startSpeed = 1.2f;
        main.startSize = 0.18f;
        main.startColor = new Color(0.55f, 0.85f, 1f, 0.8f);
        var emission = particles.emission;
        emission.rateOverTime = 22f;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.3f;
        shape.angle = 18f;
        particleObject.GetComponent<ParticleSystemRenderer>().material = particleMaterial;

        var movementParticles = player.AddComponent<PlayerMovementParticles>();
        SetObject(movementParticles, "movementParticles", particles);
        SetObject(movementParticles, "playerController", movement);

        var healthDisplay = player.AddComponent<HealthDisplay>();
        SetObject(healthDisplay, "playerHealth", health);
        SetObject(healthDisplay, "label", healthLabel);

        return player;
    }

    private static RuntimeAnimatorController CreateAnimatorController()
    {
        const string controllerPath = "Assets/Animations/Player.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null) return existing;

        var idle = CreateScaleClip("PlayerIdle", false);
        var bonus = CreateScaleClip("PlayerBonus", true);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("InBonusZone", AnimatorControllerParameterType.Bool);

        var stateMachine = controller.layers[0].stateMachine;
        var idleState = stateMachine.AddState("Normal");
        var bonusState = stateMachine.AddState("Bonus");
        idleState.motion = idle;
        bonusState.motion = bonus;
        stateMachine.defaultState = idleState;

        var toBonus = idleState.AddTransition(bonusState);
        toBonus.hasExitTime = false;
        toBonus.duration = 0.15f;
        toBonus.AddCondition(AnimatorConditionMode.If, 0f, "InBonusZone");

        var toNormal = bonusState.AddTransition(idleState);
        toNormal.hasExitTime = false;
        toNormal.duration = 0.15f;
        toNormal.AddCondition(AnimatorConditionMode.IfNot, 0f, "InBonusZone");
        return controller;
    }

    private static AnimationClip CreateScaleClip(string name, bool pulse)
    {
        var path = $"Assets/Animations/{name}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) return existing;

        var clip = new AnimationClip { name = name };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        foreach (var axis in new[] { "x", "y", "z" })
        {
            var curve = pulse
                ? new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.13f), new Keyframe(1f, 1f))
                : AnimationCurve.Constant(0f, 1f, 1f);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), $"m_LocalScale.{axis}"),
                curve);
        }

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void CreateCinemachineCamera(Transform target)
    {
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 8f, -15f), Quaternion.Euler(20f, 0f, 0f));
        var camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 58f;
        camera.clearFlags = CameraClearFlags.Skybox;

        var virtualCameraObject = new GameObject("CinemachineCamera");
        virtualCameraObject.transform.position = cameraObject.transform.position;
        var virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Follow = target;
        virtualCamera.LookAt = target;

        var follow = virtualCameraObject.AddComponent<CinemachineFollow>();
        follow.FollowOffset = new Vector3(0f, 6.5f, -10f);
        var composer = virtualCameraObject.AddComponent<CinemachineRotationComposer>();
        composer.TargetOffset = new Vector3(0f, 0.7f, 0f);
        composer.Damping = new Vector2(0.4f, 0.4f);
    }

    private static void CreateCoins(Material material)
    {
        var positions = new[]
        {
            new Vector3(-2.2f, 1.2f, -4f), new Vector3(0f, 1.2f, -2.5f),
            new Vector3(2.2f, 1.2f, -1f), new Vector3(-3f, 1.5f, 2f),
            new Vector3(0f, 1.6f, 3f), new Vector3(3f, 1.5f, 2f),
            new Vector3(5.5f, 1.2f, 6f), new Vector3(-5.5f, 1.3f, 9f)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var coin = CreatePrimitive($"Coin_{i + 1:00}", PrimitiveType.Cylinder, positions[i], new Vector3(0.55f, 0.09f, 0.55f), material);
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            coin.GetComponent<Collider>().isTrigger = true;
            coin.AddComponent<Coin>();
        }
    }

    private static void CreateBonusZone(Material material)
    {
        var zone = CreatePrimitive("BonusZone", PrimitiveType.Cube, new Vector3(6f, 0.45f, 3.5f), new Vector3(4.5f, 0.22f, 4.5f), material);
        zone.GetComponent<Collider>().isTrigger = true;
        zone.AddComponent<BonusZone>();

        var sign = CreateWorldLabel("БОНУСНАЯ ЗОНА", new Vector3(6f, 1.4f, 5.6f), 0.18f, Color.white);
        sign.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
    }

    private static GameObject CreateExplosionPrefab(Material particleMaterial)
    {
        var effect = new GameObject("ExplosionEffect", typeof(ParticleSystem));
        var particles = effect.GetComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = true;
        main.duration = 0.45f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.15f, 0.02f), new Color(1f, 0.85f, 0.08f));
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 55) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;
        effect.GetComponent<ParticleSystemRenderer>().material = particleMaterial;

        const string path = "Assets/Prefabs/ExplosionEffect.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(effect, path);
        UnityEngine.Object.DestroyImmediate(effect);
        return prefab;
    }

    private static void CreateBarrels(Material material, GameObject explosionPrefab)
    {
        var positions = new[] { new Vector3(-4f, 1.2f, 1f), new Vector3(4f, 1.2f, 8f) };
        for (var i = 0; i < positions.Length; i++)
        {
            var barrel = CreatePrimitive($"ExplosiveBarrel_{i + 1}", PrimitiveType.Cylinder, positions[i], new Vector3(0.72f, 0.85f, 0.72f), material);
            var body = barrel.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.linearDamping = 0.25f;
            var explosive = barrel.AddComponent<ExplosiveBarrel>();
            SetObject(explosive, "explosionEffect", explosionPrefab);
            SetFloat(explosive, "minimumImpactSpeed", 2.5f);
            SetFloat(explosive, "explosionRadius", 4.5f);
            SetInt(explosive, "damage", 45);
        }
    }

    private static void CreateMovingObstacles(Material material)
    {
        CreateMovingObstacle("MovingObstacleHorizontal", new Vector3(0f, 1.3f, 6f), new Vector3(2.5f, 2.5f, 1f), Vector3.right, 5.5f, 2.2f, material);
        CreateMovingObstacle("MovingObstacleVertical", new Vector3(-6f, 1.5f, 11f), new Vector3(1.3f, 3f, 1.3f), Vector3.up, 2f, 1.3f, material);
    }

    private static void CreateMovingObstacle(
        string name,
        Vector3 position,
        Vector3 scale,
        Vector3 direction,
        float distance,
        float speed,
        Material material)
    {
        var obstacle = CreatePrimitive(name, PrimitiveType.Cube, position, scale, material);
        var body = obstacle.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        var moving = obstacle.AddComponent<MovingObstacle>();
        SetVector3(moving, "localDirection", direction);
        SetFloat(moving, "distance", distance);
        SetFloat(moving, "speed", speed);
        SetInt(moving, "contactDamage", 20);
    }

    private static (TMP_Text coinLabel, TMP_Text healthLabel) CreateInterface()
    {
        var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var panel = new GameObject("InfoPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(520f, 210f);
        panel.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.07f, 0.82f);

        var coinLabel = CreateUiText("CoinCounter", panel.transform, "Монеты: 0", new Vector2(24f, -22f), 32f, new Color(1f, 0.75f, 0.1f));
        var healthLabel = CreateUiText("HealthCounter", panel.transform, "Здоровье: 100/100", new Vector2(24f, -66f), 30f, new Color(0.4f, 1f, 0.55f));
        var instructions = CreateUiText(
            "Instructions",
            panel.transform,
            "WASD / стрелки — движение\nSpace — прыжок\nСоберите монеты и войдите в бонусную зону",
            new Vector2(24f, -112f),
            22f,
            Color.white);
        instructions.rectTransform.sizeDelta = new Vector2(475f, 90f);

        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        return (coinLabel, healthLabel);
    }

    private static TextMeshProUGUI CreateUiText(
        string name,
        Transform parent,
        string text,
        Vector2 position,
        float fontSize,
        Color color)
    {
        var gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        var label = gameObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.font = GetDefaultTmpFont();
        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(470f, 42f);
        return label;
    }

    private static GameObject CreateWorldLabel(string text, Vector3 position, float scale, Color color)
    {
        var labelObject = new GameObject("BonusZoneLabel", typeof(TextMeshPro));
        labelObject.transform.position = position;
        labelObject.transform.localScale = Vector3.one * scale;
        var label = labelObject.GetComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = 10f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.font = GetDefaultTmpFont();
        return labelObject;
    }

    private static TMP_FontAsset GetDefaultTmpFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        if (font == null)
            throw new InvalidOperationException("TextMesh Pro essential resources were not imported.");
        return font;
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var gameObject = GameObject.CreatePrimitive(type);
        gameObject.name = name;
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return gameObject;
    }

    private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(propertyName)
                       ?? throw new InvalidOperationException($"Property {propertyName} not found on {target.GetType().Name}.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(UnityEngine.Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector3(UnityEngine.Object target, string propertyName, Vector3 value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).vector3Value = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RequireOne<T>(string description) where T : UnityEngine.Object
    {
        RequireCount<T>(1, description);
    }

    private static void RequireCount<T>(int minimum, string description) where T : UnityEngine.Object
    {
        var count = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include).Length;
        if (count < minimum)
            throw new InvalidOperationException($"Expected at least {minimum} {description}, found {count}.");
    }
}
