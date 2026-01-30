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
        public Button equipButton;

        private GladiatorInstance gladiator;
        private RosterView rosterView;
        private bool isInSquad;
        private GladiatorEquipmentPanel equipmentPanel;

        public void Setup(GladiatorInstance glad, bool inSquad, RosterView view)
        {
            Debug.Log($"=== GladiatorCard.Setup() for {glad.templateData.gladiatorName} ===");
            gladiator = glad;
            isInSquad = inSquad;
            rosterView = view;
            equipmentPanel = FindFirstObjectByType<GladiatorEquipmentPanel>();

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

            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
                equipButton.onClick.AddListener(OnEquipClicked);
                Debug.Log($"Equip button exists on card for {gladiator.templateData.gladiatorName}");
                Debug.Log($"  Equip button has {equipButton.onClick.GetPersistentEventCount()} persistent listeners");
            }
            else
            {
                Debug.LogWarning($"Equip button is NULL on card for {gladiator.templateData.gladiatorName}");
            }

            Debug.Log("=== GladiatorCard.Setup() COMPLETE ===");
        }

        private void UpdateDisplay()
        {
            if (gladiator == null || gladiator.templateData == null)
            {
                return;
            }

            Debug.Log($"=== UpdateDisplay for {gladiator.templateData.gladiatorName} ===");
            Debug.Log($"  Status: {gladiator.status}");
            Debug.Log($"  Injury: {gladiator.injuryBattlesRemaining}");
            Debug.Log($"  GetStatusString(): {gladiator.GetStatusString()}");

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
                string statusString = gladiator.GetStatusString();
                Debug.Log($"Displaying status for {gladiator.templateData.gladiatorName}: {statusString}");
                statusText.text = $"Status: {statusString}";
                statusText.color = gladiator.GetStatusColor();
                Debug.Log($"  StatusText set to: {statusText.text}");
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

        private void OnEquipClicked()
        {
            Debug.Log($"=== OnEquipClicked for {gladiator?.templateData?.gladiatorName ?? "NULL"} ===");

            if (gladiator == null)
            {
                Debug.LogError("Gladiator is NULL!");
                return;
            }

            Debug.Log("Looking for GladiatorEquipmentPanel...");

            if (equipmentPanel == null)
            {
                equipmentPanel = FindFirstObjectByType<GladiatorEquipmentPanel>(FindObjectsInactive.Include);
                Debug.Log($"FindFirstObjectByType (include inactive) result: {(equipmentPanel != null ? equipmentPanel.name : "NULL")}");
            }

            if (equipmentPanel != null)
            {
                Debug.Log("Found panel, calling ShowGladiatorEquipment()");
                equipmentPanel.ShowGladiatorEquipment(gladiator);
                Debug.Log($"Panel active state after show: {equipmentPanel.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogError("GladiatorEquipmentPanel not found in scene!");
            }
        }
    }
}
