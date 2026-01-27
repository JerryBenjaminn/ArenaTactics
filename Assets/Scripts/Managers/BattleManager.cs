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

        CalculateInitiative();
        battleState = BattleState.Setup;
        turnPhase = TurnPhase.WaitingForInput;
        currentTurnIndex = 0;
        currentGladiator = null;
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

        Debug.Log("BattleManager: Initiative order:");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            Gladiator gladiator = turnOrder[i];
            string name = gladiator != null && gladiator.Data != null ? gladiator.Data.gladiatorName : "Unknown";
            int speed = gladiator != null && gladiator.Data != null ? gladiator.Data.speed : 0;
            Debug.Log($"  [{i}] {name} (Speed: {speed})");
        }
    }

    /// <summary>
    /// Starts the battle loop using the current turn order.
    /// </summary>
    public void StartBattle()
    {
        if (turnOrder == null || turnOrder.Count == 0)
        {
            Debug.LogWarning("BattleManager.StartBattle - No gladiators available to start battle.");
            return;
        }

        currentTurnIndex = Mathf.Clamp(currentTurnIndex, 0, turnOrder.Count - 1);
        Gladiator first = turnOrder[currentTurnIndex];
        battleState = GetStateForGladiator(first);

        StartTurn();
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
        battleState = GetStateForGladiator(currentGladiator);

        string name = currentGladiator.Data != null ? currentGladiator.Data.gladiatorName : currentGladiator.name;
        int speed = currentGladiator.Data != null ? currentGladiator.Data.speed : 0;
        string team = currentGladiator.Data != null ? currentGladiator.Data.team.ToString() : "Unknown";
        Debug.Log($"BattleManager: Turn started for {name} (Team: {team}, Speed: {speed}).");

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
            battleState = BattleState.Defeat;
            Debug.Log("BattleManager: Defeat! All player gladiators are down.");
        }
        else if (livingEnemies == 0 && livingPlayers > 0)
        {
            battleState = BattleState.Victory;
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
        Debug.Log("BattleManager: AI turn stub - ending turn.");
        EndTurn();
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

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(360f, 10f, 340f, 400f), GUI.skin.box);
        GUILayout.Label("Battle Manager");
        GUILayout.Label($"Battle State: {battleState}");
        GUILayout.Label($"Turn Phase: {turnPhase}");

        string currentName = currentGladiator != null && currentGladiator.Data != null
            ? currentGladiator.Data.gladiatorName
            : "None";
        int currentSpeed = currentGladiator != null && currentGladiator.Data != null
            ? currentGladiator.Data.speed
            : 0;
        GUILayout.Label($"Turn: {currentName} (Speed: {currentSpeed})");

        GUILayout.Space(10f);
        GUILayout.Label("Turn Order:");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            Gladiator gladiator = turnOrder[i];
            if (gladiator == null || gladiator.Data == null)
            {
                continue;
            }

            GUILayout.Label($"[{i}] {gladiator.Data.gladiatorName} (Speed: {gladiator.Data.speed})");
        }

        GUILayout.EndArea();
    }
}
