using System.Collections.Generic;
using ArenaTactics.Data;
using ArenaTactics.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArenaTactics.UI
{
    public class RosterView : MonoBehaviour
    {
        [Header("UI References")]
        public Transform rosterContainer;
        public GameObject gladiatorCardPrefab;

        [Header("Squad Selection")]
        public TextMeshProUGUI squadCountText;
        public Button confirmSquadButton;

        private PersistentDataManager dataManager;
        private List<GladiatorInstance> selectedSquad = new List<GladiatorInstance>();

        private void Start()
        {
            Debug.Log("=== RosterView.Start() CALLED ===");
            dataManager = PersistentDataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("RosterView: PersistentDataManager not found.");
                return;
            }

            Debug.Log($"DataManager: {(dataManager != null ? "FOUND" : "NULL")}");
            Debug.Log($"Roster count: {dataManager.playerRoster.Count}");
            Debug.Log($"Active squad count: {dataManager.activeSquad.Count}");
            Debug.Log($"rosterContainer: {(rosterContainer != null ? "ASSIGNED" : "NULL")}");
            Debug.Log($"gladiatorCardPrefab: {(gladiatorCardPrefab != null ? "ASSIGNED" : "NULL")}");

            selectedSquad = new List<GladiatorInstance>(dataManager.activeSquad);
            RefreshRoster();

            if (confirmSquadButton != null)
            {
                confirmSquadButton.onClick.AddListener(OnConfirmSquad);
            }

            Debug.Log("=== RosterView.Start() COMPLETE ===");
        }

        public void RefreshRoster()
        {
            Debug.Log("=== RefreshRoster() CALLED ===");
            if (rosterContainer == null || gladiatorCardPrefab == null || dataManager == null)
            {
                Debug.LogWarning("RefreshRoster: Missing references.");
                return;
            }

            Debug.Log($"Clearing existing cards from: {rosterContainer.name}");
            foreach (Transform child in rosterContainer)
            {
                Destroy(child.gameObject);
            }

            Debug.Log($"Creating {dataManager.playerRoster.Count} new cards");
            int cardIndex = 0;
            foreach (GladiatorInstance gladiator in dataManager.playerRoster)
            {
                Debug.Log($"Creating card {cardIndex} for: {gladiator.templateData.gladiatorName}");
                CreateGladiatorCard(gladiator);
                cardIndex++;
            }

            Debug.Log($"Cards created. Container now has {rosterContainer.childCount} children");
            UpdateSquadCount();
            Debug.Log("=== RefreshRoster() COMPLETE ===");
        }

        private void CreateGladiatorCard(GladiatorInstance gladiator)
        {
            Debug.Log($"CreateGladiatorCard: Instantiating prefab for {gladiator.templateData.gladiatorName}");
            GameObject card = Instantiate(gladiatorCardPrefab, rosterContainer);
            Debug.Log($"Card instantiated: {card.name}, active: {card.activeSelf}");
            GladiatorCard cardScript = card.GetComponent<GladiatorCard>();
            if (cardScript == null)
            {
                Debug.LogError($"GladiatorCard component NOT FOUND on {card.name}!");
                return;
            }

            Debug.Log("GladiatorCard component found, calling Setup()");
            bool isInSquad = selectedSquad.Contains(gladiator);
            cardScript.Setup(gladiator, isInSquad, this);
            Debug.Log($"Card setup complete for {gladiator.templateData.gladiatorName}");
        }

        public void ToggleSquadSelection(GladiatorInstance gladiator)
        {
            if (gladiator == null)
            {
                return;
            }

            if (gladiator.status == GladiatorStatus.Dead)
            {
                Debug.LogWarning($"{gladiator.templateData.gladiatorName} is dead and cannot be selected!");
                return;
            }
            if (gladiator.status == GladiatorStatus.Injured)
            {
                Debug.LogWarning($"{gladiator.templateData.gladiatorName} is injured and cannot fight!");
                return;
            }

            if (selectedSquad.Contains(gladiator))
            {
                selectedSquad.Remove(gladiator);
                Debug.Log($"Removed {gladiator.templateData.gladiatorName} from squad");
            }
            else
            {
                if (selectedSquad.Count >= 5)
                {
                    Debug.LogWarning("Squad is full! (5 max)");
                    return;
                }

                if (!gladiator.CanFight())
                {
                    Debug.LogWarning($"{gladiator.templateData.gladiatorName} cannot fight!");
                    return;
                }

                selectedSquad.Add(gladiator);
                Debug.Log($"Added {gladiator.templateData.gladiatorName} to squad");
            }

            RefreshRoster();
        }

        private void UpdateSquadCount()
        {
            if (squadCountText == null)
            {
                return;
            }

            squadCountText.text = $"Squad: {selectedSquad.Count}/5";
            if (selectedSquad.Count < 1)
            {
                squadCountText.color = Color.red;
            }
            else if (selectedSquad.Count < 5)
            {
                squadCountText.color = Color.yellow;
            }
            else
            {
                squadCountText.color = Color.green;
            }
        }

        private void OnConfirmSquad()
        {
            if (selectedSquad.Count == 0)
            {
                Debug.LogWarning("Cannot confirm: No gladiators selected!");
                return;
            }

            dataManager.SetActiveSquad(selectedSquad);
            Debug.Log($"Squad confirmed: {selectedSquad.Count} gladiators ready for battle!");
        }
    }
}
