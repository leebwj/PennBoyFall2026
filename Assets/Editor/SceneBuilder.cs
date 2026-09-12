using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneBuilder
{
    const string SCENE_PATH = "Assets/Scenes/Main.unity";
    const string PREFAB_DIR = "Assets/Prefabs";
    const string MATERIAL_DIR = "Assets/Materials";
    const string PLAYER_PREFAB = PREFAB_DIR + "/Player.prefab";
    const string LIT_SHADER = "Universal Render Pipeline/Lit";

    const float ARENA_HALF = 20f;
    const float WALL_HEIGHT = 2f;
    const float WALL_THICKNESS = 1f;
    static readonly Vector3 CAMERA_OFFSET = new Vector3(0f, 14f, -8f);

    static readonly Vector3[] OBSTACLE_POSITIONS =
    {
        new Vector3(6f, 0.5f, 6f),
        new Vector3(-7f, 0.5f, 4f),
        new Vector3(3f, 0.5f, -8f),
        new Vector3(-10f, 0.5f, -10f),
        new Vector3(11f, 0.5f, -3f),
        new Vector3(-4f, 0.5f, 12f),
    };

    [MenuItem("PennBoy/Build Main Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) { return; }

        EnsureFolder("Assets/Scenes");
        EnsureFolder(PREFAB_DIR);
        EnsureFolder(MATERIAL_DIR);

        Material groundMat = MakeMaterial("Ground", new Color(0.32f, 0.36f, 0.3f));
        Material wallMat = MakeMaterial("Wall", new Color(0.55f, 0.5f, 0.45f));
        Material obstacleMat = MakeMaterial("Obstacle", new Color(0.7f, 0.35f, 0.25f));
        Material playerMat = MakeMaterial("Player", new Color(0.2f, 0.5f, 0.9f));

        GameObject playerPrefab = BuildPlayerPrefab(playerMat);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        BuildGround(groundMat);
        BuildWalls(wallMat);
        BuildObstacles(obstacleMat);
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(0f, 1f, 0f);
        SetupCamera(player.transform);
        SetupLight();

        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AddToBuildSettings(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log("main scene built at " + SCENE_PATH);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) { return; }
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = MATERIAL_DIR + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) { return mat; }
        mat = new Material(Shader.Find(LIT_SHADER));
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject BuildPlayerPrefab(Material mat)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB);
        if (existing != null) { return existing; }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.GetComponent<MeshRenderer>().sharedMaterial = mat;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Facing";
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.25f, 0.5f);
        nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.5f);
        nose.GetComponent<MeshRenderer>().sharedMaterial = mat;

        player.AddComponent<PlayerController>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PLAYER_PREFAB);
        Object.DestroyImmediate(player);
        return prefab;
    }

    static void BuildGround(Material mat)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(ARENA_HALF / 5f, 1f, ARENA_HALF / 5f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static void BuildWalls(Material mat)
    {
        var parent = new GameObject("Walls");
        float len = ARENA_HALF * 2f + WALL_THICKNESS;
        float y = WALL_HEIGHT * 0.5f;
        var walls = new List<(Vector3 pos, Vector3 scale)>
        {
            (new Vector3(0f, y, ARENA_HALF), new Vector3(len, WALL_HEIGHT, WALL_THICKNESS)),
            (new Vector3(0f, y, -ARENA_HALF), new Vector3(len, WALL_HEIGHT, WALL_THICKNESS)),
            (new Vector3(ARENA_HALF, y, 0f), new Vector3(WALL_THICKNESS, WALL_HEIGHT, len)),
            (new Vector3(-ARENA_HALF, y, 0f), new Vector3(WALL_THICKNESS, WALL_HEIGHT, len)),
        };
        foreach (var (pos, scale) in walls)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.SetParent(parent.transform, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }

    static void BuildObstacles(Material mat)
    {
        var parent = new GameObject("Obstacles");
        foreach (Vector3 pos in OBSTACLE_POSITIONS)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Obstacle";
            box.transform.SetParent(parent.transform, false);
            box.transform.position = pos;
            box.transform.localScale = new Vector3(2f, 1f, 2f);
            box.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }

    static void SetupCamera(Transform player)
    {
        Camera cam = Camera.main;
        cam.transform.position = player.position + CAMERA_OFFSET;
        cam.transform.rotation = Quaternion.LookRotation(-CAMERA_OFFSET, Vector3.up);

        CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player;
        so.FindProperty("offset").vector3Value = CAMERA_OFFSET;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetupLight()
    {
        Light light = Object.FindFirstObjectByType<Light>();
        if (light == null) { return; }
        light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
        light.shadows = LightShadows.Soft;
    }

    static void AddToBuildSettings(string scenePath)
    {
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.path == scenePath) { return; }
        }
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes)
        {
            new EditorBuildSettingsScene(scenePath, true),
        };
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
