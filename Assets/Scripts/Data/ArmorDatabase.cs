using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArenaTactics.Data
{
    [CreateAssetMenu(menuName = "ArenaTactics/Armor Database", fileName = "ArmorDatabase")]
    public class ArmorDatabase : ScriptableObject
    {
        public List<ArmorData> armors = new List<ArmorData>();

        public List<ArmorData> GetArmorsByTier(int tier)
        {
            return armors.Where(armor => armor != null && armor.tier == tier).ToList();
        }

        public List<ArmorData> GetArmorsByType(ArmorType type)
        {
            return armors.Where(armor => armor != null && armor.armorType == type).ToList();
        }

        public List<ArmorData> GetArmorsByCostRange(int minCost, int maxCost)
        {
            return armors.Where(armor => armor != null && armor.cost >= minCost && armor.cost <= maxCost).ToList();
        }

        public ArmorData GetRandomArmor(int tier)
        {
            List<ArmorData> candidates = GetArmorsByTier(tier);
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
