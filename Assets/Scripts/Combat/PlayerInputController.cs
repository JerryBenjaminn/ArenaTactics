using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player input for moving and attacking with gladiators.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    private static PlayerInputController instance;

    /// <summary>
    /// Gets the active <see cref="PlayerInputController"/> instance in the scene.
    /// </summary>
    public static PlayerInputController Instance => instance;

    [Header("References")]
    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private LayerMask gridLayer;

    [Header("Runtime")]
    [SerializeField]
    private Gladiator selectedGladiator;

    [SerializeField]
    private Vector2Int lastPosition;

    [SerializeField]
    private bool hasMovedThisTurn;

    [SerializeField]
    private bool hasAttackedThisTurn;

    private int lastMoveCost;
    private readonly Dictionary<Gladiator, Material> highlightedTargets = new Dictionary<Gladiator, Material>();

    /// <summary>
    /// Gets the currently selected gladiator.
    /// </summary>
    public Gladiator SelectedGladiator => selectedGladiator;

    /// <summary>
    /// Gets whether the player has moved this turn.
    /// </summary>
    public bool HasMovedThisTurn => hasMovedThisTurn;

    /// <summary>
    /// Gets whether the player has attacked this turn.
    /// </summary>
    public bool HasAttackedThisTurn => hasAttackedThisTurn;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (gridLayer == 0)
        {
            gridLayer = LayerMask.GetMask("Grid");
            if (gridLayer == 0)
            {
                gridLayer = ~0;
            }
        }
    }

    private void Update()
    {
        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentBattleState != BattleManager.BattleState.PlayerTurn)
        {
            return;
        }

        if (selectedGladiator == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// Initializes input handling for the provided gladiator.
    /// </summary>
    public void Initialize(Gladiator gladiator)
    {
        if (gladiator == null)
        {
            return;
        }

        selectedGladiator = gladiator;
        lastPosition = gladiator.CurrentGridPosition;
        lastMoveCost = 0;
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        ShowMovementAndAttackRange();
    }

    /// <summary>
    /// Clears the current selection and removes highlights.
    /// </summary>
    public void ClearSelection()
    {
        ClearAllHighlights();
        selectedGladiator = null;
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
        lastMoveCost = 0;
    }

    private void HandleMouseClick()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            Gladiator hitGladiator = hitInfo.collider.GetComponentInParent<Gladiator>();
            if (hitGladiator != null && hitGladiator != selectedGladiator)
            {
                AttackGladiator(hitGladiator);
                return;
            }
        }

        if (Physics.Raycast(ray, out RaycastHit gridHit, 100f, gridLayer))
        {
            GridCell cell = gridHit.collider.GetComponentInParent<GridCell>();
            if (cell != null)
            {
                AttemptMove(cell);
            }
        }
    }

    private void AttemptMove(GridCell targetCell)
    {
        if (selectedGladiator == null || targetCell == null)
        {
            return;
        }

        if (hasAttackedThisTurn)
        {
            Debug.Log("PlayerInputController: Cannot move after attacking this turn.");
            return;
        }

        if (selectedGladiator.RemainingMP <= 0)
        {
            Debug.Log("PlayerInputController: No MP remaining.");
            return;
        }

        List<Vector2Int> range = selectedGladiator.GetMovementRange();
        if (!range.Contains(targetCell.GridPosition))
        {
            Debug.Log("PlayerInputController: Target tile is not in movement range.");
            return;
        }

        if (!targetCell.IsWalkable || targetCell.IsOccupied)
        {
            Debug.Log("PlayerInputController: Target tile is not walkable or is occupied.");
            return;
        }

        MoveGladiator(targetCell);
    }

    private void MoveGladiator(GridCell targetCell)
    {
        Vector2Int targetPos = targetCell.GridPosition;
        Vector2Int currentPos = selectedGladiator.CurrentGridPosition;
        int cost = Mathf.Abs(targetPos.x - currentPos.x) + Mathf.Abs(targetPos.y - currentPos.y);

        if (cost <= 0)
        {
            return;
        }

        if (!selectedGladiator.TrySpendMP(cost))
        {
            Debug.Log($"PlayerInputController: Not enough MP to move. Needed {cost}.");
            return;
        }

        lastPosition = currentPos;
        lastMoveCost = cost;
        selectedGladiator.PlaceOnGrid(targetPos);
        hasMovedThisTurn = true;

        Debug.Log($"PlayerInputController: Moved to {targetPos}, {selectedGladiator.RemainingMP} MP remaining.");
        ShowMovementAndAttackRange();
    }

    private void AttackGladiator(Gladiator target)
    {
        if (selectedGladiator == null || target == null)
        {
            return;
        }

        if (hasAttackedThisTurn)
        {
            return;
        }

        if (selectedGladiator.RemainingAP <= 0)
        {
            Debug.Log("PlayerInputController: No AP remaining.");
            return;
        }

        List<Gladiator> attackableTargets = selectedGladiator.GetAttackableTargets();
        if (!attackableTargets.Contains(target))
        {
            Debug.Log("PlayerInputController: Target is not attackable.");
            return;
        }

        int damage = CombatSystem.CalculateDamage(selectedGladiator, target);
        target.TakeDamage(damage);
        selectedGladiator.TrySpendAP(1);
        hasAttackedThisTurn = true;

        Debug.Log($"PlayerInputController: Attacked {target.name} for {damage} damage.");

        ClearAllHighlights();
        EndTurn();
    }

    /// <summary>
    /// Undoes the last move if no attack has occurred.
    /// </summary>
    public void UndoMove()
    {
        if (selectedGladiator == null || !hasMovedThisTurn || hasAttackedThisTurn)
        {
            return;
        }

        selectedGladiator.PlaceOnGrid(lastPosition);
        selectedGladiator.RestoreMP(lastMoveCost);
        hasMovedThisTurn = false;
        lastMoveCost = 0;

        Debug.Log("PlayerInputController: Move undone.");
        ShowMovementAndAttackRange();
    }

    /// <summary>
    /// Highlights movement and attack ranges for the selected gladiator.
    /// </summary>
    public void ShowMovementAndAttackRange()
    {
        if (selectedGladiator == null)
        {
            return;
        }

        ClearAllHighlights();
        selectedGladiator.HighlightMovementRange();
        HighlightAttackRange();
    }

    private void HighlightAttackRange()
    {
        if (selectedGladiator == null)
        {
            return;
        }

        List<Gladiator> targets = selectedGladiator.GetAttackableTargets();
        foreach (Gladiator target in targets)
        {
            Renderer renderer = target != null ? target.GetComponentInChildren<Renderer>() : null;
            if (renderer == null)
            {
                continue;
            }

            if (!highlightedTargets.ContainsKey(target))
            {
                highlightedTargets[target] = renderer.sharedMaterial;
            }

            renderer.material.color = new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    private void ClearAllHighlights()
    {
        if (selectedGladiator != null)
        {
            selectedGladiator.ClearHighlights();
        }

        foreach (KeyValuePair<Gladiator, Material> kvp in highlightedTargets)
        {
            Gladiator target = kvp.Key;
            Material originalMat = kvp.Value;
            Renderer renderer = target != null ? target.GetComponentInChildren<Renderer>() : null;
            if (renderer != null && originalMat != null)
            {
                renderer.sharedMaterial = originalMat;
            }
        }

        highlightedTargets.Clear();
    }

    /// <summary>
    /// Ends the current turn.
    /// </summary>
    public void EndTurn()
    {
        Gladiator gladiator = selectedGladiator;
        ClearSelection();

        if (gladiator != null)
        {
            gladiator.OnTurnEnd();
        }
    }
}
