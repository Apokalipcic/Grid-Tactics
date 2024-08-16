using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Transform cellHolder;
    [SerializeField] List<Transform> cells = new List<Transform>();

    [Header("Grid Properties")]
    public float cellSize = 1f;

    [Header("Debug")]
    [SerializeField] bool debug = false;

    private Dictionary<Vector3, Transform> cellDictionary = new Dictionary<Vector3, Transform>();

    #region Cell Functions
    public void AddNewCell(Transform cell)
    {
        if (!cell)
            return;

        if (!cells.Contains(cell))
        {
            cells.Add(cell);
            Vector3 gridPosition = SnapToGrid(cell.position);
            cellDictionary[gridPosition] = cell;
        }

        OnDebug($"Added new cell {cell.name} at position {SnapToGrid(cell.position)}");
    }

    public void RemoveCell(Transform cell)
    {
        if (cells.Contains(cell))
        {
            cells.Remove(cell);
            Vector3 gridPosition = SnapToGrid(cell.position);
            cellDictionary.Remove(gridPosition);
        }

        OnDebug($"Removed old cell {cell.name}");
    }

    public bool CellExists(Vector3 worldPosition)
    {
        Vector3 gridPosition = SnapToGrid(worldPosition);
        return cellDictionary.ContainsKey(gridPosition);
    }

    public List<Vector3> GetValidNeighbors(Vector3 worldPosition, bool allowDiagonal)
    {
        List<Vector3> neighbors = new List<Vector3>();
        Vector3 gridPosition = SnapToGrid(worldPosition);

        // Orthogonal directions
        Vector3[] orthogonalDirections = new Vector3[]
        {
            new Vector3(cellSize, 0, 0),   // Right
            new Vector3(-cellSize, 0, 0),  // Left
            new Vector3(0, 0, cellSize),   // Up
            new Vector3(0, 0, -cellSize)   // Down
        };

        foreach (Vector3 direction in orthogonalDirections)
        {
            Vector3 neighborPosition = gridPosition + direction;
            if (CellExists(neighborPosition))
            {
                neighbors.Add(neighborPosition);
            }
        }

        // New: Diagonal directions (only if allowed)
        if (allowDiagonal)
        {
            Vector3[] diagonalDirections = new Vector3[]
            {
                new Vector3(cellSize, 0, cellSize),    // Top-Right
                new Vector3(-cellSize, 0, cellSize),   // Top-Left
                new Vector3(cellSize, 0, -cellSize),   // Bottom-Right
                new Vector3(-cellSize, 0, -cellSize)   // Bottom-Left
            };

            foreach (Vector3 direction in diagonalDirections)
            {
                Vector3 neighborPosition = gridPosition + direction;
                if (CellExists(neighborPosition))
                {
                    neighbors.Add(neighborPosition);
                }
            }
        }

        return neighbors;
    }

    public Vector3 GetNearestValidCell(Vector3 worldPosition)
    {
        Vector3 gridPosition = SnapToGrid(worldPosition);

        if (CellExists(gridPosition))
            return gridPosition;

        float minDistance = float.MaxValue;
        Vector3 nearestCell = gridPosition;

        foreach (var cell in cellDictionary.Keys)
        {
            float distance = Vector3.Distance(gridPosition, cell);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestCell = cell;
            }
        }

        return nearestCell;
    }
    public Transform GetCellAtPosition(Vector3 worldPosition)
    {
        Vector3 gridPosition = SnapToGrid(worldPosition);
        if (cellDictionary.TryGetValue(gridPosition, out Transform cell))
        {
            return cell;
        }
        return null;
    }
    #endregion

    #region Helper Functions
    public Vector3 SnapToGrid(Vector3 worldPosition)
    {
        return new Vector3(
            Mathf.Round(worldPosition.x / cellSize) * cellSize,
            0f,
            Mathf.Round(worldPosition.z / cellSize) * cellSize
        );
    }
    #endregion

    #region Debug
    private void OnDebug(string debugMessage)
    {
        if (debug)
            Debug.Log($"[GridController] {debugMessage}");
    }
    #endregion
}