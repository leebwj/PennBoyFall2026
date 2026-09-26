using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// builds Assets/Scenes/ColorDemo.unity from the design handoff's world-layout.json.
// world units are metres, X/Z ground plane, Y up. the json world fields already include placement
public static class ColorDemoBuilder
{
    public const string DATA_PATH = "Assets/Demo/Data/world-layout.json";
    const string SCENE_PATH = "Assets/Scenes/ColorDemo.unity";
    const string MATERIAL_DIR = "Assets/Demo/Materials";
    const string TEXTURE_DIR = "Assets/Demo/Textures";
    const string MESH_DIR = "Assets/Demo/Meshes";
    const string LIT_SHADER = "Universal Render Pipeline/Lit";

    const float CELL = 2f;
    const float SLAB = 0.3f;
    const float WALKWAY = 2f;
    const float RAIL_THICKNESS = 0.15f;

    // normal geometry is drawn cut away this far above nearby floor so the camera can see in.
    // colliders always keep their full height
    const float WALL_CUTAWAY = 1.3f;
    const float RAIL_CUTAWAY = 0.8f;
    const float COLORED_WALL_VISUAL = 1.5f;
    const float SOURCE_VISUAL = 1.8f;

    static readonly Color RED = new Color(0.88f, 0.24f, 0.27f);
    static readonly Color BLUE = new Color(0.22f, 0.45f, 0.95f);

    class Kit
    {
        public Material floor, ramp, path, wall, gate, red, blue, redPlain, bluePlain, line, plateIdle, platePressed, boss;
        public Mesh prism, diamond, ring;
    }

    class RegionParts
    {
        public int route;
        public Transform root;
        public Vector3 respawn;
        public Vector3 activation;
        public bool elevated;
        public ColorId arrival;
        public readonly List<ColorTarget> targets = new List<ColorTarget>();
        public readonly List<Rigidbody> boxes = new List<Rigidbody>();
        public ColorRegion region;
    }

    [MenuItem("PennBoy/Build Color Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) { return; }

        GameAssets.EnsureFolder("Assets/Scenes");
        GameAssets.EnsureFolder(MATERIAL_DIR);
        GameAssets.EnsureFolder(TEXTURE_DIR);
        GameAssets.EnsureFolder(MESH_DIR);

        object layout = MiniJson.Parse(File.ReadAllText(DATA_PATH));
        Kit kit = MakeKit();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Transform map = new GameObject("ContinuousColorMap").transform;
        var regions = new Dictionary<int, RegionParts>();

        foreach (object level in JsonTree.List(layout, "levels")) { BuildLevel(level, map, kit, regions); }
        BuildArena(JsonTree.Obj(layout, "boss"), map, kit, regions);
        foreach (object connector in JsonTree.List(layout, "connectors")) { BuildConnector(connector, map, kit, regions); }

        foreach (RegionParts parts in regions.Values) { FinishRegion(parts); }
        BuildKillVolume(map);

        RegionParts first = regions[regions.Keys.Min()];
        DemoPlayer player = BuildPlayer(first, kit);
        BuildSystems(player, first.region);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        RegisterScene(SCENE_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log("colour demo built at " + SCENE_PATH);
    }

    // ---------- encounters ----------

    private static void BuildLevel(object level, Transform map, Kit kit, Dictionary<int, RegionParts> regions)
    {
        int route = JsonTree.Int(level, "route");
        List<object> surfaces = JsonTree.List(level, "surfaces");
        float sign = JsonTree.Int(JsonTree.Obj(level, "placement"), "rotation") == 180 ? -1f : 1f;

        // the local grid maps to the world by a pitch of 2 m and a 0 or 180 degree turn. fit its origin
        // from one surface so records that only carry local cells (gates, plates) can be placed too
        float[] w0 = JsonTree.Floats(surfaces[0], "world");
        Vector2 origin = new Vector2(
            w0[0] - sign * CELL * JsonTree.Num(surfaces[0], "x"),
            w0[2] - sign * CELL * JsonTree.Num(surfaces[0], "z"));
        Vector3 Local(float x, float z, float h) => new Vector3(origin.x + sign * CELL * x, h, origin.y + sign * CELL * z);

        Transform root = new GameObject($"Route{route:00}").transform;
        root.SetParent(map, false);
        Transform floors = Child(root, "Floor");
        Transform walls = Child(root, "Walls");
        Transform things = Child(root, "Targets");

        Dictionary<string, object> checkpoint = JsonTree.Obj(level, "checkpoint");
        var parts = new RegionParts
        {
            route = route,
            root = root,
            respawn = V3(JsonTree.Floats(checkpoint, "respawn")),
            activation = V3(JsonTree.Floats(checkpoint, "activation")),
            elevated = JsonTree.Bool(level, "voidIsPit", false) || surfaces.Any(s => JsonTree.Num(s, "h") > 0.1f),
            arrival = (ColorId)JsonTree.Int(level, "entry"),
        };
        regions[route] = parts;

        var tops = new List<Vector3>();
        foreach (object s in surfaces)
        {
            float[] w = JsonTree.Floats(s, "world");
            tops.Add(new Vector3(w[0], w[1], w[2]));
        }

        BuildNeutralFloor(surfaces, floors, kit);
        BuildRamps(surfaces, floors, kit);
        foreach (object s in surfaces.Where(s => JsonTree.Has(s, "c")))
        {
            float[] w = JsonTree.Floats(s, "world");
            ColorId color = (ColorId)JsonTree.Int(s, "c");
            ColorTarget platform = Platform(floors, JsonTree.Str(s, "id"), new Vector3(w[0], w[1], w[2]), color, kit);
            parts.targets.Add(platform);
        }

        foreach (object w in JsonTree.List(level, "walls"))
        {
            float[] p = JsonTree.Floats(w, "world");
            Vector3 size = new Vector3(JsonTree.Num(w, "sx"), JsonTree.Num(w, "sy"), JsonTree.Num(w, "sz"));
            string role = JsonTree.Str(w, "role");
            float cut = role == "edge-rail" ? RAIL_CUTAWAY : WALL_CUTAWAY;
            float near = NearbyTop(tops, p[0], p[2], p[1] + size.y, Mathf.Max(size.x, size.z) * 0.5f + 1.2f, p[1]);
            float visualTop = Mathf.Min(p[1] + size.y, near + cut);
            NormalBlock(walls, "Wall", new Vector3(p[0], p[1], p[2]), size, visualTop, Quaternion.identity, kit.wall);
        }

        List<object> gates = JsonTree.List(level, "gates");
        var byId = new Dictionary<string, ColorTarget>();
        foreach (object t in JsonTree.List(level, "targets"))
        {
            ColorTarget target = BuildTarget(t, gates, things, kit, parts);
            byId[target.Id] = target;
            parts.targets.Add(target);
        }

        var plates = new Dictionary<string, Plate>();
        foreach (object p in JsonTree.List(level, "plates"))
        {
            Vector3 at = Local(JsonTree.Num(p, "x"), JsonTree.Num(p, "z"), JsonTree.Num(p, "h"));
            plates[JsonTree.Str(p, "id")] = BuildPlate(things, JsonTree.Str(p, "id"), at, parts.boxes.FirstOrDefault(), kit);
        }

        foreach (object g in gates)
        {
            Vector3 at = Local(JsonTree.Num(g, "x"), JsonTree.Num(g, "z"), JsonTree.Num(g, "h"));
            string control = JsonTree.Str(g, "control");
            plates.TryGetValue(control, out Plate plate);
            byId.TryGetValue(control, out ColorTarget lever);
            BuildGate(things, JsonTree.Str(g, "id"), at, plate, lever, (ColorId)JsonTree.Int(g, "openColor"), kit);
        }
    }

    // contiguous neutral cells in one row become a single slab so the capsule never catches on seams
    private static void BuildNeutralFloor(List<object> surfaces, Transform parent, Kit kit)
    {
        var rows = surfaces
            .Where(s => !JsonTree.Has(s, "c") && !JsonTree.Has(s, "ramp"))
            .GroupBy(s => (JsonTree.Num(s, "h"), JsonTree.Num(s, "z")));

        foreach (var row in rows)
        {
            List<object> cells = row.OrderBy(s => JsonTree.Num(s, "x")).ToList();
            int start = 0;
            for (int i = 1; i <= cells.Count; i++)
            {
                bool broken = i == cells.Count || JsonTree.Num(cells[i], "x") - JsonTree.Num(cells[i - 1], "x") > 1.01f;
                if (!broken) { continue; }

                float[] a = JsonTree.Floats(cells[start], "world");
                float[] b = JsonTree.Floats(cells[i - 1], "world");
                Vector3 from = new Vector3(Mathf.Min(a[0], b[0]) - CELL * 0.5f, a[1], a[2]);
                Vector3 to = new Vector3(Mathf.Max(a[0], b[0]) + CELL * 0.5f, a[1], a[2]);
                Slab(parent, "Floor", from, to, CELL, kit.floor);
                start = i;
            }
        }
    }

    // four one-metre wedges weld into one 8 m ramp, rising 4 m
    private static void BuildRamps(List<object> surfaces, Transform parent, Kit kit)
    {
        var ramps = surfaces
            .Where(s => JsonTree.Has(s, "ramp"))
            .GroupBy(s => JsonTree.Str(JsonTree.Obj(s, "ramp"), "axis") == "z" ? JsonTree.Num(s, "x") : JsonTree.Num(s, "z"));

        foreach (var ramp in ramps)
        {
            List<object> cells = ramp.OrderBy(s => JsonTree.Num(s, "h")).ToList();
            float[] low = JsonTree.Floats(cells[0], "world");
            float[] high = JsonTree.Floats(cells[cells.Count - 1], "world");
            float rise = JsonTree.Num(JsonTree.Obj(cells[0], "ramp"), "rise", 1f);

            Vector3 lowPoint = new Vector3(low[0], low[1], low[2]);
            Vector3 highPoint = new Vector3(high[0], high[1], high[2]);
            Vector3 dir = highPoint - lowPoint;
            dir.y = 0f;
            dir.Normalize();

            Vector3 bottom = lowPoint - dir * (CELL * 0.5f) + Vector3.down * (rise * 0.5f);
            Vector3 top = highPoint + dir * (CELL * 0.5f) + Vector3.up * (rise * 0.5f);
            Slab(parent, "Ramp", bottom, top, CELL, kit.ramp);
        }
    }

    private static ColorTarget Platform(Transform parent, string id, Vector3 top, ColorId color, Kit kit)
    {
        GameObject go = TargetRoot(parent, "Platform " + id, id, TargetKind.Platform, color, top);
        BoxCollider body = go.AddComponent<BoxCollider>();
        body.center = new Vector3(0f, -SLAB * 0.5f, 0f);
        body.size = new Vector3(CELL, SLAB, CELL);

        Renderer visual = Visual(go.transform, PrimitiveType.Cube, new Vector3(0f, -SLAB * 0.5f, 0f), new Vector3(CELL - 0.04f, SLAB, CELL - 0.04f), Tinted(color, kit));
        return Finish(go, new[] { visual }, kit);
    }

    private static ColorTarget BuildTarget(object t, List<object> gates, Transform parent, Kit kit, RegionParts parts)
    {
        string id = JsonTree.Str(t, "id");
        string type = JsonTree.Str(t, "type");
        ColorId color = (ColorId)JsonTree.Int(t, "c");
        float[] p = JsonTree.Floats(t, "world");
        float[] s = JsonTree.Floats(t, "size");
        Vector3 at = new Vector3(p[0], p[1], p[2]);
        Vector3 size = new Vector3(s[0], s[1], s[2]);
        Material look = Tinted(color, kit);

        switch (type)
        {
            case "wall":
            {
                GameObject go = TargetRoot(parent, "Wall " + id, id, TargetKind.Wall, color, at);
                AddBox(go, new Vector3(0f, size.y * 0.5f, 0f), size);
                Renderer r = Visual(go.transform, PrimitiveType.Cube, new Vector3(0f, COLORED_WALL_VISUAL * 0.5f, 0f), new Vector3(size.x, COLORED_WALL_VISUAL, size.z), look);
                return Finish(go, new[] { r }, kit);
            }
            case "enemy":
            {
                GameObject go = TargetRoot(parent, "Enemy " + id, id, TargetKind.Enemy, color, at);
                AddBox(go, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x * 0.9f, size.y, size.z * 0.9f));
                Renderer r = MeshVisual(go.transform, "Diamond", kit.diamond, new Vector3(0f, size.y * 0.5f, 0f), new Vector3(size.x, size.y * 0.5f, size.z), look);
                ColorTarget target = Finish(go, new[] { r }, kit);

                Dictionary<string, object> attack = JsonTree.Obj(t, "attack");
                bool attacks = attack == null || JsonTree.Bool(attack, "enabled", true);
                go.AddComponent<ColorEnemy>().Configure(attacks, r.transform, kit.redPlain, kit.bluePlain);
                return target;
            }
            case "box":
            {
                // the box pivots at its centre so the plate can compare heights directly
                GameObject go = TargetRoot(parent, "Box " + id, id, TargetKind.Box, color, at + Vector3.up * (size.y * 0.5f));
                AddBox(go, Vector3.zero, size);
                Rigidbody rb = go.AddComponent<Rigidbody>();
                rb.mass = 3f;
                rb.linearDamping = 6f;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                go.AddComponent<PushBox>();
                parts.boxes.Add(rb);

                Renderer r = Visual(go.transform, PrimitiveType.Cube, Vector3.zero, size - Vector3.one * 0.02f, look);
                return Finish(go, new[] { r }, kit);
            }
            case "lever":
            {
                GameObject go = TargetRoot(parent, "Lever " + id, id, TargetKind.Lever, color, at);
                AddBox(go, new Vector3(0f, size.y * 0.5f, 0f), size);
                Renderer post = Visual(go.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(0.36f, 0.8f, 0.36f), look);
                Renderer handle = Visual(go.transform, PrimitiveType.Cube, new Vector3(0.25f, 1.55f, 0f), new Vector3(0.9f, 0.22f, 0.22f), look);
                handle.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

                // the socket shows the colour that opens the gate
                object gate = gates.FirstOrDefault(g => JsonTree.Str(g, "control") == id);
                ColorId open = gate != null ? (ColorId)JsonTree.Int(gate, "openColor") : color;
                MeshVisual(go.transform, "Socket", kit.ring, new Vector3(0f, 0.02f, 0f), Vector3.one * 1.25f, open == ColorId.Red ? kit.redPlain : kit.bluePlain);
                return Finish(go, new[] { post, handle }, kit);
            }
            default:
                return Source(parent, id, at, size, color, kit);
        }
    }

    private static ColorTarget Source(Transform parent, string id, Vector3 at, Vector3 size, ColorId color, Kit kit)
    {
        GameObject go = TargetRoot(parent, "Source " + id, id, TargetKind.Source, color, at);
        CapsuleCollider body = go.AddComponent<CapsuleCollider>();
        body.radius = size.x * 0.5f;
        body.height = size.y;
        body.center = new Vector3(0f, size.y * 0.5f, 0f);

        Renderer r = Visual(go.transform, PrimitiveType.Cylinder, new Vector3(0f, SOURCE_VISUAL * 0.5f, 0f), new Vector3(size.x, SOURCE_VISUAL * 0.5f, size.z), Tinted(color, kit));
        return Finish(go, new[] { r }, kit);
    }

    private static Plate BuildPlate(Transform parent, string id, Vector3 at, Rigidbody box, Kit kit)
    {
        var go = new GameObject("Plate " + id);
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        Renderer ring = MeshVisual(go.transform, "Ring", kit.ring, Vector3.up * 0.02f, Vector3.one * 1.1f, kit.plateIdle);
        Plate plate = go.AddComponent<Plate>();
        plate.Configure(box, ring, kit.plateIdle, kit.platePressed);
        return plate;
    }

    private static void BuildGate(Transform parent, string id, Vector3 at, Plate plate, ColorTarget lever, ColorId openColor, Kit kit)
    {
        var go = new GameObject("Gate " + id);
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        BoxCollider body = AddBox(go, new Vector3(0f, 1.2f, 0f), new Vector3(CELL, 2.4f, CELL));

        Transform visual = Child(go.transform, "Visual");
        Visual(visual, PrimitiveType.Cube, new Vector3(0f, COLORED_WALL_VISUAL * 0.5f, 0f), new Vector3(CELL, COLORED_WALL_VISUAL, CELL), kit.gate);
        Material band = lever != null ? (openColor == ColorId.Red ? kit.redPlain : kit.bluePlain) : kit.plateIdle;
        Visual(visual, PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(CELL + 0.04f, 0.16f, CELL + 0.04f), band);

        go.AddComponent<Gate>().Configure(plate, lever, openColor, body, visual);
    }

    // ---------- connectors ----------

    private static void BuildConnector(object connector, Transform map, Kit kit, Dictionary<int, RegionParts> regions)
    {
        int from = JsonTree.Int(connector, "from");
        int to = JsonTree.Int(connector, "to");
        float railHeight = JsonTree.Num(connector, "railHeight", 3f);
        float width = JsonTree.Num(connector, "width", WALKWAY);

        Transform root = new GameObject($"Connector{from:00}-{to:00}").transform;
        root.SetParent(map, false);

        List<Vector3> points = JsonTree.List(connector, "points").Select(p => V3(JsonTree.Floats(p))).ToList();
        List<(Vector3 a, Vector3 b)> segments = MergeSegments(points);

        for (int k = 0; k < segments.Count; k++)
        {
            (Vector3 a, Vector3 b) = segments[k];
            Vector3 dir = Flat(b - a).normalized;
            float length = Flat(b - a).magnitude;
            Vector3 PointAt(float t) => a + (b - a) * (t / length);

            // square corners: extend each slab half a walkway into the turn
            float startPad = k > 0 ? width * 0.5f : 0f;
            float endPad = k < segments.Count - 1 ? width * 0.5f : 0f;
            Slab(root, "Walkway", PointAt(-startPad), PointAt(length + endPad), width, kit.path);

            Vector3 right = new Vector3(dir.z, 0f, -dir.x);
            float offset = width * 0.5f + RAIL_THICKNESS * 0.5f;
            float corner = width * 0.5f + RAIL_THICKNESS;
            int turnIn = k > 0 ? TurnSide(Flat(segments[k - 1].b - segments[k - 1].a), dir) : 0;
            int turnOut = k < segments.Count - 1 ? TurnSide(dir, Flat(segments[k + 1].b - segments[k + 1].a)) : 0;

            foreach (int side in new[] { 1, -1 })
            {
                // the inner rail of a turn stops short, the outer one runs on to close the corner
                float start = turnIn == 0 ? 0f : (turnIn == side ? corner : -corner);
                float end = turnOut == 0 ? length : length + (turnOut == side ? -corner : corner);
                if (end - start < 0.05f) { continue; }
                Vector3 shift = right * (side * offset);
                Rail(root, PointAt(start) + shift, PointAt(end) + shift, railHeight, kit.wall);
            }
        }

        Dictionary<string, object> staging = JsonTree.Obj(connector, "staging");
        if (staging != null && regions.TryGetValue(to, out RegionParts parts))
        {
            Vector3 at = V3(JsonTree.Floats(staging, "pos"));
            ColorTarget source = Source(root, JsonTree.Str(staging, "id"), at, new Vector3(1f, 2.4f, 1f), (ColorId)JsonTree.Int(staging, "color"), kit);
            parts.targets.Add(source);
        }
    }

    // collapses the one-metre centreline samples into straight runs of constant grade
    private static List<(Vector3, Vector3)> MergeSegments(List<Vector3> points)
    {
        var result = new List<(Vector3, Vector3)>();
        int i = 0;
        while (i < points.Count - 1)
        {
            int j = i + 1;
            Vector3 dir = Flat(points[j] - points[i]).normalized;
            float grade = Grade(points[i], points[j]);
            while (j + 1 < points.Count)
            {
                Vector3 next = Flat(points[j + 1] - points[j]).normalized;
                if (Vector3.Dot(next, dir) < 0.999f || Mathf.Abs(Grade(points[j], points[j + 1]) - grade) > 0.02f) { break; }
                j++;
            }
            result.Add((points[i], points[j]));
            i = j;
        }
        return result;
    }

    private static float Grade(Vector3 a, Vector3 b)
    {
        float run = Flat(b - a).magnitude;
        return run > 0.0001f ? (b.y - a.y) / run : 0f;
    }

    // +1 when the path turns right, -1 when it turns left
    private static int TurnSide(Vector3 before, Vector3 after)
    {
        float y = Vector3.Cross(before.normalized, after.normalized).y;
        return Mathf.Abs(y) < 0.01f ? 0 : (y > 0f ? 1 : -1);
    }

    // ---------- boss arena, geometry only ----------

    private static void BuildArena(Dictionary<string, object> boss, Transform map, Kit kit, Dictionary<int, RegionParts> regions)
    {
        if (boss == null) { return; }
        int route = JsonTree.Int(boss, "route");
        Dictionary<string, object> placement = JsonTree.Obj(boss, "placement");
        Dictionary<string, object> spec = JsonTree.Obj(boss, "spec");
        Dictionary<string, object> bounds = JsonTree.Obj(spec, "bounds");
        Vector3 origin = new Vector3(JsonTree.Num(placement, "x"), 0f, JsonTree.Num(placement, "z"));
        Vector3 Local(float[] xz, float y = 0f) => origin + new Vector3(xz[0], y, xz[1]);

        float w = JsonTree.Num(bounds, "right") - JsonTree.Num(bounds, "left");
        float d = JsonTree.Num(bounds, "bottom") - JsonTree.Num(bounds, "top");

        Transform root = new GameObject($"Route{route:00}").transform;
        root.SetParent(map, false);
        Transform things = Child(root, "Targets");

        Dictionary<string, object> checkpoint = JsonTree.Obj(boss, "checkpoint");
        var parts = new RegionParts
        {
            route = route,
            root = root,
            respawn = V3(JsonTree.Floats(checkpoint, "respawn")),
            activation = V3(JsonTree.Floats(checkpoint, "activation")),
            elevated = true,
            arrival = (ColorId)JsonTree.Int(boss, "entry"),
        };
        regions[route] = parts;

        Slab(root, "Floor", origin + new Vector3(0f, 0f, d * 0.5f), origin + new Vector3(w, 0f, d * 0.5f), d, kit.floor);

        // perimeter with the entry gap on the south side at x 8..10
        const float t = 0.5f;
        const float h = 3f;
        float[] entry = JsonTree.Floats(spec, "playerStart");
        Perimeter(root, origin + new Vector3(-t, 0f, -t), origin + new Vector3(w + t, 0f, -t), h, kit);
        Perimeter(root, origin + new Vector3(-t, 0f, -t), origin + new Vector3(-t, 0f, d + t), h, kit);
        Perimeter(root, origin + new Vector3(w + t, 0f, -t), origin + new Vector3(w + t, 0f, d - 2f), h, kit);
        Perimeter(root, origin + new Vector3(-t, 0f, d + t), origin + new Vector3(entry[0] - 1f, 0f, d + t), h, kit);
        Perimeter(root, origin + new Vector3(entry[0] + 1f, 0f, d + t), origin + new Vector3(w + t, 0f, d + t), h, kit);

        // the exit stays shut in this demo
        NormalBlock(root, "Exit", origin + new Vector3(w + t, 0f, d - 1f), new Vector3(t * 2f, h, 2f + t * 2f), WALL_CUTAWAY, Quaternion.identity, kit.gate);

        foreach (object anchor in JsonTree.List(spec, "anchors"))
        {
            Vector3 at = Local(JsonTree.Floats(anchor, "pos"));
            parts.targets.Add(Source(things, JsonTree.Str(anchor, "id"), at, new Vector3(1f, 2.4f, 1f), (ColorId)JsonTree.Int(anchor, "color"), kit));
        }

        foreach (object imploder in JsonTree.List(spec, "imploders"))
        {
            Material m = JsonTree.Int(imploder, "color") == 0 ? kit.redPlain : kit.bluePlain;
            float r = JsonTree.Num(spec, "implosionRadius", 1.25f);
            MeshVisual(things, "Imploder " + JsonTree.Str(imploder, "id"), kit.ring, Local(JsonTree.Floats(imploder, "pos"), 0.02f), new Vector3(r, 1f, r), m);
        }

        foreach (object fist in JsonTree.List(spec, "fists"))
        {
            Material m = JsonTree.Int(fist, "color") == 0 ? kit.redPlain : kit.bluePlain;
            float r = JsonTree.Num(spec, "fistRadius", 0.55f);
            Visual(things, PrimitiveType.Sphere, Local(JsonTree.Floats(fist, "dock"), r), Vector3.one * r * 2f, m).name = "Fist " + JsonTree.Str(fist, "id");
        }

        MeshVisual(things, "Boss", kit.diamond, Local(JsonTree.Floats(spec, "boss"), 2.5f), new Vector3(5f, 2.5f, 5f), kit.boss);
    }

    private static void Perimeter(Transform parent, Vector3 a, Vector3 b, float height, Kit kit)
    {
        Vector3 along = b - a;
        Quaternion rot = Quaternion.LookRotation(along.normalized, Vector3.up);
        NormalBlock(parent, "Perimeter", (a + b) * 0.5f, new Vector3(1f, height, along.magnitude), a.y + WALL_CUTAWAY, rot, kit.wall);
    }

    // ---------- regions, player and systems ----------

    private static void FinishRegion(RegionParts parts)
    {
        ColorRegion region = parts.root.gameObject.AddComponent<ColorRegion>();
        region.Configure(parts.route, parts.respawn + Vector3.up * 0.05f, parts.elevated, parts.targets.ToArray(), parts.boxes.ToArray());
        parts.region = region;

        var trigger = new GameObject("Checkpoint");
        trigger.transform.SetParent(parts.root, false);
        trigger.transform.position = parts.activation + Vector3.up * 1.2f;
        BoxCollider box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2.2f, 2.4f, 2.2f);
        trigger.AddComponent<CheckpointTrigger>().Configure(region);
    }

    private static void BuildKillVolume(Transform map)
    {
        var kill = new GameObject("KillVolume");
        kill.transform.SetParent(map, false);
        kill.transform.position = new Vector3(115f, -3.5f, 65f);
        BoxCollider box = kill.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(340f, 1f, 240f);
        kill.AddComponent<KillVolume>();
    }

    private static DemoPlayer BuildPlayer(RegionParts start, Kit kit)
    {
        var go = new GameObject("Player");
        go.tag = "Player";
        go.transform.position = start.respawn + Vector3.up * 0.05f;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        CapsuleCollider body = go.AddComponent<CapsuleCollider>();
        body.radius = 0.3f;
        body.height = 1.6f;
        body.center = new Vector3(0f, 0.8f, 0f);

        ColorState state = go.AddComponent<ColorState>();
        state.Configure(start.arrival);

        Renderer shape = MeshVisual(go.transform, "Body", kit.prism, Vector3.zero, Vector3.one, start.arrival == ColorId.Red ? kit.red : kit.blue);

        DemoPlayer player = go.AddComponent<DemoPlayer>();
        player.Configure(shape.transform);
        go.AddComponent<ColorTint>().Configure(state, new[] { shape }, kit.red, kit.blue);

        LineRenderer beam = Line(go.transform, "Beam", 0.12f, kit.line);
        LineRenderer guide = Line(go.transform, "Guide", 0.035f, kit.line);
        go.AddComponent<ExchangeGun>().Configure(beam, guide, RED, BLUE);
        return player;
    }

    private static void BuildSystems(DemoPlayer player, ColorRegion first)
    {
        var director = new GameObject("Director");
        director.AddComponent<DemoDirector>().Configure(player, first);

        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 120f;
        cam.transform.rotation = Quaternion.Euler(35f, 45f, 0f);
        cam.transform.position = player.transform.position - cam.transform.forward * 40f;
        cam.gameObject.AddComponent<DemoCamera>().Configure(player);

        Light sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(55f, 10f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1.2f;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.64f, 0.7f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.18f);
    }

    // ---------- geometry helpers ----------

    private static GameObject TargetRoot(Transform parent, string name, string id, TargetKind kind, ColorId color, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.AddComponent<ColorState>().Configure(color);
        go.AddComponent<ColorTarget>().Configure(id, kind);
        return go;
    }

    private static ColorTarget Finish(GameObject go, Renderer[] tinted, Kit kit)
    {
        go.AddComponent<ColorTint>().Configure(go.GetComponent<ColorState>(), tinted, kit.red, kit.blue);
        return go.GetComponent<ColorTarget>();
    }

    private static Material Tinted(ColorId color, Kit kit)
    {
        return color == ColorId.Red ? kit.red : kit.blue;
    }

    private static BoxCollider AddBox(GameObject go, Vector3 center, Vector3 size)
    {
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        return box;
    }

    // a slab whose top face runs along the line from a to b. works flat or sloped
    private static void Slab(Transform parent, string name, Vector3 a, Vector3 b, float width, Material material)
    {
        Vector3 along = b - a;
        Quaternion rot = Quaternion.LookRotation(along.normalized, Vector3.up);
        Vector3 normal = rot * Vector3.up;
        GameObject slab = GameAssets.Shape(PrimitiveType.Cube, name, new Vector3(width, SLAB, along.magnitude), material);
        slab.transform.SetParent(parent, false);
        slab.transform.SetPositionAndRotation((a + b) * 0.5f - normal * (SLAB * 0.5f), rot);
        slab.isStatic = true;
    }

    // a connector rail on the walkway edge, full height for collision and shots, drawn low
    private static void Rail(Transform parent, Vector3 a, Vector3 b, float height, Material material)
    {
        Vector3 along = b - a;
        Quaternion rot = Quaternion.LookRotation(Flat(along).normalized, Vector3.up);
        Vector3 mid = (a + b) * 0.5f;
        float length = Flat(along).magnitude + RAIL_THICKNESS;
        float lift = Mathf.Abs(along.y) * 0.5f;

        var rail = new GameObject("Rail");
        rail.transform.SetParent(parent, false);
        rail.transform.SetPositionAndRotation(mid, rot);
        rail.isStatic = true;
        AddBox(rail, new Vector3(0f, height * 0.5f, 0f), new Vector3(RAIL_THICKNESS, height + lift * 2f, length));

        // the drawn part follows the slope so it reads as a guard, not a step
        GameObject visual = GameAssets.Shape(PrimitiveType.Cube, "Visual", new Vector3(RAIL_THICKNESS, RAIL_CUTAWAY, along.magnitude + RAIL_THICKNESS), material);
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(rail.transform, false);
        visual.transform.SetPositionAndRotation(mid + Vector3.up * (RAIL_CUTAWAY * 0.5f), Quaternion.LookRotation(along.normalized, Vector3.up));
        visual.isStatic = true;
    }

    // full-size collider, drawn only up to visualTop
    private static void NormalBlock(Transform parent, string name, Vector3 basePoint, Vector3 size, float visualTop, Quaternion rotation, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(basePoint, rotation);
        go.isStatic = true;
        AddBox(go, new Vector3(0f, size.y * 0.5f, 0f), size);

        float drawn = Mathf.Clamp(visualTop - basePoint.y, 0.1f, size.y);
        Renderer r = Visual(go.transform, PrimitiveType.Cube, new Vector3(0f, drawn * 0.5f, 0f), new Vector3(size.x, drawn, size.z), material);
        r.gameObject.isStatic = true;
    }

    private static float NearbyTop(List<Vector3> tops, float x, float z, float ceiling, float reach, float fallback)
    {
        float best = float.MinValue;
        foreach (Vector3 top in tops)
        {
            if (Mathf.Abs(top.x - x) > reach || Mathf.Abs(top.z - z) > reach) { continue; }
            if (top.y > ceiling + 0.01f) { continue; }
            best = Mathf.Max(best, top.y);
        }
        return best > float.MinValue ? Mathf.Max(best, fallback) : fallback;
    }

    private static Renderer Visual(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject go = GameAssets.Shape(type, "Visual", scale, material);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.GetComponent<Renderer>();
    }

    private static Renderer MeshVisual(Transform parent, string name, Mesh mesh, Vector3 localPosition, Vector3 scale, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        return r;
    }

    private static LineRenderer Line(Transform parent, string name, float width, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.widthMultiplier = width;
        line.numCapVertices = 2;
        line.sharedMaterial = material;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.alignment = LineAlignment.View;
        return line;
    }

    private static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Vector3 V3(float[] v)
    {
        return new Vector3(v[0], v[1], v[2]);
    }

    private static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    // ---------- assets ----------

    private static Kit MakeKit()
    {
        Texture2D stripes = Pattern("RedDiagonal", (x, y) => (x + y) % 16 < 5);
        Texture2D dots = Pattern("BlueDots", (x, y) =>
        {
            int dx = x % 16 - 8;
            int dy = y % 16 - 8;
            return dx * dx + dy * dy <= 10;
        });

        var kit = new Kit
        {
            floor = Mat("DemoFloor", new Color(0.78f, 0.76f, 0.71f)),
            ramp = Mat("DemoRamp", new Color(0.62f, 0.68f, 0.6f)),
            path = Mat("DemoWalkway", new Color(0.66f, 0.66f, 0.63f)),
            wall = Mat("DemoWall", new Color(0.33f, 0.34f, 0.38f)),
            gate = Mat("DemoGate", new Color(0.2f, 0.21f, 0.25f), 0.4f),
            red = Mat("DemoRed", RED, 0.3f, 0.15f, stripes),
            blue = Mat("DemoBlue", BLUE, 0.3f, 0.15f, dots),
            redPlain = Mat("DemoRedPlain", RED, 0.5f, 0.6f),
            bluePlain = Mat("DemoBluePlain", BLUE, 0.5f, 0.6f),
            plateIdle = Mat("DemoPlateIdle", new Color(0.95f, 0.95f, 0.92f), 0.4f, 0.2f),
            platePressed = Mat("DemoPlatePressed", new Color(0.45f, 0.92f, 0.55f), 0.4f, 0.8f),
            boss = Mat("DemoBoss", new Color(0.1f, 0.1f, 0.13f), 0.6f),
            line = LineMaterial(),
            prism = SaveMesh(PrismMesh(), "PlayerPrism"),
            diamond = SaveMesh(DiamondMesh(), "EnemyDiamond"),
            ring = SaveMesh(RingMesh(), "PlateRing"),
        };
        return kit;
    }

    private static Material Mat(string name, Color color, float smoothness = 0.1f, float glow = 0f, Texture2D map = null)
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
        if (map != null)
        {
            mat.SetTexture("_BaseMap", map);
            mat.SetTextureScale("_BaseMap", Vector2.one * 2f);
        }
        if (glow > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * glow);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material LineMaterial()
    {
        string path = MATERIAL_DIR + "/DemoLine.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(mat, path);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // accessibility accents: red gets diagonal stripes, blue gets dots
    private static Texture2D Pattern(string name, System.Func<int, int, bool> mark)
    {
        const int size = 64;
        string path = TEXTURE_DIR + "/" + name + ".png";
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte v = mark(x, y) ? (byte)190 : (byte)255;
                pixels[y * size + x] = new Color32(v, v, v, 255);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = MESH_DIR + "/" + name + ".asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        existing.Clear();
        EditorUtility.CopySerialized(mesh, existing);
        Object.DestroyImmediate(mesh);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    // player: a triangle seen from above, pointing along +Z
    private static Mesh PrismMesh()
    {
        var b = new FlatMesh(new Vector3(0f, 0.7f, 0f));
        Vector3[] foot = { new Vector3(0f, 0f, 0.5f), new Vector3(0.34f, 0f, -0.32f), new Vector3(-0.34f, 0f, -0.32f) };
        Vector3 up = Vector3.up * 1.4f;
        Vector3 lift = Vector3.up * 0.04f;
        b.Tri(foot[0] + up, foot[1] + up, foot[2] + up);
        b.Tri(foot[0] + lift, foot[1] + lift, foot[2] + lift);
        for (int i = 0; i < 3; i++)
        {
            Vector3 a = foot[i];
            Vector3 c = foot[(i + 1) % 3];
            b.Quad(a + lift, a + up, c + up, c + lift);
        }
        return b.Build("PlayerPrism");
    }

    // enemy: an octahedron, unit radius 0.5 and half height 1
    private static Mesh DiamondMesh()
    {
        var b = new FlatMesh(Vector3.zero);
        Vector3 top = Vector3.up;
        Vector3 bottom = Vector3.down;
        Vector3[] ring = { new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f) };
        for (int i = 0; i < 4; i++)
        {
            b.Tri(top, ring[i], ring[(i + 1) % 4]);
            b.Tri(bottom, ring[i], ring[(i + 1) % 4]);
        }
        return b.Build("EnemyDiamond");
    }

    // plate outline: a flat annulus, outer radius 0.85
    private static Mesh RingMesh()
    {
        const int segments = 40;
        const float inner = 0.62f;
        const float outer = 0.85f;
        var b = new FlatMesh(Vector3.down);
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * Mathf.PI * 2f / segments;
            float a1 = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 i0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * inner;
            Vector3 i1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * inner;
            Vector3 o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * outer;
            Vector3 o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * outer;
            b.Quad(i0, o0, o1, i1);
        }
        return b.Build("PlateRing");
    }

    // flat shaded mesh that orients every face away from a centre point
    private class FlatMesh
    {
        private readonly Vector3 centre;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();

        public FlatMesh(Vector3 inside)
        {
            centre = inside;
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            Vector3 outward = (a + b + c) / 3f - centre;
            if (Vector3.Dot(normal, outward) < 0f) { (b, c) = (c, b); }
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
