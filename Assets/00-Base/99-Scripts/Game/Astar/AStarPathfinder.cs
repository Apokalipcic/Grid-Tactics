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

                int newCostToNeighbor = currentNode.GCost + 1;

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
        return gridController.GetValidNeighbors(node.Position, pawnMovement.CanMoveDiagonally())
            .Select(pos => new PathNode { Position = pos });
    }

    private int GetDistance(Vector3 a, Vector3 b)
    {
        return Mathf.RoundToInt(Vector3.Distance(a, b) / gridController.cellSize);
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

            if (currentNode.GCost > maxDistance) continue;

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            reachableCells[currentNode.Position] = ReconstructPath(currentNode);

            foreach (var neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor.Position)) continue;

                int newCostToNeighbor = currentNode.GCost + 1;

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
    }
    #endregion
}