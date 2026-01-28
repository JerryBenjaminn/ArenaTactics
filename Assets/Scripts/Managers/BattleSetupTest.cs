using System.Collections.Generic;
using ArenaTactics.Data;
using UnityEngine;

/// <summary>
/// Temporary test harness to spawn gladiators and exercise the grid and gladiator systems.
/// Attach this to an empty GameObject in a scene containing a GridManager.
/// </summary>
public class BattleSetupTest : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject playerGladiatorPrefab;

    [SerializeField]
    private GameObject enemyGladiatorPrefab;

    [Header("Player Class Data")]
    [SerializeField]
    private GladiatorData playerWarriorData;

    [SerializeField]
    private GladiatorData playerRogueData;

    [SerializeField]
    private GladiatorData playerArcherData;

    [SerializeField]
    private GladiatorData playerMageData;

    [SerializeField]
    private GladiatorData playerTankData;

    [Header("Enemy Class Data")]
    [SerializeField]
    private GladiatorData enemyWarriorData;

    [SerializeField]
    private GladiatorData enemyRogueData;

    [SerializeField]
    private GladiatorData enemyArcherData;

    [SerializeField]
    private GladiatorData enemyMageData;

    [SerializeField]
    private GladiatorData enemyTankData;

    [Header("Test Weapons")]
    [SerializeField]
    private WeaponData basicSword;

    [SerializeField]
    private WeaponData basicSpear;

    [Header("Runtime")]
    [SerializeField]
    private List<Gladiator> spawnedGladiators = new List<Gladiator>();

    private void Start()
    {
        // Ensure the grid manager is available.
        if (GridManager.Instance == null)
        {
            Debug.LogError("BattleSetupTest: No GridManager instance found in the scene.");
            return;
        }

        SpawnTestGladiators();
        InitializeBattleManager();
    }

    /// <summary>
    /// Spawns four test gladiators into the scene at predefined positions.
    /// </summary>
    private void SpawnTestGladiators()
    {
        spawnedGladiators.Clear();

        if (playerGladiatorPrefab == null || enemyGladiatorPrefab == null)
        {
            Debug.LogError("BattleSetupTest: Gladiator prefabs are not assigned.");
            return;
        }

        if (playerWarriorData == null || playerRogueData == null || playerArcherData == null ||
            playerMageData == null || playerTankData == null ||
            enemyWarriorData == null || enemyRogueData == null || enemyArcherData == null ||
            enemyMageData == null || enemyTankData == null)
        {
            Debug.LogError("BattleSetupTest: One or more GladiatorData assets are not assigned.");
            return;
        }

        // Player gladiators (initially placed in deployment zone).
        Gladiator playerWarrior = SpawnGladiator(playerGladiatorPrefab, playerWarriorData, new Vector2Int(0, 0), true, true);
        Gladiator playerRogue = SpawnGladiator(playerGladiatorPrefab, playerRogueData, new Vector2Int(1, 0), true, true);
        SpawnGladiator(playerGladiatorPrefab, playerArcherData, new Vector2Int(2, 0), true, true);
        SpawnGladiator(playerGladiatorPrefab, playerMageData, new Vector2Int(3, 0), true, true);
        SpawnGladiator(playerGladiatorPrefab, playerTankData, new Vector2Int(4, 0), true, true);

        // Enemy gladiators (spawned off-grid; auto-deployed by DeploymentManager).
        SpawnGladiator(enemyGladiatorPrefab, enemyWarriorData, new Vector2Int(0, 9), false, false);
        SpawnGladiator(enemyGladiatorPrefab, enemyRogueData, new Vector2Int(1, 9), false, false);
        SpawnGladiator(enemyGladiatorPrefab, enemyArcherData, new Vector2Int(2, 9), false, false);
        SpawnGladiator(enemyGladiatorPrefab, enemyMageData, new Vector2Int(3, 9), false, false);
        SpawnGladiator(enemyGladiatorPrefab, enemyTankData, new Vector2Int(4, 9), false, false);

        EquipTestWeapons(playerWarrior, playerRogue);
        Debug.Log($"BattleSetupTest: Spawned {spawnedGladiators.Count} gladiators.");
    }

    private void EquipTestWeapons(Gladiator playerWarrior, Gladiator playerRogue)
    {
        if (playerWarrior != null && basicSword != null)
        {
            playerWarrior.EquipWeapon(basicSword);
            Debug.Log($"BattleSetupTest: Equipped {basicSword.weaponName} on {playerWarrior.name}.");
        }

        if (playerRogue != null && basicSpear != null)
        {
            playerRogue.EquipWeapon(basicSpear);
            Debug.Log($"BattleSetupTest: Equipped {basicSpear.weaponName} on {playerRogue.name}.");
        }
    }

    private void InitializeBattleManager()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("BattleSetupTest: No BattleManager instance found in the scene.");
            return;
        }

        BattleManager.Instance.Initialize(spawnedGladiators);
    }

    /// <summary>
    /// Spawns a single gladiator and initializes it on the grid.
    /// </summary>
    /// <param name="prefab">The gladiator prefab to instantiate.</param>
    /// <param name="data">The data asset describing the gladiator.</param>
    /// <param name="gridPos">The desired starting grid position.</param>
    /// <param name="isPlayer">Whether the gladiator is player-controlled.</param>
    /// <returns>The spawned <see cref="Gladiator"/> component, or <c>null</c> if spawning failed.</returns>
    public Gladiator SpawnGladiator(GameObject prefab, GladiatorData data, Vector2Int gridPos, bool isPlayer, bool placeOnGrid = true)
    {
        if (prefab == null || data == null)
        {
            Debug.LogWarning("BattleSetupTest: Cannot spawn gladiator, prefab or data is null.");
            return null;
        }

        if (GridManager.Instance == null)
        {
            Debug.LogError("BattleSetupTest: No GridManager instance available when attempting to spawn a gladiator.");
            return null;
        }

        GameObject instance = Instantiate(prefab);
        instance.name = data.gladiatorName;

        Gladiator gladiator = instance.GetComponent<Gladiator>();
        if (gladiator == null)
        {
            Debug.LogError("BattleSetupTest: Spawned prefab does not contain a Gladiator component.");
            Destroy(instance);
            return null;
        }

        gladiator.Initialize(data, gridPos, isPlayer, placeOnGrid);
        spawnedGladiators.Add(gladiator);

        gladiator.CreateHealthBar();

        Debug.Log($"BattleSetupTest: Spawned gladiator '{data.gladiatorName}' at {gridPos}.");
        return gladiator;
    }

    private void Update()
    {
        if (spawnedGladiators == null || spawnedGladiators.Count == 0)
        {
            return;
        }

        // Highlight movement range for first player gladiator.
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (spawnedGladiators.Count > 0 && spawnedGladiators[0] != null)
            {
                Debug.Log("BattleSetupTest: Highlighting movement range for Player 1.");
                spawnedGladiators[0].HighlightMovementRange();
            }
        }

        // Highlight movement range for second player gladiator.
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (spawnedGladiators.Count > 1 && spawnedGladiators[1] != null)
            {
                Debug.Log("BattleSetupTest: Highlighting movement range for Player 2.");
                spawnedGladiators[1].HighlightMovementRange();
            }
        }

        // Clear all highlights.
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("BattleSetupTest: Clearing all gladiator highlights.");
            foreach (Gladiator g in spawnedGladiators)
            {
                if (g != null)
                {
                    g.ClearHighlights();
                }
            }
        }

        // Reset turn points for all gladiators.
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("BattleSetupTest: Resetting turn points for all gladiators.");
            foreach (Gladiator g in spawnedGladiators)
            {
                if (g != null)
                {
                    g.ResetTurnPoints();
                }
            }
        }

        // Advance the turn manually for testing.
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (BattleManager.Instance != null)
            {
                Debug.Log("BattleSetupTest: Advancing turn manually.");
                BattleManager.Instance.EndTurn();
            }
        }

        // Log current positions of all gladiators.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("BattleSetupTest: Gladiator positions:");

            for (int i = 0; i < spawnedGladiators.Count; i++)
            {
                Gladiator g = spawnedGladiators[i];
                if (g == null)
                {
                    continue;
                }

                string name = g.Data != null ? g.Data.gladiatorName : g.name;
                Vector2Int pos = g.CurrentGridPosition;
                Debug.Log($"  [{i}] {name} at {pos}");
            }
        }
    }

    // OnGUI debug UI removed (replaced by UI panels).
}

