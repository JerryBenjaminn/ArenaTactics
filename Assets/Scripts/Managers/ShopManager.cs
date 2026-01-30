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
            dataManager = PersistentDataManager.Instance;

            if (dataManager == null)
            {
                Debug.LogError("PersistentDataManager not found! Creating one...");
                GameObject managerObj = new GameObject("PersistentDataManager");
                dataManager = managerObj.AddComponent<PersistentDataManager>();
            }

            UpdateUI();
            SetupButtons();

            if (dataManager.battleCount == 0)
            {
                Debug.Log("Welcome to the shop! You have 2000 gold to start.");
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
