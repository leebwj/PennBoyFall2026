using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Vector3 respawnPoint = new Vector3(0f, 1f, 0f);

    public int Score { get; private set; }
    public int Deaths { get; private set; }
    public Vector3 RespawnPoint => respawnPoint;
    public PlayerController Player { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Player = FindFirstObjectByType<PlayerController>();
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; }
    }

    public void AddScore(int amount)
    {
        Score += amount;
    }

    public void ReportDeath()
    {
        Deaths++;
    }

    public void SetRespawn(Vector3 position)
    {
        respawnPoint = position;
    }
}
