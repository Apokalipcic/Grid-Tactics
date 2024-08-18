using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(PawnMovement))]
public class AIController : MonoBehaviour
{
    #region Variables
    [SerializeField] private PawnMovement pawnMovement;
    [SerializeField] private List<Transform> movementPath = new List<Transform>();
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool isPatrolling = false;
    [SerializeField] private float waitTimeBeforeMoving = 0.5f;

    private int currentPathIndex = 0;
    private bool isMovingForward = true;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (pawnMovement == null)
        {
            pawnMovement = GetComponent<PawnMovement>();
        }

        GameManager.Instance.AddPawn(pawnMovement,this.tag);

    }
    #endregion

    #region Public Methods
    public void ExecuteTurn()
    {
        if (GameManager.Instance.UseActionPoint())
        {
            if (canMove && movementPath.Count > 0)
            {
                StartCoroutine(MoveAlongPath());
            }
            else
            {
                EndTurn();
            }
        }
        else
        {
            EndTurn();
        }
    }

    public void SetMovementPath(List<Transform> path)
    {
        movementPath = path;
        currentPathIndex = 0;
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
    private IEnumerator MoveAlongPath()
    {
        if (currentPathIndex < 0 || currentPathIndex >= movementPath.Count)
        {
            EndTurn();
            yield break;
        }

        Vector3 nextPosition = movementPath[currentPathIndex].position;

        // Highlight the next move
        CubeController nextCell = GetCellAtPosition(nextPosition);
        if (nextCell != null)
        {
            nextCell.ChangeHighlightEnemyVFX(true);
        }

        yield return new WaitForSeconds(waitTimeBeforeMoving); // Wait for a second to show the highlight

        // Move the pawn
        pawnMovement.MovePath(nextPosition);

        // Wait for the move to complete
        while (pawnMovement.IsMoving())
        {
            yield return null;
        }

        // Clear the highlight
        if (nextCell != null)
        {
            nextCell.ChangeHighlightEnemyVFX(false);
        }

        // Update the path index
        UpdatePathIndex();

        EndTurn();
    }

    private void UpdatePathIndex()
    {
        if (isPatrolling)
        {
            if (isMovingForward)
            {
                currentPathIndex++;
                if (currentPathIndex >= movementPath.Count)
                {
                    currentPathIndex = movementPath.Count - 2;
                    isMovingForward = false;
                }
            }
            else
            {
                currentPathIndex--;
                if (currentPathIndex < 0)
                {
                    currentPathIndex = 1;
                    isMovingForward = true;
                }
            }
        }
        else
        {
            currentPathIndex = (currentPathIndex + 1) % movementPath.Count;
        }
    }

    private void EndTurn()
    {
        GameManager.Instance.EndCurrentTurn();
    }

    private CubeController GetCellAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f);
        foreach (Collider collider in colliders)
        {
            CubeController cubeController = collider.GetComponent<CubeController>();
            if (cubeController != null)
            {
                return cubeController;
            }
        }
        return null;
    }
    #endregion
}