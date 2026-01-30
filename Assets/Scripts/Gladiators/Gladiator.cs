using System.Collections;
using System.Collections.Generic;
using ArenaTactics.Data;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime representation of a gladiator on the tactical grid.
/// Handles stats, placement, movement range and visual feedback.
/// </summary>
public class Gladiator : MonoBehaviour
{

    [Header("Data")]
    [SerializeField]
    private GladiatorData data;

    [Header("Weapon")]
    [SerializeField]
    private WeaponData equippedWeapon;

    [Header("Armor")]
    [SerializeField]
    private ArmorData equippedArmor;

    [SerializeField]
    private int baseAttackRange = 1;

    [Header("Movement")]
    [SerializeField]
    private bool isMoving;

    [SerializeField]
    private Vector3 moveStartPosition;

    [SerializeField]
    private Vector3 moveTargetPosition;

    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private float moveLerpProgress;

    [Header("Grid")]
    [SerializeField]
    private Vector2Int currentGridPosition;

    [SerializeField]
    private GridManager gridManager;

    [Header("Turn Resources")]
    [SerializeField]
    private int remainingMP;

    [SerializeField]
    private int remainingAP;

    [Header("Control")]
    [SerializeField]
    private bool isPlayerControlled;

    [Header("Visuals")]
    [SerializeField]
    private Material originalMaterial;

    [Header("UI")]
    private HealthBarUI healthBar;

    [Header("Progression")]
    [SerializeField]
    private int currentLevel = 1;

    [SerializeField]
    private int currentXP;

    private const int MaxLevel = 10;

    [Header("Spells")]
    [SerializeField]
    private List<SpellData> knownSpells = new List<SpellData>();

    [SerializeField]
    private int currentSpellSlots;

    private readonly Dictionary<SpellData, int> spellCooldowns = new Dictionary<SpellData, int>();

    /// <summary>
    /// Current hit points for this gladiator instance.
    /// </summary>
    [SerializeField]
    private int currentHP;

    [Header("Status")]
    [SerializeField]
    private GladiatorStatus status = GladiatorStatus.Healthy;

    [SerializeField]
    private int injuryBattlesRemaining;

    [SerializeField]
    private int decayBattlesRemaining = -1;

    [SerializeField]
    private int startingDecayBattles = -1;

    [SerializeField]
    private bool isAscended;

    [SerializeField]
    private string ascendedFormName = string.Empty;

    [System.NonSerialized]
    public GladiatorInstance linkedInstance;

    // Cached highlight data so we can restore tiles after highlighting.
    private readonly Dictionary<GridCell, Material> highlightedCells = new Dictionary<GridCell, Material>();
    private readonly Dictionary<Gladiator, int> damageContributors = new Dictionary<Gladiator, int>();
    private readonly Dictionary<EffectType, ActiveEffect> activeEffects = new Dictionary<EffectType, ActiveEffect>();
    private int poisonTurnsRemaining;
    private int poisonDamagePerTurn;
    private Gladiator poisonSource;

    /// <summary>
    /// Gets this gladiator's data asset.
    /// </summary>
    public GladiatorData Data => data;

    /// <summary>
    /// Gets the weapon currently equipped by this gladiator.
    /// </summary>
    public WeaponData EquippedWeapon => equippedWeapon;

    /// <summary>
    /// Gets the armor currently equipped by this gladiator.
    /// </summary>
    public ArmorData EquippedArmor => equippedArmor;

    public GladiatorStatus Status => status;

    public int InjuryBattlesRemaining => injuryBattlesRemaining;

    public int DecayBattlesRemaining => decayBattlesRemaining;

    public int StartingDecayBattles => startingDecayBattles;

    public bool IsAscended => isAscended;

    public string AscendedFormName => ascendedFormName;

    /// <summary>
    /// Gets the gladiator's current grid position.
    /// </summary>
    public Vector2Int CurrentGridPosition => currentGridPosition;

    /// <summary>
    /// Gets whether this gladiator is controlled by the player.
    /// </summary>
    public bool IsPlayerControlled => isPlayerControlled;

    /// <summary>
    /// Gets the current hit points for this gladiator instance.
    /// </summary>
    public int CurrentHP => currentHP;

    /// <summary>
    /// Gets the current level for this gladiator.
    /// </summary>
    public int CurrentLevel => currentLevel;

    /// <summary>
    /// Gets the current XP for this gladiator.
    /// </summary>
    public int CurrentXP => currentXP;

    /// <summary>
    /// Gets the XP required to reach the next level.
    /// </summary>
    public int XPToNextLevel => currentLevel >= MaxLevel ? 0 : (currentLevel + 1) * 1000;

    /// <summary>
    /// Gets the current spell slots available.
    /// </summary>
    public int CurrentSpellSlots => currentSpellSlots;

    /// <summary>
    /// Gets the maximum spell slots available.
    /// </summary>
    public int MaxSpellSlots => GetSpellSlots();

    /// <summary>
    /// Gets the known spells for this gladiator.
    /// </summary>
    public List<SpellData> KnownSpells => knownSpells;

    /// <summary>
    /// Gets the maximum hit points for this gladiator instance.
    /// </summary>
    public int MaxHP => data != null
        ? data.MaxHP +
          GetLevelGrowthValue(data.gladiatorClass != null ? data.gladiatorClass.hpGrowth : 0) +
          (equippedArmor != null ? equippedArmor.hpBonus : 0)
        : 0;

    /// <summary>
    /// Gets the remaining movement points for this turn.
    /// </summary>
    public int RemainingMP => remainingMP;

    /// <summary>
    /// Gets the remaining action points for this turn.
    /// </summary>
    public int RemainingAP => remainingAP;

    /// <summary>
    /// Gets whether this gladiator is currently moving.
    /// </summary>
    public bool IsMoving => isMoving;

    /// <summary>
    /// Gets the maximum movement points for this gladiator.
    /// </summary>
    public int MaxMP => Mathf.Max(0, data != null
        ? data.MovementPoints + (equippedArmor != null ? equippedArmor.movementPenalty : 0)
        : 0);

    /// <summary>
    /// Gets the maximum action points for this gladiator.
    /// </summary>
    public int MaxAP => data != null ? data.ActionPoints : 0;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        // Ensure a reasonable default visual scale.
        transform.localScale = new Vector3(0.5f, 1f, 0.5f);
    }

    private void Start()
    {
        if (GetComponent<GladiatorHoverEffect>() == null)
        {
            gameObject.AddComponent<GladiatorHoverEffect>();
        }
    }

    /// <summary>
    /// Initializes the gladiator with data, starting position and control side.
    /// </summary>
    /// <param name="gladiatorData">The data asset describing this gladiator.</param>
    /// <param name="startPos">The starting grid position.</param>
    /// <param name="playerControlled">Whether this gladiator is player controlled.</param>
    public void Initialize(GladiatorData gladiatorData, Vector2Int startPos, bool playerControlled, bool placeOnGrid = true)
    {
        data = gladiatorData;
        isPlayerControlled = playerControlled;
        currentLevel = 1;
        currentXP = 0;
        status = GladiatorStatus.Healthy;
        currentHP = data != null ? MaxHP : 0;
        currentSpellSlots = MaxSpellSlots;

        if (data != null)
        {
            if (data.startingWeapon != null)
            {
                EquipWeapon(data.startingWeapon);
            }
            else
            {
                UnequipWeapon();
            }

            if (data.startingArmor != null)
            {
                EquipArmor(data.startingArmor);
            }
            else
            {
                UnequipArmor();
            }

            InitializeDecay();

            currentHP = MaxHP;
            remainingMP = MaxMP;
            remainingAP = data.ActionPoints;

            knownSpells.Clear();
            spellCooldowns.Clear();
            if (data.startingSpells != null)
            {
                knownSpells.AddRange(data.startingSpells);
            }
        }

        SetupVisuals();
        if (placeOnGrid)
        {
            PlaceOnGrid(startPos);
        }
        else
        {
            currentGridPosition = startPos;
            transform.position = new Vector3(0f, 0.5f, -100f);
        }

        CreateHealthBar();
    }

    public void InitializeFromInstance(GladiatorInstance instance, Vector2Int startPos, bool playerControlled, bool placeOnGrid = true)
    {
        if (instance == null || instance.templateData == null)
        {
            Debug.LogWarning("Gladiator: Cannot initialize from instance, template data is missing.");
            return;
        }

        Initialize(instance.templateData, startPos, playerControlled, placeOnGrid);

        linkedInstance = instance;
        currentLevel = instance.currentLevel;
        currentXP = instance.currentXP;
        status = instance.status;
        injuryBattlesRemaining = instance.injuryBattlesRemaining;
        decayBattlesRemaining = instance.decayBattlesRemaining;
        startingDecayBattles = instance.startingDecayBattles;
        isAscended = instance.isAscended;
        ascendedFormName = instance.ascendedFormName;

        if (knownSpells != null)
        {
            knownSpells.Clear();
            if (instance.knownSpells != null)
            {
                foreach (SpellData spell in instance.knownSpells)
                {
                    if (spell != null)
                    {
                        knownSpells.Add(spell);
                    }
                }
            }
        }

        if (instance.equippedWeapon != null)
        {
            EquipWeapon(instance.equippedWeapon);
        }
        else
        {
            UnequipWeapon();
        }

        if (instance.equippedArmor != null)
        {
            EquipArmor(instance.equippedArmor);
        }
        else
        {
            UnequipArmor();
        }

        currentHP = Mathf.Clamp(instance.currentHP, 0, MaxHP);
        currentSpellSlots = MaxSpellSlots;

        if (isAscended)
        {
            ApplyLichVisuals();
        }
    }

    /// <summary>
    /// Places the gladiator onto a specific grid cell.
    /// Handles occupancy flags and world position.
    /// </summary>
    /// <param name="gridPosition">The target grid position.</param>
    public void PlaceOnGrid(Vector2Int gridPosition)
    {
        if (gridManager == null)
        {
            Debug.LogError("GridManager reference is missing on Gladiator.", this);
            return;
        }

        if (!gridManager.IsPositionValid(gridPosition))
        {
            Debug.LogWarning($"Attempted to place gladiator at invalid grid position {gridPosition}.", this);
            return;
        }

        GridCell targetCell = gridManager.GetCellAtPosition(gridPosition);
        if (targetCell == null || !targetCell.IsWalkable || targetCell.IsOccupied)
        {
            Debug.LogWarning($"Cannot place gladiator at grid position {gridPosition}. Cell is not walkable or is occupied.", this);
            return;
        }

        // Clear old cell occupancy.
        if (gridManager.IsPositionValid(currentGridPosition))
        {
            GridCell oldCell = gridManager.GetCellAtPosition(currentGridPosition);
            if (oldCell != null && oldCell.OccupyingUnit == gameObject)
            {
                oldCell.ClearOccupied();
            }
        }

        // Occupy new cell.
        targetCell.SetOccupied(gameObject);
        currentGridPosition = gridPosition;

        // Align the gladiator to the center of the cell.
        Vector3 cellWorldPos = targetCell.WorldPosition;
        transform.position = new Vector3(cellWorldPos.x, 0.5f, cellWorldPos.z);
    }

    /// <summary>
    /// Resets movement and action points to their maximum values at the start of a turn.
    /// </summary>
    public void ResetTurnPoints()
    {
        if (data == null)
        {
            return;
        }

        remainingMP = data.MovementPoints +
                      GetEffectModifier(EffectType.MovementBuff) +
                      GetEffectModifier(EffectType.MovementDebuff) +
                      (equippedArmor != null ? equippedArmor.movementPenalty : 0);
        remainingMP = Mathf.Max(0, remainingMP);
        remainingAP = data.ActionPoints;
        UpdateSpellCooldowns();
        RefreshSpellSlots();
        ProcessActiveEffects();
        ProcessPoison();
        ApplyRaceTurnStartEffects();
    }

    /// <summary>
    /// Applies incoming damage, updating hit points and handling death.
    /// </summary>
    /// <param name="damage">Final damage to apply (defense already accounted for).</param>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null, false);
    }

    /// <summary>
    /// Applies incoming damage, tracking the source for XP awards.
    /// </summary>
    public void TakeDamage(int damage, Gladiator source)
    {
        TakeDamage(damage, source, false);
    }

    public void TakeDamage(int damage, Gladiator source, bool isMagical)
    {
        if (damage <= 0)
        {
            return;
        }

        int hpBeforeHit = currentHP;

        if (data != null && data.race != null)
        {
            if (!isMagical && data.race.physicalDamageReduction > 0f)
            {
                damage = Mathf.RoundToInt(damage * (1f - data.race.physicalDamageReduction));
            }

            if (isMagical && data.race.magicResistBonus > 0f)
            {
                damage = Mathf.RoundToInt(damage * (1f - data.race.magicResistBonus));
            }
        }

        damage = Mathf.Max(0, damage);
        if (damage <= 0)
        {
            return;
        }

        if (source != null && source != this)
        {
            RegisterDamage(source, damage);
        }

        currentHP -= damage;
        if (currentHP > 0)
        {
            currentHP = Mathf.Min(currentHP, MaxHP);
        }
        if (DebugSettings.LOG_COMBAT)
        {
            Debug.Log($"Gladiator.TakeDamage - {name} took {damage} damage. HP: {currentHP}/{MaxHP}", this);
        }

        if (currentHP <= 0)
        {
            int overkillDamage = Mathf.Abs(currentHP);
            currentHP = 0;
            DetermineDeathOrInjury(overkillDamage, hpBeforeHit, source);
            Debug.Log($"Gladiator {name} HP <= 0. Status after death check: {status}", this);
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHP = Mathf.Clamp(currentHP + amount, 0, MaxHP);
    }

    /// <summary>
    /// Handles gladiator death: frees the occupied cell and destroys the GameObject.
    /// </summary>
    public void Die()
    {
        Die(null);
    }

    public void Die(Gladiator killer)
    {
        DiePermanent(killer);
    }

    private void DiePermanent(Gladiator killer)
    {
        status = GladiatorStatus.Dead;
        Debug.Log($"Gladiator {name} has died.", this);
        if (linkedInstance != null)
        {
            linkedInstance.status = status;
            linkedInstance.currentLevel = currentLevel;
            linkedInstance.currentXP = currentXP;
            linkedInstance.maxHP = MaxHP;
            linkedInstance.currentHP = 0;
            linkedInstance.injuryBattlesRemaining = injuryBattlesRemaining;
            linkedInstance.decayBattlesRemaining = decayBattlesRemaining;
            linkedInstance.startingDecayBattles = startingDecayBattles;
            linkedInstance.isAscended = isAscended;
            linkedInstance.ascendedFormName = ascendedFormName;
            linkedInstance.equippedWeapon = equippedWeapon;
            linkedInstance.equippedArmor = equippedArmor;
            if (linkedInstance.knownSpells != null && knownSpells != null)
            {
                for (int i = 0; i < linkedInstance.knownSpells.Length; i++)
                {
                    linkedInstance.knownSpells[i] = i < knownSpells.Count ? knownSpells[i] : null;
                }
            }
        }
        OnDefeat(killer);
        Destroy(gameObject);
    }

    /// <summary>
    /// Assigns a prefab to be used for the health bar.
    /// </summary>
    public void SetHealthBarPrefab(GameObject prefab)
    {
        // Prefab-based health bars are no longer used.
    }

    /// <summary>
    /// Creates the floating health bar UI for this gladiator.
    /// </summary>
    public void CreateHealthBar()
    {
        if (healthBar != null)
        {
            return;
        }

        GameObject hpBarRoot = new GameObject($"HealthBar_{name}");

        Canvas canvas = hpBarRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = hpBarRoot.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        RectTransform canvasRect = hpBarRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100f, 20f);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(canvas.transform, false);
        Image bgImage = bgObject.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        RectTransform bgRect = bgObject.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(bgObject.transform, false);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = data != null && data.team == Team.Player
            ? new Color(0f, 1f, 0f, 1f)
            : new Color(1f, 0f, 0f, 1f);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = new Vector2(100f, 0f);
        fillRect.anchoredPosition = Vector2.zero;

        healthBar = hpBarRoot.AddComponent<HealthBarUI>();
        healthBar.SetReferences(this, canvas, fillImage, bgImage);

        if (DebugSettings.LOG_SYSTEM)
        {
            Debug.Log($"Created procedural health bar for {name}", this);
        }
    }

    private void DestroyHealthBar()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
            healthBar = null;
        }
    }

    /// <summary>
    /// Calculates all grid positions this gladiator can move to using BFS,
    /// constrained by remaining movement points, walkability and occupancy.
    /// </summary>
    /// <returns>A list of reachable grid positions.</returns>
    public List<Vector2Int> GetMovementRange()
    {
        var reachable = new List<Vector2Int>();

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.GetMovementRange - Getting movement range from position: {currentGridPosition}, remainingMP: {remainingMP}", this);
        }

        if (gridManager == null || remainingMP <= 0)
        {
            reachable.Add(currentGridPosition);
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log($"Gladiator.GetMovementRange - GridManager null or no MP. Only current position is reachable. Count: {reachable.Count}", this);
            }
            return reachable;
        }

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int cost)>();

        visited.Add(currentGridPosition);
        queue.Enqueue((currentGridPosition, 0));
        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.GetMovementRange - Enqueued start position {currentGridPosition} with cost 0", this);
        }

        while (queue.Count > 0)
        {
            var (pos, cost) = queue.Dequeue();
            reachable.Add(pos);
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log($"Gladiator.GetMovementRange - Dequeued {pos} with cost {cost}, added to reachable", this);
            }

            if (cost >= remainingMP)
            {
                if (DebugSettings.VERBOSE_LOGGING)
                {
                    Debug.Log($"Gladiator.GetMovementRange - Reached max cost at {pos} (cost {cost}), not exploring neighbors.", this);
                }
                continue;
            }

            foreach (GridCell neighbor in gridManager.GetWalkableNeighbors(pos))
            {
                Vector2Int neighborPos = neighbor.GridPosition;

                if (visited.Contains(neighborPos))
                {
                    continue;
                }

                visited.Add(neighborPos);
                queue.Enqueue((neighborPos, cost + 1));
                if (DebugSettings.VERBOSE_LOGGING)
                {
                    Debug.Log($"Gladiator.GetMovementRange - Enqueued neighbor {neighborPos} with cost {cost + 1}", this);
                }
            }
        }

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.GetMovementRange - Found {reachable.Count} reachable positions.", this);
        }
        return reachable;
    }

    /// <summary>
    /// Smoothly moves the gladiator to the target grid position.
    /// </summary>
    public void MoveToSmooth(Vector2Int targetGridPos)
    {
        if (isMoving)
        {
            return;
        }

        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        if (gridManager == null || !gridManager.IsPositionValid(targetGridPos))
        {
            return;
        }

        GridCell targetCell = gridManager.GetCellAtPosition(targetGridPos);
        if (targetCell == null || !targetCell.IsWalkable || targetCell.IsOccupied)
        {
            return;
        }

        StartCoroutine(MoveAlongPath(BuildPathToTarget(currentGridPosition, targetGridPos)));
    }

    private List<Vector2Int> BuildPathToTarget(Vector2Int start, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        if (gridManager == null)
        {
            return path;
        }

        var queue = new Queue<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == end)
            {
                break;
            }

            foreach (GridCell neighbor in gridManager.GetWalkableNeighbors(current))
            {
                Vector2Int next = neighbor.GridPosition;
                if (visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(end))
        {
            return path;
        }

        Vector2Int step = end;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        path.Reverse();
        return path;
    }

    private IEnumerator MoveAlongPath(List<Vector2Int> path)
    {
        isMoving = true;
        Vector2Int startPos = currentGridPosition;

        GridCell oldCell = gridManager != null ? gridManager.GetCellAtPosition(currentGridPosition) : null;
        if (oldCell != null && oldCell.OccupyingUnit == gameObject)
        {
            oldCell.ClearOccupied();
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log($"{name} moving from {startPos}. Old cell occupied: {oldCell.IsOccupied}", this);
            }
        }

        if (path.Count == 0)
        {
            if (oldCell != null)
            {
                oldCell.SetOccupied(gameObject);
            }

            isMoving = false;
            moveLerpProgress = 1f;
            yield break;
        }

        foreach (Vector2Int step in path)
        {
            GridCell stepCell = gridManager != null ? gridManager.GetCellAtPosition(step) : null;
            if (stepCell == null || stepCell.IsOccupied)
            {
                break;
            }

            moveStartPosition = transform.position;
            moveTargetPosition = stepCell.WorldPosition;
            moveTargetPosition.y = 0.5f;

            float distance = Vector3.Distance(moveStartPosition, moveTargetPosition);
            float duration = distance / Mathf.Max(0.01f, moveSpeed);
            duration = Mathf.Max(0.3f, duration);

            moveLerpProgress = 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                moveLerpProgress = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(moveStartPosition, moveTargetPosition, moveLerpProgress);
                yield return null;
            }

            transform.position = moveTargetPosition;
            currentGridPosition = step;
        }

        GridCell finalCell = gridManager != null ? gridManager.GetCellAtPosition(currentGridPosition) : null;
        if (finalCell != null)
        {
            finalCell.SetOccupied(gameObject);
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log($"{name} moved to {currentGridPosition}. New cell occupied: {finalCell.IsOccupied}", this);
            }
        }

        isMoving = false;
        moveLerpProgress = 1f;
    }

    /// <summary>
    /// Equips the provided weapon on this gladiator.
    /// </summary>
    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;

        string weaponName = equippedWeapon != null ? equippedWeapon.weaponName : "None";
        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.EquipWeapon - {name} equipped weapon: {weaponName}", this);
        }
    }

    /// <summary>
    /// Unequips the current weapon, reverting to unarmed stats.
    /// </summary>
    public void UnequipWeapon()
    {
        equippedWeapon = null;
    }

    public void EquipArmor(ArmorData armor)
    {
        equippedArmor = armor;
        currentHP = MaxHP;
    }

    public void UnequipArmor()
    {
        equippedArmor = null;
        currentHP = Mathf.Min(currentHP, MaxHP);
    }

    /// <summary>
    /// Returns the current attack range based on equipped weapon or base range.
    /// </summary>
    public int GetAttackRange()
    {
        if (equippedWeapon != null)
        {
            if (equippedWeapon.range > 0)
            {
                return equippedWeapon.range;
            }

            if (equippedWeapon.attackRange > 0)
            {
                return equippedWeapon.attackRange;
            }
        }

        return baseAttackRange;
    }

    public bool CanBasicAttack()
    {
        if (equippedWeapon != null && equippedWeapon.weaponType == WeaponType.Magic)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether this gladiator requires line of sight to attack.
    /// </summary>
    public bool RequiresLineOfSight()
    {
        if (equippedWeapon == null)
        {
            return false;
        }

        if (equippedWeapon.weaponType == WeaponType.Ranged)
        {
            return true;
        }

        return equippedWeapon.requiresLineOfSight;
    }

    /// <summary>
    /// Returns whether a target is within range and line of sight (if required).
    /// </summary>
    public bool CanAttackTarget(Gladiator target, out bool inRange, out bool hasLineOfSight)
    {
        inRange = false;
        hasLineOfSight = true;

        if (target == null)
        {
            return false;
        }

        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        int range = GetAttackRange();
        int distance = Mathf.Abs(currentGridPosition.x - target.currentGridPosition.x) +
                       Mathf.Abs(currentGridPosition.y - target.currentGridPosition.y);
        inRange = distance <= range;

        bool requiresLos = RequiresLineOfSight();
        hasLineOfSight = !requiresLos ||
                         CombatSystem.HasLineOfSight(currentGridPosition, target.currentGridPosition, gridManager);

        return inRange && hasLineOfSight;
    }

    /// <summary>
    /// Returns the total strength including equipment bonuses.
    /// </summary>
    public int GetTotalStrength()
    {
        int strength = data != null ? data.Strength + GetLevelGrowthValue(data.gladiatorClass != null ? data.gladiatorClass.strengthGrowth : 0) : 0;
        strength += GetEffectModifier(EffectType.StrengthBuff);
        strength += GetEffectModifier(EffectType.StrengthDebuff);
        if (equippedWeapon != null)
        {
            strength += equippedWeapon.strengthBonus;
        }
        if (equippedArmor != null)
        {
            strength += equippedArmor.strengthBonus;
        }
        return strength;
    }

    /// <summary>
    /// Returns the total dexterity including equipment bonuses.
    /// </summary>
    public int GetTotalDexterity()
    {
        int dexterity = data != null ? data.Dexterity + GetLevelGrowthValue(data.gladiatorClass != null ? data.gladiatorClass.dexterityGrowth : 0) : 0;
        if (equippedWeapon != null)
        {
            dexterity += equippedWeapon.dexterityBonus;
        }
        if (equippedArmor != null)
        {
            dexterity += equippedArmor.dexterityBonus;
        }
        return dexterity;
    }

    /// <summary>
    /// Returns the total intelligence including equipment bonuses.
    /// </summary>
    public int GetTotalIntelligence()
    {
        int intelligence = data != null ? data.Intelligence + GetLevelGrowthValue(data.gladiatorClass != null ? data.gladiatorClass.intelligenceGrowth : 0) : 0;
        if (equippedWeapon != null)
        {
            intelligence += equippedWeapon.intelligenceBonus;
        }
        if (equippedArmor != null)
        {
            intelligence += equippedArmor.intelligenceBonus;
        }
        if (isAscended && ascendedFormName == "Lich")
        {
            intelligence += 3;
        }
        return intelligence;
    }

    /// <summary>
    /// Returns the total defense including equipment bonuses.
    /// </summary>
    public int GetTotalDefense()
    {
        int defense = data != null ? data.Defense + GetLevelGrowthValue(data.gladiatorClass != null ? data.gladiatorClass.defenseGrowth : 0) : 0;
        defense += GetEffectModifier(EffectType.DefenseBuff);
        defense += GetEffectModifier(EffectType.DefenseDebuff);
        if (equippedWeapon != null)
        {
            defense += equippedWeapon.defenseBonus;
        }
        if (equippedArmor != null)
        {
            defense += equippedArmor.defenseBonus;
        }
        return defense;
    }

    /// <summary>
    /// Returns the total attack value based on weapon scaling.
    /// </summary>
    public int GetTotalAttack()
    {
        if (data == null)
        {
            return 0;
        }

        float baseAttack;
        ScalingStat scaling = ScalingStat.Strength;

        if (equippedWeapon != null)
        {
            baseAttack = equippedWeapon.baseDamage;
            scaling = equippedWeapon.scalingStat;
            switch (equippedWeapon.scalingStat)
            {
                case ScalingStat.Dexterity:
                    baseAttack += GetTotalDexterity();
                    break;
                case ScalingStat.Intelligence:
                    baseAttack += GetTotalIntelligence();
                    break;
                default:
                    baseAttack += GetTotalStrength();
                    break;
            }
        }
        else
        {
            baseAttack = GetTotalStrength();
        }

        if (data != null && data.race != null)
        {
            if (scaling == ScalingStat.Strength && data.race.meleeDamageBonus > 0f)
            {
                baseAttack *= 1f + data.race.meleeDamageBonus;
            }
            else if (scaling == ScalingStat.Dexterity && data.race.dexWeaponDamageBonus > 0f)
            {
                baseAttack *= 1f + data.race.dexWeaponDamageBonus;
            }
        }

        return Mathf.RoundToInt(baseAttack);
    }

    /// <summary>
    /// Returns the hit chance (accuracy) for this gladiator.
    /// </summary>
    public float GetAccuracy()
    {
        if (data == null)
        {
            return 0.75f;
        }

        float accuracy = 0.75f + (GetTotalDexterity() * 0.02f);
        if (equippedWeapon != null)
        {
            accuracy += equippedWeapon.accuracyBonus;
        }

        return Mathf.Clamp(accuracy, 0f, 0.99f);
    }

    /// <summary>
    /// Returns the chance to dodge incoming attacks.
    /// </summary>
    public float GetDodgeChance()
    {
        if (data == null)
        {
            return 0f;
        }

        float dodge = GetTotalDexterity() * 0.015f;
        if (equippedArmor != null)
        {
            dodge += equippedArmor.dodgeBonus;
        }
        if (data != null && data.race != null)
        {
            dodge += data.race.dodgeBonus;
        }
        return Mathf.Clamp(dodge, 0f, 0.5f);
    }

    /// <summary>
    /// Returns the critical hit chance for physical attacks.
    /// </summary>
    public float GetCritChance()
    {
        if (data == null)
        {
            return 0.05f;
        }

        float crit = 0.05f + (GetTotalDexterity() * 0.01f);
        if (equippedWeapon != null)
        {
            crit += equippedWeapon.critBonus;
        }

        return Mathf.Clamp(crit, 0f, 0.75f);
    }

    /// <summary>
    /// Returns the critical hit chance for spells.
    /// </summary>
    public float GetSpellCritChance()
    {
        if (data == null)
        {
            return 0.05f;
        }

        float spellCrit = 0.05f + (GetTotalIntelligence() * 0.01f);
        return Mathf.Clamp(spellCrit, 0f, 0.75f);
    }

    /// <summary>
    /// Returns the number of spell slots available.
    /// </summary>
    public int GetSpellSlots()
    {
        if (data == null)
        {
            return 0;
        }

        int slots = GetTotalIntelligence() / 3;
        if (equippedWeapon != null)
        {
            slots += equippedWeapon.spellSlotBonus;
        }
        if (equippedArmor != null)
        {
            slots += equippedArmor.spellSlotBonus;
        }
        if (data != null && data.race != null)
        {
            slots += data.race.spellSlotBonus;
        }
        if (isAscended && ascendedFormName == "Lich")
        {
            slots += 2;
        }
        return slots;
    }

    public float GetSpellPowerBonus()
    {
        float bonus = 0f;
        if (equippedWeapon != null)
        {
            bonus += equippedWeapon.spellPowerBonus;
        }
        if (equippedArmor != null)
        {
            bonus += equippedArmor.spellPowerBonus;
        }
        if (data != null && data.race != null)
        {
            bonus += data.race.spellPowerBonus;
        }
        if (isAscended && ascendedFormName == "Lich")
        {
            bonus += 0.25f;
        }

        return bonus;
    }

    /// <summary>
    /// Returns the initiative value used for turn order.
    /// </summary>
    public int GetInitiative()
    {
        if (data == null)
        {
            return 0;
        }

        int speed = data.Speed + GetLevelGrowthValue(data.gladiatorClass != null ? data.gladiatorClass.speedGrowth : 0);
        speed += GetEffectModifier(EffectType.SpeedBuff);
        speed += GetEffectModifier(EffectType.SpeedDebuff);
        return speed + (GetTotalDexterity() / 2);
    }

    /// <summary>
    /// Returns magic resistance derived from intelligence.
    /// </summary>
    public int GetMagicResistance()
    {
        if (data == null)
        {
            return 0;
        }

        int resistance = GetTotalIntelligence() / 2;
        if (data.race != null && data.race.magicResistBonus > 0f)
        {
            resistance += Mathf.RoundToInt(data.race.magicResistBonus * 100f);
        }
        return resistance;
    }

    /// <summary>
    /// Grants experience points and checks for level ups.
    /// </summary>
    public void GainXP(int amount)
    {
        if (amount <= 0 || currentLevel >= MaxLevel)
        {
            return;
        }

        if (data != null && data.race != null && data.race.xpBonusMultiplier > 0f)
        {
            amount = Mathf.RoundToInt(amount * data.race.xpBonusMultiplier);
        }

        currentXP += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        while (currentLevel < MaxLevel)
        {
            int requiredXP = (currentLevel + 1) * 1000;
            if (currentXP < requiredXP)
            {
                break;
            }

            currentXP -= requiredXP;
            currentLevel++;

            int hpGrowth = data != null && data.gladiatorClass != null ? data.gladiatorClass.hpGrowth : 0;
            if (hpGrowth > 0)
            {
                currentHP = Mathf.Clamp(currentHP + hpGrowth, 0, MaxHP);
            }

            Debug.Log($"Gladiator {name} leveled up to {currentLevel}!", this);

            if (currentLevel == MaxLevel)
            {
                CheckLichAscension();
            }
        }
    }

    private void RegisterDamage(Gladiator source, int amount)
    {
        if (source == null || amount <= 0)
        {
            return;
        }

        if (damageContributors.ContainsKey(source))
        {
            damageContributors[source] += amount;
        }
        else
        {
            damageContributors[source] = amount;
        }
    }

    private void AwardDeathXP(Gladiator killer)
    {
        int enemyLevel = currentLevel;
        if (enemyLevel <= 0)
        {
            return;
        }

        if (killer != null)
        {
            killer.GainXP(enemyLevel * 100);
        }

        foreach (KeyValuePair<Gladiator, int> entry in damageContributors)
        {
            Gladiator contributor = entry.Key;
            if (contributor == null || contributor == killer)
            {
                continue;
            }

            if (contributor.CurrentHP <= 0)
            {
                continue;
            }

            contributor.GainXP(enemyLevel * 50);
        }

        damageContributors.Clear();
    }

    private int GetLevelGrowthValue(int growthPerLevel)
    {
        if (growthPerLevel <= 0)
        {
            return 0;
        }

        int levelsGained = Mathf.Max(0, currentLevel - 1);
        return levelsGained * growthPerLevel;
    }

    public void RefreshSpellSlots()
    {
        currentSpellSlots = MaxSpellSlots;
    }

    public void UpdateSpellCooldowns()
    {
        if (spellCooldowns.Count == 0)
        {
            return;
        }

        List<SpellData> keys = new List<SpellData>(spellCooldowns.Keys);
        foreach (SpellData spell in keys)
        {
            int remaining = spellCooldowns[spell] - 1;
            if (remaining <= 0)
            {
                spellCooldowns.Remove(spell);
            }
            else
            {
                spellCooldowns[spell] = remaining;
            }
        }
    }

    public int GetSpellCooldownRemaining(SpellData spell)
    {
        if (spell == null)
        {
            return 0;
        }

        return spellCooldowns.TryGetValue(spell, out int remaining) ? remaining : 0;
    }

    public bool CanCastSpell(SpellData spell)
    {
        if (spell == null)
        {
            return false;
        }

        if (remainingAP < spell.apCost || currentSpellSlots < spell.spellSlotCost)
        {
            return false;
        }

        return GetSpellCooldownRemaining(spell) <= 0;
    }

    public bool TrySpendSpellSlots(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentSpellSlots < amount)
        {
            return false;
        }

        currentSpellSlots -= amount;
        return true;
    }

    public bool HasActiveEffect(EffectType effectType)
    {
        return activeEffects.ContainsKey(effectType);
    }

    public bool IsStunned()
    {
        return HasActiveEffect(EffectType.Stun);
    }

    public void AddOrRefreshEffect(EffectType effectType, int value, int duration)
    {
        if (effectType == EffectType.None || duration <= 0)
        {
            return;
        }

        if (effectType == EffectType.Stun && data != null && data.race != null && data.race.immuneToStunParalysis)
        {
            return;
        }

        // Store duration + 1 so effects remain active for the current turn.
        int turnsRemaining = duration + 1;

        if (activeEffects.TryGetValue(effectType, out ActiveEffect existing))
        {
            if (IsDebuffEffect(effectType))
            {
                existing.value += value;
            }
            else if (effectType == EffectType.Stun)
            {
                existing.value = value;
            }
            else
            {
                existing.value = value;
            }

            existing.turnsRemaining = turnsRemaining;
            activeEffects[effectType] = existing;
            return;
        }

        activeEffects[effectType] = new ActiveEffect
        {
            value = value,
            turnsRemaining = turnsRemaining
        };
    }

    public void ProcessActiveEffects()
    {
        if (activeEffects.Count == 0)
        {
            return;
        }

        List<EffectType> expired = null;
        List<EffectType> keys = new List<EffectType>(activeEffects.Keys);
        foreach (EffectType key in keys)
        {
            ActiveEffect effect = activeEffects[key];
            effect.turnsRemaining--;
            if (effect.turnsRemaining <= 0)
            {
                if (expired == null)
                {
                    expired = new List<EffectType>();
                }

                expired.Add(key);
            }
            else
            {
                activeEffects[key] = effect;
            }
        }

        if (expired != null)
        {
            foreach (EffectType effectType in expired)
            {
                activeEffects.Remove(effectType);
            }
        }
    }

    public bool CanCastSpell(SpellData spell, Gladiator target, out bool inRange, out bool hasLineOfSight)
    {
        inRange = false;
        hasLineOfSight = true;

        if (spell == null || target == null)
        {
            return false;
        }

        if (!CanCastSpell(spell))
        {
            return false;
        }

        int distance = Mathf.Abs(currentGridPosition.x - target.currentGridPosition.x) +
                       Mathf.Abs(currentGridPosition.y - target.currentGridPosition.y);
        inRange = distance <= spell.range;

        if (spell.requiresLineOfSight)
        {
            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
            }

            hasLineOfSight = CombatSystem.HasLineOfSight(currentGridPosition, target.currentGridPosition, gridManager);
        }

        return inRange && hasLineOfSight;
    }

    public bool CastSpell(SpellData spell, Gladiator target)
    {
        if (spell == null || target == null || data == null)
        {
            return false;
        }

        bool isEnemy = target.Data != null && target.Data.team != data.team;
        if (spell.spellType == SpellType.Buff && isEnemy)
        {
            return false;
        }

        if ((spell.spellType == SpellType.Damage || spell.spellType == SpellType.Debuff) && !isEnemy)
        {
            return false;
        }

        bool inRange;
        bool hasLineOfSight;
        if (!CanCastSpell(spell, target, out inRange, out hasLineOfSight))
        {
            return false;
        }

        if (spell.spellType == SpellType.Damage)
        {
            CombatSystem.CastDamageSpell(this, target, spell);
        }
        else if (spell.spellType == SpellType.Debuff)
        {
            CombatSystem.ApplyDebuff(this, target, spell);
        }
        else if (spell.spellType == SpellType.Buff)
        {
            CombatSystem.ApplyBuff(this, target, spell);
        }

        remainingAP = Mathf.Max(0, remainingAP - spell.apCost);
        TrySpendSpellSlots(spell.spellSlotCost);
        if (spell.cooldownTurns > 0)
        {
            spellCooldowns[spell] = spell.cooldownTurns;
        }
        return true;
    }

    public bool CastSpellAOE(SpellData spell, GridCell targetCell)
    {
        if (spell == null || targetCell == null || data == null)
        {
            return false;
        }

        if (!CanCastSpell(spell))
        {
            return false;
        }

        int distance = Mathf.Abs(currentGridPosition.x - targetCell.GridPosition.x) +
                       Mathf.Abs(currentGridPosition.y - targetCell.GridPosition.y);
        if (distance > spell.range)
        {
            return false;
        }

        if (spell.requiresLineOfSight)
        {
            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
            }

            if (gridManager == null)
            {
                return false;
            }

            if (!CombatSystem.HasLineOfSight(currentGridPosition, targetCell.GridPosition, gridManager))
            {
                return false;
            }
        }

        CombatSystem.CastAOESpell(this, targetCell, spell, BattleManager.Instance != null
            ? BattleManager.Instance.AllGladiators
            : new List<Gladiator>());

        remainingAP = Mathf.Max(0, remainingAP - spell.apCost);
        TrySpendSpellSlots(spell.spellSlotCost);
        if (spell.cooldownTurns > 0)
        {
            spellCooldowns[spell] = spell.cooldownTurns;
        }
        return true;
    }

    public bool HasValidSpellTargets(SpellData spell)
    {
        if (spell == null || BattleManager.Instance == null)
        {
            return false;
        }

        if (spell.spellType == SpellType.AOE)
        {
            return spell.range > 0;
        }

        foreach (Gladiator gladiator in BattleManager.Instance.AllGladiators)
        {
            if (gladiator == null || gladiator.Data == null)
            {
                continue;
            }

            bool isEnemy = gladiator.Data.team != data.team;
            bool shouldTargetEnemy = spell.spellType == SpellType.Damage || spell.spellType == SpellType.Debuff;

            if (shouldTargetEnemy && !isEnemy)
            {
                continue;
            }

            if (!shouldTargetEnemy && isEnemy)
            {
                continue;
            }

            bool inRange;
            bool hasLineOfSight;
            if (CanCastSpell(spell, gladiator, out inRange, out hasLineOfSight))
            {
                return true;
            }
        }

        return false;
    }

    private int GetEffectModifier(EffectType effectType)
    {
        if (activeEffects.TryGetValue(effectType, out ActiveEffect effect))
        {
            return effect.value;
        }

        return 0;
    }

    private static bool IsDebuffEffect(EffectType effectType)
    {
        return effectType == EffectType.StrengthDebuff ||
               effectType == EffectType.DefenseDebuff ||
               effectType == EffectType.SpeedDebuff ||
               effectType == EffectType.MovementDebuff;
    }

    private struct ActiveEffect
    {
        public int value;
        public int turnsRemaining;
    }

    public void ApplyPoison(int damagePerTurn, int turns, Gladiator source)
    {
        if (damagePerTurn <= 0 || turns <= 0)
        {
            return;
        }

        poisonDamagePerTurn = damagePerTurn;
        poisonTurnsRemaining = Mathf.Max(poisonTurnsRemaining, turns);
        poisonSource = source;
    }

    public void TryApplyPoison(Gladiator target)
    {
        if (target == null || data == null || data.race == null)
        {
            return;
        }

        if (!data.race.hasPoisonOnHit || data.race.poisonChance <= 0f || data.race.poisonDamagePerTurn <= 0)
        {
            return;
        }

        float roll = Random.value;
        if (roll <= data.race.poisonChance)
        {
            target.ApplyPoison(data.race.poisonDamagePerTurn, 3, this);
        }
    }

    private void ProcessPoison()
    {
        if (poisonTurnsRemaining <= 0 || poisonDamagePerTurn <= 0)
        {
            return;
        }

        TakeDamage(poisonDamagePerTurn, poisonSource, true);
        poisonTurnsRemaining--;
        if (poisonTurnsRemaining <= 0)
        {
            poisonDamagePerTurn = 0;
            poisonSource = null;
        }
    }

    private void ApplyRaceTurnStartEffects()
    {
        if (data == null || data.race == null)
        {
            return;
        }

        if (data.race.hpRegenPerTurn > 0f)
        {
            int regenAmount = Mathf.RoundToInt(MaxHP * data.race.hpRegenPerTurn);
            currentHP = Mathf.Min(currentHP + regenAmount, MaxHP);
        }
    }

    private void DetermineDeathOrInjury(int overkillDamage, int maxHpBeforeHit, Gladiator source)
    {
        float deathThreshold = GetDeathThreshold();
        Debug.Log($"{name} - Overkill: {overkillDamage}, Death threshold: {deathThreshold}", this);
        if (overkillDamage >= deathThreshold)
        {
            DiePermanent(source);
            return;
        }

        BecomeInjured(overkillDamage, source);
    }

    private float GetDeathThreshold()
    {
        float baseThreshold = MaxHP * 0.5f;
        if (data != null && data.gladiatorClass != null)
        {
            switch (data.gladiatorClass.className)
            {
                case "Tank":
                    baseThreshold = MaxHP * 0.60f;
                    break;
                case "Warrior":
                    baseThreshold = MaxHP * 0.55f;
                    break;
                case "Rogue":
                case "Mage":
                    baseThreshold = MaxHP * 0.45f;
                    break;
                case "Archer":
                    baseThreshold = MaxHP * 0.50f;
                    break;
            }
        }

        return baseThreshold;
    }

    private int CalculateInjuryDuration(int overkillDamage)
    {
        float overkillRatio = MaxHP > 0 ? (float)overkillDamage / MaxHP : 1f;

        if (overkillRatio < 0.15f)
        {
            return 1;
        }
        if (overkillRatio < 0.30f)
        {
            return 2;
        }
        if (overkillRatio < 0.45f)
        {
            return 3;
        }
        return 4;
    }

    private void BecomeInjured(int overkillDamage, Gladiator source)
    {
        if (data != null && data.race != null && data.race.raceName == "Undead")
        {
            ApplyDecayDamage(2);
            currentHP = 0;
            Debug.Log($"{name} is undead and takes decay instead of injury. Decay remaining: {decayBattlesRemaining}", this);
            OnDefeat(source);
            return;
        }

        status = GladiatorStatus.Injured;
        injuryBattlesRemaining = CalculateInjuryDuration(overkillDamage);
        Debug.Log($"{name} is injured for {injuryBattlesRemaining} battles! Overkill: {overkillDamage}");
        if (linkedInstance != null)
        {
            linkedInstance.status = status;
            linkedInstance.injuryBattlesRemaining = injuryBattlesRemaining;
        }
        currentHP = 0;
        OnDefeat(source);
    }

    private void OnDefeat(Gladiator killer)
    {
        Debug.Log($"Gladiator {name} defeated. Status: {status}.", this);
        ClearHighlights();
        DestroyHealthBar();

        AwardDeathXP(killer);

        if (gridManager != null && gridManager.IsPositionValid(currentGridPosition))
        {
            GridCell cell = gridManager.GetCellAtPosition(currentGridPosition);
            if (cell != null && cell.OccupyingUnit == gameObject)
            {
                cell.ClearOccupied();
            }
        }

        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    public void Recover()
    {
        if (status == GladiatorStatus.Injured && injuryBattlesRemaining <= 0)
        {
            status = GladiatorStatus.Healthy;
            currentHP = MaxHP;
            Debug.Log($"{name} has recovered from injury!");
        }
    }

    public void DecrementInjuryTimer()
    {
        if (status != GladiatorStatus.Injured)
        {
            return;
        }

        injuryBattlesRemaining = Mathf.Max(0, injuryBattlesRemaining - 1);
        if (injuryBattlesRemaining == 0)
        {
            Recover();
        }
    }

    public void InitializeDecay()
    {
        if (data != null && data.race != null && data.race.raceName == "Undead")
        {
            if (decayBattlesRemaining == -1)
            {
                decayBattlesRemaining = 13;
                startingDecayBattles = decayBattlesRemaining;
            }
        }
        else
        {
            decayBattlesRemaining = -1;
            startingDecayBattles = -1;
        }
    }

    public void ApplyDecayDamage(int amount)
    {
        if (decayBattlesRemaining > 0)
        {
            decayBattlesRemaining -= amount;
            Debug.Log($"{name} decay: {decayBattlesRemaining} battles remaining");

            if (decayBattlesRemaining <= 0)
            {
                Debug.Log($"{name} has completely decayed and crumbled to dust!");
                DiePermanent(null);
            }
        }
    }

    public void ProcessBattleDecay()
    {
        if (data != null && data.race != null && data.race.raceName == "Undead" && status != GladiatorStatus.Dead)
        {
            ApplyDecayDamage(1);
        }
    }

    public void CheckLichAscension()
    {
        if (currentLevel >= 10 && data != null && data.race != null && data.race.raceName == "Undead" && !isAscended)
        {
            if (decayBattlesRemaining == startingDecayBattles)
            {
                AscendToLich();
            }
            else
            {
                Debug.Log($"{name} reached Level 10 but took decay damage. No Lich transformation.");
            }
        }
    }

    private void AscendToLich()
    {
        isAscended = true;
        ascendedFormName = "Lich";
        decayBattlesRemaining = -1;
        Debug.Log($"✨ {name} has ascended to become a LICH! ✨");
        ApplyLichVisuals();
    }

    private void ApplyLichVisuals()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.8f, 0.2f, 1.0f, 1.0f);
        }
    }

    public void PlaySpellEffect(Color color, float duration = 0.2f)
    {
        StartCoroutine(FlashColor(color, duration));
    }

    private IEnumerator FlashColor(Color color, float duration)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            yield break;
        }

        Material original = renderer.sharedMaterial;
        renderer.material.color = color;
        yield return new WaitForSeconds(duration);

        if (renderer != null)
        {
            renderer.sharedMaterial = original;
        }
    }

    /// <summary>
    /// Returns a list of enemy gladiators within attack range.
    /// </summary>
    public List<Gladiator> GetAttackableTargets()
    {
        var targets = new List<Gladiator>();

        int range = GetAttackRange();
        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.GetAttackableTargets - {name}, Range: {range}, Team: {data?.team}, Pos: {currentGridPosition}", this);
        }

        if (data == null)
        {
            Debug.LogWarning("Gladiator.GetAttackableTargets - Gladiator data is null.", this);
            return targets;
        }

        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        if (gridManager == null)
        {
            Debug.LogWarning("Gladiator.GetAttackableTargets - GridManager reference is null.", this);
            return targets;
        }

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int cost)>();

        visited.Add(currentGridPosition);
        queue.Enqueue((currentGridPosition, 0));

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            var (pos, cost) = queue.Dequeue();

            if (cost > 0)
            {
                GridCell cell = gridManager.GetCellAtPosition(pos);
                if (cell != null && cell.IsOccupied && cell.OccupyingUnit != null)
                {
                    Gladiator target = cell.OccupyingUnit.GetComponent<Gladiator>();
                    if (target != null)
                    {
                        if (DebugSettings.VERBOSE_LOGGING)
                        {
                            Debug.Log($"  Found gladiator: {target.name}, Team: {target.Data?.team}", this);
                        }
                        if (target != this && target.Data != null)
                        {
                            bool isEnemy = target.Data.team != data.team;
                            if (DebugSettings.VERBOSE_LOGGING)
                            {
                                Debug.Log($"    Is enemy? {isEnemy}", this);
                            }

                            if (isEnemy)
                            {
                                bool hasLineOfSight = !RequiresLineOfSight() ||
                                                      CombatSystem.HasLineOfSight(currentGridPosition, pos, gridManager);

                                if (hasLineOfSight && !targets.Contains(target))
                                {
                                    targets.Add(target);
                                }
                            }
                        }
                    }
                }
            }

            if (cost >= range)
            {
                continue;
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = pos + dir;
                if (!gridManager.IsPositionValid(next) || visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue((next, cost + 1));
            }
        }

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.GetAttackableTargets result: {targets.Count} targets found", this);
        }
        return targets;
    }

    /// <summary>
    /// Returns a list of grid positions within attack range (filtered by LOS if needed).
    /// </summary>
    public List<Vector2Int> GetAttackableCells()
    {
        var cells = new List<Vector2Int>();

        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        if (gridManager == null)
        {
            return cells;
        }

        int range = GetAttackRange();
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int cost)>();

        visited.Add(currentGridPosition);
        queue.Enqueue((currentGridPosition, 0));

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            var (pos, cost) = queue.Dequeue();

            if (cost > 0)
            {
                bool hasLineOfSight = !RequiresLineOfSight() ||
                                      CombatSystem.HasLineOfSight(currentGridPosition, pos, gridManager);
                if (hasLineOfSight)
                {
                    cells.Add(pos);
                }
            }

            if (cost >= range)
            {
                continue;
            }

            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = pos + dir;
                if (!gridManager.IsPositionValid(next) || visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue((next, cost + 1));
            }
        }

        return cells;
    }

    /// <summary>
    /// Spends movement points if possible.
    /// </summary>
    public bool TrySpendMP(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (remainingMP < amount)
        {
            return false;
        }

        remainingMP -= amount;
        return true;
    }

    /// <summary>
    /// Restores movement points.
    /// </summary>
    public void RestoreMP(int amount)
    {
        if (amount <= 0 || data == null)
        {
            return;
        }

        remainingMP = Mathf.Clamp(remainingMP + amount, 0, data.MovementPoints);
    }

    /// <summary>
    /// Spends action points if possible.
    /// </summary>
    public bool TrySpendAP(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (remainingAP < amount)
        {
            return false;
        }

        remainingAP -= amount;
        return true;
    }

    /// <summary>
    /// Visually highlights all tiles within this gladiator's movement range.
    /// Valid destination tiles are colored green, and the current tile is yellow.
    /// </summary>
    public void HighlightMovementRange()
    {
        ClearHighlights();

        if (gridManager == null)
        {
            Debug.LogWarning("Gladiator.HighlightMovementRange - GridManager reference is null.", this);
            return;
        }

        List<Vector2Int> range = GetMovementRange();
        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.HighlightMovementRange - Highlighting {range.Count} positions.", this);
        }

        foreach (Vector2Int pos in range)
        {
            GridCell cell = gridManager.GetCellAtPosition(pos);
            if (cell == null)
            {
                Debug.LogWarning($"Gladiator.HighlightMovementRange - GridCell at position {pos} is null, skipping.", this);
                continue;
            }

            // Ensure we are only modifying the grid tile renderer, not any gladiator renderer.
            Renderer renderer = cell.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"Gladiator.HighlightMovementRange - No Renderer found on GridCell at position {pos}, skipping.", this);
                continue;
            }

            if (!highlightedCells.ContainsKey(cell))
            {
                highlightedCells[cell] = renderer.sharedMaterial;
            }

            Color color = pos == currentGridPosition ? Color.yellow : Color.green;
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log($"Gladiator.HighlightMovementRange - Highlighting position ({pos.x},{pos.y}) with color {(pos == currentGridPosition ? "Yellow" : "Green")}.", this);
            }
            renderer.material.color = color;
        }
    }

    /// <summary>
    /// Restores original tile colors for any tiles highlighted by this gladiator.
    /// </summary>
    public void ClearHighlights()
    {
        if (gridManager == null || highlightedCells.Count == 0)
        {
            highlightedCells.Clear();
            return;
        }

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"Gladiator.ClearHighlights - Clearing {highlightedCells.Count} movement cells for {name}.", this);
        }

        foreach (KeyValuePair<GridCell, Material> kvp in highlightedCells)
        {
            GridCell cell = kvp.Key;
            Material originalMat = kvp.Value;

            if (cell == null)
            {
                continue;
            }

            Renderer renderer = cell.GetComponentInChildren<Renderer>();
            if (renderer != null && originalMat != null)
            {
                renderer.sharedMaterial = originalMat;
            }
        }

        highlightedCells.Clear();
    }

    /// <summary>
    /// Signals that this gladiator has finished its turn.
    /// </summary>
    public void OnTurnEnd()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogWarning("Gladiator.OnTurnEnd - BattleManager instance is missing.", this);
            return;
        }

        BattleManager.Instance.EndTurn();
    }

    /// <summary>
    /// Applies team-based visual settings and stores the original material.
    /// </summary>
    private void SetupVisuals()
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            // Fallback: create a capsule if none exists.
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(transform);
            capsule.transform.localPosition = Vector3.zero;
            capsule.transform.localRotation = Quaternion.identity;
            capsule.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            renderer = capsule.GetComponent<Renderer>();
        }

        if (renderer == null)
        {
            return;
        }

        originalMaterial = renderer.sharedMaterial;
        if (originalMaterial == null)
        {
            originalMaterial = new Material(Shader.Find("Standard"));
            renderer.sharedMaterial = originalMaterial;
        }

        Color teamColor = Color.blue;
        if (data != null && data.team == Team.Enemy)
        {
            teamColor = Color.red;
        }
        else if (!IsPlayerControlled && (data == null || data.team == Team.Player))
        {
            // Non-player controlled but tagged as player team; keep blue as default.
            teamColor = Color.blue;
        }

        renderer.material.color = teamColor;

        // Ensure the gladiator visually fits on a grid cell.
        Vector3 baseScale = new Vector3(0.5f, 1f, 0.5f);
        transform.localScale = baseScale;

        ApplyRaceVisuals(renderer, baseScale);
    }

    private void ApplyRaceVisuals(Renderer renderer, Vector3 baseScale)
    {
        if (data == null || data.race == null || renderer == null)
        {
            return;
        }

        renderer.material.color = data.race.primaryColor;
        transform.localScale = new Vector3(baseScale.x, baseScale.y * data.race.heightScale, baseScale.z);
    }
}

