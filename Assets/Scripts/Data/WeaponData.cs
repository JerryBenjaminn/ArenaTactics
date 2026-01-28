using UnityEngine;

namespace ArenaTactics.Data
{
    /// <summary>
    /// Weapon categories used for range and behavior.
    /// </summary>
    public enum WeaponType
    {
        Melee,
        Ranged,
        Magic
    }

    /// <summary>
    /// Defines the damage type a weapon deals.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Magical
    }

    /// <summary>
    /// Determines which stat scales a weapon's damage.
    /// </summary>
    public enum ScalingStat
    {
        Strength,
        Dexterity,
        Intelligence
    }

    /// <summary>
    /// ScriptableObject containing weapon stats and behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaTactics/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Type")]
        public WeaponType weaponType;
        public DamageType damageType = DamageType.Physical;

        [Header("Combat Stats")]
        public int baseDamage = 0;
        public int range = 1;
        public int attackRange = 1;
        public bool requiresLineOfSight;
        public int actionPointCost = 1;
        public int cost = 0;
        [Range(1, 3)]
        public int weaponTier = 1;

        [Header("Scaling")]
        public ScalingStat scalingStat = ScalingStat.Strength;

        [Header("Stat Bonuses")]
        public int strengthBonus = 0;
        public int dexterityBonus = 0;
        public int intelligenceBonus = 0;
        public int defenseBonus = 0;

        [Header("Derived Stat Bonuses")]
        public float accuracyBonus = 0f;
        public float critBonus = 0f;
        public int spellSlotBonus = 0;
        public float spellPowerBonus = 0f;
    }
}
