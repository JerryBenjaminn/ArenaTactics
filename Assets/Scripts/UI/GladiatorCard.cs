using ArenaTactics.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaTactics.UI
{
    public class GladiatorCard : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI classRaceText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI decayText;
        public Image squadIndicator;
        public Button selectButton;

        private GladiatorInstance gladiator;
        private RosterView rosterView;
        private bool isInSquad;

        public void Setup(GladiatorInstance glad, bool inSquad, RosterView view)
        {
            Debug.Log($"=== GladiatorCard.Setup() for {glad.templateData.gladiatorName} ===");
            gladiator = glad;
            isInSquad = inSquad;
            rosterView = view;

            Debug.Log("Calling UpdateDisplay()...");
            UpdateDisplay();

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(OnSelectClicked);
                Debug.Log("Select button listener added");
            }
            else
            {
                Debug.LogWarning("selectButton is NULL!");
            }

            Debug.Log("=== GladiatorCard.Setup() COMPLETE ===");
        }

        private void UpdateDisplay()
        {
            if (gladiator == null || gladiator.templateData == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = gladiator.templateData.gladiatorName;
            }

            if (classRaceText != null)
            {
                string className = gladiator.templateData.gladiatorClass != null
                    ? gladiator.templateData.gladiatorClass.className
                    : "Unknown";
                string raceName = gladiator.templateData.race != null
                    ? gladiator.templateData.race.raceName
                    : "Unknown";
                classRaceText.text = $"{raceName} {className}";
            }

            if (levelText != null)
            {
                levelText.text = $"Level {gladiator.currentLevel}";
            }

            if (hpText != null)
            {
                hpText.text = $"HP: {gladiator.currentHP}/{gladiator.maxHP}";
            }

            if (statusText != null)
            {
                statusText.text = $"Status: {gladiator.GetStatusString()}";
                statusText.color = gladiator.GetStatusColor();
            }

            if (decayText != null)
            {
                if (gladiator.templateData.race != null &&
                    gladiator.templateData.race.raceName == "Undead" &&
                    gladiator.decayBattlesRemaining > 0)
                {
                    decayText.text = $"Decay: {gladiator.decayBattlesRemaining} battles";
                    decayText.color = gladiator.decayBattlesRemaining <= 3 ? Color.red : Color.yellow;
                }
                else if (gladiator.isAscended)
                {
                    decayText.text = "Eternal (No Decay)";
                    decayText.color = new Color(0.8f, 0.2f, 1.0f);
                }
                else
                {
                    decayText.text = string.Empty;
                }
            }

            if (squadIndicator != null)
            {
                squadIndicator.enabled = isInSquad;
                squadIndicator.color = Color.green;
            }

            if (selectButton != null)
            {
                selectButton.interactable = gladiator.CanFight() || isInSquad;
            }
        }

        private void OnSelectClicked()
        {
            rosterView.ToggleSquadSelection(gladiator);
        }
    }
}
