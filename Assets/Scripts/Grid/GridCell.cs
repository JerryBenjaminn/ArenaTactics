using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the type of a grid cell for environment and hazard logic.
/// </summary>
public enum CellType
{
    Normal,
    Pit,
    Spikes,
    Lava
}

/// <summary>
/// Represents a single tile within the tactical grid.
/// </summary>
public class GridCell : MonoBehaviour
{
    [Header("Grid Data")]
    [SerializeField]
    private Vector2Int gridPosition;

    [SerializeField]
    private Vector3 worldPosition;

    [SerializeField]
    private bool isWalkable = true;

    [SerializeField]
    private bool isOccupied;

    [SerializeField]
    private GameObject occupyingUnit;

    [SerializeField]
    private CellType cellType = CellType.Normal;

    /// <summary>
    /// Gets the 2D grid coordinates of this cell.
    /// </summary>
    public Vector2Int GridPosition => gridPosition;

    /// <summary>
    /// Gets the world-space position of this cell's center.
    /// </summary>
    public Vector3 WorldPosition => worldPosition;

    /// <summary>
    /// Gets or sets whether this cell can be walked on.
    /// </summary>
    public bool IsWalkable
    {
        get => isWalkable;
        set => isWalkable = value;
    }

    /// <summary>
    /// Gets whether this cell is currently occupied by a unit.
    /// </summary>
    public bool IsOccupied => isOccupied;

    /// <summary>
    /// Gets the unit currently occupying this cell, if any.
    /// </summary>
    public GameObject OccupyingUnit => occupyingUnit;

    /// <summary>
    /// Gets or sets the type of this cell (used for hazards and environment).
    /// </summary>
    public CellType CellType
    {
        get => cellType;
        set => cellType = value;
    }

    /// <summary>
    /// Initializes this cell with its logical grid position and world position.
    /// Intended to be called by <see cref="GridManager"/> when creating the grid.
    /// </summary>
    /// <param name="gridPos">The 2D grid coordinates.</param>
    /// <param name="worldPos">The world-space position of the cell center.</param>
    public void Initialize(Vector2Int gridPos, Vector3 worldPos)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        transform.position = worldPos;
    }

    /// <summary>
    /// Marks the cell as occupied by the specified unit.
    /// </summary>
    /// <param name="unit">The unit that is occupying this cell.</param>
    public void SetOccupied(GameObject unit)
    {
        occupyingUnit = unit;
        isOccupied = unit != null;
    }

    /// <summary>
    /// Clears the current occupying unit and marks the cell as unoccupied.
    /// </summary>
    public void ClearOccupied()
    {
        occupyingUnit = null;
        isOccupied = false;
    }

    /// <summary>
    /// Returns the four orthogonally adjacent neighbor cells (up, down, left, right).
    /// Cells outside the grid bounds are ignored.
    /// </summary>
    /// <returns>A list of adjacent <see cref="GridCell"/> instances.</returns>
    public List<GridCell> GetNeighbors()
    {
        var neighbors = new List<GridCell>(4);

        if (GridManager.Instance == null)
        {
            return neighbors;
        }

        // 4-directional neighbors: up, down, left, right
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var dir in directions)
        {
            Vector2Int neighborPos = gridPosition + dir;

            if (!GridManager.Instance.IsPositionValid(neighborPos))
            {
                continue;
            }

            GridCell neighbor = GridManager.Instance.GetCellAtPosition(neighborPos);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }
}

