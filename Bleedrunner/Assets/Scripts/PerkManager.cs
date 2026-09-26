using System.Collections.Generic;
using UnityEngine;

public enum PerkType
{
    PiercingSlugs,
    SiphonDash,
    RapidTrigger,
    AdrenalineBooster,
    GlassTurbine
}

[System.Serializable]
public class PerkConfig
{
    public PerkType type;
    public string perkName;
    [TextArea(2, 4)] public string description;
}

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance;

    [Header("--- PERK 1: PIERCING SLUGS ---")]
    public string piercingName = "Piercing Slugs";
    [TextArea(2, 3)] public string piercingDescription = "Projectiles drill through all targets in a direct line.";

    [Header("--- PERK 2: SIPHON DASH ---")]
    public string siphonName = "Siphon Dash";
    [TextArea(2, 3)] public string siphonDescription = "Unlocks Spacebar Dash. Dashing through enemies siphons bonus time.";
    [Tooltip("Seconds refunded per enemy dashed through")]
    public float siphonTimeRefund = 0.5f;

    [Header("--- PERK 3: RAPID TRIGGER ---")]
    public string rapidName = "Rapid Trigger";
    [TextArea(2, 3)] public string rapidDescription = "Increases weapon fire rate by 40%.";
    [Tooltip("Multiplies fire delay (0.6 = 40% faster shooting)")]
    [Range(0.1f, 1.0f)] public float fireRateMultiplier = 0.6f;

    [Header("--- PERK 4: ADRENALINE SURGE ---")]
    public string surgeName = "Adrenaline Surge";
    [TextArea(2, 3)] public string surgeDescription = "Increases max timer capacity to 8.0s and restores clock.";
    [Tooltip("New max capacity for the adrenaline clock")]
    public float surgeMaxTime = 8.0f;

    [Header("--- PERK 5: GLASS TURBINE ---")]
    public string turbineName = "Glass Turbine";
    [TextArea(2, 3)] public string turbineDescription = "+35% Movement Speed, but clock drains 25% faster.";
    [Tooltip("Speed multiplier (1.35 = +35% speed)")]
    public float turbineSpeedMultiplier = 1.35f;
    [Tooltip("Clock drain rate multiplier (1.25 = 25% faster drain)")]
    public float turbineDrainMultiplier = 1.25f;

    [HideInInspector]
    public HashSet<PerkType> activePerks = new HashSet<PerkType>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<PerkConfig> GetRandomPerks(int count = 3)
    {
        List<PerkConfig> pool = new List<PerkConfig>()
        {
            new PerkConfig { type = PerkType.PiercingSlugs, perkName = piercingName, description = piercingDescription },
            new PerkConfig { type = PerkType.SiphonDash, perkName = siphonName, description = siphonDescription },
            new PerkConfig { type = PerkType.RapidTrigger, perkName = rapidName, description = rapidDescription },
            new PerkConfig { type = PerkType.AdrenalineBooster, perkName = surgeName, description = surgeDescription },
            new PerkConfig { type = PerkType.GlassTurbine, perkName = turbineName, description = turbineDescription }
        };

        // Remove perks already drafted
        pool.RemoveAll(p => activePerks.Contains(p.type));

        List<PerkConfig> selection = new List<PerkConfig>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int randIndex = Random.Range(0, pool.Count);
            selection.Add(pool[randIndex]);
            pool.RemoveAt(randIndex);
        }
        return selection;
    }

    public void ApplyPerk(PerkType perk)
    {
        activePerks.Add(perk);

        PlayerController25D player = FindFirstObjectByType<PlayerController25D>();
        PlayerShooting shooting = FindFirstObjectByType<PlayerShooting>();

        switch (perk)
        {
            case PerkType.PiercingSlugs:
                if (shooting != null) shooting.hasPiercingSlugs = true;
                break;

            case PerkType.SiphonDash:
                if (player != null)
                {
                    player.canDash = true;
                    player.hasSiphonDash = true;
                    player.siphonTimeGain = siphonTimeRefund;
                }
                break;

            case PerkType.RapidTrigger:
                if (shooting != null) shooting.fireRate *= fireRateMultiplier;
                break;

            case PerkType.AdrenalineBooster:
                if (TimeManager.Instance != null)
                {
                    TimeManager.Instance.maxTime = surgeMaxTime;
                    TimeManager.Instance.currentTime = surgeMaxTime;
                }
                break;

            case PerkType.GlassTurbine:
                if (player != null) player.moveSpeed *= turbineSpeedMultiplier;
                if (TimeManager.Instance != null) TimeManager.Instance.timerDrainRate = turbineDrainMultiplier;
                break;
        }

        Debug.Log($"<color=cyan>[PerkManager]</color> Applied: {perk}");
    }
}