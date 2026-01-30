using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ArenaTactics.Data;
using ArenaTactics.Managers;

namespace ArenaTactics.UI
{
    public class RecruitmentView : MonoBehaviour
    {
        [Header("UI References")]
        public Transform offersContainer;
        public GameObject recruitmentCardPrefab;
        public TextMeshProUGUI titleText;

        [Header("References")]
        private PersistentDataManager dataManager;
        private GladiatorGenerator generator;

        [Header("Current Offers")]
        private readonly List<RecruitmentOffer> currentOffers = new List<RecruitmentOffer>();
        private readonly Dictionary<RecruitmentOffer, RecruitmentCard> offerCards = new Dictionary<RecruitmentOffer, RecruitmentCard>();

        private void Start()
        {
            dataManager = PersistentDataManager.Instance;
            generator = FindFirstObjectByType<GladiatorGenerator>();

            if (generator == null)
            {
                Debug.LogError("RecruitmentView: GladiatorGenerator not found in scene!");
                return;
            }

            if (offersContainer == null || recruitmentCardPrefab == null)
            {
                Debug.LogError("RecruitmentView: Missing UI references for offers container or card prefab.");
                return;
            }

            RefreshOffers();
        }

        public void RefreshOffers()
        {
            Debug.Log("=== Refreshing Recruitment Offers ===");

            if (generator == null || dataManager == null)
            {
                Debug.LogError("RecruitmentView: Missing references.");
                return;
            }

            ClearOffers();

            List<GladiatorInstance> generatedGladiators = generator.GenerateShopPool(dataManager.battleCount);

            foreach (GladiatorInstance gladiator in generatedGladiators)
            {
                if (gladiator == null || gladiator.templateData == null)
                {
                    continue;
                }

                GladiatorQuality quality = DetermineQuality(gladiator);
                int price = CalculateOfferPrice(gladiator, quality);

                RecruitmentOffer offer = new RecruitmentOffer(gladiator, price, quality);
                currentOffers.Add(offer);
            }

            DisplayOffers();

            Debug.Log($"Generated {currentOffers.Count} recruitment offers");
        }

        private void ClearOffers()
        {
            currentOffers.Clear();
            offerCards.Clear();

            foreach (Transform child in offersContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void DisplayOffers()
        {
            foreach (RecruitmentOffer offer in currentOffers)
            {
                CreateOfferCard(offer);
            }
        }

        private void CreateOfferCard(RecruitmentOffer offer)
        {
            GameObject card = Instantiate(recruitmentCardPrefab, offersContainer);
            RecruitmentCard cardScript = card.GetComponent<RecruitmentCard>();

            if (cardScript != null)
            {
                cardScript.Setup(offer, this);
                offerCards[offer] = cardScript;
            }
            else
            {
                Debug.LogWarning("RecruitmentView: RecruitmentCard component missing on prefab.");
            }
        }

        private GladiatorQuality DetermineQuality(GladiatorInstance gladiator)
        {
            int totalVariance = (gladiator.templateData.hpModifier / 5) +
                                gladiator.templateData.strengthModifier +
                                gladiator.templateData.dexterityModifier +
                                gladiator.templateData.intelligenceModifier +
                                gladiator.templateData.defenseModifier +
                                gladiator.templateData.speedModifier;

            if (totalVariance <= -3)
            {
                return GladiatorQuality.Poor;
            }
            if (totalVariance <= 3)
            {
                return GladiatorQuality.Average;
            }
            if (totalVariance <= 8)
            {
                return GladiatorQuality.Good;
            }
            return GladiatorQuality.Excellent;
        }

        private int CalculateOfferPrice(GladiatorInstance instance, GladiatorQuality quality)
        {
            int basePrice = 500;

            if (instance.currentLevel >= 8)
            {
                basePrice = 4000;
            }
            else if (instance.currentLevel >= 4)
            {
                basePrice = 2000;
            }

            float qualityMult = 1.0f;
            switch (quality)
            {
                case GladiatorQuality.Poor:
                    qualityMult = 0.8f;
                    break;
                case GladiatorQuality.Average:
                    qualityMult = 1.0f;
                    break;
                case GladiatorQuality.Good:
                    qualityMult = 1.3f;
                    break;
                case GladiatorQuality.Excellent:
                    qualityMult = 1.7f;
                    break;
            }

            int price = Mathf.RoundToInt(basePrice * qualityMult);

            int statBonus = (instance.maxHP / 10) +
                            (instance.templateData.strengthModifier * 20) +
                            (instance.templateData.dexterityModifier * 20) +
                            (instance.templateData.intelligenceModifier * 20);

            price += statBonus;

            return Mathf.Clamp(price, 300, 10000);
        }

        public void PurchaseGladiator(RecruitmentOffer offer)
        {
            if (offer == null || offer.gladiator == null)
            {
                return;
            }

            if (offer.purchased)
            {
                Debug.LogWarning("RecruitmentView: Gladiator already purchased!");
                return;
            }

            if (dataManager.playerGold < offer.price)
            {
                Debug.LogWarning($"Not enough gold! Need {offer.price}, have {dataManager.playerGold}");
                return;
            }

            if (dataManager.SpendGold(offer.price))
            {
                dataManager.AddGladiatorToRoster(offer.gladiator);
                offer.purchased = true;

                Debug.Log($"Purchased {offer.gladiator.templateData.gladiatorName} for {offer.price}g!");

                RosterView rosterView = FindFirstObjectByType<RosterView>();
                if (rosterView != null)
                {
                    rosterView.RefreshRoster();
                }

                ShopManager shopManager = FindFirstObjectByType<ShopManager>();
                if (shopManager != null)
                {
                    shopManager.RefreshGoldDisplay();
                }

                RefreshOfferDisplay(offer);
                RefreshAllOfferDisplays();
            }
        }

        private void RefreshOfferDisplay(RecruitmentOffer offer)
        {
            if (offerCards.TryGetValue(offer, out RecruitmentCard card) && card != null)
            {
                card.RefreshDisplay();
            }
        }

        private void RefreshAllOfferDisplays()
        {
            foreach (RecruitmentCard card in offerCards.Values)
            {
                if (card != null)
                {
                    card.RefreshDisplay();
                }
            }
        }
    }
}
