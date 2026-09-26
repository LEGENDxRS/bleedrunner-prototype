using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Prefabs")]
    // FormerlySerializedAs preserves existing Inspector assignments if the name changed
    [FormerlySerializedAs("fodderEnemyPrefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Balances")]
    public float baseSpawnInterval = 1.0f;
    public int maxEnemiesAlive = 10;
    public float spawnRadiusMin = 7f;
    public float spawnRadiusMax = 13f;
    public float arenaLimit = 22f;

    [Header("Telemetry Debug")]
    [SerializeField] private float currentTension = 0.5f;

    private float nextSpawnTime;
    private Transform player;
    public static int ActiveEnemyCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ActiveEnemyCount = 0;
    }

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.isDead) return;

        // Auto-recover player reference if Start missed it
        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        // Calculate Tension Factor: (CurrentTimer / MaxTimer) * KillVelocity
        if (TimeManager.Instance != null)
        {
            float timerRatio = TimeManager.Instance.currentTime / TimeManager.Instance.maxTime;
            currentTension = timerRatio * Mathf.Max(TimeManager.Instance.killVelocity, 0.5f);
        }

        if (Time.time >= nextSpawnTime)
        {
            if (ActiveEnemyCount < maxEnemiesAlive)
            {
                EvaluateAndSpawn(currentTension);
            }

            // Adjust spawn cadence dynamically based on tension
            float dynamicInterval = (currentTension < 0.25f) ? baseSpawnInterval * 0.4f : baseSpawnInterval;
            nextSpawnTime = Time.time + dynamicInterval;
        }
    }

    void FindPlayer()
    {
        PlayerController25D p = FindFirstObjectByType<PlayerController25D>();
        if (p != null) player = p.transform;
    }

    void EvaluateAndSpawn(float tension)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("<color=red>[EnemySpawner]</color> Enemy Prefab slot is EMPTY in the Inspector!");
            return;
        }

        Vector3 spawnPos;

        if (tension < 0.25f)
        {
            // High Tension: Spawn directly in the player's forward vector for buzzer-beater saves
            Vector3 forwardOffset = player.forward * Random.Range(spawnRadiusMin, spawnRadiusMax * 0.8f);
            Vector3 lateralJitter = player.right * Random.Range(-2.5f, 2.5f);
            spawnPos = player.position + forwardOffset + lateralJitter;
        }
        else
        {
            // Normal / Low Tension: Ambient perimeter distribution
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
            spawnPos = new Vector3(player.position.x + circle.x, 1f, player.position.z + circle.y);
        }

        spawnPos.y = 1f;
        spawnPos.x = Mathf.Clamp(spawnPos.x, -arenaLimit, arenaLimit);
        spawnPos.z = Mathf.Clamp(spawnPos.z, -arenaLimit, arenaLimit);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}