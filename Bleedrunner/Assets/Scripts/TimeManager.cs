using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Timer Settings")]
    public float maxTime = 6.0f;
    public float currentTime;
    public float timerDrainRate = 1.0f;
    public bool isDead = false;
    public bool isPaused = false;

    [Header("Warning & Visual Settings")]
    public float criticalTimeThreshold = 2.0f;
    public float warningFlashSpeed = 8.0f;
    public Color normalTimerColor = new Color(1f, 0.25f, 0.2f);
    public Color criticalColorA = Color.red;
    public Color criticalColorB = Color.yellow;

    [Header("UI Hooks")]
    public TextMeshProUGUI timerDisplay;
    public GameObject gameOverUI;
    [Tooltip("Drag your GameOverText object here to let it switch messages automatically.")]
    public TextMeshProUGUI gameOverText;

    [Header("Device-Specific Death Text")]
    [TextArea(2, 3)]
    public string pcDeathMessage = "FLATLINED\n<size=55%><color=#AAAAAA>PRESS [R] TO REBOOT</color></size>";
    [TextArea(2, 3)]
    public string mobileDeathMessage = "FLATLINED\n<size=55%><color=#AAAAAA>TAP ANYWHERE TO REBOOT</color></size>";

    [Header("Telemetry & Director")]
    public int killsThisSecond = 0;
    public float killVelocity = 0f;
    private float velocitySampleTimer = 0f;

    private PlayerController25D player;
    private Vector3 playerStartPos;
    private float initialMaxTime;
    private float deadTimeElapsed = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        initialMaxTime = maxTime;
        currentTime = maxTime;
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController25D>();
        if (player != null) playerStartPos = player.transform.position;
    }

    void Update()
    {
        // --- DEATH & REBOOT LOGIC ---
        if (isDead)
        {
            deadTimeElapsed += Time.unscaledDeltaTime;

            // Small 0.35s grace window prevents accidental instant skips while mashing controls
            if (deadTimeElapsed >= 0.35f)
            {
                bool rPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
                bool screenTapped = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

                // On Mobile: Tap anywhere (or touch screen)
                // On PC: Press [R] or Left Click
                if (rPressed || screenTapped || (IsMobileDevice() && mouseClicked))
                {
                    SoftResetRun();
                }
            }
            return;
        }

        if (isPaused) return;

        currentTime -= Time.deltaTime * timerDrainRate;

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

            if (currentTime <= criticalTimeThreshold)
            {
                timerDisplay.color = Color.Lerp(criticalColorA, criticalColorB, Mathf.PingPong(Time.time * warningFlashSpeed, 1f));
            }
            else
            {
                timerDisplay.color = normalTimerColor;
            }
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
        deadTimeElapsed = 0f;
        Debug.Log("<color=red>[BLEEDRUNNER]</color> Player flatlined!");

        // Update the message based on device
        if (gameOverText != null)
        {
            gameOverText.text = IsMobileDevice() ? mobileDeathMessage : pcDeathMessage;
        }

        if (gameOverUI != null) gameOverUI.SetActive(true);
        if (player != null) player.enabled = false;
    }

    public bool IsMobileDevice()
    {
#if UNITY_EDITOR
        // Test mobile behavior in Editor if you have mobile controls enabled
        return MobileControls.Instance != null && MobileControls.Instance.showInEditorForTesting;
#else
        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
#endif
    }

    public void SoftResetRun()
    {
        EnemyTarget[] activeEnemies = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
        for (int i = 0; i < activeEnemies.Length; i++)
        {
            Destroy(activeEnemies[i].gameObject);
        }

        if (player != null)
        {
            player.enabled = true;
            player.ResetPlayerStats();
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
        }

        PlayerShooting shooting = FindFirstObjectByType<PlayerShooting>();
        if (shooting != null)
        {
            shooting.ResetShootingStats();
        }

        if (PerkManager.Instance != null)
        {
            PerkManager.Instance.ResetPerks();
        }

        maxTime = initialMaxTime;
        currentTime = maxTime;
        timerDrainRate = 1.0f;
        isDead = false;
        isPaused = false;
        deadTimeElapsed = 0f;
        killsThisSecond = 0;
        killVelocity = 0f;
        velocitySampleTimer = 0f;

        if (ChamberManager.Instance != null)
        {
            ChamberManager.Instance.ResetProgression();
        }

        if (gameOverUI != null) gameOverUI.SetActive(false);
    }
}