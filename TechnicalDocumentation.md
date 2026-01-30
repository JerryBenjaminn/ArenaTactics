# Arena Tactics - Technical Documentation
*Version 1.0 - January 2026*

---

## 1. PROJECT OVERVIEW

**Engine:** Unity 6.3  
**Language:** C#  
**Architecture:** Manager-based with ScriptableObject data  
**Scope:** Turn-based tactical gladiator combat with roguelike progression

**Key Features:**
- Turn-based tactical combat on a grid
- Procedural gladiator generation (quality + stat variance)
- Equipment shop (weapons, armor, spells)
- Shop-based progression (recruit, manage, equip)
- Injury/Death/Decay mechanics
- Battle → Shop persistence loop

---

## 2. ARCHITECTURE

### 2.1 Core Managers

**PersistentDataManager (Singleton)**  
- Location: `Assets/Scripts/Managers/PersistentDataManager.cs`  
- Purpose: Persistent game state across scenes  
- Responsibilities:
  - Gold economy (earn, spend, track)
  - Gladiator roster management (`List<GladiatorInstance>`)
  - Active squad selection (max 5)
  - Battle count progression
  - Inventory management (owned weapons/armors/spells)
  - Post-battle effects (injury/decay countdowns)
- Lifecycle: `DontDestroyOnLoad`, persists entire session  
- Key Methods:
  - `AddGold(int amount)` / `SpendGold(int amount)`
  - `AddGladiatorToRoster(GladiatorInstance)`
  - `SetActiveSquad(List<GladiatorInstance>)`
  - `OnBattleComplete(bool victory, int reward)`
  - `ProcessPostBattleEffects(string caller)`
  - `AddWeaponToInventory(WeaponData)` / `AddArmorToInventory(ArmorData)` / `AddSpellToInventory(SpellData)`

**ShopManager**  
- Location: `Assets/Scripts/Managers/ShopManager.cs`  
- Purpose: Shop UI coordination and tab management  
- Responsibilities:
  - Tab system (Recruit / Manage / Equipment)
  - Display updates (gold, battle count, squad count)
  - Tab switching and panel visibility
- Key Methods:
  - `SwitchTab(ShopTab)`
  - `RefreshGoldDisplay()`
  - `RefreshSquadCount()`

**BattleManager**  
- Location: `Assets/Scripts/Managers/BattleManager.cs`  
- Purpose: Combat flow and turn management  
- Responsibilities:
  - Turn order and combat phases
  - Victory/defeat detection
  - Save battle results back to `PersistentDataManager`
- Key Methods:
  - `Initialize(List<Gladiator>)`
  - `StartDeploymentPhase()`
  - `EndTurn()`
  - `CheckVictoryConditions()`
  - `SaveBattleResultsToShop(PersistentDataManager)`

**BattleSetupTest**  
- Location: `Assets/Scripts/Managers/BattleSetupTest.cs`  
- Purpose: Load squad into battle scene  
- Responsibilities:
  - Spawn player gladiators from `activeSquad`
  - Spawn enemy gladiators (static for now)
  - Initialize battle scene
- Key Methods:
  - `SpawnPlayerSquad(List<GladiatorInstance>)`
  - `SpawnPlayerGladiator(GladiatorInstance, Vector2Int)`

**GladiatorGenerator**  
- Location: `Assets/Scripts/Managers/GladiatorGenerator.cs`  
- Purpose: Procedural gladiator generation  
- Responsibilities:
  - Random class/race selection
  - Quality tier application (stat variance)
  - Price calculation (stat-based)
  - Tier progression (battle count → better offers)
- Key Methods:
  - `GenerateGladiator(int level, GladiatorQuality quality)`
  - `GenerateShopPool(int battleCount)`
  - `ApplyQualityVariance(GladiatorInstance, GladiatorQuality)`
  - `CalculatePrice(GladiatorInstance, GladiatorQuality)`

---

### 2.2 UI Systems

**RosterView**  
- Location: `Assets/Scripts/UI/RosterView.cs`  
- Purpose: Display player's gladiator roster  
- Responsibilities:
  - Display all owned gladiators
  - Squad selection (toggle, max 5)
  - Status display (Healthy / Injured / Dead)
  - Refresh on roster changes or when panel is enabled
- Key Methods:
  - `RefreshRoster()`
  - `ToggleSquadSelection(GladiatorInstance)`
  - `OnConfirmSquad()`

**RecruitmentView**  
- Location: `Assets/Scripts/UI/RecruitmentView.cs`  
- Purpose: Display and purchase generated gladiators  
- Responsibilities:
  - Generate 5 recruitment offers (via `GladiatorGenerator`)
  - Display offer cards
  - Process purchases (gold deduction, add to roster)
- Key Methods:
  - `RefreshOffers()`
  - `PurchaseGladiator(RecruitmentOffer)`

**EquipmentShopView**  
- Location: `Assets/Scripts/UI/EquipmentShopView.cs`  
- Purpose: Browse and purchase equipment  
- Responsibilities:
  - Category switching (Weapons / Armors / Spells)
  - Display all available items
  - Process purchases (gold deduction, add to inventory)
  - Show "OWNED" indicator on items already purchased
- Key Methods:
  - `ShowCategory(ItemCategory)`
  - `PurchaseWeapon(WeaponData, int)`
  - `PurchaseArmor(ArmorData, int)`
  - `PurchaseSpell(SpellData, int)`

**GladiatorEquipmentPanel**  
- Location: `Assets/Scripts/UI/GladiatorEquipmentPanel.cs`  
- Purpose: Equip items to gladiators  
- Responsibilities:
  - Display selected gladiator equipment
  - Populate dropdowns with owned items
  - Equip weapon/armor, learn spells
  - Update stats on equip
- Key Methods:
  - `ShowGladiatorEquipment(GladiatorInstance)`
  - `OnEquipWeapon()` / `OnEquipArmor()`
  - `OnLearnSpell()`

---

## 3. DATA STRUCTURES

### 3.1 Runtime Data

**GladiatorInstance**  
- Location: `Assets/Scripts/Data/GladiatorInstance.cs`  
- Purpose: Runtime state of a gladiator  
- Fields:
  - `string instanceID`
  - `GladiatorData templateData`
  - `int currentLevel, currentXP`
  - `int currentHP, maxHP`
  - `GladiatorStatus status`
  - `int injuryBattlesRemaining`
  - `int decayBattlesRemaining`
  - `bool isAscended`
  - `WeaponData equippedWeapon`
  - `ArmorData equippedArmor`
  - `SpellData[] knownSpells`
- Key Methods:
  - `CalculateMaxHP()`
  - `EquipWeapon(WeaponData)`
  - `EquipArmor(ArmorData)`
  - `LearnSpell(SpellData, int slot)`
  - `CanFight()`
  - `GetStatusString()`
  - `GetStatusColor()`

**RecruitmentOffer**  
- Location: `Assets/Scripts/Data/RecruitmentOffer.cs`  
- Purpose: Encapsulate a shop recruitment offer  
- Fields:
  - `GladiatorInstance gladiator`
  - `int price`
  - `GladiatorQuality quality`
  - `bool purchased`

---

### 3.2 ScriptableObject Data

**GladiatorData**  
- Location: `Assets/Scripts/Data/GladiatorData.cs`  
- Purpose: Base template for gladiator stats  
- Fields:
  - `string gladiatorName`
  - `GladiatorClass gladiatorClass`
  - `RaceData race`
  - Modifiers: `hpModifier, strengthModifier, dexterityModifier, intelligenceModifier, defenseModifier, speedModifier`
  - `Team team`
  - `WeaponData startingWeapon`, `ArmorData startingArmor`, `List<SpellData> startingSpells`

**GladiatorClass**  
- Location: `Assets/Scripts/Data/GladiatorClass.cs`  
- Fields:
  - `string className`
  - Base stats: `baseHP, baseStrength, baseDexterity, baseIntelligence, baseDefense, baseSpeed`
  - Action points: `baseMovementPoints, baseActionPoints`
  - Growth rates: `hpGrowth, strengthGrowth, dexterityGrowth, intelligenceGrowth, defenseGrowth, speedGrowth`

**RaceData**  
- Location: `Assets/Scripts/Data/RaceData.cs`  
- Fields:
  - `string raceName`
  - Modifiers: `hpModifier, strengthModifier, dexterityModifier, intelligenceModifier, defenseModifier, speedModifier`
  - Special traits: `xpBonusMultiplier, meleeDamageBonus, magicResistBonus, dodgeBonus, spellPowerBonus`, etc.
  - Poison and regeneration flags: `hasPoisonOnHit, poisonChance, poisonDamagePerTurn, hpRegenPerTurn`

**WeaponData**  
- Location: `Assets/Scripts/Data/WeaponData.cs`  
- Fields:
  - `string weaponName`
  - `WeaponType weaponType`
  - `DamageType damageType`
  - `int baseDamage`
  - `int range`, `int attackRange`, `int actionPointCost`
  - Bonuses: `strengthBonus, dexterityBonus, intelligenceBonus, defenseBonus`
  - Derived: `accuracyBonus, critBonus, spellSlotBonus, spellPowerBonus`
  - Economy: `int cost`, `int weaponTier`

**ArmorData**  
- Location: `Assets/Scripts/Data/ArmorData.cs`  
- Fields:
  - `string armorName`
  - `ArmorType armorType`
  - Bonuses: `hpBonus, defenseBonus, strengthBonus, dexterityBonus, intelligenceBonus`
  - `float dodgeBonus`
  - `int movementPenalty`
  - `float spellPowerBonus`, `int spellSlotBonus`
  - Economy: `int cost`, `int tier`

**SpellData**  
- Location: `Assets/Scripts/Data/SpellData.cs`  
- Fields:
  - `string spellName`
  - `SpellType spellType`
  - `int basePower`
  - `SpellScalingStat scalingStat`
  - `int range`, `int apCost`, `int spellSlotCost`, `int cooldownTurns`
  - AOE: `int aoeRadius`
  - Effects: `EffectType effectType, int effectValue`
  - Secondary: `EffectType secondaryEffectType, int secondaryEffectValue`
  - Economy: `int cost`, `int tier`

---

### 3.3 Enums

**GladiatorStatus**  
- Location: `Assets/Scripts/Data/GladiatorInstance.cs`  
- Values: `Healthy, Injured, Dead`

**GladiatorQuality**  
- Location: `Assets/Scripts/Data/GladiatorQuality.cs`  
- Values: `Poor, Average, Good, Excellent`  
- Default modifiers are defined in `GladiatorGenerator.InitializeQualityTable()`.

**WeaponType**  
- Location: `Assets/Scripts/Data/WeaponData.cs`  
- Values: `Melee, Ranged, Magic`

**ArmorType**  
- Location: `Assets/Scripts/Data/ArmorData.cs`  
- Values: `Light, Medium, Heavy, Robes`

**SpellType / EffectType**  
- Location: `Assets/Scripts/Data/SpellData.cs`  
- `SpellType`: `Damage, AOE, Debuff, Buff`  
- `EffectType`: `None, Damage, Heal, StrengthDebuff, DefenseDebuff, SpeedDebuff, MovementDebuff, Stun, StrengthBuff, DefenseBuff, SpeedBuff, MovementBuff, ImmunityBuff`

---

## 4. GAME FLOW

### 4.1 Shop → Battle Flow

1. **Shop Scene Load**
   - `PersistentDataManager` loads (singleton)
   - `ShopManager` initializes (gold, battle count, squad)
   - `RosterView` displays roster (Manage tab)
   - `RecruitmentView` generates 5 offers (Recruit tab)

2. **Player Actions**
   - **Recruit tab:** purchase gladiators → added to roster
   - **Manage tab:** select squad (max 5) → activeSquad updated
   - **Equipment tab:** purchase equipment → added to inventory, equip via Manage tab

3. **Start Battle**
   - Validates: active squad > 0
   - Loads Battle scene
   - `BattleSetupTest` spawns gladiators from `activeSquad`

### 4.2 Battle → Shop Flow

1. **Battle Resolution**
   - Victory or defeat
   - `BattleManager.SaveBattleResultsToShop()` copies battle state to `GladiatorInstance`
   - Survivors heal to full; defeated retain HP and status

2. **Post-Battle Processing**
   - `PersistentDataManager.OnBattleComplete()`:
     - increments battle count
     - awards gold
     - `ProcessPostBattleEffects()` (injury/decay countdowns)
     - loads Shop scene

3. **Shop Scene Return**
   - `RosterView` refreshes automatically on enable
   - Recruitment offers regenerate on shop load

---

## 5. KEY SYSTEMS

### 5.1 Injury & Death

- Trigger: Gladiator HP reaches 0 in combat.
- Overkill damage is computed from negative HP before clamping.
- `DetermineDeathOrInjury()` applies:
  - **Dead** if overkill exceeds threshold
  - **Injured** otherwise (with injury duration)
- Injury duration is based on overkill ratio.
- `PersistentDataManager.ProcessPostBattleEffects()` decrements injury counters once per battle cycle.

### 5.2 Undead Decay

- Undead gladiators use `decayBattlesRemaining` instead of injury.
- Each battle decrements decay; defeat applies additional decay.
- When decay reaches 0 → status becomes Dead.

### 5.3 Procedural Gladiator Generation

- Random class + race
- Quality-based stat variance applied to template modifiers
- Tiered pool generation based on battle count
- Pricing uses base tier price + stat-derived adjustments

### 5.4 Equipment System

**Purchase Flow**
1. Buy item in Equipment tab
2. Gold deducted (via `SpendGold`)
3. Item added to inventory (`ownedWeapons/ownedArmors/ownedSpells`)
4. Card shows `OWNED` and buy button is disabled

**Equipping Flow**
1. Manage tab → select gladiator → Equip panel
2. Select weapon/armor from owned inventory dropdowns
3. Equip item, update stats and roster display

---

## 6. TESTING & DEBUGGING

### 6.1 Debug Features

- `GladiatorGenerator` has a context menu test generator.
- Equipment shop includes runtime container debug hooks.
- RosterView auto-refreshes on enable and logs card creation.

### 6.2 Common Issues

**Equipment cards overlapping**
- Ensure the assigned container has `GridLayoutGroup`.
- The container reference in `EquipmentShopView` must point to the object that owns the grid.

**Equip panel not opening**
- Ensure `GladiatorEquipmentPanel` exists in the scene and is under the Manage tab hierarchy.

---

## 7. ASSET ORGANIZATION

Assets/  
├── Scenes/  
│   ├── Shop.unity  
│   └── Battle.unity  
├── Scripts/  
│   ├── Combat/  
│   ├── Data/  
│   ├── Gladiators/  
│   ├── Managers/  
│   └── UI/  
├── ScriptableObjects/  
│   ├── Gladiators/  
│   ├── Weapons/  
│   ├── Armors/  
│   ├── Spells/  
│   └── (Classes/Races/etc.)  
└── Prefabs/  
    └── UI/

---

## 8. FUTURE ENHANCEMENTS

- Enemy team procedural generation
- Save/load system (beyond session persistence)
- Equipment crafting/upgrading
- Tournament progression
- UI scaling and performance optimization

---

**END OF TECHNICAL DOCUMENTATION**
