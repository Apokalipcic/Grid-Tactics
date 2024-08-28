using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PawnMovement))]
public class AIController : MonoBehaviour
{
    #region Variables
    [Header("Destination")]
    [SerializeField] private Vector3 targetDestination;

    [Header("Variables")]
    [SerializeField] private int actionPointsToUse = 3;
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool isPatrolling = false;

    [Header("Components")]
    [SerializeField] private PawnMovement pawnMovement;
    [SerializeField] private GridController gridController;

    [Header("Debug")]
    [SerializeField] private bool debug = true;

    private Vector3 originPosition;
    private Vector3 currentPreferableDestination = Vector3.zero;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (pawnMovement == null)
        {
            pawnMovement = GetComponent<PawnMovement>();
        }

        if (gridController == null)
            gridController = FindObjectOfType<GridController>();

        originPosition = gridController.SnapToGrid(transform.position);
        targetDestination = gridController.SnapToGrid(targetDestination);
        currentPreferableDestination = targetDestination;

        pawnMovement.SetMovementRange(actionPointsToUse);
    }
    #endregion

    #region Public Methods
    public int ExecuteTurn()
    {
        OnDebug($"Trying to move AI pawn [{this.name}]");
        OnDebug($"Current position: {transform.position}, Target destination: {targetDestination}");
        OnDebug($"Can move: {canMove}, Is patrolling: {isPatrolling}");

        if (!canMove)
        {
            OnDebug("AI pawn cannot move.");
            return 0;
        }

        UpdateDestination();

        OnDebug($"Moving towards {currentPreferableDestination}");
        int pointsUsed = MoveTowardsDestination();

        return pointsUsed;
    }

    public void SetCanMove(bool state)
    {
        canMove = state;
    }

    public void SetPatrolling(bool state)
    {
        isPatrolling = state;
    }
    #endregion

    #region Private Methods
    private void UpdateDestination()
    {
        Vector3 currentPosition = gridController.SnapToGrid(transform.position);

        if (currentPosition == targetDestination && isPatrolling)
        {
            currentPreferableDestination = originPosition;
            OnDebug($"Patrolling: Changing destination to origin: {originPosition}");
        }
        else if (currentPosition == originPosition && isPatrolling)
        {
            currentPreferableDestination = targetDestination;
            OnDebug($"Patrolling: Changing destination to target: {targetDestination}");
        }
    }

    private int MoveTowardsDestination()
    {
        int pointsUsed = 0;
        Vector3 currentPosition = transform.position;

        while (pointsUsed < actionPointsToUse)
        {
            pawnMovement.CalculateReachableCells();
            List<Vector3> validMoves = pawnMovement.GetValidMoves();

            Vector3 bestMove = GetBestMove(validMoves);

            if (bestMove == currentPosition)
            {
                OnDebug("No valid moves available", "Warning");
                break;
            }

            OnDebug($"Moving to {bestMove}");
            pawnMovement.MovePath(bestMove);
            pointsUsed++;

            currentPosition = bestMove;

            if (currentPosition == currentPreferableDestination)
            {
                OnDebug("Reached destination");
                break;
            }
        }

        return pointsUsed;
    }

    private Vector3 GetBestMove(List<Vector3> validMoves)
    {
        Vector3 bestMove = transform.position;
        float shortestDistance = float.MaxValue;

        foreach (Vector3 move in validMoves)
        {
            // Check if the cell exists
            if (gridController.CellExists(move))
            {
                float distance = Vector3.Distance(move, currentPreferableDestination);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    bestMove = move;
                }
            }
            else
            {
                OnDebug($"Ignoring non-existent cell at {move}", "Warning");
            }
        }

        if (bestMove == transform.position)
        {
            OnDebug("No valid moves available", "Warning");
        }

        return bestMove;
    }
    #endregion

    #region Debug Methods
    private void OnDebug(string message, string type = "Log")
    {
        if (debug)
        {
            switch (type)
            {
                case "Log":
                    Debug.Log($"[AIController] {message}");
                    break;
                case "Warning":
                    Debug.LogWarning($"[AIController] {message}");
                    break;
                case "Error":
                    Debug.LogError($"[AIController] {message}");
                    break;
                default:
                    Debug.Log($"[AIController] {message}");
                    break;
            }
        }
    }
    #endregion
}