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

    /// <summary>
    /// Current hit points for this gladiator instance.
    /// </summary>
    [SerializeField]
    private int currentHP;

    // Cached highlight data so we can restore tiles after highlighting.
    private readonly Dictionary<GridCell, Material> highlightedCells = new Dictionary<GridCell, Material>();

    /// <summary>
    /// Gets this gladiator's data asset.
    /// </summary>
    public GladiatorData Data => data;

    /// <summary>
    /// Gets the weapon currently equipped by this gladiator.
    /// </summary>
    public WeaponData EquippedWeapon => equippedWeapon;

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
    /// Gets the maximum hit points for this gladiator instance.
    /// </summary>
    public int MaxHP => data != null ? data.maxHP : 0;

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
    public int MaxMP => data != null ? data.movementPoints : 0;

    /// <summary>
    /// Gets the maximum action points for this gladiator.
    /// </summary>
    public int MaxAP => data != null ? data.actionPoints : 0;

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
        currentHP = data != null ? data.maxHP : 0;

        if (data != null)
        {
            remainingMP = data.movementPoints;
            remainingAP = data.actionPoints;
            if (data.startingWeapon != null)
            {
                EquipWeapon(data.startingWeapon);
            }
            else
            {
                UnequipWeapon();
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

        remainingMP = data.movementPoints;
        remainingAP = data.actionPoints;
    }

    /// <summary>
    /// Applies incoming damage, updating hit points and handling death.
    /// </summary>
    /// <param name="damage">Final damage to apply (defense already accounted for).</param>
    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, data != null ? data.maxHP : 0);
        if (DebugSettings.LOG_COMBAT)
        {
            Debug.Log($"Gladiator.TakeDamage - {name} took {damage} damage. HP: {currentHP}/{MaxHP}", this);
        }

        if (currentHP <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles gladiator death: frees the occupied cell and destroys the GameObject.
    /// </summary>
    public void Die()
    {
        Debug.Log($"Gladiator {name} has died.", this);
        ClearHighlights();
        DestroyHealthBar();

        if (gridManager != null && gridManager.IsPositionValid(currentGridPosition))
        {
            GridCell cell = gridManager.GetCellAtPosition(currentGridPosition);
            if (cell != null && cell.OccupyingUnit == gameObject)
            {
                cell.ClearOccupied();
            }
        }

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

    /// <summary>
    /// Returns the current attack range based on equipped weapon or base range.
    /// </summary>
    public int GetAttackRange()
    {
        if (equippedWeapon != null && equippedWeapon.attackRange > 0)
        {
            return equippedWeapon.attackRange;
        }

        return baseAttackRange;
    }

    /// <summary>
    /// Returns the total strength including equipment bonuses.
    /// </summary>
    public int GetTotalStrength()
    {
        int strength = data != null ? data.strength : 0;
        if (equippedWeapon != null)
        {
            strength += equippedWeapon.strengthBonus;
        }
        return strength;
    }

    /// <summary>
    /// Returns the total dexterity including equipment bonuses.
    /// </summary>
    public int GetTotalDexterity()
    {
        int dexterity = data != null ? data.dexterity : 0;
        if (equippedWeapon != null)
        {
            dexterity += equippedWeapon.dexterityBonus;
        }
        return dexterity;
    }

    /// <summary>
    /// Returns the total intelligence including equipment bonuses.
    /// </summary>
    public int GetTotalIntelligence()
    {
        int intelligence = data != null ? data.intelligence : 0;
        if (equippedWeapon != null)
        {
            intelligence += equippedWeapon.intelligenceBonus;
        }
        return intelligence;
    }

    /// <summary>
    /// Returns the total defense including equipment bonuses.
    /// </summary>
    public int GetTotalDefense()
    {
        int defense = data != null ? data.defense : 0;
        if (equippedWeapon != null)
        {
            defense += equippedWeapon.defenseBonus;
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

        if (equippedWeapon != null)
        {
            int baseAttack = equippedWeapon.baseDamage;
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
            return baseAttack;
        }

        return GetTotalStrength();
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
        return slots;
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

        return data.speed + (GetTotalDexterity() / 2);
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

        return GetTotalIntelligence() / 2;
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
                                bool hasLineOfSight = equippedWeapon == null || !equippedWeapon.requiresLineOfSight ||
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

        remainingMP = Mathf.Clamp(remainingMP + amount, 0, data.movementPoints);
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
        transform.localScale = new Vector3(0.5f, 1f, 0.5f);
    }
}

