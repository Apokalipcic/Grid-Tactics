using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PawnMovement : MonoBehaviour
{
    #region Variables
    [Header("Pawn Properties")]
    public PawnType pawnType;
    [Range(1, 10)]
    [SerializeField] private int movementRange = 1;
    [SerializeField] private int movementBuff = 0;

    [Header("Grid Properties")]
    [SerializeField] private float cellSize = 1f;

    [Header("Movement Properties")]
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveSpeed = 2f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private bool isMoving = false;
    private Coroutine currentMovement;
    private Collider pawnCollider;

    [Header("Booster Properties")]
    private string currentBooster = "None";

    public enum PawnType
    {
        Normal,
        Diagonal,
        Queen
    }
    #endregion

    #region Initialization
    private void Start()
    {
        pawnCollider = GetComponent<Collider>();

        GameManager.Instance.AddPawn(this, this.tag);
    }
    #endregion

    #region Movement Methods
    public void MovePath(Vector3 destination)
    {
        if (!isMoving)
        {
            StoreCurrentState();
            List<Vector3> path = CalculatePath(transform.position, destination);
            if (path != null && path.Count > 0)
            {
                currentMovement = StartCoroutine(MoveAlongPath(path));
            }
            else
            {
                Debug.Log("No valid path found");
            }
        }
    }

    private IEnumerator MoveAlongPath(List<Vector3> path)
    {
        isMoving = true;
        pawnCollider.enabled = false;

        foreach (Vector3 cellPosition in path)
        {
            Vector3 startPosition = transform.position;
            float journeyLength = Vector3.Distance(startPosition, cellPosition);
            float journeyTime = journeyLength / moveSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < journeyTime)
            {
                float t = elapsedTime / journeyTime;
                float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

                Vector3 newPosition = Vector3.Lerp(startPosition, cellPosition, t);
                newPosition.y += height;

                transform.position = newPosition;

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = cellPosition;
        }

        isMoving = false;
        pawnCollider.enabled = true;
        currentMovement = null;
    }

    public bool IsValidMove(Vector3 startPosition, Vector3 endPosition)
    {
        if (startPosition == endPosition) return false;

        if (currentBooster == "BigJump")
        {
            return IsValidBigJumpMove(startPosition, endPosition);
        }

        List<Vector3> path = CalculatePath(startPosition, endPosition);
        bool isValid = path != null && path.Count > 0 && path.Count <= movementRange + movementBuff;
        Debug.Log($"IsValidMove: Start={startPosition}, End={endPosition}, PathCount={path?.Count}, IsValid={isValid}");
        return isValid;
    }

    private bool IsValidBigJumpMove(Vector3 startPosition, Vector3 endPosition)
    {
        Vector3 delta = endPosition - startPosition;
        int deltaX = Mathf.RoundToInt(delta.x / cellSize);
        int deltaZ = Mathf.RoundToInt(delta.z / cellSize);
        int distance = Mathf.Abs(deltaX) + Mathf.Abs(deltaZ);

        return distance <= movementRange + movementBuff;
    }

    private List<Vector3> CalculatePath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 current = start;
        int steps = 0;
        int maxSteps = movementRange + movementBuff;

        while (current != end && steps < maxSteps)
        {
            Vector3 next = GetNextCellTowards(current, end);
            if (next == current) // No valid next cell found
            {
                Debug.Log($"CalculatePath: No valid next cell found. Current={current}, End={end}");
                return null;
            }
            path.Add(next);
            current = next;
            steps++;
        }

        if (current != end)
        {
            Debug.Log($"CalculatePath: Destination not reached. Current={current}, End={end}, Steps={steps}");
            return null;
        }

        Debug.Log($"CalculatePath: Path found. Start={start}, End={end}, Steps={steps}");
        return path;
    }

    private Vector3 GetNextCellTowards(Vector3 current, Vector3 target)
    {
        Vector3 direction = target - current;
        Vector3 nextCell = current;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
        {
            nextCell.x += Mathf.Sign(direction.x) * cellSize;
            if (IsCellWalkable(nextCell))
            {
                return nextCell;
            }
            nextCell = current;
            nextCell.z += Mathf.Sign(direction.z) * cellSize;
        }
        else
        {
            nextCell.z += Mathf.Sign(direction.z) * cellSize;
            if (IsCellWalkable(nextCell))
            {
                return nextCell;
            }
            nextCell = current;
            nextCell.x += Mathf.Sign(direction.x) * cellSize;
        }

        return IsCellWalkable(nextCell) ? nextCell : current;
    }

    private bool IsCellWalkable(Vector3 cellPosition)
    {
        CubeController cell = GetCellAtPosition(cellPosition);
        bool isWalkable = cell != null && cell.isWalkable;
        Debug.Log($"IsCellWalkable: Position={cellPosition}, IsWalkable={isWalkable}");
        return isWalkable;
    }

    private CubeController GetCellAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f, LayerMask.GetMask("Walkable"));
        CubeController cell = colliders.Length > 0 ? colliders[0].GetComponent<CubeController>() : null;
        Debug.Log($"GetCellAtPosition: Position={position}, CellFound={cell != null}");
        return cell;
    }

    public void ApplyBooster(string boosterType)
    {
        currentBooster = boosterType;
        switch (boosterType)
        {
            case "BigJump":
                // Big Jump is now handled in IsValidMove
                break;
            // Add other booster types here
            default:
                currentBooster = "None";
                break;
        }
    }

    public List<Vector3> GetValidMoves(Vector3 startPosition)
    {
        List<Vector3> validMoves = new List<Vector3>();
        int totalMovement = movementRange + movementBuff;

        for (int x = -totalMovement; x <= totalMovement; x++)
        {
            for (int z = -totalMovement; z <= totalMovement; z++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(z) <= totalMovement) // Manhattan distance check
                {
                    Vector3 potentialMove = new Vector3(
                        startPosition.x + x * cellSize,
                        startPosition.y,
                        startPosition.z + z * cellSize
                    );

                    if (IsValidMove(startPosition, potentialMove))
                    {
                        validMoves.Add(potentialMove);
                    }
                }
            }
        }

        Debug.Log($"GetValidMoves: StartPosition={startPosition}, ValidMovesCount={validMoves.Count}");
        return validMoves;
    }
    #endregion

    #region State Management
    private void StoreCurrentState()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    public void Reset()
    {
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }
        transform.position = lastPosition;
        transform.rotation = lastRotation;
        isMoving = false;
        pawnCollider.enabled = true;
        currentMovement = null;
        // AudioManager.Instance.Play("PawnReset");
    }

    public void UndoMove()
    {
        Reset();
        // AudioManager.Instance.Play("UndoMove");
    }
    #endregion

    #region Helper Methods
    public void DeselectThisPawn()
    {
        pawnCollider.enabled = true;
    }
    public void LookAt(Vector3 target)
    {
        transform.LookAt(target);
        // AudioManager.Instance.Play("PawnRotate");
    }

    public PawnType GetPawnType()
    {
        return pawnType;
    }

    public void SetMovementBuff(int buff)
    {
        movementBuff = Mathf.Max(0, buff);
    }

    public bool IsMoving()
    {
        return isMoving;
    }
    #endregion
}