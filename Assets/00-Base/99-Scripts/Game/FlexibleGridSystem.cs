using UnityEngine;
using System.Collections.Generic;

public class FlexibleGridSystem : MonoBehaviour
{
    private float cellSize = 1f;
    [Header("Components")]
    [SerializeField] public Transform walkableSurfacesParent;

    private Dictionary<Vector2Int, Transform> gridCells = new Dictionary<Vector2Int, Transform>();

    void Start()
    {
        InitializeGrid();
    }

    void InitializeGrid()
    {
        if (walkableSurfacesParent == null)
        {
            Debug.LogError("Walkable surfaces parent is not set!");
            return;
        }

        cellSize = walkableSurfacesParent.childCount;
        Debug.Log($"cellSize = {cellSize}");

        for (int i = 0; i < cellSize; i++)
        {
            Transform surface = walkableSurfacesParent.GetChild(i);

            Vector3 surfacePosition = surface.position;
            Vector2Int gridPosition = WorldToGridPosition(surfacePosition);

            if (!gridCells.ContainsKey(gridPosition))
            {
                gridCells.Add(gridPosition, surface);
            }
            else
            {
                Debug.LogWarning($"Duplicate grid position detected at {gridPosition}. Only the first surface will be used.");
            }
        }

        Debug.Log($"Gridd Cells count = {gridCells.Count}");
        Debug.Log($"cellSize = {cellSize}");
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        if (gridCells.TryGetValue(gridPos, out Transform cellTransform))
        {
            return cellTransform.position;
        }
        return new Vector3(gridPos.x * cellSize, 0, gridPos.y * cellSize);
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int z = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector2Int(x, z);
    }

    public bool IsWalkable(Vector2Int gridPos)
    {
        return gridCells.ContainsKey(gridPos);
    }

    public List<Vector2Int> GetNeighbors(Vector2Int gridPos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = gridPos + dir;
            if (IsWalkable(neighborPos))
            {
                neighbors.Add(neighborPos);
            }
        }

        return neighbors;
    }

    public void VisualizeGrid()
    {
        foreach (var cell in gridCells)
        {
            Vector3 worldPos = GridToWorldPosition(cell.Key);
            Debug.DrawLine(worldPos, worldPos + Vector3.up, Color.green, 100f);
        }
    }
}