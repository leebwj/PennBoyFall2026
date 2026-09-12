using UnityEditor;
using UnityEngine;

public static class GameAssets
{
    public const string MATERIAL_DIR = "Assets/Materials";
    public const string PREFAB_DIR = "Assets/Prefabs";

    const string LIT_SHADER = "Universal Render Pipeline/Lit";
    const string PARTICLE_SHADER = "Universal Render Pipeline/Particles/Unlit";

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) { return; }
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    public static Material Surface(string name, Color color, float smoothness = 0.15f, float glow = 0f)
    {
        string path = MATERIAL_DIR + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(LIT_SHADER));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        if (glow > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * glow);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Material ParticleSurface(string name, Color color)
    {
        string path = MATERIAL_DIR + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find(PARTICLE_SHADER));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static void Wire(Object target, params (string name, object value)[] fields)
    {
        var serialized = new SerializedObject(target);
        foreach ((string name, object value) in fields)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
            {
                Debug.LogError($"no serialized field '{name}' on {target.GetType().Name}");
                continue;
            }

            switch (value)
            {
                case float f: property.floatValue = f; break;
                case int i when property.propertyType == SerializedPropertyType.Enum: property.enumValueIndex = i; break;
                case int i: property.intValue = i; break;
                case bool b: property.boolValue = b; break;
                case Color c: property.colorValue = c; break;
                case Vector2 v: property.vector2Value = v; break;
                case Vector3 v: property.vector3Value = v; break;
                case Object o: property.objectReferenceValue = o; break;
                case null: property.objectReferenceValue = null; break;
                default: Debug.LogError($"unsupported value for field '{name}'"); break;
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static GameObject Save(GameObject source, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, PREFAB_DIR + "/" + name + ".prefab");
        Object.DestroyImmediate(source);
        return prefab;
    }

    public static GameObject Shape(PrimitiveType type, string name, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.localScale = localScale;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    public static GameObject Burst(string name, Color color, int count, float speed, float size, float life)
    {
        var effect = new GameObject(name);
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = life;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.gravityModifier = 0.8f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        effect.GetComponent<ParticleSystemRenderer>().sharedMaterial = ParticleSurface(name + "Material", color);
        return Save(effect, name);
    }

    public static GameObject ProjectilePrefab(Material material, GameObject impactEffect)
    {
        GameObject shot = Shape(PrimitiveType.Sphere, "Projectile", Vector3.one * 0.28f, material);
        shot.GetComponent<SphereCollider>().isTrigger = true;

        // trigger callbacks need a rigidbody on the moving side, and gravity would arc the shot
        Rigidbody body = shot.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        Projectile script = shot.AddComponent<Projectile>();
        Wire(script, ("speed", 26f), ("lifeSeconds", 3f), ("impactEffect", impactEffect));
        return Save(shot, "Projectile");
    }

    public static GameObject PlayerPrefab(Material body, GameObject projectilePrefab, GameObject deathEffect)
    {
        GameObject player = Shape(PrimitiveType.Capsule, "Player", Vector3.one, body);
        player.tag = "Player";

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.mass = 1.2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        GameObject nose = Shape(PrimitiveType.Cube, "Facing", new Vector3(0.22f, 0.22f, 0.55f), body);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.2f, 0.45f);

        var muzzle = new GameObject("Muzzle");
        muzzle.transform.SetParent(player.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0.25f, 0.85f);

        Damageable health = player.AddComponent<Damageable>();
        Wire(health, ("maxHealth", 120f), ("hitIntervalSeconds", 0.25f));

        player.AddComponent<PlayerController>();

        PlayerShooter shooter = player.AddComponent<PlayerShooter>();
        Wire(shooter,
            ("projectilePrefab", projectilePrefab.GetComponent<Projectile>()),
            ("muzzle", muzzle.transform),
            ("shotsPerSecond", 5f),
            ("damagePerShot", 25f));

        PlayerLife life = player.AddComponent<PlayerLife>();
        Wire(life, ("respawnDelaySeconds", 1.2f), ("deathEffect", deathEffect));

        return Save(player, "Player");
    }

    public static GameObject EnemyPrefab(Material body, GameObject deathEffect)
    {
        GameObject enemy = Shape(PrimitiveType.Capsule, "Enemy", Vector3.one * 0.9f, body);

        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject nose = Shape(PrimitiveType.Cube, "Facing", new Vector3(0.2f, 0.2f, 0.5f), body);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.transform.SetParent(enemy.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.2f, 0.45f);

        Damageable health = enemy.AddComponent<Damageable>();
        Wire(health, ("maxHealth", 60f), ("hitIntervalSeconds", 0.05f));

        EnemyBrain brain = enemy.AddComponent<EnemyBrain>();
        Wire(brain,
            ("moveSpeed", 3.4f),
            ("sightRange", 18f),
            ("contactDamage", 12f),
            ("scoreValue", 10),
            ("deathEffect", deathEffect));

        return Save(enemy, "Enemy");
    }

    public static GameObject PickupPrefab(string name, Material body, int kind, float amount)
    {
        var root = new GameObject(name);
        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1f;

        GameObject visual = Shape(PrimitiveType.Cube, "Visual", Vector3.one * 0.5f, body);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(root.transform, false);
        visual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);

        Pickup pickup = root.AddComponent<Pickup>();
        Wire(pickup, ("kind", kind), ("amount", amount), ("respawnSeconds", 6f));

        return Save(root, name);
    }

}
