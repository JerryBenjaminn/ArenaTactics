using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the tactical grid and provides utility methods
/// for converting between grid and world coordinates.
/// </summary>
[ExecuteInEditMode]
public class GridManager : MonoBehaviour
{
    private static GridManager instance;

    /// <summary>
    /// Gets the active <see cref="GridManager"/> instance in the scene.
    /// </summary>
    public static GridManager Instance => instance;

    [Header("Grid Settings")]
    [SerializeField]
    private int gridWidth = 10;

    [SerializeField]
    private int gridHeight = 10;

    [SerializeField]
    private float cellSize = 1f;

    [SerializeField]
    private float cellSpacing = 0.05f;

    [Header("Visualization")]
    [SerializeField]
    private GameObject gridCellPrefab;

    [SerializeField]
    private Transform gridParent;

    [SerializeField]
    private bool generateGridInEditor;

    /// <summary>
    /// Backing storage for all grid cells.
    /// </summary>
    private GridCell[,] grid;

    /// <summary>
    /// Gets the current grid width (in cells).
    /// </summary>
    public int GridWidth => gridWidth;

    /// <summary>
    /// Gets the current grid height (in cells).
    /// </summary>
    public int GridHeight => gridHeight;

    /// <summary>
    /// Gets the size of a single cell in world units.
    /// </summary>
    public float CellSize => cellSize;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Multiple GridManager instances found. Destroying duplicate.", this);
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

        if (Application.isPlaying)
        {
            InitializeGrid();
        }
    }

    private void OnValidate()
    {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        cellSize = Mathf.Max(0.1f, cellSize);
        cellSpacing = Mathf.Clamp(cellSpacing, 0f, cellSize * 0.5f);

        // Allow grid generation directly from the inspector in edit mode.
        if (!Application.isPlaying && generateGridInEditor)
        {
            InitializeGrid();
            generateGridInEditor = false;
        }
    }

    /// <summary>
    /// Creates and initializes the grid and its visual representation.
    /// </summary>
    public void InitializeGrid()
    {
        EnsureGridParent();
        ClearExistingGrid();

        grid = new GridCell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorldPosition(gridPos);

                GridCell cell = CreateGridCell(gridPos, worldPos);
                grid[x, y] = cell;
            }
        }
    }

    /// <summary>
    /// Returns the <see cref="GridCell"/> at the specified grid coordinates.
    /// </summary>
    /// <param name="gridPos">The grid coordinates.</param>
    /// <returns>The cell at the position, or <c>null</c> if out of bounds or not initialized.</returns>
    public GridCell GetCellAtPosition(Vector2Int gridPos)
    {
        if (grid == null)
        {
            return null;
        }

        if (!IsPositionValid(gridPos))
        {
            return null;
        }

        return grid[gridPos.x, gridPos.y];
    }

    /// <summary>
    /// Returns the <see cref="GridCell"/> that contains the specified world-space position.
    /// </summary>
    /// <param name="worldPos">The world-space position.</param>
    /// <returns>The corresponding cell, or <c>null</c> if outside the grid.</returns>
    public GridCell GetCellAtWorldPosition(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGridPosition(worldPos);
        return GetCellAtPosition(gridPos);
    }

    /// <summary>
    /// Converts a world-space position to grid coordinates.
    /// </summary>
    /// <param name="worldPos">The world-space position.</param>
    /// <returns>The corresponding 2D grid coordinates.</returns>
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        Vector3 origin = GetGridOriginCenter();

        float relativeX = (worldPos.x - origin.x) / cellSize;
        float relativeY = (worldPos.z - origin.z) / cellSize;

        int x = Mathf.FloorToInt(relativeX + 0.5f);
        int y = Mathf.FloorToInt(relativeY + 0.5f);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// Converts grid coordinates to a world-space position representing the center of the cell.
    /// </summary>
    /// <param name="gridPos">The 2D grid coordinates.</param>
    /// <returns>The world-space position for the center of the cell.</returns>
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        Vector3 origin = GetGridOriginCenter();

        float worldX = origin.x + gridPos.x * cellSize;
        float worldZ = origin.z + gridPos.y * cellSize;

        return new Vector3(worldX, 0f, worldZ);
    }

    /// <summary>
    /// Checks whether the specified grid coordinates are within the grid bounds.
    /// </summary>
    /// <param name="gridPos">The 2D grid coordinates.</param>
    /// <returns><c>true</c> if the position is valid; otherwise, <c>false</c>.</returns>
    public bool IsPositionValid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 &&
               gridPos.x < gridWidth &&
               gridPos.y >= 0 &&
               gridPos.y < gridHeight;
    }

    /// <summary>
    /// Returns a list of walkable neighbor cells around the provided grid position.
    /// Only the four orthogonal neighbors are considered.
    /// </summary>
    /// <param name="gridPos">The origin grid position.</param>
    /// <returns>A list of walkable neighboring cells.</returns>
    public List<GridCell> GetWalkableNeighbors(Vector2Int gridPos)
    {
        var result = new List<GridCell>(4);

        if (grid == null || !IsPositionValid(gridPos))
        {
            return result;
        }

        GridCell originCell = GetCellAtPosition(gridPos);
        if (originCell == null)
        {
            return result;
        }

        foreach (GridCell neighbor in originCell.GetNeighbors())
        {
            if (neighbor.IsWalkable && !neighbor.IsOccupied)
            {
                result.Add(neighbor);
            }
        }

        return result;
    }

    /// <summary>
    /// Rebuilds the grid visualization in the scene using Unity primitives.
    /// This is effectively a wrapper around <see cref="InitializeGrid"/>.
    /// </summary>
    public void VisualizeGrid()
    {
        InitializeGrid();
    }

    /// <summary>
    /// Ensures that a parent transform exists for all grid tiles.
    /// </summary>
    private void EnsureGridParent()
    {
        if (gridParent == null)
        {
            const string parentName = "GridParent";

            Transform existing = transform.Find(parentName);
            if (existing != null)
            {
                gridParent = existing;
                return;
            }

            GameObject parentObject = new GameObject(parentName);
            parentObject.transform.SetParent(transform);
            parentObject.transform.localPosition = Vector3.zero;
            parentObject.transform.localRotation = Quaternion.identity;
            parentObject.transform.localScale = Vector3.one;

            gridParent = parentObject.transform;
        }
    }

    /// <summary>
    /// Removes any previously created grid tiles from the scene.
    /// </summary>
    private void ClearExistingGrid()
    {
        if (gridParent == null)
        {
            return;
        }

        // Destroy all previous children under the grid parent.
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = gridParent.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }

        grid = null;
    }

    /// <summary>
    /// Creates a single grid cell at the specified position, including its visual tile.
    /// </summary>
    /// <param name="gridPos">Grid coordinates.</param>
    /// <param name="worldPos">World-space center position.</param>
    /// <returns>The created <see cref="GridCell"/> component.</returns>
    private GridCell CreateGridCell(Vector2Int gridPos, Vector3 worldPos)
    {
        GameObject tileObject;

        if (gridCellPrefab != null)
        {
            tileObject = Instantiate(gridCellPrefab, worldPos, Quaternion.identity, gridParent);
        }
        else
        {
            // Fallback: create a quad primitive to visualize the tile.
            tileObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tileObject.name = $"Cell_{gridPos.x}_{gridPos.y}";
            tileObject.transform.SetParent(gridParent);
            tileObject.transform.position = worldPos;

            // Rotate the quad to lie flat on the XZ plane.
            tileObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // Set default color to light gray.
            var renderer = tileObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"));
                renderer.sharedMaterial.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        // Apply scaling and spacing.
        float scaledSize = Mathf.Max(0.01f, cellSize - cellSpacing);
        tileObject.transform.localScale = new Vector3(scaledSize, scaledSize, 1f);

        GridCell cell = tileObject.GetComponent<GridCell>();
        if (cell == null)
        {
            cell = tileObject.AddComponent<GridCell>();
        }

        cell.Initialize(gridPos, worldPos);
        return cell;
    }

    /// <summary>
    /// Calculates the world-space origin (center of cell (0,0)) so that the grid
    /// is centered around the world origin (0,0,0).
    /// </summary>
    /// <returns>World-space position of the center of cell (0,0).</returns>
    private Vector3 GetGridOriginCenter()
    {
        float totalWidth = gridWidth * cellSize;
        float totalHeight = gridHeight * cellSize;

        // Center the grid on world origin (0,0,0).
        float originX = -totalWidth * 0.5f + cellSize * 0.5f;
        float originZ = -totalHeight * 0.5f + cellSize * 0.5f;

        return new Vector3(originX, 0f, originZ);
    }
}

