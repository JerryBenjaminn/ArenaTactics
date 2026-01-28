using UnityEngine;

namespace ArenaTactics.Data
{
    public enum SpellType
    {
        Damage,
        AOE,
        Debuff,
        Buff
    }

    public enum SpellScalingStat
    {
        Intelligence,
        Strength,
        Dexterity
    }

    public enum EffectType
    {
        None,
        Damage,
        Heal,
        StrengthDebuff,
        DefenseDebuff,
        SpeedDebuff,
        MovementDebuff,
        Stun,
        StrengthBuff,
        DefenseBuff,
        SpeedBuff,
        MovementBuff,
        ImmunityBuff
    }

    [CreateAssetMenu(menuName = "ArenaTactics/Spell Data", fileName = "NewSpellData")]
    public class SpellData : ScriptableObject
    {
        [Header("Identity")]
        public string spellName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Core")]
        public SpellType spellType = SpellType.Damage;
        public int basePower = 0;
        public SpellScalingStat scalingStat = SpellScalingStat.Intelligence;
        public int range = 4;
        public int apCost = 1;
        public int spellSlotCost = 1;
        public int cooldownTurns = 0;
        public bool requiresLineOfSight = true;

        [Header("AOE")]
        public int aoeRadius = 0;

        [Header("Effects")]
        public int duration = 0;
        public EffectType effectType = EffectType.None;
        public int effectValue = 0;
        public EffectType secondaryEffectType = EffectType.None;
        public int secondaryEffectValue = 0;

        [Header("Economy")]
        public int cost = 0;
        [Range(1, 3)]
        public int tier = 1;
    }
}
