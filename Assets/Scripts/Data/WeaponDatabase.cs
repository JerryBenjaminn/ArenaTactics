using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArenaTactics.Data
{
    /// <summary>
    /// Registry of all weapon data assets for shops and lookup.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaTactics/Weapon Database", fileName = "WeaponDatabase")]
    public class WeaponDatabase : ScriptableObject
    {
        public List<WeaponData> weapons = new List<WeaponData>();

        public List<WeaponData> GetWeaponsByTier(int tier)
        {
            return weapons.Where(weapon => weapon != null && weapon.weaponTier == tier).ToList();
        }

        public List<WeaponData> GetWeaponsByType(WeaponType type)
        {
            return weapons.Where(weapon => weapon != null && weapon.weaponType == type).ToList();
        }

        public List<WeaponData> GetWeaponsByScalingStat(ScalingStat stat)
        {
            return weapons.Where(weapon => weapon != null && weapon.scalingStat == stat).ToList();
        }

        public List<WeaponData> GetWeaponsByCostRange(int minCost, int maxCost)
        {
            return weapons.Where(weapon =>
                    weapon != null && weapon.cost >= minCost && weapon.cost <= maxCost)
                .ToList();
        }

        public WeaponData GetRandomWeapon(int tier)
        {
            List<WeaponData> candidates = GetWeaponsByTier(tier);
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
