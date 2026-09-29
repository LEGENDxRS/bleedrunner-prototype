using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance;

    [Header("Chamber Clock Configuration")]
    [Tooltip("Starting clock balance at the start of a run.")]
    public float startingTime = 6f;
    [Tooltip("Maximum ceiling for the clock. Perks dynamically raise this value.")]
    public float maxTime = 6f;
    [Tooltip("Rate at which time drains per real second.")]
    public float timerDrainRate = 1.0f;
    public float currentTime;

    [Header("UI Telemetry Bindings")]
    public TextMeshProUGUI clockText;
    [Tooltip("Drag your single GameOverText GameObject here.")]
    public GameObject gameOverText;

    [Header("State Telemetry (Read-only)")]
    public bool isDead = false;
    public bool isPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResetClock();
    }

    void Update()
    {
        if (isDead || isPaused)
        {
            if (isDead)
            {
                // 1. PC: strictly requires the [R] key
                bool keyboardRestart = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

                // 2. Mobile / Device Simulator: strictly requires screen touch (no mouse clicks)
                bool touchRestart = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

                if (keyboardRestart || touchRestart)
                {
                    TriggerQuickRestart();
                }
            }
            return;
        }

        currentTime -= timerDrainRate * Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            TriggerDeath();
        }

        UpdateClockUI();
    }

    public void AddTime(float seconds)
    {
        if (isDead) return;
        currentTime += seconds;
        if (maxTime > 0f && currentTime > maxTime)
        {
            currentTime = maxTime;
        }
        UpdateClockUI();
    }

    public void DeductTime(float seconds)
    {
        if (isDead) return;
        currentTime -= seconds;
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            TriggerDeath();
        }
        UpdateClockUI();
    }

    public void IncreaseMaxCapacity(float addedCap)
    {
        maxTime += addedCap;
        currentTime += addedCap;
        UpdateClockUI();
    }

    void UpdateClockUI()
    {
        if (clockText != null)
        {
            clockText.text = currentTime.ToString("F2") + "s";
            clockText.color = (currentTime <= 2.5f) ? Color.red : Color.white;
        }
    }

    public void ResetClock()
    {
        currentTime = startingTime;
        isDead = false;
        isPaused = false;
        if (gameOverText != null) gameOverText.SetActive(false);
        UpdateClockUI();
    }

    public void TriggerDeath()
    {
        isDead = true;

        // Simply activates your UI object without overwriting what you typed in the Inspector
        if (gameOverText != null)
        {
            gameOverText.SetActive(true);
        }
    }

    public void TriggerQuickRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}