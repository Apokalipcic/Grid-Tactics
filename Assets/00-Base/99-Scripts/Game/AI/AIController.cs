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
    [Tooltip("Maximum number of moves the AI can make per turn")]
    private int maxMovesPerTurn = 3;
    [SerializeField]
    [Tooltip("Whether the AI can move")]
    private bool canMove = true;
    [SerializeField]
    [Tooltip("Whether the AI should patrol between origin and target")]
    private bool isPatrolling = false;
    [SerializeField]
    [Tooltip("Delay in seconds before AI makes its move")]
    private float moveDelay = 0.5f;

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
    }

    private void SetupPositions()
    {
        originPosition = gridController.SnapToGrid(transform.position);
        targetDestination = gridController.SnapToGrid(targetDestination);
        currentPreferableDestination = targetDestination;
    }
    #endregion

    #region Public Methods
    public IEnumerator CalculateAndExecuteMove()
    {
        if (!canMove || pawnMovement.GetIsDead())
        {
            Debug.Log($"AI {name} cannot move or is dead. Skipping turn.");
            yield break;
        }

        Debug.Log($"Calculating moves for AI pawn [{name}]");

        int availableActionPoints = Mathf.Min(GameManager.Instance.GetAmountOfAvailableActionPoints(), maxMovesPerTurn);
        (List<Vector3> path, bool containsPushMove) = CalculatePath(availableActionPoints);

        if (path == null || path.Count <= 1)
        {
            Debug.Log($"AI {name} couldn't find a valid path. Skipping turn.");
            yield break;
        }

        for (int i = 1; i < path.Count && i <= availableActionPoints; i++)
        {
            Vector3 nextMove = path[i];
            if (pawnMovement.GetValidMoves().Contains(nextMove))
            {
                yield return StartCoroutine(ExecuteMove(nextMove, false));
            }
            else if (pawnMovement.GetPushMoves().Contains(nextMove))
            {
                if (containsPushMove)
                {
                    yield return StartCoroutine(ExecuteMove(nextMove, true));
                }
                else
                {
                    Debug.Log($"AI {name} encountered a pushable object but it's not the most efficient path. Recalculating.");
                    (path, containsPushMove) = CalculatePath(availableActionPoints - i + 1);
                    i = 0; // Reset the counter to start from the beginning of the new path
                    continue;
                }
            }
            else
            {
                Debug.Log($"AI {name} encountered an invalid move. Recalculating path.");
                (path, containsPushMove) = CalculatePath(availableActionPoints - i + 1);
                i = 0; // Reset the counter to start from the beginning of the new path
                continue;
            }

            if (GameManager.Instance.GetAmountOfAvailableActionPoints() <= 0 || Vector3.Distance(transform.position, currentPreferableDestination) < 0.1f)
                break;
        }

        UpdatePreferableDestination();
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
        if (!isPatrolling)
            return;

        if (Vector3.Distance(transform.position, currentPreferableDestination) < 0.1f)
        {
            currentPreferableDestination = (currentPreferableDestination == targetDestination) ? originPosition : targetDestination;
            Debug.Log($"AI {name} switching patrol destination to {currentPreferableDestination}");
        }
    }

    private (List<Vector3>, bool) CalculatePath(int maxDistance)
    {
        pawnMovement.CalculateReachableCells();
        pawnMovement.CalculatePushableMoves();
        return pawnMovement.Pathfinder.FindPartialPath(transform.position, currentPreferableDestination, maxDistance);
    }

    private IEnumerator ExecuteMove(Vector3 targetPosition, bool isPushMove)
    {
        CubeController nextCell = GetCellAtPosition(targetPosition);
        if (nextCell != null)
        {
            nextCell.ChangeHighlightEnemyVFX(true);
        }

        yield return new WaitForSeconds(moveDelay);

        if (nextCell != null)
        {
            nextCell.ChangeHighlightEnemyVFX(false);
        }

        Debug.Log($"AI {name} attempting to move to {targetPosition}");

        if (isPushMove)
        {
            Vector3 pushDirection = (targetPosition - transform.position).normalized;
            if (pawnMovement.TryPush(pushDirection))
            {
                Debug.Log($"AI {name} successfully pushed object at {targetPosition}");
            }
            else
            {
                Debug.Log($"AI {name} failed to push object at {targetPosition}");
                yield break; // End the turn if push failed
            }
        }
        else
        {
            pawnMovement.MovePath(targetPosition, false);
            yield return new WaitUntil(() => !pawnMovement.IsMoving());
        }
    }

    private CubeController GetCellAtPosition(Vector3 position)
    {
        return gridController.GetCellAtPosition(position)?.GetComponent<CubeController>();
    }
    #endregion
}