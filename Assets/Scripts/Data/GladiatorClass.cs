using UnityEngine;

namespace ArenaTactics.Data
{
    /// <summary>
    /// ScriptableObject that defines base stats and traits for a gladiator class.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaTactics/Gladiator Class", fileName = "NewClass")]
    public class GladiatorClass : ScriptableObject
    {
        [Header("Identity")]
        public string className;

        [TextArea(2, 4)]
        public string description;

        [Header("Base Stats")]
        public int baseHP = 100;
        public int baseSpeed = 5;

        [Header("Combat Stats")]
        public int baseStrength = 5;
        public int baseDexterity = 5;
        public int baseIntelligence = 5;
        public int baseDefense = 3;

        [Header("Action Points")]
        public int baseMovementPoints = 3;
        public int baseActionPoints = 1;

        [Header("Growth Rates (Per Level)")]
        public int hpGrowth = 10;
        public int strengthGrowth = 1;
        public int dexterityGrowth = 1;
        public int intelligenceGrowth = 1;
        public int defenseGrowth = 1;
        public int speedGrowth = 0;

        [Header("Class Traits")]
        [TextArea(2, 4)]
        public string traits;
    }
}
