using System.Collections.Generic;
using ArenaTactics.Data;
using UnityEngine;

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
    /// Gets the remaining movement points for this turn.
    /// </summary>
    public int RemainingMP => remainingMP;

    /// <summary>
    /// Gets the remaining action points for this turn.
    /// </summary>
    public int RemainingAP => remainingAP;

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

    /// <summary>
    /// Initializes the gladiator with data, starting position and control side.
    /// </summary>
    /// <param name="gladiatorData">The data asset describing this gladiator.</param>
    /// <param name="startPos">The starting grid position.</param>
    /// <param name="playerControlled">Whether this gladiator is player controlled.</param>
    public void Initialize(GladiatorData gladiatorData, Vector2Int startPos, bool playerControlled)
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
        PlaceOnGrid(startPos);
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
    /// <param name="damage">Amount of damage to apply before defense.</param>
    public void TakeDamage(int damage)
    {
        if (data == null)
        {
            return;
        }

        int mitigatedDamage = Mathf.Max(0, damage - data.defense);
        currentHP -= mitigatedDamage;
        currentHP = Mathf.Clamp(currentHP, 0, data.maxHP);

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
        ClearHighlights();

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
    /// Calculates all grid positions this gladiator can move to using BFS,
    /// constrained by remaining movement points, walkability and occupancy.
    /// </summary>
    /// <returns>A list of reachable grid positions.</returns>
    public List<Vector2Int> GetMovementRange()
    {
        var reachable = new List<Vector2Int>();

        Debug.Log($"Gladiator.GetMovementRange - Getting movement range from position: {currentGridPosition}, remainingMP: {remainingMP}", this);

        if (gridManager == null || remainingMP <= 0)
        {
            reachable.Add(currentGridPosition);
            Debug.Log($"Gladiator.GetMovementRange - GridManager null or no MP. Only current position is reachable. Count: {reachable.Count}", this);
            return reachable;
        }

        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int cost)>();

        visited.Add(currentGridPosition);
        queue.Enqueue((currentGridPosition, 0));
        Debug.Log($"Gladiator.GetMovementRange - Enqueued start position {currentGridPosition} with cost 0", this);

        while (queue.Count > 0)
        {
            var (pos, cost) = queue.Dequeue();
            reachable.Add(pos);
            Debug.Log($"Gladiator.GetMovementRange - Dequeued {pos} with cost {cost}, added to reachable", this);

            if (cost >= remainingMP)
            {
                Debug.Log($"Gladiator.GetMovementRange - Reached max cost at {pos} (cost {cost}), not exploring neighbors.", this);
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
                Debug.Log($"Gladiator.GetMovementRange - Enqueued neighbor {neighborPos} with cost {cost + 1}", this);
            }
        }

        Debug.Log($"Gladiator.GetMovementRange - Found {reachable.Count} reachable positions.", this);
        return reachable;
    }

    /// <summary>
    /// Equips the provided weapon on this gladiator.
    /// </summary>
    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;

        string weaponName = equippedWeapon != null ? equippedWeapon.weaponName : "None";
        Debug.Log($"Gladiator.EquipWeapon - {name} equipped weapon: {weaponName}", this);
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
    /// Returns the total attack value including weapon damage.
    /// </summary>
    public int GetTotalAttack()
    {
        int baseAttack = data != null ? data.attack : 0;
        int weaponDamage = equippedWeapon != null ? equippedWeapon.baseDamage : 0;
        return baseAttack + weaponDamage;
    }

    /// <summary>
    /// Returns a list of enemy gladiators within attack range.
    /// </summary>
    public List<Gladiator> GetAttackableTargets()
    {
        var targets = new List<Gladiator>();

        if (gridManager == null)
        {
            gridManager = GridManager.Instance;
        }

        if (gridManager == null)
        {
            Debug.LogWarning("Gladiator.GetAttackableTargets - GridManager reference is null.", this);
            return targets;
        }

        int range = GetAttackRange();
        Debug.Log($"Gladiator.GetAttackableTargets - Checking targets within range {range} from {currentGridPosition}.", this);

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
                    if (target != null && target != this && target.Data != null && data != null &&
                        target.Data.team != data.team)
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

        Debug.Log($"Gladiator.GetAttackableTargets - Found {targets.Count} target(s).", this);
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
        Debug.Log($"Gladiator.HighlightMovementRange - Highlighting {range.Count} positions.", this);

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
            Debug.Log($"Gladiator.HighlightMovementRange - Highlighting position ({pos.x},{pos.y}) with color {(pos == currentGridPosition ? "Yellow" : "Green")}.", this);
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

