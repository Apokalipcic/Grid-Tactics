using UnityEngine;
using System.Collections;
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
    [SerializeField]
    [Tooltip("Delay in seconds before AI makes its move")]
    private float moveDelay = 1f;

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
    public IEnumerator ExecuteTurn()
    {
        if (!canMove || pawnMovement.GetIsDead())
            yield break;

        Debug.Log($"Executing turn for AI pawn [{name}]");

        UpdatePreferableDestination();
        Vector3 bestMove = CalculateBestMove();

        if (pawnMovement.IsValidMove(bestMove))
        {
            // Highlight the chosen move
            CubeController nextCell = GetCellAtPosition(bestMove);
            if (nextCell != null)
            {
                nextCell.ChangeHighlightEnemyVFX(true);
            }

            Debug.Log($"AI {name} will move to {bestMove} after {moveDelay} seconds");
            yield return new WaitForSeconds(moveDelay);

            // Clear the highlight
            if (nextCell != null)
            {
                nextCell.ChangeHighlightEnemyVFX(false);
            }

            Debug.Log($"AI {name} moving to {bestMove}");
            pawnMovement.MovePath(bestMove);
            yield return new WaitUntil(() => !pawnMovement.IsMoving());

            GameManager.Instance.UseActionPoint(true);
        }
        else
        {
            Debug.Log($"AI {name} couldn't find a valid move");
        }
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

        if (validMoves.Count == 0)
        {
            Debug.Log($"AI {name} has no valid moves");
            return transform.position; // Return current position if no valid moves
        }

        return validMoves.OrderBy(move => Vector3.Distance(move, currentPreferableDestination)).First();
    }

    private CubeController GetCellAtPosition(Vector3 position)
    {
        return gridController.GetCellAtPosition(position)?.GetComponent<CubeController>();
    }
    #endregion
}