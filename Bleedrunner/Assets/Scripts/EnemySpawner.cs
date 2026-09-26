using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public float spawnInterval = 1.2f;
    public float spawnRadiusMin = 6f;
    public float spawnRadiusMax = 14f;
    public int maxEnemiesAlive = 8;

    [Header("Arena Bounds")]
    public float arenaLimit = 22f;

    private float nextSpawnTime;
    private Transform player;

    void Start()
    {
        PlayerController25D p = FindFirstObjectByType<PlayerController25D>();
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (TimeManager.Instance != null && TimeManager.Instance.isDead) return;

        if (Time.time >= nextSpawnTime)
        {
            // Only spawn if we haven't hit the active enemy cap
            int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (currentEnemies < maxEnemiesAlive)
            {
                SpawnEnemy();
            }
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        Vector3 spawnCenter = player != null ? player.position : Vector3.zero;

        // Pick a random direction around the player within min/max radius
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 spawnPos = new Vector3(spawnCenter.x + randomCircle.x, 1f, spawnCenter.z + randomCircle.y);

        // Clamp inside arena boundary
        spawnPos.x = Mathf.Clamp(spawnPos.x, -arenaLimit, arenaLimit);
        spawnPos.z = Mathf.Clamp(spawnPos.z, -arenaLimit, arenaLimit);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }
}