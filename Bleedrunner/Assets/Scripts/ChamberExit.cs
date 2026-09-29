using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChamberExit : MonoBehaviour
{
    public static ChamberExit Instance;

    [Header("Visual Feedback")]
    public ParticleSystem[] gateRenderer;
    public Color lockedColor = new Color(0.4f, 0.1f, 0.1f, 0.4f);
    public Color readyColor = new Color(1f, 1f, 1f, 1f);

    private bool activated = false;
    private Collider col;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        col = GetComponent<Collider>();
        col.isTrigger = true;

    }

    public void SetGateLocked(bool locked)
    {
        activated = false;

        foreach (var gateRenderer in gateRenderer)  
        {
            if (gateRenderer == null) continue;

            var gateMain = gateRenderer.main;
            gateMain.startColor = locked ? lockedColor : readyColor;
        }
    }

    // Fires the frame you step in
    void OnTriggerEnter(Collider other)
    {
        TryExtract(other);
    }

    // Continuously checks while you are standing on the gate
    void OnTriggerStay(Collider other)
    {
        TryExtract(other);
    }

    void TryExtract(Collider other)
    {
        if (activated) return;

        // Block entry if chamber objective isn't met yet
        if (ChamberManager.Instance != null && !ChamberManager.Instance.isExitUnlocked)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            activated = true;

            // Pause timer decay and movement
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.isPaused = true;
            }

            // Open 3-card perk drafting overlay
            if (PerkDraftUI.Instance != null)
            {
                PerkDraftUI.Instance.OpenDraft();
            }
        }
    }

    public void ResetExit()
    {
        activated = false;
    }
}