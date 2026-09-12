using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneBuilder
{
    const string SCENE_DIR = "Assets/Scenes";
    const string SCENE_PATH = SCENE_DIR + "/Main.unity";

    const float ARENA_HALF = 20f;
    const float WALL_HEIGHT = 2.5f;
    const float WALL_THICKNESS = 1f;

    static readonly Vector3 PLAYER_SPAWN = new Vector3(0f, 1.1f, -12f);

    static readonly Vector3[] OBSTACLES =
    {
        new Vector3(-8f, 0.75f, -4f),
        new Vector3(7f, 0.75f, -7f),
        new Vector3(-5f, 0.75f, 7f),
        new Vector3(9f, 0.75f, 6f),
        new Vector3(0f, 0.75f, 2f),
        new Vector3(-12f, 0.75f, 12f),
    };

    static readonly Vector3[] ENEMY_SPAWNS =
    {
        new Vector3(-6f, 1f, 12f),
        new Vector3(10f, 1f, 11f),
        new Vector3(13f, 1f, -2f),
    };

    [MenuItem("PennBoy/Build Main Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) { return; }

        GameAssets.EnsureFolder(SCENE_DIR);
        GameAssets.EnsureFolder(GameAssets.MATERIAL_DIR);
        GameAssets.EnsureFolder(GameAssets.PREFAB_DIR);

        Material ground = GameAssets.Surface("Ground", new Color(0.30f, 0.33f, 0.31f));
        Material wall = GameAssets.Surface("Wall", new Color(0.52f, 0.50f, 0.46f));
        Material obstacle = GameAssets.Surface("Obstacle", new Color(0.62f, 0.40f, 0.28f));
        Material playerBody = GameAssets.Surface("Player", new Color(0.20f, 0.50f, 0.90f), 0.4f);
        Material enemyBody = GameAssets.Surface("Enemy", new Color(0.85f, 0.25f, 0.25f), 0.3f);
        Material shot = GameAssets.Surface("Projectile", new Color(1f, 0.85f, 0.25f), 0.5f, 2f);
        Material healthBody = GameAssets.Surface("PickupHealth", new Color(0.25f, 0.85f, 0.45f), 0.4f, 1.5f);
        Material scoreBody = GameAssets.Surface("PickupScore", new Color(0.95f, 0.75f, 0.20f), 0.4f, 1.5f);

        GameObject hitEffect = GameAssets.Burst("HitEffect", new Color(1f, 0.9f, 0.4f), 12, 4f, 0.12f, 0.3f);
        GameObject enemyDeath = GameAssets.Burst("EnemyDeathEffect", new Color(1f, 0.45f, 0.2f), 30, 6f, 0.2f, 0.6f);
        GameObject playerDeath = GameAssets.Burst("PlayerDeathEffect", new Color(0.4f, 0.7f, 1f), 40, 7f, 0.22f, 0.7f);

        GameObject projectile = GameAssets.ProjectilePrefab(shot, hitEffect);
        GameObject player = GameAssets.PlayerPrefab(playerBody, projectile, playerDeath);
        GameObject enemy = GameAssets.EnemyPrefab(enemyBody, enemyDeath);
        GameObject healthPickup = GameAssets.PickupPrefab("PickupHealth", healthBody, (int)Pickup.Kind.Health, 30f);
        GameObject scorePickup = GameAssets.PickupPrefab("PickupScore", scoreBody, (int)Pickup.Kind.Score, 25f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        BuildGround(ground);
        BuildWalls(wall);
        BuildObstacles(obstacle);
        BuildEnemies(enemy);
        BuildPickups(healthPickup, scorePickup);

        GameObject playerInstance = Place(player, PLAYER_SPAWN, null);
        BuildSystems(playerInstance);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        RegisterScene(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log("main scene built at " + SCENE_PATH);
    }

    private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material material)
    {
        GameObject box = GameAssets.Shape(PrimitiveType.Cube, name, size, material);
        box.transform.SetParent(parent, false);
        box.transform.position = center;
        box.isStatic = true;
        return box;
    }

    private static GameObject Place(GameObject prefab, Vector3 position, Transform parent)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        return instance;
    }

    private static void BuildGround(Material material)
    {
        GameObject ground = GameAssets.Shape(PrimitiveType.Plane, "Ground", new Vector3(ARENA_HALF / 5f, 1f, ARENA_HALF / 5f), material);
        ground.isStatic = true;
    }

    private static void BuildWalls(Material material)
    {
        Transform parent = new GameObject("Walls").transform;
        float span = ARENA_HALF * 2f + WALL_THICKNESS;
        float y = WALL_HEIGHT * 0.5f;

        var walls = new List<(Vector3 center, Vector3 size)>
        {
            (new Vector3(0f, y, ARENA_HALF), new Vector3(span, WALL_HEIGHT, WALL_THICKNESS)),
            (new Vector3(0f, y, -ARENA_HALF), new Vector3(span, WALL_HEIGHT, WALL_THICKNESS)),
            (new Vector3(ARENA_HALF, y, 0f), new Vector3(WALL_THICKNESS, WALL_HEIGHT, span)),
            (new Vector3(-ARENA_HALF, y, 0f), new Vector3(WALL_THICKNESS, WALL_HEIGHT, span)),
        };
        foreach ((Vector3 center, Vector3 size) in walls)
        {
            Box("Wall", parent, center, size, material);
        }
    }

    private static void BuildObstacles(Material material)
    {
        Transform parent = new GameObject("Obstacles").transform;
        foreach (Vector3 center in OBSTACLES)
        {
            Box("Obstacle", parent, center, new Vector3(3f, 1.5f, 3f), material);
        }
    }

    private static void BuildEnemies(GameObject enemyPrefab)
    {
        Transform parent = new GameObject("Enemies").transform;
        foreach (Vector3 position in ENEMY_SPAWNS)
        {
            Place(enemyPrefab, position, parent);
        }

        var spawnerObject = new GameObject("EnemySpawner");
        spawnerObject.transform.SetParent(parent, false);
        spawnerObject.transform.position = new Vector3(0f, 0f, 10f);
        GameAssets.Wire(spawnerObject.AddComponent<EnemySpawner>(),
            ("enemyPrefab", enemyPrefab.GetComponent<EnemyBrain>()),
            ("radius", 8f),
            ("maxAlive", 3),
            ("intervalSeconds", 6f),
            ("firstSpawnDelaySeconds", 8f));
    }

    private static void BuildPickups(GameObject healthPickup, GameObject scorePickup)
    {
        Transform parent = new GameObject("Pickups").transform;
        Place(healthPickup, new Vector3(-4f, 1f, -8f), parent);
        Place(scorePickup, new Vector3(4f, 1f, -8f), parent);
    }

    private static void BuildSystems(GameObject playerInstance)
    {
        var systems = new GameObject("Systems");

        GameManager manager = systems.AddComponent<GameManager>();
        GameAssets.Wire(manager, ("respawnPoint", PLAYER_SPAWN));

        GameAssets.Wire(systems.AddComponent<DebugHud>(),
            ("playerHealth", playerInstance.GetComponent<Damageable>()),
            ("player", playerInstance.GetComponent<PlayerController>()),
            ("visible", true));

        Camera cam = Camera.main;
        CameraRig rig = cam.gameObject.AddComponent<CameraRig>();
        GameAssets.Wire(rig,
            ("target", playerInstance.transform),
            ("height", 17f),
            ("distance", 11f),
            ("aimLead", 2.5f),
            ("focusMin", new Vector2(-ARENA_HALF + 8f, -ARENA_HALF + 8f)),
            ("focusMax", new Vector2(ARENA_HALF - 8f, ARENA_HALF - 8f)));
        cam.transform.position = playerInstance.transform.position + new Vector3(0f, 17f, -11f);
        cam.transform.rotation = Quaternion.Euler(57f, 0f, 0f);
        cam.farClipPlane = 200f;

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.1f;
        }
    }

    private static void RegisterScene(string path)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        var scenes = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
        {
            if (existing.path != path) { scenes.Add(existing); }
        }
        scenes.Insert(0, new EditorBuildSettingsScene(new GUID(guid), true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
