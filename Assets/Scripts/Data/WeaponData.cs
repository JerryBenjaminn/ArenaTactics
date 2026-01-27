using UnityEngine;

namespace ArenaTactics.Data
{
    /// <summary>
    /// Weapon categories used for range and behavior.
    /// </summary>
    public enum WeaponType
    {
        Sword,
        Spear,
        Bow,
        Sling,
        Staff
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
    /// ScriptableObject containing weapon stats and behavior.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaTactics/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName;

        [Header("Type")]
        public WeaponType weaponType;
        public DamageType damageType = DamageType.Physical;

        [Header("Combat Stats")]
        public int baseDamage = 0;
        public int attackRange = 1;
        public bool requiresLineOfSight;
        public int actionPointCost = 1;
    }
}
