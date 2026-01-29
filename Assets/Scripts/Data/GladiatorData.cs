using System.Collections.Generic;
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

        [Header("Race")]
        public RaceData race;

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

        public int MaxHP => (gladiatorClass != null ? gladiatorClass.baseHP : maxHP) +
                            (race != null ? race.hpModifier : 0) +
                            hpModifier;
        public int Speed => (gladiatorClass != null ? gladiatorClass.baseSpeed : speed) +
                            (race != null ? race.speedModifier : 0) +
                            speedModifier;
        public int Strength => (gladiatorClass != null ? gladiatorClass.baseStrength : strength) +
                               (race != null ? race.strengthModifier : 0) +
                               strengthModifier;
        public int Dexterity => (gladiatorClass != null ? gladiatorClass.baseDexterity : dexterity) +
                                (race != null ? race.dexterityModifier : 0) +
                                dexterityModifier;
        public int Intelligence => (gladiatorClass != null ? gladiatorClass.baseIntelligence : intelligence) +
                                   (race != null ? race.intelligenceModifier : 0) +
                                   intelligenceModifier;
        public int Defense => (gladiatorClass != null ? gladiatorClass.baseDefense : defense) +
                              (race != null ? race.defenseModifier : 0) +
                              defenseModifier;
        public int MovementPoints => gladiatorClass != null ? gladiatorClass.baseMovementPoints : movementPoints;
        public int ActionPoints => gladiatorClass != null ? gladiatorClass.baseActionPoints : actionPoints;

        [Header("Team")]
        public Team team = Team.Player;

        [Header("Starting Equipment")]
        public WeaponData startingWeapon;

        public ArmorData startingArmor;

        [Header("Starting Spells")]
        public List<SpellData> startingSpells = new List<SpellData>();
    }
}

