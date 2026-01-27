using System.Collections;
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

    [Header("UI")]
    [SerializeField]
    private GladiatorInfoWindow infoWindow;

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
    private Coroutine autoEndRoutine;
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

    /// <summary>
    /// Gets whether undo is currently available.
    /// </summary>
    public bool CanUndoMove => selectedGladiator != null &&
                               hasMovedThisTurn &&
                               !hasAttackedThisTurn &&
                               selectedGladiator.RemainingMP > 0 &&
                               lastMoveCost > 0;

    /// <summary>
    /// Returns whether undo is currently available.
    /// </summary>
    public bool CanUndo()
    {
        return selectedGladiator != null && hasMovedThisTurn && !hasAttackedThisTurn;
    }

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
        if (BattleManager.Instance != null)
        {
            BattleManager.BattleState state = BattleManager.Instance.GetBattleState();
            if (state == BattleManager.BattleState.Deployment)
            {
                Debug.Log("PlayerInputController.Update - Skipping, in Deployment");
                return;
            }
        }

        if (BattleManager.Instance == null ||
            BattleManager.Instance.CurrentBattleState != BattleManager.BattleState.PlayerTurn)
        {
            return;
        }

        if (selectedGladiator == null)
        {
            return;
        }

        if (selectedGladiator.IsMoving)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
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
        StopAutoEndRoutine();
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

    private void HandleRightClick()
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
            if (hitGladiator != null)
            {
                if (infoWindow != null)
                {
                    if (infoWindow.IsShowing() && infoWindow.GetCurrentGladiator() == hitGladiator)
                    {
                        infoWindow.Hide();
                    }
                    else
                    {
                        infoWindow.Show(hitGladiator);
                    }
                }

                return;
            }
        }

        if (infoWindow != null && infoWindow.IsShowing())
        {
            infoWindow.Hide();
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
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: Cannot move after attacking this turn.");
            }
            return;
        }

        if (selectedGladiator.RemainingMP <= 0)
        {
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: No MP remaining.");
            }
            return;
        }

        List<Vector2Int> range = selectedGladiator.GetMovementRange();
        if (!range.Contains(targetCell.GridPosition))
        {
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: Target tile is not in movement range.");
            }
            return;
        }

        if (!targetCell.IsWalkable || targetCell.IsOccupied)
        {
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: Target tile is not walkable or is occupied.");
            }
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
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log($"PlayerInputController: Not enough MP to move. Needed {cost}.");
            }
            return;
        }

        lastPosition = currentPos;
        lastMoveCost = cost;
        selectedGladiator.MoveToSmooth(targetPos);
        hasMovedThisTurn = true;

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"PlayerInputController: Moving to {targetPos}, {selectedGladiator.RemainingMP} MP remaining.");
        }
        StartCoroutine(WaitForMoveCompletion());
    }

    private void AttackGladiator(Gladiator target)
    {
        if (selectedGladiator == null || target == null)
        {
            return;
        }

        if (selectedGladiator.IsMoving)
        {
            return;
        }

        if (hasAttackedThisTurn)
        {
            return;
        }

        if (selectedGladiator.RemainingAP <= 0)
        {
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: No AP remaining.");
            }
            return;
        }

        List<Gladiator> attackableTargets = selectedGladiator.GetAttackableTargets();
        if (!attackableTargets.Contains(target))
        {
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: Target is not attackable.");
            }
            return;
        }

        int damage = CombatSystem.CalculateDamage(selectedGladiator, target);
        target.TakeDamage(damage);
        selectedGladiator.TrySpendAP(1);
        hasAttackedThisTurn = true;

        if (DebugSettings.LOG_COMBAT)
        {
            Debug.Log($"PlayerInputController: Attacked {target.name} for {damage} damage.");
        }

        ClearAllHighlights();
        EndTurn();
    }

    /// <summary>
    /// Undoes the last move if no attack has occurred.
    /// </summary>
    public void UndoMove()
    {
        if (selectedGladiator == null || !CanUndoMove)
        {
            return;
        }

        StopAutoEndRoutine();
        selectedGladiator.PlaceOnGrid(lastPosition);
        selectedGladiator.RestoreMP(lastMoveCost);
        hasMovedThisTurn = false;
        lastMoveCost = 0;

        if (DebugSettings.LOG_TURNS)
        {
            Debug.Log("PlayerInputController: Move undone.");
        }
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

    public void CloseInfoWindow()
    {
        if (infoWindow != null && infoWindow.IsShowing())
        {
            infoWindow.Hide();
        }
    }

    private IEnumerator WaitForMoveCompletion()
    {
        if (selectedGladiator == null)
        {
            yield break;
        }

        while (selectedGladiator.IsMoving)
        {
            yield return null;
        }

        ShowMovementAndAttackRange();
        CheckAutoEndTurn();
    }

    private void CheckAutoEndTurn()
    {
        if (selectedGladiator == null)
        {
            return;
        }

        bool canMove = selectedGladiator.RemainingMP > 0 && HasValidMoves();
        bool canAttack = selectedGladiator.RemainingAP > 0 && HasValidAttacks();

        if (!canMove && !canAttack)
        {
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: No actions remaining, auto-ending turn.");
            }
            StartAutoEndRoutine();
        }
    }

    private bool HasValidMoves()
    {
        if (selectedGladiator == null)
        {
            return false;
        }

        List<Vector2Int> movementRange = selectedGladiator.GetMovementRange();
        if (movementRange == null)
        {
            return false;
        }

        foreach (Vector2Int pos in movementRange)
        {
            if (pos != selectedGladiator.CurrentGridPosition)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasValidAttacks()
    {
        if (selectedGladiator == null)
        {
            return false;
        }

        List<Gladiator> targets = selectedGladiator.GetAttackableTargets();
        return targets != null && targets.Count > 0;
    }

    private void StartAutoEndRoutine()
    {
        StopAutoEndRoutine();
        autoEndRoutine = StartCoroutine(AutoEndTurnAfterDelay(0.5f));
    }

    private void StopAutoEndRoutine()
    {
        if (autoEndRoutine != null)
        {
            StopCoroutine(autoEndRoutine);
            autoEndRoutine = null;
        }
    }

    private IEnumerator AutoEndTurnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndTurn();
    }
}
