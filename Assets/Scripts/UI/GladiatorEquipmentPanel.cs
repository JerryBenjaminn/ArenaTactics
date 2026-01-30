using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArenaTactics.Data;
using ArenaTactics.Managers;

namespace ArenaTactics.UI
{
    public class GladiatorEquipmentPanel : MonoBehaviour
    {
        [Header("Selected Gladiator Display")]
        public TextMeshProUGUI gladiatorNameText;
        public TextMeshProUGUI gladiatorStatsText;

        [Header("Equipped Items Display")]
        public TextMeshProUGUI equippedWeaponText;
        public TextMeshProUGUI equippedArmorText;
        public TextMeshProUGUI knownSpellsText;

        [Header("Equip Buttons")]
        public Button equipWeaponButton;
        public Button equipArmorButton;
        public Button learnSpellButton;

        [Header("Item Selection Dropdowns")]
        public TMP_Dropdown weaponDropdown;
        public TMP_Dropdown armorDropdown;
        public TMP_Dropdown spellDropdown;

        private GladiatorInstance selectedGladiator;
        private PersistentDataManager dataManager;

        private void Start()
        {
            Debug.Log($"=== GladiatorEquipmentPanel.Start() on {gameObject.name} ===");
            dataManager = PersistentDataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("PersistentDataManager not found!");
            }

            if (equipWeaponButton != null)
            {
                equipWeaponButton.onClick.AddListener(OnEquipWeapon);
            }

            if (equipArmorButton != null)
            {
                equipArmorButton.onClick.AddListener(OnEquipArmor);
            }

            if (learnSpellButton != null)
            {
                learnSpellButton.onClick.AddListener(OnLearnSpell);
            }

            Debug.Log("Setting panel inactive at start");
            gameObject.SetActive(false);
        }

        public void ShowGladiatorEquipment(GladiatorInstance gladiator)
        {
            Debug.Log($"=== ShowGladiatorEquipment called for {gladiator?.templateData?.gladiatorName ?? "NULL"} ===");
            if (gladiator == null)
            {
                Debug.LogError("Gladiator parameter is NULL!");
                return;
            }

            Transform parent = transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                {
                    Debug.LogError($"Parent {parent.name} is inactive! Panel won't show.");
                }
                parent = parent.parent;
            }

            selectedGladiator = gladiator;
            Debug.Log($"Setting panel active: {gameObject.name}");
            gameObject.SetActive(true);
            Debug.Log($"Panel is now active: {gameObject.activeInHierarchy}");

            UpdateDisplay();
            PopulateDropdowns();
            Debug.Log("=== ShowGladiatorEquipment complete ===");
        }

        public void Hide()
        {
            selectedGladiator = null;
            gameObject.SetActive(false);
        }

        private void UpdateDisplay()
        {
            if (selectedGladiator == null)
            {
                return;
            }

            if (gladiatorNameText != null)
            {
                gladiatorNameText.text = $"{selectedGladiator.templateData.gladiatorName} - Equipment";
            }

            if (gladiatorStatsText != null)
            {
                gladiatorStatsText.text = $"HP: {selectedGladiator.currentHP}/{selectedGladiator.maxHP}\n" +
                                          $"Status: {selectedGladiator.GetStatusString()}";
            }

            if (equippedWeaponText != null)
            {
                equippedWeaponText.text = selectedGladiator.equippedWeapon != null
                    ? $"Weapon: {selectedGladiator.equippedWeapon.weaponName}"
                    : "Weapon: None";
            }

            if (equippedArmorText != null)
            {
                equippedArmorText.text = selectedGladiator.equippedArmor != null
                    ? $"Armor: {selectedGladiator.equippedArmor.armorName}"
                    : "Armor: None";
            }

            if (knownSpellsText != null)
            {
                int spellCount = 0;
                foreach (SpellData spell in selectedGladiator.knownSpells)
                {
                    if (spell != null)
                    {
                        spellCount++;
                    }
                }
                knownSpellsText.text = $"Spells: {spellCount}/9";
            }
        }

        private void PopulateDropdowns()
        {
            if (dataManager == null || selectedGladiator == null)
            {
                return;
            }

            if (weaponDropdown != null)
            {
                weaponDropdown.ClearOptions();
                List<string> weaponOptions = new List<string> { "None" };

                foreach (WeaponData weapon in dataManager.ownedWeapons)
                {
                    if (weapon != null)
                    {
                        weaponOptions.Add(weapon.weaponName);
                    }
                }

                weaponDropdown.AddOptions(weaponOptions);

                if (selectedGladiator.equippedWeapon != null)
                {
                    int index = dataManager.ownedWeapons.IndexOf(selectedGladiator.equippedWeapon) + 1;
                    weaponDropdown.value = Mathf.Max(0, index);
                }
                else
                {
                    weaponDropdown.value = 0;
                }
            }

            if (armorDropdown != null)
            {
                armorDropdown.ClearOptions();
                List<string> armorOptions = new List<string> { "None" };

                foreach (ArmorData armor in dataManager.ownedArmors)
                {
                    if (armor != null)
                    {
                        armorOptions.Add(armor.armorName);
                    }
                }

                armorDropdown.AddOptions(armorOptions);

                if (selectedGladiator.equippedArmor != null)
                {
                    int index = dataManager.ownedArmors.IndexOf(selectedGladiator.equippedArmor) + 1;
                    armorDropdown.value = Mathf.Max(0, index);
                }
                else
                {
                    armorDropdown.value = 0;
                }
            }

            if (spellDropdown != null)
            {
                spellDropdown.ClearOptions();
                List<string> spellOptions = new List<string> { "None" };

                foreach (SpellData spell in dataManager.ownedSpells)
                {
                    if (spell != null)
                    {
                        spellOptions.Add(spell.spellName);
                    }
                }

                spellDropdown.AddOptions(spellOptions);
                spellDropdown.value = 0;
            }
        }

        private void OnEquipWeapon()
        {
            if (selectedGladiator == null || weaponDropdown == null || dataManager == null)
            {
                return;
            }

            int index = weaponDropdown.value - 1;

            if (index < 0)
            {
                selectedGladiator.EquipWeapon(null);
                Debug.Log($"{selectedGladiator.templateData.gladiatorName} unequipped weapon");
            }
            else if (index < dataManager.ownedWeapons.Count)
            {
                WeaponData weapon = dataManager.ownedWeapons[index];
                selectedGladiator.EquipWeapon(weapon);
                Debug.Log($"{selectedGladiator.templateData.gladiatorName} equipped {weapon.weaponName}");
            }

            UpdateDisplay();
            RefreshRoster();
        }

        private void OnEquipArmor()
        {
            if (selectedGladiator == null || armorDropdown == null || dataManager == null)
            {
                return;
            }

            int index = armorDropdown.value - 1;

            if (index < 0)
            {
                selectedGladiator.EquipArmor(null);
                Debug.Log($"{selectedGladiator.templateData.gladiatorName} unequipped armor");
            }
            else if (index < dataManager.ownedArmors.Count)
            {
                ArmorData armor = dataManager.ownedArmors[index];
                selectedGladiator.EquipArmor(armor);
                Debug.Log($"{selectedGladiator.templateData.gladiatorName} equipped {armor.armorName}");
            }

            UpdateDisplay();
            RefreshRoster();
        }

        private void OnLearnSpell()
        {
            if (selectedGladiator == null || spellDropdown == null || dataManager == null)
            {
                return;
            }

            int index = spellDropdown.value - 1;

            if (index >= 0 && index < dataManager.ownedSpells.Count)
            {
                SpellData spell = dataManager.ownedSpells[index];

                for (int i = 0; i < selectedGladiator.knownSpells.Length; i++)
                {
                    if (selectedGladiator.knownSpells[i] == null)
                    {
                        selectedGladiator.LearnSpell(spell, i);
                        Debug.Log($"{selectedGladiator.templateData.gladiatorName} learned {spell.spellName} in slot {i + 1}");
                        UpdateDisplay();
                        return;
                    }
                }

                Debug.LogWarning($"{selectedGladiator.templateData.gladiatorName} already knows 9 spells!");
            }
        }

        private void RefreshRoster()
        {
            RosterView rosterView = FindFirstObjectByType<RosterView>();
            if (rosterView != null)
            {
                rosterView.RefreshRoster();
            }
        }
    }
}
