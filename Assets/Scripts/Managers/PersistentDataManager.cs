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
        [SerializeField] private bool startWithTestGladiators = false;
        [SerializeField] private List<GladiatorData> initialRosterTemplates = new List<GladiatorData>();

        [Header("Inventory (Owned but not equipped)")]
        public List<WeaponData> ownedWeapons = new List<WeaponData>();
        public List<ArmorData> ownedArmors = new List<ArmorData>();
        public List<SpellData> ownedSpells = new List<SpellData>();

        [Header("Battle Results (from last battle)")]
        public bool lastBattleVictory = false;
        public int lastBattleGoldReward = 0;

        private bool postBattleEffectsProcessed = false;

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
            if (weapon != null && !ownedWeapons.Contains(weapon))
            {
                ownedWeapons.Add(weapon);
                Debug.Log($"Added {weapon.weaponName} to inventory");
            }
        }

        public void AddArmorToInventory(ArmorData armor)
        {
            if (armor != null && !ownedArmors.Contains(armor))
            {
                ownedArmors.Add(armor);
                Debug.Log($"Added {armor.armorName} to inventory");
            }
        }

        public void AddSpellToInventory(SpellData spell)
        {
            if (spell != null && !ownedSpells.Contains(spell))
            {
                ownedSpells.Add(spell);
                Debug.Log($"Added {spell.spellName} to inventory");
            }
        }

        public bool OwnsWeapon(WeaponData weapon)
        {
            return weapon != null && ownedWeapons.Contains(weapon);
        }

        public bool OwnsArmor(ArmorData armor)
        {
            return armor != null && ownedArmors.Contains(armor);
        }

        public bool OwnsSpell(SpellData spell)
        {
            return spell != null && ownedSpells.Contains(spell);
        }

        public void PrepareBattle()
        {
            if (activeSquad.Count == 0 && playerRoster.Count == 0)
            {
                Debug.LogError("Cannot start battle: No gladiators available!");
                return;
            }

            postBattleEffectsProcessed = false;
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

            if (!postBattleEffectsProcessed)
            {
                ProcessPostBattleEffects("OnBattleComplete");
                postBattleEffectsProcessed = true;
            }
            else
            {
                Debug.LogWarning("ProcessPostBattleEffects already processed for this battle. Skipping duplicate call.");
            }
            SceneManager.LoadScene("Shop");
        }

        private void ProcessPostBattleEffects(string caller = "Unknown")
        {
            Debug.Log($"=== ProcessPostBattleEffects START ({caller}) ===");
            Debug.Log($"Guard flag before check: {postBattleEffectsProcessed}");
            if (postBattleEffectsProcessed)
            {
                Debug.LogWarning($"[{caller}] BLOCKED by guard flag!");
                return;
            }
            Debug.Log($"[{caller}] Processing effects for {playerRoster.Count} gladiators...");
            foreach (GladiatorInstance gladiator in playerRoster)
            {
                if (gladiator == null)
                {
                    continue;
                }

                Debug.Log($"  {gladiator.templateData.gladiatorName}: Status={gladiator.status}, Injury={gladiator.injuryBattlesRemaining}, Decay={gladiator.decayBattlesRemaining}");
                if (gladiator.status == GladiatorStatus.Injured)
                {
                    Debug.Log($"[{caller}] DECREMENTING injury for {gladiator.templateData.gladiatorName}");
                    int before = gladiator.injuryBattlesRemaining;
                    if (gladiator.injuryBattlesRemaining <= 0)
                    {
                        Debug.LogError($"ERROR: {gladiator.templateData.gladiatorName} is Injured but injuryBattlesRemaining is {gladiator.injuryBattlesRemaining}!");
                    }
                    Debug.Log($"  Before: {before}");
                    Debug.Log($"  Stack trace: {UnityEngine.StackTraceUtility.ExtractStackTrace()}");
                    gladiator.injuryBattlesRemaining--;
                    Debug.Log($"  After: {gladiator.injuryBattlesRemaining}");
                    if (gladiator.injuryBattlesRemaining <= 0)
                    {
                        gladiator.status = GladiatorStatus.Healthy;
                        gladiator.currentHP = gladiator.maxHP;
                        Debug.Log($"[{caller}] {gladiator.templateData.gladiatorName} recovered!");
                    }
                }

                if (gladiator.templateData != null &&
                    gladiator.templateData.race != null &&
                    gladiator.templateData.race.raceName == "Undead" &&
                    gladiator.decayBattlesRemaining > 0)
                {
                    gladiator.decayBattlesRemaining--;
                    Debug.Log($"[{caller}] {gladiator.templateData.gladiatorName} decay: {gladiator.decayBattlesRemaining} battles remaining");

                    if (gladiator.decayBattlesRemaining <= 0)
                    {
                        gladiator.status = GladiatorStatus.Dead;
                        Debug.Log($"[{caller}] {gladiator.templateData.gladiatorName} has completely decayed!");
                    }
                }
            }

            postBattleEffectsProcessed = true;
            Debug.Log($"Guard flag set to true by {caller}");
            Debug.Log($"=== ProcessPostBattleEffects END ({caller}) ===");
        }

        public void InitializeTestData()
        {
            playerGold = 2000;
            battleCount = 0;

            playerRoster.Clear();
            activeSquad.Clear();

            if (!startWithTestGladiators)
            {
                Debug.Log("Starting with empty roster (test gladiators disabled).");
                return;
            }

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
