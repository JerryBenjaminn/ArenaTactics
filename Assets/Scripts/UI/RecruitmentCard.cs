using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArenaTactics.Data;
using ArenaTactics.Managers;

namespace ArenaTactics.UI
{
    public class RecruitmentCard : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI classRaceText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI qualityText;
        public Button buyButton;
        public GameObject soldOverlay;

        private RecruitmentOffer offer;
        private RecruitmentView recruitmentView;

        public void Setup(RecruitmentOffer recruitOffer, RecruitmentView view)
        {
            offer = recruitOffer;
            recruitmentView = view;

            RefreshDisplay();

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        public void RefreshDisplay()
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (offer == null || offer.gladiator == null || offer.gladiator.templateData == null)
            {
                return;
            }

            GladiatorInstance glad = offer.gladiator;

            if (nameText != null)
            {
                nameText.text = glad.templateData.gladiatorName;
            }

            if (classRaceText != null)
            {
                string className = glad.templateData.gladiatorClass != null
                    ? glad.templateData.gladiatorClass.className
                    : "Unknown";
                string raceName = glad.templateData.race != null
                    ? glad.templateData.race.raceName
                    : "Unknown";
                classRaceText.text = $"{raceName} {className}";
            }

            if (levelText != null)
            {
                levelText.text = $"Level {glad.currentLevel}";
            }

            if (statsText != null)
            {
                statsText.text = $"HP: {glad.maxHP}\n" +
                                 $"STR: {GetTotalStat(glad, "STR")} | " +
                                 $"DEX: {GetTotalStat(glad, "DEX")} | " +
                                 $"INT: {GetTotalStat(glad, "INT")}\n" +
                                 $"DEF: {GetTotalStat(glad, "DEF")} | " +
                                 $"SPD: {GetTotalStat(glad, "SPD")}";
            }

            if (priceText != null)
            {
                priceText.text = $"{offer.price}g";

                PersistentDataManager dataManager = PersistentDataManager.Instance;
                if (dataManager != null)
                {
                    priceText.color = dataManager.playerGold >= offer.price ? Color.green : Color.red;
                }
            }

            if (qualityText != null)
            {
                qualityText.text = offer.quality.ToString();

                switch (offer.quality)
                {
                    case GladiatorQuality.Poor:
                        qualityText.color = Color.gray;
                        break;
                    case GladiatorQuality.Average:
                        qualityText.color = Color.white;
                        break;
                    case GladiatorQuality.Good:
                        qualityText.color = Color.cyan;
                        break;
                    case GladiatorQuality.Excellent:
                        qualityText.color = new Color(1f, 0.8f, 0f);
                        break;
                }
            }

            if (buyButton != null)
            {
                buyButton.interactable = !offer.purchased;
            }

            if (soldOverlay != null)
            {
                soldOverlay.SetActive(offer.purchased);
            }
        }

        private int GetTotalStat(GladiatorInstance instance, string statName)
        {
            int total = 0;

            switch (statName)
            {
                case "STR":
                    if (instance.templateData.gladiatorClass != null)
                    {
                        total += instance.templateData.gladiatorClass.baseStrength;
                        total += (instance.currentLevel - 1) * instance.templateData.gladiatorClass.strengthGrowth;
                    }
                    if (instance.templateData.race != null)
                    {
                        total += instance.templateData.race.strengthModifier;
                    }
                    total += instance.templateData.strengthModifier;
                    break;
                case "DEX":
                    if (instance.templateData.gladiatorClass != null)
                    {
                        total += instance.templateData.gladiatorClass.baseDexterity;
                        total += (instance.currentLevel - 1) * instance.templateData.gladiatorClass.dexterityGrowth;
                    }
                    if (instance.templateData.race != null)
                    {
                        total += instance.templateData.race.dexterityModifier;
                    }
                    total += instance.templateData.dexterityModifier;
                    break;
                case "INT":
                    if (instance.templateData.gladiatorClass != null)
                    {
                        total += instance.templateData.gladiatorClass.baseIntelligence;
                        total += (instance.currentLevel - 1) * instance.templateData.gladiatorClass.intelligenceGrowth;
                    }
                    if (instance.templateData.race != null)
                    {
                        total += instance.templateData.race.intelligenceModifier;
                    }
                    total += instance.templateData.intelligenceModifier;
                    break;
                case "DEF":
                    if (instance.templateData.gladiatorClass != null)
                    {
                        total += instance.templateData.gladiatorClass.baseDefense;
                    }
                    if (instance.templateData.race != null)
                    {
                        total += instance.templateData.race.defenseModifier;
                    }
                    total += instance.templateData.defenseModifier;
                    break;
                case "SPD":
                    if (instance.templateData.gladiatorClass != null)
                    {
                        total += instance.templateData.gladiatorClass.baseSpeed;
                    }
                    if (instance.templateData.race != null)
                    {
                        total += instance.templateData.race.speedModifier;
                    }
                    total += instance.templateData.speedModifier;
                    break;
            }

            return total;
        }

        private void OnBuyClicked()
        {
            if (recruitmentView != null)
            {
                recruitmentView.PurchaseGladiator(offer);
            }
        }
    }
}
