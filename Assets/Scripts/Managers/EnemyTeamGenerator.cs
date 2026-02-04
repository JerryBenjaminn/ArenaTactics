using System.Collections.Generic;
using UnityEngine;
using ArenaTactics.Data;

namespace ArenaTactics.Managers
{
    public class EnemyTeamGenerator : MonoBehaviour
    {
        private static EnemyTeamGenerator instance;
        public static EnemyTeamGenerator Instance => instance;

        [Header("References")]
        [SerializeField] private GladiatorGenerator gladiatorGenerator;

        [Header("Data References")]
        [SerializeField] private List<WeaponData> allWeapons = new List<WeaponData>();
        [SerializeField] private List<ArmorData> allArmors = new List<ArmorData>();
        [SerializeField] private List<SpellData> allSpells = new List<SpellData>();

        [Header("Team Composition Settings")]
        [SerializeField] private bool useStrategicComposition = true;

        public enum TeamArchetype
        {
            Balanced,
            Aggressive,
            Ranged,
            TankWall,
            Random
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (gladiatorGenerator == null)
            {
                gladiatorGenerator = GladiatorGenerator.Instance;
            }
            if (gladiatorGenerator == null)
            {
                gladiatorGenerator = FindFirstObjectByType<GladiatorGenerator>(FindObjectsInactive.Include);
            }
        }

        public List<GladiatorInstance> GenerateEnemyTeam(int battleCount)
        {
            List<GladiatorInstance> enemyTeam = new List<GladiatorInstance>();

            TeamArchetype archetype = DetermineArchetype();
            Debug.Log($"[EnemyTeamGenerator] Generating {archetype} team for battle {battleCount}");

            List<string> classNames = GetClassComposition(archetype);
            foreach (string className in classNames)
            {
                GladiatorInstance gladiator = GenerateEnemyGladiator(battleCount, className);
                if (gladiator != null)
                {
                    enemyTeam.Add(gladiator);
                }
            }

            Debug.Log($"[EnemyTeamGenerator] Generated enemy team with {enemyTeam.Count} gladiators");
            return enemyTeam;
        }

        public List<GladiatorInstance> GenerateTeamWithBudget(int budget, TeamArchetype? forcedArchetype)
        {
            List<GladiatorInstance> team = new List<GladiatorInstance>();
            TeamArchetype archetype = forcedArchetype ?? DetermineArchetype();
            List<string> classNames = GetClassComposition(archetype);

            int remaining = budget;
            int targetCount = Mathf.Clamp(budget / 400, 2, 5);

            for (int i = 0; i < classNames.Count && team.Count < targetCount; i++)
            {
                GladiatorQuality quality = DetermineQualityFromBudget(remaining, budget);
                int level = DetermineLevelFromBudget(remaining);
                GladiatorInstance gladiator = GenerateEnemyGladiatorFromBudget(classNames[i], level, quality, remaining);
                if (gladiator == null)
                {
                    continue;
                }

                int cost = EstimateGladiatorCost(gladiator);
                if (cost > remaining && team.Count >= 2)
                {
                    break;
                }

                remaining = Mathf.Max(0, remaining - cost);
                team.Add(gladiator);
            }

            return team;
        }

        private GladiatorInstance GenerateEnemyGladiator(int battleCount, string preferredClass = null)
        {
            if (gladiatorGenerator == null)
            {
                Debug.LogError("[EnemyTeamGenerator] GladiatorGenerator reference missing.");
                return null;
            }

            int tier = DetermineTier(battleCount);
            GladiatorQuality quality = DetermineQuality(battleCount);
            int level = DetermineLevel(battleCount);

            GladiatorInstance gladiator = string.IsNullOrEmpty(preferredClass)
                ? gladiatorGenerator.GenerateGladiator(level, quality)
                : gladiatorGenerator.GenerateGladiatorWithClass(level, quality, preferredClass);

            if (gladiator == null || gladiator.templateData == null)
            {
                Debug.LogError("[EnemyTeamGenerator] Failed to generate gladiator.");
                return null;
            }

            gladiator.templateData.team = Team.Enemy;

            EquipGladiator(gladiator, tier);

            return gladiator;
        }

        private GladiatorInstance GenerateEnemyGladiatorFromBudget(string preferredClass, int level, GladiatorQuality quality, int remainingBudget)
        {
            if (gladiatorGenerator == null)
            {
                Debug.LogError("[EnemyTeamGenerator] GladiatorGenerator reference missing.");
                return null;
            }

            GladiatorInstance gladiator = string.IsNullOrEmpty(preferredClass)
                ? gladiatorGenerator.GenerateGladiator(level, quality)
                : gladiatorGenerator.GenerateGladiatorWithClass(level, quality, preferredClass);

            if (gladiator == null || gladiator.templateData == null)
            {
                return null;
            }

            gladiator.templateData.team = Team.Enemy;

            int tier = DetermineTierFromBudget(remainingBudget);
            EquipGladiator(gladiator, tier);

            return gladiator;
        }

        private int DetermineTier(int battleCount)
        {
            if (battleCount <= 1)
            {
                return 1;
            }
            if (battleCount <= 3)
            {
                return Random.Range(1, 3);
            }
            return Random.Range(2, 4);
        }

        private GladiatorQuality DetermineQuality(int battleCount)
        {
            float roll = Random.value;

            if (battleCount <= 1)
            {
                if (roll < 0.4f) return GladiatorQuality.Poor;
                if (roll < 0.8f) return GladiatorQuality.Average;
                if (roll < 0.95f) return GladiatorQuality.Good;
                return GladiatorQuality.Excellent;
            }
            if (battleCount <= 3)
            {
                if (roll < 0.2f) return GladiatorQuality.Poor;
                if (roll < 0.5f) return GladiatorQuality.Average;
                if (roll < 0.85f) return GladiatorQuality.Good;
                return GladiatorQuality.Excellent;
            }

            if (roll < 0.05f) return GladiatorQuality.Poor;
            if (roll < 0.25f) return GladiatorQuality.Average;
            if (roll < 0.60f) return GladiatorQuality.Good;
            return GladiatorQuality.Excellent;
        }

        private int DetermineLevel(int battleCount)
        {
            if (battleCount <= 1) return 1;
            if (battleCount <= 3) return Random.Range(1, 4);
            if (battleCount <= 5) return Random.Range(2, 6);
            return Random.Range(4, 8);
        }

        private int DetermineTierFromBudget(int budget)
        {
            if (budget <= 1800) return 1;
            if (budget <= 2500) return 2;
            return 3;
        }

        private GladiatorQuality DetermineQualityFromBudget(int remaining, int total)
        {
            float ratio = total > 0 ? (float)remaining / total : 0f;
            if (ratio < 0.2f) return GladiatorQuality.Poor;
            if (ratio < 0.5f) return GladiatorQuality.Average;
            if (ratio < 0.8f) return GladiatorQuality.Good;
            return GladiatorQuality.Excellent;
        }

        private int DetermineLevelFromBudget(int remaining)
        {
            if (remaining < 1200) return 1;
            if (remaining < 2000) return Random.Range(1, 3);
            if (remaining < 2600) return Random.Range(2, 5);
            return Random.Range(3, 6);
        }

        private TeamArchetype DetermineArchetype()
        {
            if (!useStrategicComposition)
            {
                return TeamArchetype.Random;
            }

            int roll = Random.Range(0, 4);
            return (TeamArchetype)roll;
        }

        private List<string> GetClassComposition(TeamArchetype archetype)
        {
            List<string> classes = new List<string>();

            switch (archetype)
            {
                case TeamArchetype.Balanced:
                    classes.Add("Tank");
                    classes.Add("Warrior");
                    classes.Add("Warrior");
                    classes.Add("Archer");
                    classes.Add("Mage");
                    break;
                case TeamArchetype.Aggressive:
                    classes.Add("Tank");
                    classes.Add("Warrior");
                    classes.Add("Warrior");
                    classes.Add("Warrior");
                    classes.Add("Rogue");
                    break;
                case TeamArchetype.Ranged:
                    classes.Add("Tank");
                    classes.Add("Archer");
                    classes.Add("Archer");
                    classes.Add("Mage");
                    classes.Add("Mage");
                    break;
                case TeamArchetype.TankWall:
                    classes.Add("Tank");
                    classes.Add("Tank");
                    classes.Add("Tank");
                    classes.Add("Warrior");
                    classes.Add("Mage");
                    break;
                case TeamArchetype.Random:
                default:
                    string[] allClasses = { "Warrior", "Tank", "Rogue", "Archer", "Mage" };
                    for (int i = 0; i < 5; i++)
                    {
                        classes.Add(allClasses[Random.Range(0, allClasses.Length)]);
                    }
                    break;
            }

            return classes;
        }

        private void EquipGladiator(GladiatorInstance gladiator, int tier)
        {
            if (gladiator == null)
            {
                return;
            }

            List<WeaponData> validWeapons = allWeapons.FindAll(w => w != null && w.weaponTier <= tier);
            List<ArmorData> validArmors = allArmors.FindAll(a => a != null && a.tier <= tier);
            List<SpellData> validSpells = allSpells.FindAll(s => s != null && s.tier <= tier);

            WeaponData weapon = SelectAppropriateWeapon(gladiator, validWeapons);
            if (weapon != null)
            {
                gladiator.EquipWeapon(weapon);
            }

            ArmorData armor = SelectAppropriateArmor(gladiator, validArmors);
            if (armor != null)
            {
                gladiator.EquipArmor(armor);
            }

            if (gladiator.templateData.gladiatorClass != null &&
                (gladiator.templateData.gladiatorClass.className == "Mage" ||
                 gladiator.templateData.gladiatorClass.baseIntelligence >= 10))
            {
                int spellCount = Random.Range(2, 5);
                for (int i = 0; i < spellCount && i < gladiator.knownSpells.Length; i++)
                {
                    if (validSpells.Count > 0)
                    {
                        SpellData spell = validSpells[Random.Range(0, validSpells.Count)];
                        gladiator.LearnSpell(spell, i);
                    }
                }
            }
        }

        private int EstimateGladiatorCost(GladiatorInstance gladiator)
        {
            if (gladiator == null || gladiator.templateData == null)
            {
                return 0;
            }

            int baseCost = 300 + gladiator.currentLevel * 150;
            int weaponCost = gladiator.equippedWeapon != null ? gladiator.equippedWeapon.cost : 0;
            int armorCost = gladiator.equippedArmor != null ? gladiator.equippedArmor.cost : 0;
            int spellCost = 0;
            if (gladiator.knownSpells != null)
            {
                foreach (SpellData spell in gladiator.knownSpells)
                {
                    if (spell != null)
                    {
                        spellCost += spell.cost;
                    }
                }
            }

            return baseCost + weaponCost + armorCost + spellCost;
        }

        private WeaponData SelectAppropriateWeapon(GladiatorInstance gladiator, List<WeaponData> weapons)
        {
            if (gladiator == null || gladiator.templateData == null || weapons.Count == 0)
            {
                return null;
            }

            string className = gladiator.templateData.gladiatorClass != null
                ? gladiator.templateData.gladiatorClass.className
                : string.Empty;

            List<WeaponData> preferred = new List<WeaponData>();

            switch (className)
            {
                case "Archer":
                    preferred = weapons.FindAll(w => w.weaponType == WeaponType.Ranged);
                    break;
                case "Mage":
                    preferred = weapons.FindAll(w => w.weaponType == WeaponType.Magic || w.intelligenceBonus > 0);
                    break;
                case "Rogue":
                    preferred = weapons.FindAll(w => w.weaponType == WeaponType.Melee && w.critBonus > 5f);
                    break;
                case "Warrior":
                case "Tank":
                    preferred = weapons.FindAll(w => w.weaponType == WeaponType.Melee);
                    break;
            }

            if (preferred.Count == 0)
            {
                preferred = weapons;
            }

            return preferred[Random.Range(0, preferred.Count)];
        }

        private ArmorData SelectAppropriateArmor(GladiatorInstance gladiator, List<ArmorData> armors)
        {
            if (gladiator == null || gladiator.templateData == null || armors.Count == 0)
            {
                return null;
            }

            string className = gladiator.templateData.gladiatorClass != null
                ? gladiator.templateData.gladiatorClass.className
                : string.Empty;

            List<ArmorData> preferred = new List<ArmorData>();

            switch (className)
            {
                case "Tank":
                    preferred = armors.FindAll(a => a.armorType == ArmorType.Heavy);
                    break;
                case "Warrior":
                    preferred = armors.FindAll(a => a.armorType == ArmorType.Medium || a.armorType == ArmorType.Heavy);
                    break;
                case "Mage":
                    preferred = armors.FindAll(a => a.armorType == ArmorType.Robes);
                    break;
                case "Rogue":
                case "Archer":
                    preferred = armors.FindAll(a => a.armorType == ArmorType.Light);
                    break;
            }

            if (preferred.Count == 0)
            {
                preferred = armors;
            }

            return preferred[Random.Range(0, preferred.Count)];
        }
    }
}
