using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PawnMovement))]
public class AIController : MonoBehaviour
{
    #region Variables
    [Header("Destination")]
    [SerializeField] private Vector3 targetDestination;

    [Header("Movement Properties")]
    [SerializeField]
    [Tooltip("Maximum number of action points to use per turn")]
    private int maxActionPointsPerTurn = 3;
    [SerializeField]
    [Tooltip("Whether the AI can move")]
    private bool canMove = true;
    [SerializeField]
    [Tooltip("Whether the AI should patrol between origin and target")]
    private bool isPatrolling = false;

    [Header("Components")]
    [SerializeField] private PawnMovement pawnMovement;
    [SerializeField] private GridController gridController;

    private Vector3 originPosition;
    private Vector3 currentPreferableDestination;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeComponents();
        SetupPositions();
    }

    private void InitializeComponents()
    {
        if (pawnMovement == null)
            pawnMovement = GetComponent<PawnMovement>();

        if (gridController == null)
            gridController = FindObjectOfType<GridController>();

        pawnMovement.SetMovementRange(maxActionPointsPerTurn);
    }

    private void SetupPositions()
    {
        originPosition = gridController.SnapToGrid(transform.position);
        targetDestination = gridController.SnapToGrid(targetDestination);
        currentPreferableDestination = targetDestination;
    }
    #endregion

    #region Public Methods
    public int ExecuteTurn()
    {
        if (!canMove)
            return 0;

        Debug.Log($"Executing turn for AI pawn [{name}]");

        UpdatePreferableDestination();
        Vector3 bestMove = CalculateBestMove();

        if (pawnMovement.IsValidMove(bestMove))
        {
            Debug.Log($"AI {name} moving to {bestMove}");
            pawnMovement.MovePath(bestMove);
            return CalculateActionPointsUsed(bestMove);
        }

        Debug.Log($"AI {name} couldn't find a valid move");
        return 0;
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
    private void UpdatePreferableDestination()
    {
        if (isPatrolling && gridController.SnapToGrid(transform.position) == targetDestination)
            currentPreferableDestination = originPosition;
        else if (isPatrolling && gridController.SnapToGrid(transform.position) == originPosition)
            currentPreferableDestination = targetDestination;
    }

    private Vector3 CalculateBestMove()
    {
        List<Vector3> validMoves = pawnMovement.GetValidMoves();
        return validMoves.OrderBy(move => Vector3.Distance(move, currentPreferableDestination)).FirstOrDefault();
    }

    private int CalculateActionPointsUsed(Vector3 move)
    {
        float distance = Vector3.Distance(transform.position, move);
        return Mathf.Min(Mathf.CeilToInt(distance / gridController.cellSize), maxActionPointsPerTurn);
    }
    #endregion
}