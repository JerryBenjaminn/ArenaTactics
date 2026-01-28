using System.Collections;
using System.Collections.Generic;
using ArenaTactics.Data;
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
    private readonly Dictionary<GridCell, Material> highlightedAttackCells = new Dictionary<GridCell, Material>();
    private readonly Dictionary<GridCell, Material> highlightedSpellCells = new Dictionary<GridCell, Material>();
    private Vector3 rightClickStartPos;
    private const float MinRightClickDrag = 5f;
    private int selectedSpellIndex = -1;

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
                if (Input.GetMouseButtonDown(1))
                {
                    rightClickStartPos = Input.mousePosition;
                }

                if (Input.GetMouseButtonUp(1))
                {
                    float dragDistance = Vector3.Distance(Input.mousePosition, rightClickStartPos);
                    if (dragDistance < MinRightClickDrag)
                    {
                        CameraController cameraController = FindAnyObjectByType<CameraController>();
                        if (cameraController == null || !cameraController.IsRotating())
                        {
                            HandleRightClick();
                        }
                    }
                }

                if (DebugSettings.VERBOSE_LOGGING)
                {
                    Debug.Log("PlayerInputController.Update - Skipping combat input, in Deployment");
                }
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

        HandleSpellHotkeys();

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }

        if (Input.GetMouseButtonDown(1))
        {
            rightClickStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, rightClickStartPos);
            if (dragDistance >= MinRightClickDrag)
            {
                return;
            }

            CameraController cameraController = FindAnyObjectByType<CameraController>();
            if (cameraController != null && cameraController.IsRotating())
            {
                return;
            }

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

        if (selectedGladiator != null && selectedGladiator != gladiator)
        {
            ClearSelection();
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
        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log("PlayerInputController: Cleared selection and highlights.");
        }
        selectedGladiator = null;
        hasMovedThisTurn = false;
        hasAttackedThisTurn = false;
        lastMoveCost = 0;
        selectedSpellIndex = -1;
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

        if (selectedSpellIndex >= 0)
        {
            if (TryCastSelectedSpell())
            {
                return;
            }
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
            bool inRange;
            bool hasLineOfSight;
            selectedGladiator.CanAttackTarget(target, out inRange, out hasLineOfSight);
            if (!inRange)
            {
                if (DebugSettings.VERBOSE_LOGGING)
                {
                    Debug.Log("PlayerInputController: Target is out of range.");
                }
            }
            else if (!hasLineOfSight)
            {
                if (DebugSettings.VERBOSE_LOGGING)
                {
                    Debug.Log("PlayerInputController: Line of sight is blocked.");
                }
            }

            StartCoroutine(FlashInvalidTarget(target));
            if (DebugSettings.VERBOSE_LOGGING)
            {
                Debug.Log("PlayerInputController: Target is not attackable.");
            }
            return;
        }

        bool didCrit;
        bool didMiss;
        int damage = CombatSystem.CalculateDamage(selectedGladiator, target, out didCrit, out didMiss);
        if (damage > 0)
        {
            target.TakeDamage(damage, selectedGladiator);
        }
        selectedGladiator.TrySpendAP(1);
        hasAttackedThisTurn = true;

        if (DebugSettings.LOG_COMBAT)
        {
            string result = didMiss ? "missed" : $"hit for {damage} damage";
            string critNote = didCrit ? " (CRIT)" : string.Empty;
            Debug.Log($"PlayerInputController: Attacked {target.name} and {result}{critNote}.");
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

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"PlayerInputController: Setting highlights for {selectedGladiator.name}.");
        }

        ClearAllHighlights();
        selectedGladiator.HighlightMovementRange();
        HighlightAttackRange();
        HighlightSpellRange();
    }

    private void HighlightAttackRange()
    {
        if (selectedGladiator == null)
        {
            return;
        }

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log($"PlayerInputController: Setting attack highlights for {selectedGladiator.name}.");
        }

        List<Vector2Int> attackableCells = selectedGladiator.GetAttackableCells();
        foreach (Vector2Int pos in attackableCells)
        {
            GridCell cell = GridManager.Instance != null ? GridManager.Instance.GetCellAtPosition(pos) : null;
            if (cell == null)
            {
                continue;
            }

            Renderer renderer = cell.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                continue;
            }

            if (!highlightedAttackCells.ContainsKey(cell))
            {
                highlightedAttackCells[cell] = renderer.sharedMaterial;
            }

            renderer.material.color = new Color(1f, 0.6f, 0.1f, 0.6f);
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

    private void HighlightSpellRange()
    {
        SpellData spell = GetSelectedSpell();
        if (selectedGladiator == null || spell == null || GridManager.Instance == null)
        {
            return;
        }

        List<Vector2Int> cells = GetSpellTargetCells(spell);
        foreach (Vector2Int pos in cells)
        {
            GridCell cell = GridManager.Instance.GetCellAtPosition(pos);
            if (cell == null)
            {
                continue;
            }

            Renderer renderer = cell.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                continue;
            }

            if (!highlightedSpellCells.ContainsKey(cell))
            {
                highlightedSpellCells[cell] = renderer.sharedMaterial;
            }

            renderer.material.color = new Color(0.4f, 0.6f, 1f, 0.6f);
        }
    }

    public void ClearAllHighlights()
    {
        if (selectedGladiator != null)
        {
            selectedGladiator.ClearHighlights();
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ClearAllCellHighlights();
        }

        foreach (KeyValuePair<GridCell, Material> kvp in highlightedSpellCells)
        {
            GridCell cell = kvp.Key;
            Material originalMat = kvp.Value;
            Renderer renderer = cell != null ? cell.GetComponentInChildren<Renderer>() : null;
            if (renderer != null && originalMat != null)
            {
                renderer.sharedMaterial = originalMat;
            }
        }

        highlightedSpellCells.Clear();

        foreach (KeyValuePair<GridCell, Material> kvp in highlightedAttackCells)
        {
            GridCell cell = kvp.Key;
            Material originalMat = kvp.Value;
            Renderer renderer = cell != null ? cell.GetComponentInChildren<Renderer>() : null;
            if (renderer != null && originalMat != null)
            {
                renderer.sharedMaterial = originalMat;
            }
        }

        highlightedAttackCells.Clear();

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

        if (DebugSettings.VERBOSE_LOGGING)
        {
            Debug.Log("PlayerInputController: Cleared attack cell and target highlights.");
        }
    }

    private void HandleSpellHotkeys()
    {
        if (selectedGladiator == null || selectedGladiator.KnownSpells == null)
        {
            selectedSpellIndex = -1;
            return;
        }

        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSpellIndex(i);
                break;
            }
        }
    }

    public void SelectSpellIndex(int index)
    {
        if (selectedGladiator == null || selectedGladiator.KnownSpells == null)
        {
            selectedSpellIndex = -1;
            return;
        }

        if (index < 0 || index >= selectedGladiator.KnownSpells.Count)
        {
            selectedSpellIndex = -1;
            ShowMovementAndAttackRange();
            return;
        }

        selectedSpellIndex = index;
        ShowMovementAndAttackRange();
    }

    private SpellData GetSelectedSpell()
    {
        if (selectedGladiator == null || selectedGladiator.KnownSpells == null)
        {
            return null;
        }

        if (selectedSpellIndex < 0 || selectedSpellIndex >= selectedGladiator.KnownSpells.Count)
        {
            return null;
        }

        return selectedGladiator.KnownSpells[selectedSpellIndex];
    }

    private List<Vector2Int> GetSpellTargetCells(SpellData spell)
    {
        var cells = new List<Vector2Int>();

        if (selectedGladiator == null || spell == null || GridManager.Instance == null)
        {
            return cells;
        }

        Vector2Int origin = selectedGladiator.CurrentGridPosition;
        int range = spell.range;

        for (int x = 0; x < GridManager.Instance.GridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.GridHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                int distance = Mathf.Abs(pos.x - origin.x) + Mathf.Abs(pos.y - origin.y);
                if (distance > range)
                {
                    continue;
                }

                if (spell.requiresLineOfSight &&
                    !CombatSystem.HasLineOfSight(origin, pos, GridManager.Instance))
                {
                    continue;
                }

                cells.Add(pos);
            }
        }

        return cells;
    }

    private bool TryCastSelectedSpell()
    {
        SpellData spell = GetSelectedSpell();
        if (selectedGladiator == null || spell == null)
        {
            return false;
        }

        if (selectedGladiator.RemainingAP < spell.apCost ||
            selectedGladiator.CurrentSpellSlots < spell.spellSlotCost)
        {
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (spell.spellType == SpellType.AOE && spell.aoeRadius > 0)
        {
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f, gridLayer))
            {
                GridCell cell = hitInfo.collider.GetComponentInParent<GridCell>();
                if (cell != null && selectedGladiator.CastSpellAOE(spell, cell))
                {
                    selectedSpellIndex = -1;
                    ClearAllHighlights();
                    EndTurn();
                    return true;
                }
            }

            return false;
        }

        if (Physics.Raycast(ray, out RaycastHit targetHit, 100f))
        {
            Gladiator target = targetHit.collider.GetComponentInParent<Gladiator>();
            if (target != null && selectedGladiator.CastSpell(spell, target))
            {
                selectedSpellIndex = -1;
                ClearAllHighlights();
                EndTurn();
                return true;
            }
        }

        return false;
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

    private IEnumerator FlashInvalidTarget(Gladiator target)
    {
        if (target == null)
        {
            yield break;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            yield break;
        }

        Material originalMat = renderer.sharedMaterial;
        renderer.material.color = new Color(1f, 0.1f, 0.1f, 1f);
        yield return new WaitForSeconds(0.15f);

        if (renderer != null)
        {
            renderer.sharedMaterial = originalMat;
        }
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
