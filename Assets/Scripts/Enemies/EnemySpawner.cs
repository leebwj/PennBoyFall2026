using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private EnemyBrain enemyPrefab;
    [SerializeField] private float radius = 6f;
    [SerializeField] private int maxAlive = 4;
    [SerializeField] private float intervalSeconds = 4f;
    [SerializeField] private float firstSpawnDelaySeconds = 2f;

    private readonly List<EnemyBrain> alive = new List<EnemyBrain>();
    private float timer;

    void Start()
    {
        timer = firstSpawnDelaySeconds;
    }

    void Update()
    {
        alive.RemoveAll(enemy => enemy == null);
        if (enemyPrefab == null || alive.Count >= maxAlive) { return; }

        timer -= Time.deltaTime;
        if (timer > 0f) { return; }

        timer = intervalSeconds;
        Spawn();
    }

    private void Spawn()
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
        alive.Add(Instantiate(enemyPrefab, position, Quaternion.identity));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
