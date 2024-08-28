using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AStarPathfinder
{
    private GridController gridController;
    private PawnMovement pawnMovement;

    public AStarPathfinder(GridController gridController, PawnMovement pawnMovement)
    {
        this.gridController = gridController;
        this.pawnMovement = pawnMovement;
    }

    #region A* Algorithm
    public List<Vector3> FindPath(Vector3 start, Vector3 end, int maxDistance)
    {
        var startNode = new PathNode { Position = gridController.SnapToGrid(start) };
        var endNode = new PathNode { Position = gridController.SnapToGrid(end) };

        var openSet = new List<PathNode> { startNode };
        var closedSet = new HashSet<Vector3>();

        while (openSet.Count > 0)
        {
            var currentNode = openSet.OrderBy(n => n.FCost).First();

            if (currentNode.Position == endNode.Position)
            {
                return ReconstructPath(currentNode);
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            foreach (var neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor.Position)) continue;

                int newCostToNeighbor = currentNode.GCost + GetMoveCost(currentNode.Position, neighbor.Position);

                if (newCostToNeighbor > maxDistance) continue;

                if (!openSet.Contains(neighbor) || newCostToNeighbor < neighbor.GCost)
                {
                    neighbor.GCost = newCostToNeighbor;
                    neighbor.HCost = GetDistance(neighbor.Position, endNode.Position);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    private List<Vector3> ReconstructPath(PathNode endNode)
    {
        var path = new List<Vector3>();
        var currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    private IEnumerable<PathNode> GetNeighbors(PathNode node)
    {
        List<Vector3> neighborPositions = gridController.GetValidNeighbors(node.Position, pawnMovement.CanMoveDiagonally());

        foreach (Vector3 pos in neighborPositions)
        {
            if (IsValidMove(node.Position, pos))
            {
                yield return new PathNode { Position = pos };
            }
        }
    }


    private bool IsValidMove(Vector3 from, Vector3 to)
    {
        if (from == to) return false;

        CubeController toCube = gridController.GetCellAtPosition(to)?.GetComponent<CubeController>();

        if (toCube == null) return false;

        if (toCube.IsOccupied())
        {
            GameObject occupant = toCube.GetOccupant();

            if (pawnMovement.tag == "Player")
            {

                // Check if the occupant is an enemy
                if (occupant.CompareTag("Enemy"))
                {
                    // Consider it a valid move if it's occupied by an enemy, regardless of isWalkable
                    return true;
                }
                else
                {
                    // For non-enemy occupants, consider it an invalid move
                    return false;
                }
            }
            else if (pawnMovement.tag == "Enemy")
            {

                // Check if the occupant is an enemy
                if (occupant.CompareTag("Player"))
                {
                    // Consider it a valid move if it's occupied by an enemy, regardless of isWalkable
                    return true;
                }
                else
                {
                    // For non-enemy occupants, consider it an invalid move
                    return false;
                }
            }
        }

        // For unoccupied cells, respect the isWalkable property
        return toCube.isWalkable;
    }

    private int GetMoveCost(Vector3 from, Vector3 to)
    {
        CubeController toCube = gridController.GetCellAtPosition(to)?.GetComponent<CubeController>();
        if (toCube == null) return int.MaxValue; // Invalid move

        if (toCube.IsOccupied())
        {
            GameObject occupant = toCube.GetOccupant();

            if (pawnMovement.CompareTag("Player"))
            {
                if (occupant.CompareTag("Enemy"))
                {
                    // Prioritize moving towards enemy pawns for player
                    return 0;
                }
            }
            else if (pawnMovement.CompareTag("Enemy"))
            {
                if (occupant.CompareTag("Player"))
                {
                    // Prioritize moving towards player pawns for enemy
                    return 0;
                }
            }

            // Higher cost for occupied cells that are not priority targets
            return 3;
        }

        // Default cost for unoccupied cells
        return 1;
    }

    private int GetDistance(Vector3 a, Vector3 b)
    {
        return Mathf.RoundToInt(Vector3.Distance(a, b) / gridController.cellSize);
    }

    private Vector3[] GetPushDirections()
    {
        return new Vector3[]
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };
    }
    #endregion

    #region Reachable Cells
    public Dictionary<Vector3, List<Vector3>> FindAllReachableCells(Vector3 start, int maxDistance)
    {
        var reachableCells = new Dictionary<Vector3, List<Vector3>>();
        var startNode = new PathNode { Position = gridController.SnapToGrid(start) };

        var openSet = new List<PathNode> { startNode };
        var closedSet = new HashSet<Vector3>();

        while (openSet.Count > 0)
        {
            var currentNode = openSet.OrderBy(n => n.GCost).First();

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            // Only add to reachable cells if it's not the starting position
            if (currentNode.Position != startNode.Position)
            {
                reachableCells[currentNode.Position] = ReconstructPath(currentNode);
            }

            foreach (var neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor.Position)) continue;

                int newCostToNeighbor = currentNode.GCost + GetMoveCost(currentNode.Position, neighbor.Position);

                if (newCostToNeighbor > maxDistance) continue;

                if (!openSet.Contains(neighbor) || newCostToNeighbor < neighbor.GCost)
                {
                    neighbor.GCost = newCostToNeighbor;
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return reachableCells;
    }
    #endregion

    #region Helper Classes
    private class PathNode
    {
        public Vector3 Position;
        public int GCost;
        public int HCost;
        public int FCost => GCost + HCost;
        public PathNode Parent;
        public bool IsPushMove;
    }
    #endregion
}