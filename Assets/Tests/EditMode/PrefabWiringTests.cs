using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class PrefabWiringTests
{
    private static GameObject Load(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/{name}.prefab");
        Assert.IsNotNull(prefab, $"{name}.prefab is missing, rebuild from the PennBoy menu");
        return prefab;
    }

    private static void AssertWired(Component component, params string[] fields)
    {
        Assert.IsNotNull(component, "component is missing from the prefab");
        var serialized = new SerializedObject(component);
        foreach (string field in fields)
        {
            SerializedProperty property = serialized.FindProperty(field);
            Assert.IsNotNull(property, $"{component.GetType().Name} has no serialized field '{field}'");
            Assert.IsNotNull(property.objectReferenceValue, $"{component.GetType().Name}.{field} is not wired by the builder");
        }
    }

    [Test]
    public void PlayerPrefabIsWired()
    {
        GameObject player = Load("Player");

        Assert.AreEqual("Player", player.tag, "enemies and pickups find the player by tag");
        Assert.IsNotNull(player.GetComponent<PlayerController>());
        Assert.IsNotNull(player.GetComponent<Rigidbody>());
        Assert.Greater(player.GetComponent<Damageable>().Max, 0f);
        AssertWired(player.GetComponent<PlayerShooter>(), "projectilePrefab", "muzzle");
        AssertWired(player.GetComponent<PlayerLife>(), "deathEffect");
    }

    [Test]
    public void EnemyPrefabIsWired()
    {
        GameObject enemy = Load("Enemy");

        Assert.IsNotNull(enemy.GetComponent<Rigidbody>());
        Assert.Greater(enemy.GetComponent<Damageable>().Max, 0f);
        AssertWired(enemy.GetComponent<EnemyBrain>(), "deathEffect");
    }

    [Test]
    public void ProjectilePrefabIsWired()
    {
        GameObject projectile = Load("Projectile");

        Assert.IsTrue(projectile.GetComponent<Collider>().isTrigger, "the projectile reports hits through trigger callbacks");
        Assert.IsTrue(projectile.GetComponent<Rigidbody>().isKinematic);
        AssertWired(projectile.GetComponent<Projectile>(), "impactEffect");
    }

    [Test]
    public void PickupPrefabsUseATrigger()
    {
        foreach (string name in new[] { "PickupHealth", "PickupScore" })
        {
            GameObject pickup = Load(name);
            Assert.IsNotNull(pickup.GetComponent<Pickup>(), $"{name} has no Pickup component");
            Assert.IsTrue(pickup.GetComponent<Collider>().isTrigger, $"{name} must use a trigger collider");
        }
    }

    [Test]
    public void EveryPlayerInputActionExists()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        Assert.IsNotNull(asset, "the project wide input actions asset is missing");

        foreach (string path in new[] { "Player/Move", "Player/Point", "Player/Aim", "Player/Dash", "Player/Sprint", "Player/Attack", "Player/Zoom" })
        {
            Assert.IsNotNull(asset.FindAction(path), $"input action '{path}' is missing and the scripts that read it would fail at runtime");
        }
    }
}
