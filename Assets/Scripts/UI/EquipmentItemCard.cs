using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArenaTactics.Data;
using ArenaTactics.Managers;

namespace ArenaTactics.UI
{
    public class EquipmentItemCard : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statsText;
        public TextMeshProUGUI priceText;
        public TextMeshProUGUI typeText;
        public Button buyButton;

        private WeaponData weapon;
        private ArmorData armor;
        private SpellData spell;
        private EquipmentShopView shopView;
        private int price;

        public void SetupWeapon(WeaponData weaponData, EquipmentShopView view)
        {
            weapon = weaponData;
            armor = null;
            spell = null;
            shopView = view;

            if (weapon == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = weapon.weaponName;
            }

            if (typeText != null)
            {
                typeText.text = weapon.weaponType.ToString();
            }

            if (statsText != null)
            {
                string stats = $"DMG: {weapon.baseDamage}";
                if (weapon.accuracyBonus != 0f)
                {
                    stats += $"\nACC: {weapon.accuracyBonus:+#;-#;0}%";
                }
                if (weapon.critBonus != 0f)
                {
                    stats += $"\nCRIT: {weapon.critBonus:+#;-#;0}%";
                }
                if (weapon.range > 1)
                {
                    stats += $"\nRange: {weapon.range}";
                }
                statsText.text = stats;
            }

            price = CalculateWeaponPrice(weapon);
            UpdatePriceLabel();

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyWeapon);
            }
        }

        public void SetupArmor(ArmorData armorData, EquipmentShopView view)
        {
            armor = armorData;
            weapon = null;
            spell = null;
            shopView = view;

            if (armor == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = armor.armorName;
            }

            if (typeText != null)
            {
                typeText.text = armor.armorType.ToString();
            }

            if (statsText != null)
            {
                string stats = $"DEF: +{armor.defenseBonus}";
                if (armor.hpBonus != 0)
                {
                    stats += $"\nHP: +{armor.hpBonus}";
                }
                if (armor.dodgeBonus != 0f)
                {
                    stats += $"\nDODGE: {armor.dodgeBonus:+#;-#;0}%";
                }
                if (armor.movementPenalty != 0)
                {
                    stats += $"\nSPD: {armor.movementPenalty}";
                }
                statsText.text = stats;
            }

            price = CalculateArmorPrice(armor);
            UpdatePriceLabel();

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyArmor);
            }
        }

        public void SetupSpell(SpellData spellData, EquipmentShopView view)
        {
            spell = spellData;
            weapon = null;
            armor = null;
            shopView = view;

            if (spell == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = spell.spellName;
            }

            if (typeText != null)
            {
                typeText.text = spell.spellType.ToString();
            }

            if (statsText != null)
            {
                string stats = $"Cost: {spell.apCost} AP";
                if (spell.basePower > 0)
                {
                    stats += $"\nPWR: {spell.basePower}";
                }
                if (spell.effectType == EffectType.Heal && spell.effectValue != 0)
                {
                    stats += $"\nHeal: {spell.effectValue}";
                }
                if (spell.effectType != EffectType.None && spell.effectValue != 0)
                {
                    stats += $"\nEffect: {spell.effectType} {spell.effectValue:+#;-#;0}";
                }
                if (spell.range > 1)
                {
                    stats += $"\nRange: {spell.range}";
                }
                statsText.text = stats;
            }

            price = CalculateSpellPrice(spell);
            UpdatePriceLabel();

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuySpell);
            }
        }

        private void UpdatePriceLabel()
        {
            if (priceText == null)
            {
                return;
            }

            priceText.text = $"{price}g";
            UpdatePriceColor();
        }

        private void UpdatePriceColor()
        {
            PersistentDataManager dataManager = PersistentDataManager.Instance;
            if (dataManager != null && priceText != null)
            {
                priceText.color = dataManager.playerGold >= price ? Color.green : Color.red;
            }
        }

        private int CalculateWeaponPrice(WeaponData weaponData)
        {
            int basePrice = 300;
            int dmgValue = weaponData.baseDamage * 50;
            int bonusValue = Mathf.RoundToInt((weaponData.accuracyBonus + weaponData.critBonus) * 20f);
            int rangeValue = weaponData.range > 1 ? (weaponData.range - 1) * 100 : 0;

            return basePrice + dmgValue + bonusValue + rangeValue;
        }

        private int CalculateArmorPrice(ArmorData armorData)
        {
            int basePrice = 250;
            int defValue = armorData.defenseBonus * 80;
            int hpValue = armorData.hpBonus * 5;
            int dodgeValue = Mathf.RoundToInt(armorData.dodgeBonus * 30f);
            int movePenaltyValue = armorData.movementPenalty != 0 ? -armorData.movementPenalty * 20 : 0;

            return Mathf.Max(100, basePrice + defValue + hpValue + dodgeValue + movePenaltyValue);
        }

        private int CalculateSpellPrice(SpellData spellData)
        {
            int basePrice = 400;
            int powerValue = spellData.basePower * 40;
            int effectValue = Mathf.Abs(spellData.effectValue) * 60;
            int secondaryValue = Mathf.Abs(spellData.secondaryEffectValue) * 30;

            return basePrice + powerValue + effectValue + secondaryValue;
        }

        private void OnBuyWeapon()
        {
            if (shopView != null && weapon != null)
            {
                shopView.PurchaseWeapon(weapon, price);
            }
        }

        private void OnBuyArmor()
        {
            if (shopView != null && armor != null)
            {
                shopView.PurchaseArmor(armor, price);
            }
        }

        private void OnBuySpell()
        {
            if (shopView != null && spell != null)
            {
                shopView.PurchaseSpell(spell, price);
            }
        }
    }
}
