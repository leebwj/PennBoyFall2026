# Adding a feature

A worked example, start to finish. The feature is a bomb the player drops on a key press, which explodes after a delay and damages anything nearby. It touches every part of the project you are likely to need.

## 1. Branch

```
git switch main
git pull --ff-only
git switch -c player-bomb
```

If `git pull --ff-only` refuses, you have local commits on main. Stop and ask rather than forcing it.

## 2. Put the rules in a plain class

The part worth testing is the fuse: it counts down, it fires once, and it cannot be restarted. None of that needs a GameObject, so it goes in `Assets/Scripts/Common/Fuse.cs` with no `using UnityEngine`:

```csharp
using System;

public class Fuse
{
    private readonly float length;
    private float elapsed;

    public bool HasFired { get; private set; }

    public Fuse(float seconds)
    {
        if (seconds <= 0f) { throw new ArgumentOutOfRangeException(nameof(seconds)); }
        length = seconds;
    }

    public bool Tick(float deltaSeconds)
    {
        if (HasFired) { return false; }
        elapsed += deltaSeconds;
        if (elapsed < length) { return false; }
        HasFired = true;
        return true;
    }
}
```

`Tick` returning true exactly once is the whole contract, and it is the reason this is a class instead of two fields on a MonoBehaviour.

## 3. Test it

`Assets/Tests/EditMode/FuseTests.cs`:

```csharp
using NUnit.Framework;

public class FuseTests
{
    [Test]
    public void FiresOnceWhenTheFuseRunsOut()
    {
        var fuse = new Fuse(1f);

        Assert.IsFalse(fuse.Tick(0.6f));
        Assert.IsTrue(fuse.Tick(0.6f));
        Assert.IsFalse(fuse.Tick(0.6f));
        Assert.IsTrue(fuse.HasFired);
    }
}
```

Run Window, General, Test Runner. Watch it pass before you write the MonoBehaviour, because from here on a failure could be in either half.

## 4. Write the component

`Assets/Scripts/Combat/Bomb.cs`. It stays thin, because the interesting part is already tested:

```csharp
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField] private float fuseSeconds = 2f;
    [SerializeField] private float radius = 4f;
    [SerializeField] private float damage = 60f;
    [SerializeField] private GameObject explosionEffect;

    private Fuse fuse;

    void Awake()
    {
        fuse = new Fuse(fuseSeconds);
    }

    void Update()
    {
        if (!fuse.Tick(Time.deltaTime)) { return; }
        Explode();
    }

    private void Explode()
    {
        foreach (Collider hit in Physics.OverlapSphere(transform.position, radius))
        {
            Damageable target = hit.GetComponentInParent<Damageable>();
            if (target != null) { target.ApplyDamage(damage); }
        }

        if (explosionEffect != null) { Instantiate(explosionEffect, transform.position, Quaternion.identity); }
        Destroy(gameObject);
    }
}
```

`OverlapSphere` can return the same object through more than one collider, so if you later give enemies several colliders you will need to filter duplicates.

## 5. Add the input action

Open `Assets/InputSystem_Actions.inputactions`, select the Player map, and add a Button action named `Bomb`. Bind it to `Q` under Keyboard and Mouse, and to a face button under Gamepad. Save the asset.

Then read it the same way every other script does:

```csharp
private InputAction bombAction;

void Awake()
{
    bombAction = InputRef.Find("Player/Bomb", this);
}
```

Add `"Player/Bomb"` to the list in `PrefabWiringTests.EveryPlayerInputActionExists`, so a missing binding fails a test instead of failing silently at runtime.

## 6. Build the prefab in code

Prefabs are generated, so add a factory method to `Assets/Editor/GameAssets.cs` beside the others:

```csharp
public static GameObject BombPrefab(Material body, GameObject explosionEffect)
{
    GameObject bomb = Shape(PrimitiveType.Sphere, "Bomb", Vector3.one * 0.5f, body);
    bomb.AddComponent<Rigidbody>();

    Wire(bomb.AddComponent<Bomb>(),
        ("fuseSeconds", 2f),
        ("radius", 4f),
        ("damage", 60f),
        ("explosionEffect", explosionEffect));

    return Save(bomb, "Bomb");
}
```

The strings in `Wire` are field names. Get one wrong and the builder logs an error naming the field, which is why you should read the log after a rebuild rather than assuming it worked.

Call it from `SceneBuilder.Build`, pass the prefab to whatever spawns it, then run PennBoy, Build Main Scene.

## 7. Check the whole thing

Run the EditMode tests again, then press Play and drop a bomb next to an enemy. The tests will not catch a bomb that spawns inside the player or an explosion radius that feels wrong.

## 8. Commit

```
git add -A
git commit -m "add player bomb"
git push -u origin player-bomb
```

Open a pull request and let someone else read it. If the scene changed, say so in the description, since that is the file most likely to conflict with what someone else is doing.

## What to copy from this

Logic that can be tested without a scene should be. The MonoBehaviour is a shell around it. Input goes through the action map. Prefabs are built in `GameAssets` so that a rebuild does not throw your work away. Tests get updated in the same commit as the thing they cover.
