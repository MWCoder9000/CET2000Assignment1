#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// CET2000 Lab 05A - builds the behaviour tree guard scene: tags, layers, floor, walls, obstacles,
// four patrol waypoints, the guard, the player, the camera and the on-screen panel.
// Menu: CET2000 -> Create Lab05A Behaviour Tree Scene. The first time the project opens it offers to run this.
// Compatible with Unity 5.6 up to Unity 6.5 (C# 4 syntax, version guards for the prefab API).
public class SetupLab05A
{
    public const string ScenePath = "Assets/_Scenes/GuardBTScene.unity";
    const string SpriteFolder = "Assets/Sprites";
    const string PrefabFolder = "Assets/Prefabs";

    [MenuItem("CET2000/Create Lab05A Behaviour Tree Scene", priority = 1)]
    public static void CreateFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("CET2000 Lab 05A", "GuardBTScene already exists. Rebuild it from scratch?", "Rebuild", "Cancel"))
            return;
        CreateAll();
    }

    [MenuItem("CET2000/Open Lab05A Behaviour Tree Scene", priority = 2)]
    public static void OpenScene()
    {
        if (File.Exists(ScenePath) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }

    public static void CreateAll()
    {
        // ---- tags and layers ----
        EnsureTag("Obstacle");
        int guardLayer = EnsureLayer("GuardLayer");
        int playerLayer = EnsureLayer("PlayerLayer");
        int obstacleLayer = EnsureLayer("ObstacleLayer");
        LayerMask playerMask = 1 << playerLayer;
        LayerMask obstacleMask = 1 << obstacleLayer;

        // ---- sprites ----
        Sprite square = ImportSprite(SpriteFolder + "/Square.png");
        Sprite guardSprite = ImportSprite(SpriteFolder + "/Guard.png");
        Sprite playerSprite = ImportSprite(SpriteFolder + "/Player.png");
        Sprite obstacleSprite = ImportSprite(SpriteFolder + "/Obstacle.png");
        if (square == null || guardSprite == null || playerSprite == null || obstacleSprite == null)
        {
            EditorUtility.DisplayDialog("CET2000 Lab 05A", "Sprites are missing from Assets/Sprites. Re-extract the starter pack and try again.", "OK");
            return;
        }

        // ---- scene ----
        EnsureFolder("Assets/_Scenes");
        EnsureFolder(PrefabFolder);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.86f, 0.88f, 0.9f);
        camGo.transform.position = new Vector3(-1.8f, 0f, -10f);   // leaves room on the left for the panel
        camGo.AddComponent<AudioListener>();

        var env = new GameObject("Environment");
        MakeSprite("Floor", square, new Color(0.96f, 0.94f, 0.88f), Vector2.zero, new Vector2(10f, 6f), -10).transform.SetParent(env.transform);
        Color wall = new Color(0.25f, 0.27f, 0.33f);
        MakeObstacle("Wall_Top", square, wall, new Vector2(0f, 3.15f), new Vector2(10.6f, 0.3f), obstacleLayer, env.transform);
        MakeObstacle("Wall_Bottom", square, wall, new Vector2(0f, -3.15f), new Vector2(10.6f, 0.3f), obstacleLayer, env.transform);
        MakeObstacle("Wall_Left", square, wall, new Vector2(-5.15f, 0f), new Vector2(0.3f, 6f), obstacleLayer, env.transform);
        MakeObstacle("Wall_Right", square, wall, new Vector2(5.15f, 0f), new Vector2(0.3f, 6f), obstacleLayer, env.transform);

        Color block = new Color(0.45f, 0.47f, 0.52f);
        var obstacles = new GameObject("Obstacles");
        MakeObstacle("Block_Centre", obstacleSprite, block, new Vector2(0f, 0f), new Vector2(2.4f, 1.2f), obstacleLayer, obstacles.transform);
        MakeObstacle("Pillar_Left", obstacleSprite, block, new Vector2(-2.2f, 0f), new Vector2(0.5f, 0.5f), obstacleLayer, obstacles.transform);
        MakeObstacle("Pillar_Right", obstacleSprite, block, new Vector2(2.2f, 0f), new Vector2(0.5f, 0.5f), obstacleLayer, obstacles.transform);

        // ---- patrol waypoints (a loop around the centre block) ----
        var wpParent = new GameObject("Waypoints");
        Vector2[] wpPos = { new Vector2(-3.6f, -1.8f), new Vector2(3.6f, -1.8f), new Vector2(3.6f, 1.8f), new Vector2(-3.6f, 1.8f) };
        var waypoints = new Transform[wpPos.Length];
        for (int i = 0; i < wpPos.Length; i++)
        {
            var wp = MakeSprite("WP_" + i, square, new Color(0.3f, 0.6f, 0.95f, 0.45f), wpPos[i], Vector2.one * 0.25f, -5);
            wp.transform.SetParent(wpParent.transform, true);
            waypoints[i] = wp.transform;
        }

        // ---- player ----
        var playerGo = MakeSprite("Player", playerSprite, new Color(0.2f, 0.75f, 0.35f), new Vector2(0f, 2.5f), Vector2.one * 0.45f, 10);
        playerGo.tag = "Player";
        playerGo.layer = playerLayer;
        var prb = playerGo.AddComponent<Rigidbody2D>();
        prb.gravityScale = 0f;
        prb.interpolation = RigidbodyInterpolation2D.Interpolate;
        prb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerGo.AddComponent<CircleCollider2D>();
        var pc = playerGo.AddComponent<BTPlayerController>();
        pc.obstacleLayer = obstacleMask;

        // ---- guard ----
        var guardGo = MakeSprite("Guard", guardSprite, new Color(0.65f, 0.65f, 0.65f), wpPos[0], Vector2.one * 0.55f, 10);
        guardGo.layer = guardLayer;
        guardGo.transform.rotation = Quaternion.Euler(0f, 0f, -90f);   // facing right, towards WP_1
        var grb = guardGo.AddComponent<Rigidbody2D>();
        grb.gravityScale = 0f;
        grb.interpolation = RigidbodyInterpolation2D.Interpolate;
        grb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        guardGo.AddComponent<CircleCollider2D>();
        var guard = guardGo.AddComponent<GuardBT>();
        guard.player = playerGo.transform;
        guard.waypoints = waypoints;
        guard.playerLayer = playerMask;
        guard.obstacleLayer = obstacleMask;

        // ---- on-screen panel ----
        var gm = new GameObject("GameManager");
        var hud = gm.AddComponent<BTHud>();
        hud.guard = guard;
        hud.player = pc;

        SavePrefab(guardGo, PrefabFolder + "/Guard.prefab");
        SavePrefab(playerGo, PrefabFolder + "/Player.prefab");

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = guardGo;
        Debug.Log("[CET2000] GuardBTScene created. Open BehaviourTree.cs and start with TODO 1.");
    }

    // ------------------------------------------------------------------ helpers
    // Unity 2018.3 replaced the old prefab API, so pick the right call for the editor version.
    static void SavePrefab(GameObject go, string path)
    {
#if UNITY_2018_3_OR_NEWER
        PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
#else
        PrefabUtility.CreatePrefab(path, go, ReplacePrefabOptions.ConnectToPrefab);
#endif
    }

    static GameObject MakeSprite(string name, Sprite sprite, Color colour, Vector2 position, Vector2 scale, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = colour;
        sr.sortingOrder = order;
        go.transform.position = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        return go;
    }

    static GameObject MakeObstacle(string name, Sprite sprite, Color colour, Vector2 position, Vector2 scale, int layer, Transform parent)
    {
        var go = MakeSprite(name, sprite, colour, position, scale, 0);
        go.AddComponent<BoxCollider2D>();
        go.tag = "Obstacle";
        go.layer = layer;
        go.transform.SetParent(parent, true);
        return go;
    }

    static Sprite ImportSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        bool changed = importer.textureType != TextureImporterType.Sprite ||
                       importer.spriteImportMode != SpriteImportMode.Single ||
                       !Mathf.Approximately(importer.spritePixelsPerUnit, 128f);
        if (changed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static SerializedObject TagManager()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        return assets != null && assets.Length > 0 ? new SerializedObject(assets[0]) : null;
    }

    static void EnsureTag(string tag)
    {
        SerializedObject so = TagManager();
        if (so == null) return;
        SerializedProperty tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;
        SerializedObject so = TagManager();
        SerializedProperty layers = so.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)          // 0-7 are reserved by Unity
        {
            SerializedProperty p = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(p.stringValue))
            {
                p.stringValue = layerName;
                so.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        Debug.LogError("[CET2000] No free layer slot for " + layerName + ".");
        return 0;
    }
}

// Offers to build the scene the first time the project is opened.
[InitializeOnLoad]
static class SetupLab05AAutoRun
{
    // EditorPrefs is shared by every project on the computer, so the key includes this project's path.
    static string AskedKey { get { return "CET2000_Lab05A_AutoSetupAsked_" + Application.dataPath; } }
    static bool checkedThisLoad;

    static SetupLab05AAutoRun()
    {
        EditorApplication.delayCall += () =>
        {
            if (UnityEditorInternal.InternalEditorUtility.inBatchMode || checkedThisLoad) return;
            checkedThisLoad = true;
            if (File.Exists(SetupLab05A.ScenePath))
            {
                Scene active = SceneManager.GetActiveScene();
                if (string.IsNullOrEmpty(active.path) && !active.isDirty && !EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorSceneManager.OpenScene(SetupLab05A.ScenePath);
                return;
            }
            if (EditorPrefs.GetBool(AskedKey, false)) return;
            EditorPrefs.SetBool(AskedKey, true);
            if (EditorUtility.DisplayDialog("CET2000 Lab 05A",
                    "Create the GuardBTScene (guard, player, waypoints, obstacles) now?\n\nYou can also do this later from the menu CET2000 > Create Lab05A Behaviour Tree Scene.",
                    "Create scene", "Later"))
                SetupLab05A.CreateAll();
        };
    }
}
#endif
