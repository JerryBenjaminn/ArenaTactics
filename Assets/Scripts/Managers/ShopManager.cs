using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaTactics.Managers
{
    public class ShopManager : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI battleCountText;
        public Button startBattleButton;
        public Button mainMenuButton;

        private PersistentDataManager dataManager;

        private void Start()
        {
            Debug.Log("=== ShopManager.Start() ===");
            dataManager = PersistentDataManager.Instance;

            if (dataManager == null)
            {
                Debug.LogError("PersistentDataManager not found! Creating one...");
                GameObject managerObj = new GameObject("PersistentDataManager");
                dataManager = managerObj.AddComponent<PersistentDataManager>();
            }

            Debug.Log("Roster state on shop load:");
            foreach (var glad in dataManager.playerRoster)
            {
                if (glad == null || glad.templateData == null)
                {
                    continue;
                }
                Debug.Log($"  {glad.templateData.gladiatorName}: Status={glad.status}, Injury={glad.injuryBattlesRemaining}, HP={glad.currentHP}/{glad.maxHP}");
            }

            UpdateUI();
            SetupButtons();

            if (dataManager.battleCount == 0)
            {
                Debug.Log("Welcome to the shop! You have 2000 gold to start.");
            }

            ArenaTactics.UI.RosterView rosterView = FindFirstObjectByType<ArenaTactics.UI.RosterView>();
            if (rosterView != null)
            {
                Debug.Log("Forcing roster refresh...");
                rosterView.RefreshRoster();
                Debug.Log("ShopManager: Roster refreshed on shop start.");
            }
        }

        private void SetupButtons()
        {
            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = false;
            }
        }

        private void UpdateUI()
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: {dataManager.playerGold}g";
            }

            if (battleCountText != null)
            {
                battleCountText.text = $"Battle: {dataManager.battleCount}";
            }
        }

        private void OnStartBattleClicked()
        {
            if (dataManager.activeSquad.Count == 0 && dataManager.playerRoster.Count == 0)
            {
                Debug.LogWarning("Cannot start battle: No gladiators available!");
                return;
            }

            dataManager.PrepareBattle();
        }

        public void RefreshGoldDisplay()
        {
            UpdateUI();
        }
    }
}
