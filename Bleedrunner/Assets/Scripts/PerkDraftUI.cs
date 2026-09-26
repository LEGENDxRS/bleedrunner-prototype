using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PerkDraftUI : MonoBehaviour
{
    public static PerkDraftUI Instance;

    [Header("Panels")]
    public GameObject draftUIPanel;

    [Header("Card 1")]
    public Button cardButton1;
    public TextMeshProUGUI title1;
    public TextMeshProUGUI desc1;

    [Header("Card 2")]
    public Button cardButton2;
    public TextMeshProUGUI title2;
    public TextMeshProUGUI desc2;

    [Header("Card 3")]
    public Button cardButton3;
    public TextMeshProUGUI title3;
    public TextMeshProUGUI desc3;

    private List<PerkData> currentChoices;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (draftUIPanel != null) draftUIPanel.SetActive(false);
    }

    public void OpenDraft()
    {
        if (PerkManager.Instance == null) return;

        currentChoices = PerkManager.Instance.GetRandomPerks(3);
        if (currentChoices == null || currentChoices.Count == 0)
        {
            CloseDraft();
            return;
        }

        draftUIPanel.SetActive(true);

        SetupCard(cardButton1, title1, desc1, 0);
        SetupCard(cardButton2, title2, desc2, 1);
        SetupCard(cardButton3, title3, desc3, 2);
    }

    void SetupCard(Button btn, TextMeshProUGUI titleText, TextMeshProUGUI descText, int index)
    {
        if (btn == null) return;

        if (index < currentChoices.Count)
        {
            btn.gameObject.SetActive(true);
            PerkData perk = currentChoices[index];

            if (titleText != null)
            {
                titleText.text = perk.perkName;
                titleText.color = perk.cardColor;
            }

            if (descText != null) descText.text = perk.description;

            // Tint button background slightly to match rarity
            Image cardBg = btn.GetComponent<Image>();
            if (cardBg != null)
            {
                cardBg.color = new Color(perk.cardColor.r * 0.25f, perk.cardColor.g * 0.25f, perk.cardColor.b * 0.25f, 0.95f);
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectPerk(perk));
        }
        else
        {
            btn.gameObject.SetActive(false);
        }
    }

    void SelectPerk(PerkData perk)
    {
        if (PerkManager.Instance != null)
        {
            PerkManager.Instance.ApplyPerk(perk);
        }
        CloseDraft();
    }

    void CloseDraft()
    {
        if (draftUIPanel != null) draftUIPanel.SetActive(false);

        if (ChamberManager.Instance != null)
        {
            ChamberManager.Instance.AdvanceToNextChamber();
        }

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.isPaused = false;
        }

        PlayerController25D player = FindFirstObjectByType<PlayerController25D>();
        if (player != null)
        {
            player.transform.position += -player.transform.forward * 3.5f;
        }
    }
}