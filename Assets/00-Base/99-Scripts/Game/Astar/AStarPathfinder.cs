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
    public (List<Vector3>, bool) FindPartialPath(Vector3 start, Vector3 end, int maxDistance)
    {
        var startNode = new PathNode { Position = gridController.SnapToGrid(start) };
        var endNode = new PathNode { Position = gridController.SnapToGrid(end) };

        var openSet = new List<PathNode> { startNode };
        var closedSet = new HashSet<Vector3>();

        while (openSet.Count > 0)
        {
            var currentNode = openSet.OrderBy(n => n.FCost).First();

            if (currentNode.GCost >= maxDistance || currentNode.Position == endNode.Position)
            {
                return (ReconstructPath(currentNode), PathContainsPushMove(currentNode));
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

        return (null, false); // No path found
    }
    private bool PathContainsPushMove(PathNode endNode)
    {
        var currentNode = endNode;
        while (currentNode != null)
        {
            if (currentNode.IsPushMove)
                return true;
            currentNode = currentNode.Parent;
        }
        return false;
    }

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
                Debug.Log("Path found!");
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

        Debug.Log("No path found");
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
                yield return new PathNode { Position = pos, IsPushMove = false };
            }
        }

        // Add pushable moves separately
        foreach (Vector3 pushPos in pawnMovement.GetPushMoves())
        {
            if (Vector3.Distance(node.Position, pushPos) <= gridController.cellSize)
            {
                yield return new PathNode { Position = pushPos, IsPushMove = true };
            }
        }
    }

    private bool IsPushableMove(Vector3 from, Vector3 to)
    {
        CubeController toCube = gridController.GetCellAtPosition(to)?.GetComponent<CubeController>();
        if (toCube == null || !toCube.IsOccupied()) return false;

        GameObject occupant = toCube.GetOccupant();
        if (occupant.GetComponent<IPushable>() != null)
        {
            return pawnMovement.CanPushChain((to - from).normalized);
        }

        return false;
    }


    private bool IsValidMove(Vector3 from, Vector3 to)
    {
        if (from == to) return false;

        CubeController toCube = gridController.GetCellAtPosition(to)?.GetComponent<CubeController>();

        if (toCube == null) return false;

        if (toCube.IsOccupied())
        {
            GameObject occupant = toCube.GetOccupant();

            if (pawnMovement.CompareTag("Player"))
            {
                return occupant.CompareTag("Enemy"); // Only valid if it's an enemy
            }
            else if (pawnMovement.CompareTag("Enemy"))
            {
                return occupant.CompareTag("Player"); // Only valid if it's a player
            }
        }

        return toCube.isWalkable; // For unoccupied cells
    }

    private int GetMoveCost(Vector3 from, Vector3 to)
    {
        CubeController toCube = gridController.GetCellAtPosition(to)?.GetComponent<CubeController>();
        if (toCube == null) return int.MaxValue;

        if (toCube.IsOccupied())
        {
            GameObject occupant = toCube.GetOccupant();

            if (pawnMovement.CompareTag("Player"))
            {
                if (occupant.CompareTag("Enemy"))
                {
                    return 0; // Prioritize moving towards enemy pawns for player
                }
            }
            else if (pawnMovement.CompareTag("Enemy"))
            {
                if (occupant.CompareTag("Player"))
                {
                    return 0; // Prioritize moving towards player pawns for enemy
                }
                if (occupant.GetComponent<IPushable>() != null)
                {
                    return 3; // Higher cost for pushable objects
                }
            }
            return 3; // Highest cost for other occupied cells
        }

        return 1; // Default cost for unoccupied cells
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