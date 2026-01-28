using System.Collections.Generic;
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
    }
}
