using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PerkData
{
    [Header("Identity & Odds")]
    public string perkID = "new_perk";
    public string perkName = "New Perk Name";
    [TextArea(2, 3)] public string description = "Perk description here.";
    public Color cardColor = Color.white;
    [Range(1, 100)]
    [Tooltip("Higher = more common. Lower = rarer.")]
    public int dropWeight = 50;
    [Tooltip("If checked, can only be drafted once per run.")]
    public bool isUnique = true;

    [Header("Stat Modifiers (0 or 1 = unchanged)")]
    public float moveSpeedMultiplier = 1.0f;
    public float fireRateMultiplier = 1.0f;
    public float extraMaxClockTime = 0f;
    public float extraTimePerKill = 0f;
    public float clockDrainMultiplier = 1.0f;

    [Header("Special Abilities")]
    public bool unlocksPiercing = false;
    public bool unlocksDash = false;
    public bool unlocksSiphon = false;
    public float siphonTimeGain = 0f;
}

public class PerkManager : MonoBehaviour
{
    public static PerkManager Instance;

    [Header("Debug Controls")]
    public bool forceDebugPerk = false;
    public string debugPerkIDToForce = "piercing_slugs";

    [Header("Perk Database (Add / Edit / Remove)")]
    public List<PerkData> perkDatabase = new List<PerkData>()
    {
        new PerkData {
            perkID = "piercing_slugs",
            perkName = "Piercing Slugs",
            description = "Projectiles drill through all targets in a direct line.",
            cardColor = new Color(0.9f, 0.4f, 0.1f),
            dropWeight = 20,
            isUnique = true,
            unlocksPiercing = true
        },
        new PerkData {
            perkID = "siphon_dash",
            perkName = "Siphon Dash",
            description = "Unlocks Spacebar Dash. Dashing through enemies grants +0.5s time.",
            cardColor = new Color(0.2f, 0.8f, 1f),
            dropWeight = 35,
            isUnique = true,
            unlocksDash = true,
            unlocksSiphon = true,
            siphonTimeGain = 0.5f
        },
        new PerkData {
            perkID = "rapid_trigger",
            perkName = "Rapid Trigger",
            description = "Increases weapon fire rate by 40%.",
            cardColor = Color.white,
            dropWeight = 70,
            isUnique = false,
            fireRateMultiplier = 0.6f
        },
        new PerkData {
            perkID = "adrenaline_surge",
            perkName = "Adrenaline Surge",
            description = "+2.0s to Max Timer capacity and immediately restores the clock.",
            cardColor = new Color(1f, 0.85f, 0.2f),
            dropWeight = 10,
            isUnique = true,
            extraMaxClockTime = 2.0f
        },
        new PerkData {
            perkID = "glass_turbine",
            perkName = "Glass Turbine",
            description = "+35% Movement Speed, but clock drains 25% faster.",
            cardColor = new Color(0.8f, 0.2f, 0.2f),
            dropWeight = 40,
            isUnique = true,
            moveSpeedMultiplier = 1.35f,
            clockDrainMultiplier = 1.25f
        }
    };

    [HideInInspector]
    public HashSet<string> draftedPerkIDs = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<PerkData> GetRandomPerks(int count = 3)
    {
        List<PerkData> availablePool = new List<PerkData>();
        for (int i = 0; i < perkDatabase.Count; i++)
        {
            if (perkDatabase[i].isUnique && draftedPerkIDs.Contains(perkDatabase[i].perkID))
            {
                continue;
            }
            availablePool.Add(perkDatabase[i]);
        }

        List<PerkData> selected = new List<PerkData>();

        if (forceDebugPerk)
        {
            PerkData forced = availablePool.Find(p => p.perkID == debugPerkIDToForce);
            if (forced != null)
            {
                selected.Add(forced);
                availablePool.Remove(forced);
            }
        }

        while (selected.Count < count && availablePool.Count > 0)
        {
            int totalWeight = 0;
            for (int i = 0; i < availablePool.Count; i++)
            {
                totalWeight += availablePool[i].dropWeight;
            }

            int randomRoll = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < availablePool.Count; i++)
            {
                cumulativeWeight += availablePool[i].dropWeight;
                if (randomRoll < cumulativeWeight)
                {
                    selected.Add(availablePool[i]);
                    availablePool.RemoveAt(i);
                    break;
                }
            }
        }

        return selected;
    }

    public void ApplyPerk(PerkData perk)
    {
        if (perk == null) return;

        if (perk.isUnique)
        {
            draftedPerkIDs.Add(perk.perkID);
        }

        PlayerController25D player = FindFirstObjectByType<PlayerController25D>();
        PlayerShooting shooting = FindFirstObjectByType<PlayerShooting>();

        if (player != null && perk.moveSpeedMultiplier != 1.0f)
        {
            player.moveSpeed *= perk.moveSpeedMultiplier;
        }

        if (shooting != null && perk.fireRateMultiplier != 1.0f)
        {
            shooting.fireRate *= perk.fireRateMultiplier;
        }

        if (player != null)
        {
            if (perk.unlocksDash) player.canDash = true;
            if (perk.unlocksSiphon)
            {
                player.hasSiphonDash = true;
                player.siphonTimeGain += perk.siphonTimeGain;
            }
        }

        if (shooting != null && perk.unlocksPiercing)
        {
            shooting.hasPiercingSlugs = true;
        }

        if (TimeManager.Instance != null)
        {
            if (perk.extraMaxClockTime > 0f)
            {
                TimeManager.Instance.maxTime += perk.extraMaxClockTime;
                TimeManager.Instance.currentTime = TimeManager.Instance.maxTime;
            }

            if (perk.clockDrainMultiplier != 1.0f)
            {
                TimeManager.Instance.timerDrainRate *= perk.clockDrainMultiplier;
            }
        }

        if (perk.extraTimePerKill > 0f)
        {
            EnemyTarget[] enemies = FindObjectsByType<EnemyTarget>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i].timeReward += perk.extraTimePerKill;
            }
        }

        Debug.Log($"<color=green>[PerkManager]</color> Applied: {perk.perkName}");
    }

    public void ResetPerks()
    {
        draftedPerkIDs.Clear();
    }
}