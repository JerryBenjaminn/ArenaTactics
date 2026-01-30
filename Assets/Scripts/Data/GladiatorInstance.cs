using System;
using UnityEngine;

namespace ArenaTactics.Data
{
    [Serializable]
    public class GladiatorInstance
    {
        [Header("Identity")]
        public string instanceID;
        public GladiatorData templateData;

        [Header("Runtime State")]
        public int currentLevel = 1;
        public int currentXP = 0;
        public int currentHP;
        public int maxHP;

        [Header("Status")]
        public GladiatorStatus status = GladiatorStatus.Healthy;
        public int injuryBattlesRemaining = 0;
        public int decayBattlesRemaining = -1;
        public int startingDecayBattles = -1;

        [Header("Ascension")]
        public bool isAscended = false;
        public string ascendedFormName = string.Empty;

        [Header("Equipment")]
        public WeaponData equippedWeapon;
        public ArmorData equippedArmor;
        public SpellData[] knownSpells = new SpellData[9];

        public GladiatorInstance(GladiatorData template, int level = 1)
        {
            instanceID = Guid.NewGuid().ToString();
            templateData = template;
            currentLevel = level;
            currentXP = 0;

            if (templateData != null)
            {
                equippedWeapon = templateData.startingWeapon;
                equippedArmor = templateData.startingArmor;
                if (templateData.startingSpells != null)
                {
                    for (int i = 0; i < Mathf.Min(knownSpells.Length, templateData.startingSpells.Count); i++)
                    {
                        knownSpells[i] = templateData.startingSpells[i];
                    }
                }
            }

            maxHP = CalculateMaxHP();
            currentHP = maxHP;

            if (templateData != null && templateData.race != null && templateData.race.raceName == "Undead")
            {
                decayBattlesRemaining = 13;
                startingDecayBattles = 13;
            }

            status = GladiatorStatus.Healthy;
        }

        public int CalculateMaxHP()
        {
            if (templateData == null)
            {
                return 0;
            }

            int hp = 0;

            if (templateData.gladiatorClass != null)
            {
                hp += templateData.gladiatorClass.baseHP;
            }
            else
            {
                hp += templateData.maxHP;
            }

            if (templateData.race != null)
            {
                hp += templateData.race.hpModifier;
            }

            if (templateData.gladiatorClass != null)
            {
                hp += (currentLevel - 1) * templateData.gladiatorClass.hpGrowth;
            }

            hp += templateData.hpModifier;

            if (equippedArmor != null)
            {
                hp += equippedArmor.hpBonus;
            }

            return hp;
        }

        public void EquipWeapon(WeaponData weapon)
        {
            equippedWeapon = weapon;
        }

        public void EquipArmor(ArmorData armor)
        {
            equippedArmor = armor;
            RecalculateStats();
        }

        public void LearnSpell(SpellData spell, int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < knownSpells.Length)
            {
                knownSpells[slotIndex] = spell;
            }
        }

        private void RecalculateStats()
        {
            maxHP = CalculateMaxHP();
            currentHP = maxHP;
        }

        public string GetStatusString()
        {
            Debug.Log($"GetStatusString for {templateData.gladiatorName}: status={status}, injured={injuryBattlesRemaining}", null);
            if (isAscended)
            {
                return ascendedFormName.ToUpperInvariant();
            }

            switch (status)
            {
                case GladiatorStatus.Healthy:
                    return "Healthy";
                case GladiatorStatus.Injured:
                    return $"Injured ({injuryBattlesRemaining} battles)";
                case GladiatorStatus.Dead:
                    return "Dead";
                default:
                    return "Unknown";
            }
        }

        public Color GetStatusColor()
        {
            if (isAscended)
            {
                return new Color(0.8f, 0.2f, 1.0f);
            }

            switch (status)
            {
                case GladiatorStatus.Healthy:
                    return Color.green;
                case GladiatorStatus.Injured:
                    return Color.yellow;
                case GladiatorStatus.Dead:
                    return Color.red;
                default:
                    return Color.white;
            }
        }

        public bool CanFight()
        {
            return status == GladiatorStatus.Healthy && currentHP > 0;
        }
    }

    public enum GladiatorStatus
    {
        Healthy,
        Injured,
        Dead
    }
}
