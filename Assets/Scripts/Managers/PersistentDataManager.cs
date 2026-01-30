using System.Collections.Generic;
using ArenaTactics.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaTactics.Managers
{
    public class PersistentDataManager : MonoBehaviour
    {
        public static PersistentDataManager Instance { get; private set; }

        [Header("Player Resources")]
        public int playerGold = 2000;
        public int battleCount = 0;

        [Header("Gladiator Roster")]
        public List<GladiatorInstance> playerRoster = new List<GladiatorInstance>();
        public List<GladiatorInstance> activeSquad = new List<GladiatorInstance>();

        [Header("Test Data")]
        [SerializeField] private List<GladiatorData> initialRosterTemplates = new List<GladiatorData>();

        [Header("Inventory (Owned but not equipped)")]
        public List<WeaponData> ownedWeapons = new List<WeaponData>();
        public List<ArmorData> ownedArmors = new List<ArmorData>();
        public List<SpellData> ownedSpells = new List<SpellData>();

        [Header("Battle Results (from last battle)")]
        public bool lastBattleVictory = false;
        public int lastBattleGoldReward = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (playerRoster.Count == 0 && activeSquad.Count == 0)
            {
                InitializeTestData();
            }
        }

        public void AddGold(int amount)
        {
            playerGold += amount;
            Debug.Log($"Gold added: +{amount}. Total: {playerGold}g");
        }

        public bool SpendGold(int amount)
        {
            if (playerGold >= amount)
            {
                playerGold -= amount;
                Debug.Log($"Gold spent: -{amount}. Remaining: {playerGold}g");
                return true;
            }

            Debug.LogWarning($"Not enough gold! Need {amount}, have {playerGold}");
            return false;
        }

        public void AddGladiatorToRoster(GladiatorInstance gladiator)
        {
            if (gladiator != null && !playerRoster.Contains(gladiator))
            {
                playerRoster.Add(gladiator);
                Debug.Log($"Added {gladiator.templateData.gladiatorName} to roster");
            }
        }

        public void RemoveGladiatorFromRoster(GladiatorInstance gladiator)
        {
            playerRoster.Remove(gladiator);
            activeSquad.Remove(gladiator);
            Debug.Log($"Removed {gladiator?.templateData.gladiatorName} from roster");
        }

        public void SetActiveSquad(List<GladiatorInstance> squad)
        {
            if (squad == null)
            {
                return;
            }

            if (squad.Count > 5)
            {
                Debug.LogWarning("Squad cannot exceed 5 gladiators!");
                return;
            }

            activeSquad = new List<GladiatorInstance>(squad);
            Debug.Log($"Active squad set: {activeSquad.Count} gladiators");
        }

        public void AddWeaponToInventory(WeaponData weapon)
        {
            if (weapon != null)
            {
                ownedWeapons.Add(weapon);
                Debug.Log($"Added {weapon.weaponName} to inventory");
            }
        }

        public void AddArmorToInventory(ArmorData armor)
        {
            if (armor != null)
            {
                ownedArmors.Add(armor);
                Debug.Log($"Added {armor.armorName} to inventory");
            }
        }

        public void AddSpellToInventory(SpellData spell)
        {
            if (spell != null)
            {
                ownedSpells.Add(spell);
                Debug.Log($"Added {spell.spellName} to inventory");
            }
        }

        public void PrepareBattle()
        {
            if (activeSquad.Count == 0 && playerRoster.Count == 0)
            {
                Debug.LogError("Cannot start battle: No gladiators available!");
                return;
            }

            Debug.Log($"Starting battle {battleCount + 1} with {Mathf.Max(activeSquad.Count, playerRoster.Count)} gladiators");
            SceneManager.LoadScene("Battle");
        }

        public void OnBattleComplete(bool victory, int goldReward)
        {
            lastBattleVictory = victory;
            lastBattleGoldReward = goldReward;

            if (victory)
            {
                battleCount++;
                AddGold(goldReward);
                Debug.Log($"Battle {battleCount} won! Reward: {goldReward}g");
            }
            else
            {
                AddGold(goldReward);
                Debug.Log($"Battle {battleCount + 1} lost. Reward: {goldReward}g");
            }

            ProcessPostBattleEffects();
            SceneManager.LoadScene("Shop");
        }

        private void ProcessPostBattleEffects()
        {
            foreach (GladiatorInstance gladiator in playerRoster)
            {
                if (gladiator == null)
                {
                    continue;
                }

                if (gladiator.status == GladiatorStatus.Injured)
                {
                    gladiator.injuryBattlesRemaining--;
                    if (gladiator.injuryBattlesRemaining <= 0)
                    {
                        gladiator.status = GladiatorStatus.Healthy;
                        gladiator.currentHP = gladiator.maxHP;
                        Debug.Log($"{gladiator.templateData.gladiatorName} recovered from injury!");
                    }
                }

                if (gladiator.templateData != null &&
                    gladiator.templateData.race != null &&
                    gladiator.templateData.race.raceName == "Undead" &&
                    gladiator.decayBattlesRemaining > 0)
                {
                    gladiator.decayBattlesRemaining--;
                    Debug.Log($"{gladiator.templateData.gladiatorName} decay: {gladiator.decayBattlesRemaining} battles remaining");

                    if (gladiator.decayBattlesRemaining <= 0)
                    {
                        gladiator.status = GladiatorStatus.Dead;
                        Debug.Log($"{gladiator.templateData.gladiatorName} has completely decayed!");
                    }
                }
            }

            Debug.Log("Post-battle effects processed");
        }

        public void InitializeTestData()
        {
            playerGold = 2000;
            battleCount = 0;

            playerRoster.Clear();
            activeSquad.Clear();

            List<GladiatorData> templates = new List<GladiatorData>();
            if (initialRosterTemplates != null && initialRosterTemplates.Count > 0)
            {
                templates.AddRange(initialRosterTemplates);
            }
            else
            {
                GladiatorData testWarrior = Resources.Load<GladiatorData>("ScriptableObjects/Gladiators/TestWarrior");
                GladiatorData testRogue = Resources.Load<GladiatorData>("ScriptableObjects/Gladiators/TestRogue");
                GladiatorData testMage = Resources.Load<GladiatorData>("ScriptableObjects/Gladiators/TestMage");

                if (testWarrior != null) templates.Add(testWarrior);
                if (testRogue != null) templates.Add(testRogue);
                if (testMage != null) templates.Add(testMage);
            }

            foreach (GladiatorData template in templates)
            {
                if (template == null)
                {
                    continue;
                }

                GladiatorInstance instance = new GladiatorInstance(template, 1);
                AddGladiatorToRoster(instance);
            }

            Debug.Log($"Starting roster: {playerRoster.Count} gladiators, 2000 gold");
        }

        [ContextMenu("Debug: Add 1000 Gold")]
        private void DebugAddGold()
        {
            AddGold(1000);
        }

        [ContextMenu("Debug: Reset All Data")]
        private void DebugResetData()
        {
            playerGold = 2000;
            battleCount = 0;
            playerRoster.Clear();
            activeSquad.Clear();
            ownedWeapons.Clear();
            ownedArmors.Clear();
            ownedSpells.Clear();
            Debug.Log("Debug: All data reset");
        }
    }
}
