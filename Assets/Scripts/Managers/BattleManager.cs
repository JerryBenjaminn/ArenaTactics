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
        turnOrder = allGladiators
            .Where(g => g != null)
            .OrderByDescending(g => g.Data != null ? g.Data.speed : 0)
            .ThenBy(_ => Random.value)
            .ToList();

        if (DebugSettings.LOG_TURNS)
        {
            Debug.Log("BattleManager: Initiative order:");
            for (int i = 0; i < turnOrder.Count; i++)
            {
                Gladiator gladiator = turnOrder[i];
                string name = gladiator != null && gladiator.Data != null ? gladiator.Data.gladiatorName : "Unknown";
                int speed = gladiator != null && gladiator.Data != null ? gladiator.Data.speed : 0;
                Debug.Log($"  [{i}] {name} (Speed: {speed})");
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
        int speed = currentGladiator.Data != null ? currentGladiator.Data.speed : 0;
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
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("=== DEPLOYMENT CLICK DETECTED ===");

            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("HandleDeploymentInput - Camera.main is NULL!");
                return;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Debug.Log($"HandleDeploymentInput - Raycast from mouse pos: {Input.mousePosition}");

            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            Debug.Log($"HandleDeploymentInput - RaycastAll found {hits.Length} hits");

            foreach (RaycastHit hit in hits)
            {
                Debug.Log($"  Hit: {hit.collider.name} (layer: {hit.collider.gameObject.layer})");
            }

            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
            {
                Debug.Log($"HandleDeploymentInput - Main hit: {hitInfo.collider.name}");

                GridCell cell = hitInfo.collider.GetComponent<GridCell>();
                if (cell == null)
                {
                    cell = hitInfo.collider.GetComponentInParent<GridCell>();
                }

                if (cell != null)
                {
                    Debug.Log($"HandleDeploymentInput - Found GridCell at {cell.GridPosition}");

                    if (deploymentManager != null)
                    {
                        Debug.Log("HandleDeploymentInput - Calling deploymentManager.HandleDeploymentClick");
                        deploymentManager.HandleDeploymentClick(cell.GridPosition);
                    }
                    else
                    {
                        Debug.LogError("HandleDeploymentInput - deploymentManager is NULL!");
                    }
                }
                else
                {
                    Debug.LogWarning($"HandleDeploymentInput - No GridCell found on {hitInfo.collider.name}");

                    Gladiator glad = hitInfo.collider.GetComponent<Gladiator>();
                    if (glad == null)
                    {
                        glad = hitInfo.collider.GetComponentInParent<Gladiator>();
                    }

                    if (glad != null)
                    {
                        Debug.Log($"HandleDeploymentInput - Hit gladiator {glad.name} at grid pos {glad.CurrentGridPosition}");
                        if (deploymentManager != null)
                        {
                            deploymentManager.HandleDeploymentClick(glad.CurrentGridPosition);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("HandleDeploymentInput - Physics.Raycast hit NOTHING");
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
