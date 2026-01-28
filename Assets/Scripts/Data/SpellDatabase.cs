using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArenaTactics.Data
{
    [CreateAssetMenu(menuName = "ArenaTactics/Spell Database", fileName = "SpellDatabase")]
    public class SpellDatabase : ScriptableObject
    {
        public List<SpellData> spells = new List<SpellData>();

        public List<SpellData> GetSpellsByTier(int tier)
        {
            return spells.Where(spell => spell != null && spell.tier == tier).ToList();
        }

        public List<SpellData> GetSpellsByType(SpellType type)
        {
            return spells.Where(spell => spell != null && spell.spellType == type).ToList();
        }

        public List<SpellData> GetSpellsByScalingStat(SpellScalingStat stat)
        {
            return spells.Where(spell => spell != null && spell.scalingStat == stat).ToList();
        }

        public List<SpellData> GetSpellsByCostRange(int minCost, int maxCost)
        {
            return spells.Where(spell => spell != null && spell.cost >= minCost && spell.cost <= maxCost).ToList();
        }

        public SpellData GetRandomSpell(int tier)
        {
            List<SpellData> candidates = GetSpellsByTier(tier);
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
