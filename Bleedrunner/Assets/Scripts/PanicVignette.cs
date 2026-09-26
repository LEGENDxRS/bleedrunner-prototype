using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PanicVignette : MonoBehaviour
{
    public static PanicVignette Instance;

    [Header("Trigger Thresholds")]
    [Tooltip("Time in seconds below which the vignette starts pulsing")]
    public float panicTimeThreshold = 2.0f;
    [Tooltip("Maximum alpha intensity of the red vignette edges")]
    [Range(0.1f, 1f)] public float maxVignetteAlpha = 0.65f;
    [Tooltip("Pulse frequency speed")]
    public float pulseSpeed = 9f;

    [Header("Color")]
    public Color vignetteColor = new Color(0.85f, 0.05f, 0.05f);

    private Image vignetteImage;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        vignetteImage = GetComponent<Image>();
        vignetteImage.raycastTarget = false;

        // Automatically generate a procedural soft-edge vignette texture if none is assigned
        if (vignetteImage.sprite == null)
        {
            vignetteImage.sprite = GenerateVignetteSprite();
        }

        vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
    }

    void Update()
    {
        if (TimeManager.Instance == null) return;

        float currentTime = TimeManager.Instance.currentTime;
        bool isPanic = currentTime <= panicTimeThreshold && !TimeManager.Instance.isDead && !TimeManager.Instance.isPaused;

        if (isPanic)
        {
            // Intensity escalates as the clock approaches zero
            float dangerRatio = 1f - (currentTime / panicTimeThreshold);
            float pingPongPulse = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            float targetAlpha = Mathf.Lerp(0.2f, maxVignetteAlpha, dangerRatio * pingPongPulse);

            vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, targetAlpha);
        }
        else if (TimeManager.Instance.isDead)
        {
            // Flatlined: Flash heavy red edge
            vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0.8f);
        }
        else
        {
            // Smoothly fade out when out of danger
            Color c = vignetteImage.color;
            c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 3f);
            vignetteImage.color = c;
        }
    }

    Sprite GenerateVignetteSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size * 0.7071f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normDist = Mathf.Clamp01(dist / maxDist);
                // Quadratic falloff: fully transparent in the center, opaque at the outer corners
                float alpha = Mathf.Pow(normDist, 2.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}