using System.Collections.Generic;
using ArenaTactics.Data;
using UnityEngine;

/// <summary>
/// Handles pre-battle deployment for player and enemy gladiators.
/// </summary>
public class DeploymentManager : MonoBehaviour
{
    [Header("Deployment Zones")]
    [SerializeField] private int playerDeploymentStartRow = 0;
    [SerializeField] private int playerDeploymentEndRow = 1;
    [SerializeField] private int enemyDeploymentStartRow = 8;
    [SerializeField] private int enemyDeploymentEndRow = 9;

    [Header("Visual Feedback")]
    [SerializeField] private Color deploymentZoneColor = new Color(0.3f, 1f, 0.3f, 0.5f);
    [SerializeField] private Color invalidZoneColor = new Color(1f, 0.3f, 0.3f, 0.3f);

    private readonly List<Gladiator> playerGladiators = new List<Gladiator>();
    private readonly List<Gladiator> enemyGladiators = new List<Gladiator>();
    private readonly Dictionary<GridCell, Material> highlightedCells = new Dictionary<GridCell, Material>();
    private Gladiator selectedGladiator;

    public void StartDeployment(List<Gladiator> allGladiators)
    {
        playerGladiators.Clear();
        enemyGladiators.Clear();

        foreach (Gladiator glad in allGladiators)
        {
            if (glad == null || glad.Data == null)
            {
                continue;
            }

            if (glad.Data.team == Team.Player)
            {
                playerGladiators.Add(glad);
            }
            else
            {
                enemyGladiators.Add(glad);
            }
        }

        HighlightDeploymentZone(playerDeploymentStartRow, playerDeploymentEndRow);
        AutoDeployEnemies();

        if (DebugSettings.LOG_SYSTEM)
        {
            Debug.Log($"Deployment started. {playerGladiators.Count} player gladiators to deploy.");
        }
    }

    public void HandleDeploymentClick(Vector2Int gridPosition)
    {
        Debug.Log($"DeploymentManager.HandleDeploymentClick - Grid position: {gridPosition}");

        if (GridManager.Instance == null)
        {
            Debug.LogError("DeploymentManager - GridManager.Instance is NULL!");
            return;
        }

        GridCell cell = GridManager.Instance.GetCellAtPosition(gridPosition);
        if (cell == null)
        {
            Debug.LogError($"DeploymentManager - No cell at position {gridPosition}");
            return;
        }

        Debug.Log($"DeploymentManager - Cell found. Occupied: {cell.IsOccupied}");

        if (cell.IsOccupied && cell.OccupyingUnit != null)
        {
            Gladiator clicked = cell.OccupyingUnit.GetComponent<Gladiator>();
            if (clicked != null && clicked.IsPlayerControlled)
            {
                Debug.Log($"DeploymentManager - Selecting gladiator: {clicked.name}");
                SelectGladiator(clicked);
                return;
            }
        }

        if (selectedGladiator != null)
        {
            Debug.Log($"DeploymentManager - Attempting to place {selectedGladiator.name} at {gridPosition}");

            bool isValid = IsValidDeploymentPosition(gridPosition);
            Debug.Log($"DeploymentManager - Position valid: {isValid}, Cell occupied: {cell.IsOccupied}");

            if (isValid && !cell.IsOccupied)
            {
                PlaceGladiator(selectedGladiator, gridPosition);
                DeselectGladiator();
                HighlightDeploymentZone(playerDeploymentStartRow, playerDeploymentEndRow);
            }
            else
            {
                Debug.LogWarning($"DeploymentManager - Cannot place: valid={isValid}, occupied={cell.IsOccupied}");
            }
        }
        else
        {
            Debug.Log("DeploymentManager - No gladiator selected");
        }
    }

    public bool AllGladiatorsDeployed()
    {
        foreach (Gladiator glad in playerGladiators)
        {
            if (!IsValidDeploymentPosition(glad.CurrentGridPosition))
            {
                return false;
            }
        }

        return true;
    }

    public void CompleteDeployment()
    {
        ClearHighlights();
        DeselectGladiator();

        if (DebugSettings.LOG_SYSTEM)
        {
            Debug.Log("Deployment phase complete!");
        }
    }

    private void HighlightDeploymentZone(int startRow, int endRow)
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        ClearHighlights();

        for (int y = startRow; y <= endRow; y++)
        {
            for (int x = 0; x < GridManager.Instance.GridWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GridCell cell = GridManager.Instance.GetCellAtPosition(pos);
                if (cell == null || !cell.IsWalkable)
                {
                    continue;
                }

                Renderer renderer = cell.GetComponentInChildren<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                if (!highlightedCells.ContainsKey(cell))
                {
                    highlightedCells[cell] = renderer.sharedMaterial;
                }

                Color highlightColor = cell.IsOccupied ? invalidZoneColor : deploymentZoneColor;
                renderer.material.color = highlightColor;
            }
        }
    }

    private void AutoDeployEnemies()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        List<Vector2Int> enemyPositions = GetDeploymentPositions(
            enemyDeploymentStartRow,
            enemyDeploymentEndRow,
            enemyGladiators.Count);

        for (int i = 0; i < enemyGladiators.Count && i < enemyPositions.Count; i++)
        {
            enemyGladiators[i].PlaceOnGrid(enemyPositions[i]);
        }

        if (DebugSettings.LOG_SYSTEM)
        {
            Debug.Log($"Auto-deployed {enemyGladiators.Count} enemies.");
        }
    }

    private List<Vector2Int> GetDeploymentPositions(int startRow, int endRow, int count)
    {
        var positions = new List<Vector2Int>();
        var availablePositions = new List<Vector2Int>();

        for (int y = startRow; y <= endRow; y++)
        {
            for (int x = 0; x < GridManager.Instance.GridWidth; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GridCell cell = GridManager.Instance.GetCellAtPosition(pos);
                if (cell != null && cell.IsWalkable && !cell.IsOccupied)
                {
                    availablePositions.Add(pos);
                }
            }
        }

        for (int i = 0; i < count && i < availablePositions.Count; i++)
        {
            positions.Add(availablePositions[i]);
        }

        return positions;
    }

    private void SelectGladiator(Gladiator gladiator)
    {
        selectedGladiator = gladiator;
    }

    private void DeselectGladiator()
    {
        selectedGladiator = null;
    }

    private void PlaceGladiator(Gladiator gladiator, Vector2Int position)
    {
        gladiator.PlaceOnGrid(position);
    }

    private bool IsValidDeploymentPosition(Vector2Int position)
    {
        return position.y >= playerDeploymentStartRow && position.y <= playerDeploymentEndRow;
    }

    private void ClearHighlights()
    {
        foreach (KeyValuePair<GridCell, Material> kvp in highlightedCells)
        {
            if (kvp.Key == null)
            {
                continue;
            }

            Renderer renderer = kvp.Key.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = kvp.Value;
            }
        }

        highlightedCells.Clear();
    }
}
