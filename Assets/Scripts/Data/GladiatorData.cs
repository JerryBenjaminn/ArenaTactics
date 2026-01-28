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

        [Header("Class")]
        public GladiatorClass gladiatorClass;

        [Header("Stat Modifiers (added to class base)")]
        [Tooltip("These modify the base stats from the class")]
        public int hpModifier = 0;
        public int strengthModifier = 0;
        public int dexterityModifier = 0;
        public int intelligenceModifier = 0;
        public int defenseModifier = 0;
        public int speedModifier = 0;

        [Header("Deprecated Stats (Legacy Fallback)")]
        public int maxHP = 100;
        public int currentHP = 100;
        public int speed = 5;
        public int strength = 5;
        public int dexterity = 5;
        public int intelligence = 5;
        public int defense = 5;
        public int movementPoints = 3;
        public int actionPoints = 1;

        public int MaxHP => (gladiatorClass != null ? gladiatorClass.baseHP : maxHP) + hpModifier;
        public int Speed => (gladiatorClass != null ? gladiatorClass.baseSpeed : speed) + speedModifier;
        public int Strength => (gladiatorClass != null ? gladiatorClass.baseStrength : strength) + strengthModifier;
        public int Dexterity => (gladiatorClass != null ? gladiatorClass.baseDexterity : dexterity) + dexterityModifier;
        public int Intelligence => (gladiatorClass != null ? gladiatorClass.baseIntelligence : intelligence) + intelligenceModifier;
        public int Defense => (gladiatorClass != null ? gladiatorClass.baseDefense : defense) + defenseModifier;
        public int MovementPoints => gladiatorClass != null ? gladiatorClass.baseMovementPoints : movementPoints;
        public int ActionPoints => gladiatorClass != null ? gladiatorClass.baseActionPoints : actionPoints;

        [Header("Team")]
        public Team team = Team.Player;

        [Header("Starting Equipment")]
        public WeaponData startingWeapon;
    }
}

