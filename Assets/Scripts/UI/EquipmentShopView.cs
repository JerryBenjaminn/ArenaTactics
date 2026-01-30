using UnityEngine;
using UnityEngine.UI;
using ArenaTactics.Data;
using ArenaTactics.Managers;

namespace ArenaTactics.UI
{
    public class EquipmentShopView : MonoBehaviour
    {
        [Header("UI References")]
        public Transform weaponsContainer;
        public Transform armorsContainer;
        public Transform spellsContainer;
        public GameObject itemCardPrefab;

        [Header("Category Toggles")]
        public Button showWeaponsButton;
        public Button showArmorsButton;
        public Button showSpellsButton;
        public GameObject weaponsPanel;
        public GameObject armorsPanel;
        public GameObject spellsPanel;

        [Header("Data Sources")]
        public WeaponData[] availableWeapons;
        public ArmorData[] availableArmors;
        public SpellData[] availableSpells;

        private PersistentDataManager dataManager;
        private ItemCategory currentCategory = ItemCategory.Weapons;

        private enum ItemCategory { Weapons, Armors, Spells }

        private void Awake()
        {
            if (weaponsPanel != null)
            {
                weaponsPanel.SetActive(true);
            }

            if (armorsPanel != null)
            {
                armorsPanel.SetActive(true);
            }

            if (spellsPanel != null)
            {
                spellsPanel.SetActive(true);
            }

            if (weaponsContainer == null)
            {
                Debug.LogError("EquipmentShopView: weaponsContainer not assigned in Inspector!");
            }
            if (armorsContainer == null)
            {
                Debug.LogError("EquipmentShopView: armorsContainer not assigned in Inspector!");
            }
            if (spellsContainer == null)
            {
                Debug.LogError("EquipmentShopView: spellsContainer not assigned in Inspector!");
            }
        }

        private void Start()
        {
            dataManager = PersistentDataManager.Instance;

            if (showWeaponsButton != null)
            {
                showWeaponsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Weapons));
            }

            if (showArmorsButton != null)
            {
                showArmorsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Armors));
            }

            if (showSpellsButton != null)
            {
                showSpellsButton.onClick.AddListener(() => ShowCategory(ItemCategory.Spells));
            }

            ShowCategory(ItemCategory.Weapons);
        }

        private void ShowCategory(ItemCategory category)
        {
            currentCategory = category;

            Debug.Log($"=== ShowCategory({category}) ===");
            Debug.Log($"weaponsContainer exists: {weaponsContainer != null}, gameObject: {weaponsContainer?.gameObject.name}");
            Debug.Log($"armorsContainer exists: {armorsContainer != null}, gameObject: {armorsContainer?.gameObject.name}");
            Debug.Log($"spellsContainer exists: {spellsContainer != null}, gameObject: {spellsContainer?.gameObject.name}");

            if (weaponsPanel != null)
            {
                Debug.Log($"Setting weaponsPanel active: {category == ItemCategory.Weapons}");
                weaponsPanel.SetActive(category == ItemCategory.Weapons);
                if (weaponsContainer != null)
                {
                    Debug.Log($"After toggle - weaponsContainer still exists: {weaponsContainer.gameObject.activeInHierarchy}");
                }
            }

            if (armorsPanel != null)
            {
                Debug.Log($"Setting armorsPanel active: {category == ItemCategory.Armors}");
                armorsPanel.SetActive(category == ItemCategory.Armors);
            }

            if (spellsPanel != null)
            {
                Debug.Log($"Setting spellsPanel active: {category == ItemCategory.Spells}");
                spellsPanel.SetActive(category == ItemCategory.Spells);
            }

            UpdateCategoryButtons();
            PopulateItems();
        }

        private void UpdateCategoryButtons()
        {
            Color activeColor = new Color(0.3f, 0.6f, 1f);
            Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);

            if (showWeaponsButton != null)
            {
                ColorBlock colors = showWeaponsButton.colors;
                colors.normalColor = currentCategory == ItemCategory.Weapons ? activeColor : inactiveColor;
                showWeaponsButton.colors = colors;
            }

            if (showArmorsButton != null)
            {
                ColorBlock colors = showArmorsButton.colors;
                colors.normalColor = currentCategory == ItemCategory.Armors ? activeColor : inactiveColor;
                showArmorsButton.colors = colors;
            }

            if (showSpellsButton != null)
            {
                ColorBlock colors = showSpellsButton.colors;
                colors.normalColor = currentCategory == ItemCategory.Spells ? activeColor : inactiveColor;
                showSpellsButton.colors = colors;
            }
        }

        private void PopulateItems()
        {
            switch (currentCategory)
            {
                case ItemCategory.Weapons:
                    ClearContainer(weaponsContainer);
                    DebugContainer(weaponsContainer, "Weapons");
                    int weaponIndex = 0;
                    if (availableWeapons != null)
                    {
                        foreach (WeaponData weapon in availableWeapons)
                        {
                            if (weapon != null)
                            {
                                CreateWeaponCard(weapon);
                                weaponIndex++;
                                if (weaponIndex <= 3)
                                {
                                    DebugCardPosition(weaponsContainer, weaponIndex, weapon.weaponName);
                                }
                            }
                        }
                    }
                    Debug.Log($"Total weapon cards: {(weaponsContainer != null ? weaponsContainer.childCount : 0)}");
                    ForceLayoutRebuild(weaponsContainer);
                    break;
                case ItemCategory.Armors:
                    ClearContainer(armorsContainer);
                    DebugContainer(armorsContainer, "Armors");
                    int armorIndex = 0;
                    if (availableArmors != null)
                    {
                        foreach (ArmorData armor in availableArmors)
                        {
                            if (armor != null)
                            {
                                CreateArmorCard(armor);
                                armorIndex++;
                                if (armorIndex <= 3)
                                {
                                    DebugCardPosition(armorsContainer, armorIndex, armor.armorName);
                                }
                            }
                        }
                    }
                    Debug.Log($"Total armor cards: {(armorsContainer != null ? armorsContainer.childCount : 0)}");
                    ForceLayoutRebuild(armorsContainer);
                    break;
                case ItemCategory.Spells:
                    ClearContainer(spellsContainer);
                    DebugContainer(spellsContainer, "Spells");
                    int spellIndex = 0;
                    if (availableSpells != null)
                    {
                        foreach (SpellData spell in availableSpells)
                        {
                            if (spell != null)
                            {
                                CreateSpellCard(spell);
                                spellIndex++;
                                if (spellIndex <= 3)
                                {
                                    DebugCardPosition(spellsContainer, spellIndex, spell.spellName);
                                }
                            }
                        }
                    }
                    Debug.Log($"Total spell cards: {(spellsContainer != null ? spellsContainer.childCount : 0)}");
                    ForceLayoutRebuild(spellsContainer);
                    break;
            }
        }

        private void ClearContainer(Transform container)
        {
            if (container == null)
            {
                Debug.LogWarning("ClearContainer called with null container!");
                return;
            }

            Debug.Log($"Clearing container: {container.name}, child count: {container.childCount}");
            foreach (Transform child in container)
            {
                Debug.Log($"  Destroying child: {child.name}");
                Destroy(child.gameObject);
            }
            Debug.Log($"Container cleared. Remaining children: {container.childCount}");
        }

        private void CreateWeaponCard(WeaponData weapon)
        {
            if (itemCardPrefab == null || weaponsContainer == null)
            {
                if (weaponsContainer == null)
                {
                    Debug.LogError("weaponsContainer is NULL!");
                }
                return;
            }

            GameObject card = Instantiate(itemCardPrefab, weaponsContainer);
            if (card.transform.parent != weaponsContainer)
            {
                Debug.LogError($"Weapon card parent wrong! Expected {weaponsContainer.name}, got {card.transform.parent?.name}");
            }
            EquipmentItemCard cardScript = card.GetComponent<EquipmentItemCard>();

            if (cardScript != null)
            {
                cardScript.SetupWeapon(weapon, this);
            }
            else
            {
                Debug.LogError("Card missing EquipmentItemCard component!");
            }
        }

        private void CreateArmorCard(ArmorData armor)
        {
            if (itemCardPrefab == null || armorsContainer == null)
            {
                if (armorsContainer == null)
                {
                    Debug.LogError("armorsContainer is NULL!");
                }
                return;
            }

            GameObject card = Instantiate(itemCardPrefab, armorsContainer);
            if (card.transform.parent != armorsContainer)
            {
                Debug.LogError($"Armor card parent wrong! Expected {armorsContainer.name}, got {card.transform.parent?.name}");
            }
            EquipmentItemCard cardScript = card.GetComponent<EquipmentItemCard>();

            if (cardScript != null)
            {
                cardScript.SetupArmor(armor, this);
            }
            else
            {
                Debug.LogError("Card missing EquipmentItemCard component!");
            }
        }

        private void CreateSpellCard(SpellData spell)
        {
            if (itemCardPrefab == null || spellsContainer == null)
            {
                if (spellsContainer == null)
                {
                    Debug.LogError("spellsContainer is NULL!");
                }
                return;
            }

            GameObject card = Instantiate(itemCardPrefab, spellsContainer);
            if (card.transform.parent != spellsContainer)
            {
                Debug.LogError($"Spell card parent wrong! Expected {spellsContainer.name}, got {card.transform.parent?.name}");
            }
            EquipmentItemCard cardScript = card.GetComponent<EquipmentItemCard>();

            if (cardScript != null)
            {
                cardScript.SetupSpell(spell, this);
            }
            else
            {
                Debug.LogError("Card missing EquipmentItemCard component!");
            }
        }

        public void PurchaseWeapon(WeaponData weapon, int price)
        {
            if (weapon == null || dataManager == null)
            {
                return;
            }

            if (dataManager.SpendGold(price))
            {
                dataManager.AddWeaponToInventory(weapon);
                Debug.Log($"Purchased weapon: {weapon.weaponName} for {price}g");
                RefreshGold();
                PopulateItems();
            }
            else
            {
                Debug.LogWarning("Not enough gold!");
            }
        }

        public void PurchaseArmor(ArmorData armor, int price)
        {
            if (armor == null || dataManager == null)
            {
                return;
            }

            if (dataManager.SpendGold(price))
            {
                dataManager.AddArmorToInventory(armor);
                Debug.Log($"Purchased armor: {armor.armorName} for {price}g");
                RefreshGold();
                PopulateItems();
            }
            else
            {
                Debug.LogWarning("Not enough gold!");
            }
        }

        public void PurchaseSpell(SpellData spell, int price)
        {
            if (spell == null || dataManager == null)
            {
                return;
            }

            if (dataManager.SpendGold(price))
            {
                dataManager.AddSpellToInventory(spell);
                Debug.Log($"Purchased spell: {spell.spellName} for {price}g");
                RefreshGold();
                PopulateItems();
            }
            else
            {
                Debug.LogWarning("Not enough gold!");
            }
        }

        private void RefreshGold()
        {
            ShopManager shopManager = FindFirstObjectByType<ShopManager>();
            if (shopManager != null)
            {
                shopManager.RefreshGoldDisplay();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                Debug.Log("=== RUNTIME CONTAINER CHECK ===");
                Debug.Log($"weaponsContainer: {(weaponsContainer != null ? weaponsContainer.name : "NULL")}");
                Debug.Log($"armorsContainer: {(armorsContainer != null ? armorsContainer.name : "NULL")}");
                Debug.Log($"spellsContainer: {(spellsContainer != null ? spellsContainer.name : "NULL")}");

                if (weaponsPanel != null)
                {
                    Debug.Log("weaponsPanel children:");
                    for (int i = 0; i < weaponsPanel.transform.childCount; i++)
                    {
                        Debug.Log($"  [{i}] {weaponsPanel.transform.GetChild(i).name}");
                    }
                }
            }
        }

        [ContextMenu("Debug All Containers")]
        private void DebugAllContainers()
        {
            Debug.Log("========================================");
            Debug.Log("EQUIPMENT SHOP CONTAINER DEBUG");
            Debug.Log("========================================");

            DebugContainer(weaponsContainer, "Weapons");
            Debug.Log(string.Empty);
            DebugContainer(armorsContainer, "Armors");
            Debug.Log(string.Empty);
            DebugContainer(spellsContainer, "Spells");

            Debug.Log("========================================");
        }

        private void DebugContainer(Transform container, string categoryName)
        {
            if (container == null)
            {
                Debug.LogError($"{categoryName}Container is NULL!");
                return;
            }

            Debug.Log($"=== {categoryName} Container Debug ===");

            RectTransform rt = container.GetComponent<RectTransform>();
            Debug.Log($"  Name: {container.name}");
            Debug.Log($"  Active: {container.gameObject.activeInHierarchy}");
            if (rt != null)
            {
                Debug.Log($"  RectTransform: anchorMin={rt.anchorMin}, anchorMax={rt.anchorMax}");
                Debug.Log($"  Size: {rt.rect.width} x {rt.rect.height}");
            }

            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid == null)
            {
                Debug.LogError($"  {categoryName}Container is MISSING GridLayoutGroup component!");
            }
            else
            {
                Debug.Log($"  GridLayout: enabled={grid.enabled}");
                Debug.Log($"  CellSize: {grid.cellSize}");
                Debug.Log($"  Spacing: {grid.spacing}");
                Debug.Log($"  Constraint: {grid.constraint}");
                Debug.Log($"  ConstraintCount: {grid.constraintCount}");
                Debug.Log($"  StartCorner: {grid.startCorner}");
                Debug.Log($"  ChildAlignment: {grid.childAlignment}");
            }

            ContentSizeFitter csf = container.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                Debug.Log($"  ContentSizeFitter: H={csf.horizontalFit}, V={csf.verticalFit}");
            }
        }

        private void DebugCardPosition(Transform container, int index, string itemName)
        {
            if (container != null && container.childCount >= index)
            {
                Transform card = container.GetChild(index - 1);
                RectTransform rt = card.GetComponent<RectTransform>();
                Debug.Log($"  Card {index} ({itemName}): LocalPos={card.localPosition}, AnchoredPos={rt.anchoredPosition}");
            }
        }

        private void ForceLayoutRebuild(Transform container)
        {
            RectTransform rt = container != null ? container.GetComponent<RectTransform>() : null;
            if (rt == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            Debug.Log($"  Forced layout rebuild on {container.name}");
        }
    }
}
