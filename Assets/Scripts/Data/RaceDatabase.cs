using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArenaTactics.Data
{
    [CreateAssetMenu(menuName = "ArenaTactics/Race Database", fileName = "RaceDatabase")]
    public class RaceDatabase : ScriptableObject
    {
        public List<RaceData> races = new List<RaceData>();

        public RaceData GetRandomRace()
        {
            List<RaceData> candidates = races.Where(race => race != null).ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
