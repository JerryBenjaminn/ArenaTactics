using System.Collections.Generic;
using System.Linq;
using ArenaTactics.Data;
using UnityEngine;

/// <summary>
/// Core combat controller that manages turn order and battle flow.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public enum BattleState
    {
        Setup,
        Deployment,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }

    public enum TurnPhase
    {
        WaitingForInput,
        MovementSelected,
        ActionSelected,
        TurnEnding
    }

    private static BattleManager instance;

    /// <summary>
    /// Gets the active <see cref="BattleManager"/> instance in the scene.
    /// </summary>
    public static BattleManager Instance => instance;

    [Header("Battle Runtime")]
    [SerializeField]
    private List<Gladiator> allGladiators = new List<Gladiator>();

    [SerializeField]
    private List<Gladiator> turnOrder = new List<Gladiator>();

    [SerializeField]
    private int currentTurnIndex;

    [SerializeField]
    private Gladiator currentGladiator;

    [SerializeField]
    private BattleState battleState = BattleState.Setup;

    [SerializeField]
    private TurnPhase turnPhase = TurnPhase.WaitingForInput;

    [Header("UI")]
    [SerializeField]
    private CurrentTurnPanel currentTurnPanel;

    [SerializeField]
    private InitiativeQueue initiativeQueue;

    [Header("Deployment")]
    [SerializeField]
    private DeploymentManager deploymentManager;

    [SerializeField]
    private DeploymentUI deploymentUI;

    private Vector3 lastMouseWorldPos;
    private bool wasMouseDown;
    private Vector3 dragStartMousePos;
    private const float MinDragDistance = 0.1f;

    /// <summary>
    /// Gets all gladiators participating in the battle.
    /// </summary>
    public List<Gladiator> AllGladiators => allGladiators;

    /// <summary>
    /// Gets the current ordered list of gladiators by initiative.
    /// </summary>
    public List<Gladiator> TurnOrder => turnOrder;

    /// <summary>
    /// Gets the gladiator whose turn is currently active.
    /// </summary>
    public Gladiator CurrentGladiator => currentGladiator;

    /// <summary>
    /// Gets the current battle state.
    /// </summary>
    public BattleState CurrentBattleState => battleState;

    /// <summary>
    /// Returns the current battle state.
    /// </summary>
    public BattleState GetBattleState()
    {
        return battleState;
    }

    /// <summary>
    /// Gets the current turn phase.
    /// </summary>
    public TurnPhase CurrentTurnPhase => turnPhase;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Multiple BattleManager instances found. Destroying duplicate.", this);
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(this);
            }
            else
            {
                Destroy(gameObject);
            }
#else
            Destroy(gameObject);
#endif
            return;
        }

        instance = this;
    }

    /// <summary>
    /// Initializes the battle with a list of participating gladiators.
    /// </summary>
    /// <param name="gladiators">Gladiators participating in this battle.</param>
    public void Initialize(List<Gladiator> gladiators)
    {
        allGladiators = gladiators == null
            ? new List<Gladiator>()
            : gladiators.Where(g => g != null).ToList();

        SetBattleState(BattleState.Setup);
        turnPhase = TurnPhase.WaitingForInput;
        currentTurnIndex = 0;
        currentGladiator = null;

        StartDeploymentPhase();
    }

    /// <summary>
    /// Calculates the initiative order based on gladiator speed.
    /// </summary>
    public void CalculateInitiative()
    {
        if (allGladiators == null || allGladiators.Count == 0)
        {
            turnOrder = new List<Gladiator>();
            return;
        }

        turnOrder = allGladiators
            .Where(g => g != null)
            .OrderByDescending(g => g.GetInitiative())
            .ThenBy(_ => Random.value)
            .ToList();

        if (DebugSettings.LOG_TURNS)
        {
            Debug.Log("BattleManager: Initiative order:");
            for (int i = 0; i < turnOrder.Count; i++)
            {
                Gladiator gladiator = turnOrder[i];
                if (gladiator == null || gladiator.Data == null)
                {
                    continue;
                }

                int initiative = gladiator.GetInitiative();
                Debug.Log($"  [{i}] {gladiator.Data.gladiatorName}: {initiative} (Speed {gladiator.Data.Speed} + DEX {gladiator.Data.Dexterity}/2)");
            }
        }
    }

    /// <summary>
    /// Starts the battle loop using the current turn order.
    /// </summary>
    public void StartBattle()
    {
        CalculateInitiative();

        if (turnOrder == null || turnOrder.Count == 0)
        {
            Debug.LogWarning("BattleManager.StartBattle - No gladiators available to start battle.");
            return;
        }

        currentTurnIndex = Mathf.Clamp(currentTurnIndex, 0, turnOrder.Count - 1);
        Gladiator first = turnOrder[currentTurnIndex];
        SetBattleState(GetStateForGladiator(first));

        if (initiativeQueue != null)
        {
            initiativeQueue.UpdateQueue(turnOrder, currentTurnIndex);
        }

        StartTurn();
    }

    public void CompleteDeployment()
    {
        if (deploymentManager != null)
        {
            if (!deploymentManager.AllGladiatorsDeployed())
            {
                Debug.LogWarning("Not all gladiators are deployed!");
                return;
            }

            deploymentManager.CompleteDeployment();
        }

        if (deploymentUI != null)
        {
            deploymentUI.Hide();
        }

        StartBattle();
    }

    /// <summary>
    /// Starts the current gladiator's turn.
    /// </summary>
    public void StartTurn()
    {
        if (turnOrder == null || turnOrder.Count == 0)
        {
            Debug.LogWarning("BattleManager.StartTurn - No gladiators in turn order.");
            return;
        }

        currentTurnIndex = Mathf.Clamp(currentTurnIndex, 0, turnOrder.Count - 1);
        currentGladiator = turnOrder[currentTurnIndex];
        if (currentGladiator == null)
        {
            EndTurn();
            return;
        }

        currentGladiator.ResetTurnPoints();
        turnPhase = TurnPhase.WaitingForInput;
        SetBattleState(GetStateForGladiator(currentGladiator));

        string name = currentGladiator.Data != null ? currentGladiator.Data.gladiatorName : currentGladiator.name;
        int speed = currentGladiator.Data != null ? currentGladiator.Data.Speed : 0;
        string team = currentGladiator.Data != null ? currentGladiator.Data.team.ToString() : "Unknown";
        if (DebugSettings.LOG_TURNS)
        {
            Debug.Log($"BattleManager: Turn started for {name} (Team: {team}, Speed: {speed}).");
        }

        if (currentTurnPanel != null)
        {
            currentTurnPanel.UpdatePanel(currentGladiator);
        }

        if (initiativeQueue != null)
        {
            initiativeQueue.UpdateQueue(turnOrder, currentTurnIndex);
        }

        if (currentGladiator.IsPlayerControlled)
        {
            EnsurePlayerInputController().Initialize(currentGladiator);
            return;
        }

        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.ClearSelection();
        }

        AITakeTurn();
    }

    /// <summary>
    /// Ends the current gladiator's turn and advances to the next.
    /// </summary>
    public void EndTurn()
    {
        if (PlayerInputController.Instance != null)
        {
            PlayerInputController.Instance.CloseInfoWindow();
        }

        if (currentGladiator != null)
        {
            currentGladiator.ClearHighlights();
        }

        currentTurnIndex++;
        if (turnOrder == null || turnOrder.Count == 0)
        {
            return;
        }

        if (currentTurnIndex >= turnOrder.Count)
        {
            currentTurnIndex = 0;
        }

        RemoveDeadGladiators();
        CheckVictoryConditions();

        if (battleState == BattleState.Victory || battleState == BattleState.Defeat)
        {
            return;
        }

        if (turnOrder == null || turnOrder.Count == 0)
        {
            return;
        }

        if (currentTurnIndex >= turnOrder.Count)
        {
            currentTurnIndex = 0;
        }

        StartTurn();
    }

    /// <summary>
    /// Checks for battle victory or defeat conditions.
    /// </summary>
    public void CheckVictoryConditions()
    {
        int livingPlayers = 0;
        int livingEnemies = 0;

        foreach (Gladiator gladiator in allGladiators)
        {
            if (gladiator == null || gladiator.CurrentHP <= 0 || gladiator.Data == null)
            {
                continue;
            }

            if (gladiator.Data.team == Team.Player)
            {
                livingPlayers++;
            }
            else if (gladiator.Data.team == Team.Enemy)
            {
                livingEnemies++;
            }
        }

        if (livingPlayers == 0 && livingEnemies > 0)
        {
            SetBattleState(BattleState.Defeat);
            Debug.Log("BattleManager: Defeat! All player gladiators are down.");
        }
        else if (livingEnemies == 0 && livingPlayers > 0)
        {
            SetBattleState(BattleState.Victory);
            Debug.Log("BattleManager: Victory! All enemy gladiators are down.");
        }
    }

    /// <summary>
    /// Removes dead gladiators from the turn order.
    /// </summary>
    public void RemoveDeadGladiators()
    {
        if (turnOrder == null)
        {
            return;
        }

        turnOrder = turnOrder
            .Where(g => g != null && g.CurrentHP > 0)
            .ToList();
    }

    /// <summary>
    /// Returns the current gladiator.
    /// </summary>
    public Gladiator GetCurrentGladiator()
    {
        return currentGladiator;
    }

    /// <summary>
    /// Returns the current turn order list.
    /// </summary>
    public List<Gladiator> GetTurnOrder()
    {
        return turnOrder;
    }

    private void AITakeTurn()
    {
        if (currentGladiator == null)
        {
            return;
        }

        if (DebugSettings.LOG_AI)
        {
            Debug.Log($"BattleManager: AI taking turn for {currentGladiator.name}");
        }
        StartCoroutine(AIController.ExecuteAITurn(currentGladiator));
    }

    private void Update()
    {
        if (battleState == BattleState.Deployment)
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"Update - In Deployment state (frame {Time.frameCount})");
            }

            HandleDeploymentInput();
        }
    }

    private void StartDeploymentPhase()
    {
        SetBattleState(BattleState.Deployment);

        if (deploymentManager != null)
        {
            deploymentManager.StartDeployment(allGladiators);
        }

        if (deploymentUI != null)
        {
            deploymentUI.Show();
        }

        if (DebugSettings.LOG_SYSTEM)
        {
            Debug.Log("Entered deployment phase. Position your gladiators and click Ready.");
        }
    }

    private void SetBattleState(BattleState newState)
    {
        if (battleState == newState)
        {
            return;
        }

        if (DebugSettings.LOG_SYSTEM)
        {
            Debug.Log($"BattleManager: BattleState {battleState} -> {newState}");
        }

        battleState = newState;
    }

    private void HandleDeploymentInput()
    {
        if (deploymentManager == null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        Vector3 worldPos = Vector3.zero;

        if (groundPlane.Raycast(ray, out distance))
        {
            worldPos = ray.GetPoint(distance);
            lastMouseWorldPos = worldPos;
        }

        if (Input.GetMouseButtonDown(0))
        {
            wasMouseDown = true;
            dragStartMousePos = Input.mousePosition;

            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            {
                Gladiator glad = hitInfo.collider.GetComponentInParent<Gladiator>();
                if (glad != null && glad.IsPlayerControlled)
                {
                    deploymentManager.StartDrag(glad.CurrentGridPosition);
                    return;
                }

                GridCell cell = hitInfo.collider.GetComponentInParent<GridCell>();
                if (cell != null && cell.IsOccupied)
                {
                    glad = cell.OccupyingUnit != null
                        ? cell.OccupyingUnit.GetComponent<Gladiator>()
                        : null;
                    if (glad != null && glad.IsPlayerControlled)
                    {
                        deploymentManager.StartDrag(cell.GridPosition);
                        return;
                    }
                }
            }
        }

        if (Input.GetMouseButton(0) && wasMouseDown)
        {
            if (deploymentManager.IsDragging())
            {
                deploymentManager.UpdateDrag(lastMouseWorldPos);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            wasMouseDown = false;

            if (deploymentManager.IsDragging())
            {
                Debug.Log("=== ENDING DRAG ===");

                Ray releaseRay = cam.ScreenPointToRay(Input.mousePosition);
                Debug.Log($"EndDrag - Mouse position: {Input.mousePosition}");

                float dragDistance = Vector3.Distance(Input.mousePosition, dragStartMousePos);
                Debug.Log($"Mouse released, drag distance: {dragDistance}");

                if (dragDistance <= MinDragDistance)
                {
                    Debug.Log("Drag distance too small, treating as click");
                    deploymentManager.CancelDrag();

                    if (Physics.Raycast(releaseRay, out RaycastHit clickHit, 100f))
                    {
                        GridCell clickCell = clickHit.collider.GetComponentInParent<GridCell>();
                        if (clickCell != null)
                        {
                            deploymentManager.HandleDeploymentClick(clickCell.GridPosition);
                        }
                    }
                    return;
                }

                if (Physics.Raycast(releaseRay, out RaycastHit hitInfo, 100f))
                {
                    Debug.Log($"EndDrag - Hit: {hitInfo.collider.name}");

                    GridCell cell = hitInfo.collider.GetComponent<GridCell>();
                    if (cell == null)
                    {
                        cell = hitInfo.collider.GetComponentInParent<GridCell>();
                    }

                    if (cell != null)
                    {
                        Debug.Log($"EndDrag - Found GridCell at {cell.GridPosition}");
                        deploymentManager.EndDrag(cell.GridPosition);
                        return;
                    }

                    Debug.LogWarning($"EndDrag - No GridCell on {hitInfo.collider.name}");
                }

                if (groundPlane.Raycast(releaseRay, out distance))
                {
                    worldPos = releaseRay.GetPoint(distance);
                    Debug.Log($"EndDrag - World position: {worldPos}");

                    if (GridManager.Instance != null)
                    {
                        Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPos);
                        Debug.Log($"EndDrag - Converted to grid position: {gridPos}");

                        if (GridManager.Instance.IsPositionValid(gridPos))
                        {
                            Debug.Log("EndDrag - Using converted grid position");
                            deploymentManager.EndDrag(gridPos);
                            return;
                        }

                        Debug.LogWarning($"EndDrag - Converted position {gridPos} is invalid");
                    }
                }

                Debug.LogWarning("EndDrag - No valid position found, cancelling");
                deploymentManager.CancelDrag();
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    GridCell cell = hit.collider.GetComponentInParent<GridCell>();
                    if (cell != null)
                    {
                        deploymentManager.HandleDeploymentClick(cell.GridPosition);
                    }
                }
            }
        }
    }

    private BattleState GetStateForGladiator(Gladiator gladiator)
    {
        if (gladiator != null && gladiator.Data != null && gladiator.Data.team == Team.Enemy)
        {
            return BattleState.EnemyTurn;
        }

        return BattleState.PlayerTurn;
    }

    private PlayerInputController EnsurePlayerInputController()
    {
        if (PlayerInputController.Instance != null)
        {
            return PlayerInputController.Instance;
        }

        GameObject controllerObject = new GameObject("PlayerInputController");
        return controllerObject.AddComponent<PlayerInputController>();
    }

    // OnGUI debug UI removed (replaced by CurrentTurnPanel + InitiativeQueue).
}
