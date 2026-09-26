using UnityEngine;
using UnityEngine.SceneManagement;
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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentTime = maxTime;
    }

    void Update()
    {
        if (isDead)
        {
            // Reload active scene using name so buildIndex doesn't fail
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }

        currentTime -= Time.deltaTime;

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

            if (currentTime <= 2.0f)
            {
                timerDisplay.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 8f, 1f));
            }
            else
            {
                timerDisplay.color = new Color(1f, 0.25f, 0.2f);
            }
        }
    }

    public void AddTime(float amount)
    {
        if (isDead) return;
        currentTime = Mathf.Min(currentTime + amount, maxTime);
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
        Debug.Log("<color=red>[BLEEDRUNNER]</color> Player flatlined!");

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        PlayerController25D player = FindFirstObjectByType<PlayerController25D>();
        if (player != null)
        {
            player.enabled = false;
        }
    }
}