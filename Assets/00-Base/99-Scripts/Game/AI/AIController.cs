using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

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
        List<Vector3> path = CalculatePath(availableActionPoints);

        if (path == null)
            yield break;

        for (int i = 1; i < path.Count && i <= availableActionPoints; i++)
        {
            Vector3 nextMove = path[i];
            if (pawnMovement.IsValidMove(nextMove))
            {
                yield return StartCoroutine(ExecuteMove(nextMove));

                if (GameManager.Instance.GetAmountOfAvailableActionPoints() <= 0 || Vector3.Distance(transform.position, currentPreferableDestination) < 0.1f)
                    break;
            }
            else
            {
                Debug.Log($"AI {name} encountered an invalid move. Stopping movement.");

                break;
            }
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

    private List<Vector3> CalculatePath(int maxDistance)
    {
        List<Vector3> path = pawnMovement.Pathfinder.FindPartialPath(transform.position, currentPreferableDestination, maxDistance);
        pawnMovement.CalculatePushableMoves();

        return path;
    }

    private IEnumerator ExecuteMove(Vector3 targetPosition)
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

        Debug.Log($"AI {name} moving to {targetPosition}");
        pawnMovement.MovePath(targetPosition, false);
        yield return new WaitUntil(() => !pawnMovement.IsMoving());
    }

    private CubeController GetCellAtPosition(Vector3 position)
    {
        return gridController.GetCellAtPosition(position)?.GetComponent<CubeController>();
    }
    #endregion
}