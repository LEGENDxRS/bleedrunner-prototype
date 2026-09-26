using UnityEngine;
using TMPro;

public class FloatingClockHUD : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform followTarget;
    public Vector3 worldOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Text Hook")]
    public TextMeshProUGUI timerText;

    [Header("Visual Styling")]
    public Color normalColor = new Color(0.2f, 0.9f, 1f); // Crisp Cyan
    public Color criticalColor = new Color(1f, 0.15f, 0.15f); // Neon Red
    public float criticalThreshold = 2.0f;
    public float pulseSpeed = 10f;
    public float pulseScaleAmount = 0.18f;

    private Camera mainCam;
    private Vector3 initialScale;

    void Awake()
    {
        mainCam = Camera.main;
        initialScale = transform.localScale;
    }

    void Start()
    {
        if (followTarget == null)
        {
            PlayerController25D player = FindFirstObjectByType<PlayerController25D>();
            if (player != null) followTarget = player.transform;
        }
    }

    void LateUpdate()
    {
        if (followTarget == null || TimeManager.Instance == null) return;

        // Position directly above the player
        transform.position = followTarget.position + worldOffset;

        // Lock billboard rotation to match camera angle without flipping
        if (mainCam != null)
        {
            transform.rotation = mainCam.transform.rotation;
        }

        float currentTime = TimeManager.Instance.currentTime;

        if (timerText != null)
        {
            timerText.text = currentTime.ToString("F2") + "s";

            if (currentTime <= criticalThreshold && !TimeManager.Instance.isDead)
            {
                // Flashing red alarm state
                float wave = Mathf.PingPong(Time.time * pulseSpeed, 1f);
                timerText.color = Color.Lerp(criticalColor, Color.yellow, wave);

                // Punchy heartbeat scale pulse
                float scaleMod = 1f + (Mathf.Sin(Time.time * pulseSpeed) * pulseScaleAmount);
                transform.localScale = initialScale * scaleMod;
            }
            else
            {
                timerText.color = normalColor;
                transform.localScale = initialScale;
            }
        }
    }
}