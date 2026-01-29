using UnityEngine;

namespace ArenaTactics.Data
{
    [CreateAssetMenu(fileName = "Race", menuName = "ArenaTactics/Race")]
    public class RaceData : ScriptableObject
    {
        [Header("Identity")]
        public string raceName;
        [TextArea(3, 5)]
        public string description;

        [Header("Stat Modifiers")]
        public int hpModifier;
        public int strengthModifier;
        public int dexterityModifier;
        public int intelligenceModifier;
        public int defenseModifier;
        public int speedModifier;

        [Header("Special Abilities")]
        public float xpBonusMultiplier = 1.0f;
        public float meleeDamageBonus = 0f;
        public float dexWeaponDamageBonus = 0f;
        public float magicResistBonus = 0f;
        public float physicalDamageReduction = 0f;
        public float dodgeBonus = 0f;
        public float spellPowerBonus = 0f;
        public int spellSlotBonus = 0;
        public bool immuneToStunParalysis = false;
        public bool hasPoisonOnHit = false;
        public float poisonChance = 0f;
        public int poisonDamagePerTurn = 0;
        public float hpRegenPerTurn = 0f;

        [Header("Visual")]
        public Color primaryColor = Color.white;
        public float heightScale = 1.0f;
        public Sprite icon;
    }
}
