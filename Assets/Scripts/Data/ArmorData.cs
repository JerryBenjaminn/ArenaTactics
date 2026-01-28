using UnityEngine;

namespace ArenaTactics.Data
{
    public enum ArmorType
    {
        Light,
        Medium,
        Heavy,
        Robes
    }

    [CreateAssetMenu(menuName = "ArenaTactics/Armor Data", fileName = "NewArmorData")]
    public class ArmorData : ScriptableObject
    {
        [Header("Identity")]
        public string armorName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Type")]
        public ArmorType armorType = ArmorType.Light;

        [Header("Stat Bonuses")]
        public int hpBonus = 0;
        public int defenseBonus = 0;
        public int strengthBonus = 0;
        public int dexterityBonus = 0;
        public int intelligenceBonus = 0;
        public float dodgeBonus = 0f;
        public int movementPenalty = 0;
        public float spellPowerBonus = 0f;
        public int spellSlotBonus = 0;

        [Header("Economy")]
        public int cost = 0;
        [Range(1, 3)]
        public int tier = 1;
    }
}
