using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ArenaTactics.UI;

namespace ArenaTactics.Managers
{
    public class ShopManager : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI goldText;
        public TextMeshProUGUI battleCountText;
        public TextMeshProUGUI squadCountText;
        public Button startBattleButton;
        public Button mainMenuButton;

        [Header("Tab System")]
        public Button recruitTabButton;
        public Button manageTabButton;
        public GameObject recruitmentPanel;
        public GameObject rosterPanel;

        [Header("Tab Colors")]
        public Color activeTabColor = new Color(0.3f, 0.6f, 1f);
        public Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f);

        private PersistentDataManager dataManager;
        private enum ShopTab { Recruit, Manage }
        private ShopTab currentTab = ShopTab.Recruit;

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

            SetupButtons();

            RefreshGoldDisplay();
            RefreshBattleCount();
            RefreshSquadCount();

            if (dataManager.battleCount == 0)
            {
                Debug.Log("Welcome to the shop! You have 2000 gold to start.");
            }

            RosterView rosterView = FindFirstObjectByType<RosterView>();
            if (rosterView != null)
            {
                Debug.Log("Forcing roster refresh...");
                rosterView.RefreshRoster();
                Debug.Log("ShopManager: Roster refreshed on shop start.");
            }

            SwitchTab(ShopTab.Recruit);
        }

        private void SetupButtons()
        {
            if (recruitTabButton != null)
            {
                recruitTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Recruit));
            }

            if (manageTabButton != null)
            {
                manageTabButton.onClick.AddListener(() => SwitchTab(ShopTab.Manage));
            }

            if (startBattleButton != null)
            {
                startBattleButton.onClick.AddListener(OnStartBattleClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.interactable = false;
            }
        }

        private void SwitchTab(ShopTab tab)
        {
            currentTab = tab;

            if (recruitmentPanel != null)
            {
                recruitmentPanel.SetActive(tab == ShopTab.Recruit);
            }

            if (rosterPanel != null)
            {
                rosterPanel.SetActive(tab == ShopTab.Manage);
            }

            UpdateTabButtonColors();

            Debug.Log($"Switched to {tab} tab");
        }

        private void UpdateTabButtonColors()
        {
            if (recruitTabButton != null)
            {
                ColorBlock colors = recruitTabButton.colors;
                colors.normalColor = currentTab == ShopTab.Recruit ? activeTabColor : inactiveTabColor;
                recruitTabButton.colors = colors;
            }

            if (manageTabButton != null)
            {
                ColorBlock colors = manageTabButton.colors;
                colors.normalColor = currentTab == ShopTab.Manage ? activeTabColor : inactiveTabColor;
                manageTabButton.colors = colors;
            }
        }

        private void RefreshBattleCount()
        {
            if (battleCountText != null)
            {
                battleCountText.text = $"Battle: {dataManager.battleCount}";
            }
        }

        public void RefreshSquadCount()
        {
            if (squadCountText == null || dataManager == null)
            {
                return;
            }

            squadCountText.text = $"Squad: {dataManager.activeSquad.Count}/5";
            if (dataManager.activeSquad.Count == 0)
            {
                squadCountText.color = Color.red;
            }
            else if (dataManager.activeSquad.Count < 5)
            {
                squadCountText.color = Color.yellow;
            }
            else
            {
                squadCountText.color = Color.green;
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
            if (goldText != null && dataManager != null)
            {
                goldText.text = $"Gold: {dataManager.playerGold}g";
            }
        }
    }
}
