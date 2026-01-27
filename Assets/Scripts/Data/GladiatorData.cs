using UnityEngine;

namespace ArenaTactics.Data
{
    /// <summary>
    /// Team affiliation for a gladiator.
    /// </summary>
    public enum Team
    {
        Player,
        Enemy
    }

    /// <summary>
    /// ScriptableObject that defines base stats for a gladiator type.
    /// </summary>
    [CreateAssetMenu(menuName = "ArenaTactics/Gladiator Data", fileName = "NewGladiatorData")]
    public class GladiatorData : ScriptableObject
    {
        [Header("Identity")]
        public string gladiatorName;

        [Header("Stats")]
        public int maxHP = 100;
        public int currentHP = 100;
        public int speed = 5;
        public int attack = 10;
        public int defense = 5;

        [Header("Turn Resources")]
        public int movementPoints = 3;
        public int actionPoints = 1;

        [Header("Team")]
        public Team team = Team.Player;

        [Header("Starting Equipment")]
        public WeaponData startingWeapon;
    }
}

