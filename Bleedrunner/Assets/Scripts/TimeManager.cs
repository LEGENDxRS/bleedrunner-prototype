using UnityEngine;
using TMPro;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Timer Settings")]
    public float maxTime = 6.0f;
    public float currentTime;
    public bool isDead = false;

    [Header("UI Hooks")]
    public TextMeshProUGUI timerDisplay;
    public GameObject gameOverUI;

    [Header("Telemetry & Director")]
    public int killsThisSecond = 0;
    public float killVelocity = 0f;
    private float velocitySampleTimer = 0f;

    private PlayerController25D player;
    private Vector3 playerStartPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentTime = maxTime;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        if (player != null) playerStartPos = player.transform.position;
    }

    void Update()
    {
        // 150ms Instant Restart Requirement (No Scene Hitching)
        if (isDead)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SoftResetRun();
            }
            return;
        }

        currentTime -= Time.deltaTime;

        // Track kills per second for the AI Director
        velocitySampleTimer += Time.deltaTime;
        if (velocitySampleTimer >= 1.0f)
        {
            killVelocity = killsThisSecond / velocitySampleTimer;
            killsThisSecond = 0;
            velocitySampleTimer = 0f;
        }

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            TriggerDeath();
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (timerDisplay != null)
        {
            timerDisplay.text = currentTime.ToString("F2") + "s";
            timerDisplay.color = (currentTime <= 2.0f)
                ? Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 8f, 1f))
                : new Color(1f, 0.25f, 0.2f);
        }
    }

    public void AddTime(float amount)
    {
        if (isDead) return;
        currentTime = Mathf.Min(currentTime + amount, maxTime);
        killsThisSecond++;
    }

    public void DeductTime(float amount)
    {
        if (isDead) return;
        currentTime -= amount;
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            TriggerDeath();
        }
    }

    void TriggerDeath()
    {
        isDead = true;
        if (gameOverUI != null) gameOverUI.SetActive(true);
        if (player != null) player.enabled = false;
    }

    public void SoftResetRun()
    {
        // Purge active enemies instantly
        EnemyTarget[] activeEnemies = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
        for (int i = 0; i < activeEnemies.Length; i++)
        {
            Destroy(activeEnemies[i].gameObject);
        }

        // Reset Player
        if (player != null)
        {
            player.transform.position = playerStartPos;
            player.enabled = true;
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        // Reset Timers & Director State
        currentTime = maxTime;
        isDead = false;
        killsThisSecond = 0;
        killVelocity = 0f;
        velocitySampleTimer = 0f;

        if (gameOverUI != null) gameOverUI.SetActive(false);
    }
}