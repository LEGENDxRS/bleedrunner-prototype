using UnityEngine;
using TMPro;

public class ChamberManager : MonoBehaviour
{
    public static ChamberManager Instance;

    [Header("Chamber Difficulty Scaling")]
    public int currentChamber = 1;
    [Tooltip("Starting kills required in Chamber 1")]
    public int baseKillsRequired = 6;
    [Tooltip("Additional kills added for each subsequent chamber")]
    public int killsPerChamberIncrease = 2;
    public int killsRequired;
    public int currentChamberKills = 0;
    public bool isExitUnlocked = false;

    [Header("Chamber Buffer (Grace Invulnerability)")]
    [Tooltip("Duration in seconds of invulnerability when entering a new chamber")]
    public float bufferDuration = 0.5f;
    public bool isGraceBufferActive = false;

    [Header("Objective Display Formatting")]
    public string chamberPrefix = "CHAMBER ";
    public string eliminatePrefix = "ELIMINATE: ";
    public string gateUnlockedMessage = "<color=#00FF66>GATE BREACH READY - EXTRACT NOW</color>";

    [Header("UI Hooks")]
    public TextMeshProUGUI chamberText;
    public TextMeshProUGUI objectiveText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartChamber(1);
    }

    public void StartChamber(int chamberNumber)
    {
        currentChamber = chamberNumber;
        currentChamberKills = 0;
        killsRequired = baseKillsRequired + ((currentChamber - 1) * killsPerChamberIncrease);
        isExitUnlocked = false;

        StartCoroutine(ChamberBufferRoutine());

        if (ChamberExit.Instance != null)
        {
            ChamberExit.Instance.SetGateLocked(true);
        }

        UpdateUI();
    }

    System.Collections.IEnumerator ChamberBufferRoutine()
    {
        isGraceBufferActive = true;
        yield return new WaitForSeconds(bufferDuration);
        isGraceBufferActive = false;
    }

    public void RegisterKill()
    {
        currentChamberKills++;

        if (!isExitUnlocked && currentChamberKills >= killsRequired)
        {
            UnlockExit();
        }

        UpdateUI();
    }

    void UnlockExit()
    {
        isExitUnlocked = true;
        if (ChamberExit.Instance != null)
        {
            ChamberExit.Instance.SetGateLocked(false);
        }
    }

    public void AdvanceToNextChamber()
    {
        StartChamber(currentChamber + 1);
    }

    void UpdateUI()
    {
        if (chamberText != null)
        {
            chamberText.text = chamberPrefix + currentChamber;
        }

        if (objectiveText != null)
        {
            if (isExitUnlocked)
            {
                objectiveText.text = gateUnlockedMessage;
            }
            else
            {
                objectiveText.text = $"{eliminatePrefix}{currentChamberKills} / {killsRequired}";
            }
        }
    }

    public void ResetProgression()
    {
        StartChamber(1);
    }
}