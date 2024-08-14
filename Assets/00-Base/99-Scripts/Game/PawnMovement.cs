using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PawnMovement : MonoBehaviour
{
    #region Variables
    [Header("Pawn Properties")]
    [Range(1, 10)]
    [SerializeField] private int movementRange = 1;
    [SerializeField] private int movementBuff = 0;

    [Header("Movement Options")]
    [SerializeField] private bool canMoveDiagonally = false;

    [Header("Grid Properties")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private GridController gridController;

    [Header("Movement Properties")]
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Grid Reference")]


    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private bool isMoving = false;
    private Coroutine currentMovement;
    private Collider pawnCollider;

    [Header("Booster Properties")]
    private string currentBooster = "None";
    #endregion

    #region Initialization
    private void Start()
    {
        originPosition = transform.position;

        originRotation = transform.rotation;

        pawnCollider = GetComponent<Collider>();

        if (gridController)
            cellSize = gridController.cellSize;
        else
            Debug.LogError($"Grid Controller in pawn {this.name} not found");


        GameManager.Instance.AddPawn(this, this.tag);
    }
    #endregion

    #region Movement Methods
    public void MovePath(Vector3 destination)
    {
        if (!isMoving)
        {
            StoreCurrentState();

            if (currentBooster == "BigJump")
            {
                currentMovement = StartCoroutine(PerformBigJump(destination));
            }
            else
            {
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
    }
    private IEnumerator PerformBigJump(Vector3 destination)
    {
        isMoving = true;
        pawnCollider.enabled = false;

        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, destination);
        float journeyTime = journeyLength / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < journeyTime)
        {
            float t = elapsedTime / journeyTime;
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight * (journeyLength / cellSize);

            Vector3 newPosition = Vector3.Lerp(startPosition, destination, t);
            newPosition.y += height;

            transform.position = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = destination;
        isMoving = false;
        pawnCollider.enabled = true;
        currentMovement = null;
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

        //Debug.Log($"BigJump: distance = {distance} <= movementRnage = {movementRange} + movementBuff {movementBuff} - {distance <= movementRange + movementBuff}");

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
        List<Vector3> neighbors = gridController.GetValidNeighbors(current);
        Vector3 bestNext = current;
        float minDistance = Vector3.Distance(current, target);

        foreach (Vector3 neighbor in neighbors)
        {
            float distance = Vector3.Distance(neighbor, target);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestNext = neighbor;
            }
        }

        return bestNext;
    }

    private bool IsCellWalkable(Vector3 position)
    {
        return gridController.CellExists(position);
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

        Debug.Log($"Booster: {boosterType} applied");
    }

    public List<Vector3> GetValidMoves(Vector3 startPosition)
    {
        List<Vector3> validMoves = new List<Vector3>();
        int totalRange = movementRange + movementBuff;

        // Use a queue for breadth-first search
        Queue<Vector3> toExplore = new Queue<Vector3>();
        HashSet<Vector3> explored = new HashSet<Vector3>();

        toExplore.Enqueue(startPosition);
        explored.Add(startPosition);

        while (toExplore.Count > 0)
        {
            Vector3 current = toExplore.Dequeue();
            int currentDistance = CalculateDistance(startPosition, current);

            if (currentDistance < totalRange)
            {
                List<Vector3> neighbors = GetNeighbors(current);
                foreach (Vector3 neighbor in neighbors)
                {
                    if (!explored.Contains(neighbor) && gridController.CellExists(neighbor))
                    {
                        explored.Add(neighbor);
                        toExplore.Enqueue(neighbor);
                        validMoves.Add(neighbor);
                    }
                }
            }
        }

        return validMoves;
    }

    private List<Vector3> GetNeighbors(Vector3 position)
    {
        float cellSize = gridController.cellSize;
        List<Vector3> neighbors = new List<Vector3>
        {
            position + new Vector3(cellSize, 0, 0),   // Right
            position + new Vector3(-cellSize, 0, 0),  // Left
            position + new Vector3(0, 0, cellSize),   // Up
            position + new Vector3(0, 0, -cellSize)   // Down
        };

        if (canMoveDiagonally)
        {
            neighbors.AddRange(new List<Vector3>
            {
                position + new Vector3(cellSize, 0, cellSize),    // Top-Right
                position + new Vector3(-cellSize, 0, cellSize),   // Top-Left
                position + new Vector3(cellSize, 0, -cellSize),   // Bottom-Right
                position + new Vector3(-cellSize, 0, -cellSize)   // Bottom-Left
            });
        }

        return neighbors;
    }

    private int CalculateDistance(Vector3 start, Vector3 end)
    {
        Vector3 difference = end - start;
        return Mathf.Abs(Mathf.RoundToInt(difference.x / gridController.cellSize)) +
               Mathf.Abs(Mathf.RoundToInt(difference.z / gridController.cellSize));
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
        Debug.Log($"Undo move by {this.name}");
        Reset();
        // AudioManager.Instance.Play("UndoMove");
    }
    #endregion

    #region Helper Methods
    public void DeselectThisPawn()
    {
        pawnCollider.enabled = true;
    }
    public void SelectThisPawn()
    {
        pawnCollider.enabled = false;
    }
    public void OriginReset(float resetSpeed)
    {
        if (currentMovement != null)
        {

            StopCoroutine(currentMovement);

        }

        currentMovement = StartCoroutine(ResetToOrigin(resetSpeed));

        // AudioManager.Instance.Play("PawnResetStart");

    }



    private IEnumerator ResetToOrigin(float resetSpeed)
    {
        isMoving = true;

        pawnCollider.enabled = false;

        Vector3 startPosition = transform.position;

        Quaternion startRotation = transform.rotation;

        float journeyLength = Vector3.Distance(startPosition, originPosition);

        float startTime = Time.time;

        while (transform.position != originPosition || transform.rotation != originRotation)

        {

            float distanceCovered = (Time.time - startTime) * resetSpeed;

            float fractionOfJourney = distanceCovered / journeyLength;

            transform.position = Vector3.Lerp(startPosition, originPosition, fractionOfJourney);

            transform.rotation = Quaternion.Slerp(startRotation, originRotation, fractionOfJourney);


            yield return null;
        }

        lastPosition = originPosition;
        lastRotation = originRotation;

        Reset();
    }


    public void LookAt(Vector3 target)
    {
        transform.LookAt(target);
        // AudioManager.Instance.Play("PawnRotate");
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