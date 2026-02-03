using System.Collections.Generic;
using UnityEngine;
using ArenaTactics.Data;

namespace ArenaTactics.Managers
{
    public class GladiatorGenerator : MonoBehaviour
    {
        private static GladiatorGenerator instance;

        public static GladiatorGenerator Instance => instance;

        [Header("Data Sources")]
        public GladiatorClass[] availableClasses;
        public RaceData[] availableRaces;

        [Header("Quality Definitions")]
        private Dictionary<GladiatorQuality, QualityModifiers> qualityTable;

        [Header("Name Lists")]
        private readonly string[] maleNames = { "Marcus", "Brutus", "Maximus", "Spartacus", "Draven", "Thorne", "Ajax", "Cassius" };
        private readonly string[] femaleNames = { "Luna", "Aria", "Valeria", "Xena", "Nyx", "Sable", "Raven", "Kira" };
        private readonly string[] neutralNames = { "Shadow", "Storm", "Blade", "Crimson", "Phoenix", "Ash", "Ghost", "Venom" };

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitializeQualityTable();
        }

        private void InitializeQualityTable()
        {
            qualityTable = new Dictionary<GladiatorQuality, QualityModifiers>
            {
                { GladiatorQuality.Poor, new QualityModifiers(GladiatorQuality.Poor, -2, 1, 0.8f) },
                { GladiatorQuality.Average, new QualityModifiers(GladiatorQuality.Average, 0, 2, 1.0f) },
                { GladiatorQuality.Good, new QualityModifiers(GladiatorQuality.Good, 1, 3, 1.3f) },
                { GladiatorQuality.Excellent, new QualityModifiers(GladiatorQuality.Excellent, 2, 5, 1.7f) }
            };
        }

        public GladiatorInstance GenerateGladiator(int level, GladiatorQuality quality)
        {
            if (availableClasses == null || availableClasses.Length == 0 ||
                availableRaces == null || availableRaces.Length == 0)
            {
                Debug.LogError("GladiatorGenerator: Available classes or races not assigned.");
                return null;
            }

            GladiatorClass randomClass = availableClasses[Random.Range(0, availableClasses.Length)];
            RaceData randomRace = availableRaces[Random.Range(0, availableRaces.Length)];

            GladiatorData template = CreateTemplateData(randomClass, randomRace);
            GladiatorInstance instance = new GladiatorInstance(template, level);

            ApplyQualityVariance(instance, quality);

            instance.templateData.gladiatorName = GenerateRandomName();

            int price = CalculatePrice(instance, quality);

            Debug.Log($"Generated: {instance.templateData.gladiatorName} ({randomRace.raceName} {randomClass.className}) - Level {level}, Quality: {quality}, Price: {price}g");

            return instance;
        }

        public GladiatorInstance GenerateGladiatorWithClass(int level, GladiatorQuality quality, string className)
        {
            if (availableClasses == null || availableClasses.Length == 0 ||
                availableRaces == null || availableRaces.Length == 0)
            {
                Debug.LogError("GladiatorGenerator: Available classes or races not assigned.");
                return null;
            }

            if (string.IsNullOrEmpty(className))
            {
                return GenerateGladiator(level, quality);
            }

            GladiatorClass targetClass = null;
            foreach (GladiatorClass gladiatorClass in availableClasses)
            {
                if (gladiatorClass != null && gladiatorClass.className == className)
                {
                    targetClass = gladiatorClass;
                    break;
                }
            }

            if (targetClass == null)
            {
                Debug.LogWarning($"GladiatorGenerator: Class '{className}' not found. Generating random class.");
                return GenerateGladiator(level, quality);
            }

            RaceData randomRace = availableRaces[Random.Range(0, availableRaces.Length)];
            GladiatorData template = CreateTemplateData(targetClass, randomRace);
            GladiatorInstance instance = new GladiatorInstance(template, level);

            ApplyQualityVariance(instance, quality);
            instance.templateData.gladiatorName = GenerateRandomName();

            return instance;
        }

        private GladiatorData CreateTemplateData(GladiatorClass gladClass, RaceData race)
        {
            GladiatorData template = ScriptableObject.CreateInstance<GladiatorData>();

            template.gladiatorClass = gladClass;
            template.race = race;
            template.gladiatorName = "Generated";

            template.hpModifier = 0;
            template.strengthModifier = 0;
            template.dexterityModifier = 0;
            template.intelligenceModifier = 0;
            template.defenseModifier = 0;
            template.speedModifier = 0;

            return template;
        }

        private void ApplyQualityVariance(GladiatorInstance instance, GladiatorQuality quality)
        {
            if (instance == null || instance.templateData == null)
            {
                return;
            }

            QualityModifiers mods = qualityTable[quality];

            int hpVariance = Random.Range(mods.minStatVariance * 5, (mods.maxStatVariance + 1) * 5);
            instance.templateData.hpModifier = hpVariance;

            instance.templateData.strengthModifier = Random.Range(mods.minStatVariance, mods.maxStatVariance + 1);
            instance.templateData.dexterityModifier = Random.Range(mods.minStatVariance, mods.maxStatVariance + 1);
            instance.templateData.intelligenceModifier = Random.Range(mods.minStatVariance, mods.maxStatVariance + 1);
            instance.templateData.defenseModifier = Random.Range(mods.minStatVariance, mods.maxStatVariance + 1);
            instance.templateData.speedModifier = Random.Range(mods.minStatVariance, mods.maxStatVariance + 1);

            instance.maxHP = instance.CalculateMaxHP();
            instance.currentHP = instance.maxHP;

            Debug.Log($"  Quality variance ({quality}): HP{hpVariance:+#;-#;0}, STR{instance.templateData.strengthModifier:+#;-#;0}, DEX{instance.templateData.dexterityModifier:+#;-#;0}");
        }

        private int CalculatePrice(GladiatorInstance instance, GladiatorQuality quality)
        {
            if (instance == null || instance.templateData == null)
            {
                return 0;
            }

            int basePrice = 500;

            if (instance.currentLevel >= 8)
            {
                basePrice = 4000;
            }
            else if (instance.currentLevel >= 4)
            {
                basePrice = 2000;
            }

            int statValue = 0;
            statValue += instance.maxHP / 10;
            statValue += GetTotalStrength(instance) * 10;
            statValue += GetTotalDexterity(instance) * 10;
            statValue += GetTotalIntelligence(instance) * 10;
            statValue += GetTotalDefense(instance) * 10;
            statValue += GetTotalSpeed(instance) * 5;

            int pricePerStat = 2;

            int calculatedPrice = basePrice + (statValue * pricePerStat);

            QualityModifiers mods = qualityTable[quality];
            calculatedPrice = Mathf.RoundToInt(calculatedPrice * mods.priceMultiplier);

            calculatedPrice = Mathf.Clamp(calculatedPrice, 300, 10000);

            return calculatedPrice;
        }

        private int GetTotalStrength(GladiatorInstance instance)
        {
            int total = 0;
            if (instance.templateData.gladiatorClass != null)
            {
                total += instance.templateData.gladiatorClass.baseStrength;
                total += (instance.currentLevel - 1) * instance.templateData.gladiatorClass.strengthGrowth;
            }
            if (instance.templateData.race != null)
            {
                total += instance.templateData.race.strengthModifier;
            }
            total += instance.templateData.strengthModifier;
            return total;
        }

        private int GetTotalDexterity(GladiatorInstance instance)
        {
            int total = 0;
            if (instance.templateData.gladiatorClass != null)
            {
                total += instance.templateData.gladiatorClass.baseDexterity;
                total += (instance.currentLevel - 1) * instance.templateData.gladiatorClass.dexterityGrowth;
            }
            if (instance.templateData.race != null)
            {
                total += instance.templateData.race.dexterityModifier;
            }
            total += instance.templateData.dexterityModifier;
            return total;
        }

        private int GetTotalIntelligence(GladiatorInstance instance)
        {
            int total = 0;
            if (instance.templateData.gladiatorClass != null)
            {
                total += instance.templateData.gladiatorClass.baseIntelligence;
                total += (instance.currentLevel - 1) * instance.templateData.gladiatorClass.intelligenceGrowth;
            }
            if (instance.templateData.race != null)
            {
                total += instance.templateData.race.intelligenceModifier;
            }
            total += instance.templateData.intelligenceModifier;
            return total;
        }

        private int GetTotalDefense(GladiatorInstance instance)
        {
            int total = 0;
            if (instance.templateData.gladiatorClass != null)
            {
                total += instance.templateData.gladiatorClass.baseDefense;
            }
            if (instance.templateData.race != null)
            {
                total += instance.templateData.race.defenseModifier;
            }
            total += instance.templateData.defenseModifier;
            return total;
        }

        private int GetTotalSpeed(GladiatorInstance instance)
        {
            int total = 0;
            if (instance.templateData.gladiatorClass != null)
            {
                total += instance.templateData.gladiatorClass.baseSpeed;
            }
            if (instance.templateData.race != null)
            {
                total += instance.templateData.race.speedModifier;
            }
            total += instance.templateData.speedModifier;
            return total;
        }

        private string GenerateRandomName()
        {
            List<string> allNames = new List<string>();
            allNames.AddRange(maleNames);
            allNames.AddRange(femaleNames);
            allNames.AddRange(neutralNames);

            return allNames[Random.Range(0, allNames.Count)];
        }

        public List<GladiatorInstance> GenerateShopPool(int battleCount)
        {
            List<GladiatorInstance> pool = new List<GladiatorInstance>();

            if (battleCount <= 1)
            {
                pool.Add(GenerateGladiator(1, GladiatorQuality.Poor));
                pool.Add(GenerateGladiator(1, GladiatorQuality.Poor));
                pool.Add(GenerateGladiator(1, GladiatorQuality.Average));
                pool.Add(GenerateGladiator(1, GladiatorQuality.Average));
                pool.Add(GenerateGladiator(1, GladiatorQuality.Good));
            }
            else if (battleCount <= 3)
            {
                pool.Add(GenerateGladiator(1, GladiatorQuality.Poor));
                pool.Add(GenerateGladiator(1, GladiatorQuality.Good));
                pool.Add(GenerateGladiator(1, GladiatorQuality.Good));
                pool.Add(GenerateGladiator(Random.Range(4, 6), GladiatorQuality.Average));
                pool.Add(GenerateGladiator(Random.Range(4, 6), GladiatorQuality.Good));
            }
            else
            {
                pool.Add(GenerateGladiator(1, GladiatorQuality.Excellent));
                pool.Add(GenerateGladiator(Random.Range(4, 6), GladiatorQuality.Good));
                pool.Add(GenerateGladiator(Random.Range(4, 6), GladiatorQuality.Excellent));
                pool.Add(GenerateGladiator(Random.Range(8, 10), GladiatorQuality.Average));
                pool.Add(GenerateGladiator(Random.Range(8, 10), GladiatorQuality.Good));
            }

            Debug.Log($"=== Generated shop pool for battle {battleCount + 1}: {pool.Count} gladiators ===");

            return pool;
        }

        [ContextMenu("Test: Generate 5 Gladiators")]
        private void TestGeneration()
        {
            Debug.Log("=== TESTING GLADIATOR GENERATION ===");

            List<GladiatorInstance> pool = GenerateShopPool(0);

            foreach (GladiatorInstance glad in pool)
            {
                if (glad == null || glad.templateData == null)
                {
                    continue;
                }

                Debug.Log($"{glad.templateData.gladiatorName}: {glad.templateData.race.raceName} {glad.templateData.gladiatorClass.className} (Lvl {glad.currentLevel})");
                Debug.Log($"  HP: {glad.currentHP}/{glad.maxHP}, Stats: STR {GetTotalStrength(glad)}, DEX {GetTotalDexterity(glad)}, INT {GetTotalIntelligence(glad)}");
            }
        }
    }
}
