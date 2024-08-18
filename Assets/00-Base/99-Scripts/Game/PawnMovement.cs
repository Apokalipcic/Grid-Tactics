using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PawnMovement : MonoBehaviour, IPushable
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

    [Header("Pushing Properties")]
    [SerializeField] private bool canPush = true;
    [SerializeField] private bool canChainPush = true;  // New variable to toggle chain pushing
    [SerializeField] private float pushSpeed = 2f;

    private bool isPushing = false;

    private int amountOfActionPointsUsed = 0;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private bool isMoving = false;
    private Coroutine currentMovement;
    private Collider pawnCollider;
    private AStarPathfinder pathfinder;
    private Dictionary<Vector3, List<Vector3>> cachedPaths;
    private bool moved = false;

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
        pathfinder = new AStarPathfinder(gridController, this);
    }
    #endregion

    #region Movement Methods
    public void CalculateReachableCells()
    {
        int maxDistance = Mathf.Min(movementRange + movementBuff, GameManager.Instance.GetAmountOfAvailableActionPoints());
        cachedPaths = pathfinder.FindAllReachableCells(transform.position, maxDistance);
    }
    public List<Vector3> GetValidMoves()
    {
        if (cachedPaths == null)
        {
            CalculateReachableCells();
        }

        List<Vector3> validMoves = new List<Vector3>(cachedPaths.Keys);

        // Add potential push moves
        List<Vector3> pushMoves = GetPushMoves();
        foreach (Vector3 pushMove in pushMoves)
        {
            if (!validMoves.Contains(pushMove))
            {
                validMoves.Add(pushMove);
            }
        }

        return validMoves;
    }
    public void MovePath(Vector3 destination)
    {
        if (isMoving || isPushing)
        {
            Debug.LogWarning("Cannot move: Pawn is already moving.");
            return;
        }

        if (cachedPaths == null || !cachedPaths.ContainsKey(destination))
        {
            Debug.LogError("Invalid move: Path not found in cached paths");
            return;
        }

        Vector3 direction = (destination - transform.position).normalized;

        bool pushed = false;

        // Check if this is a push move
        if (CanPushChain(direction))
        {
            pushed = TryPush(direction);
        }


        StoreCurrentState();
        moved = true;
        amountOfActionPointsUsed = 0;

        if (currentBooster == "BigJump")
        {
            if (currentMovement != null)
            {
                StopCoroutine(currentMovement);
            }
            currentMovement = StartCoroutine(PerformBigJump(destination));
        }
        else
        {
            List<Vector3> path = cachedPaths[destination];
            if (currentMovement != null)
            {
                StopCoroutine(currentMovement);
            }
            currentMovement = StartCoroutine(MoveAlongPath(path));
        }

        // Clear the cache after starting movement
        cachedPaths = null;
    }
    private IEnumerator PerformBigJump(Vector3 destination)
    {
        isMoving = true;
        pawnCollider.enabled = false;

        // Deoccupy the starting position
        UpdateCubeOccupation(transform.position, false);

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

        // Occupy the new position
        UpdateCubeOccupation(destination, true);

        // Only one point for big jump
        GameManager.Instance.UseActionPoint();
        amountOfActionPointsUsed++;
    }

    private IEnumerator MoveAlongPath(List<Vector3> path)
    {
        isMoving = true;
        pawnCollider.enabled = false;

        // Deoccupy the starting position
        UpdateCubeOccupation(transform.position, false);

        bool isFirstStep = true;

        Vector3 lastPosition = transform.position;

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

            lastPosition = cellPosition;

            // Consume Action Point, but not for the first step
            if (!isFirstStep)
            {
                GameManager.Instance.UseActionPoint();
                amountOfActionPointsUsed++;
            }
            isFirstStep = false;
        }
        
        // Occupy the new position
        UpdateCubeOccupation(lastPosition, true);

        isMoving = false;
        pawnCollider.enabled = true;
        currentMovement = null;
    }
    private void UpdateCubeOccupation(Vector3 position, bool occupy)
    {
        CubeController cube = gridController.GetCellAtPosition(position)?.GetComponent<CubeController>();
        if (cube != null)
        {
            if (occupy)
            {
                cube.OnOccupy(gameObject);
            }
            else
            {
                cube.OnDeoccupy();
            }
        }
        else
            Debug.Log($"Cannot Update Cube Occupation, because cube doesn't exists on {position}");
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

    public int CalculateDistance(Vector3 start, Vector3 end)
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
        if (!moved)
            return;

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }
        transform.position = lastPosition;
        transform.rotation = lastRotation;
        isMoving = false;
        moved = false;
        pawnCollider.enabled = true;
        currentMovement = null;
        GameManager.Instance.ReturnActionPoints(amountOfActionPointsUsed);
        amountOfActionPointsUsed = 0;
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
    public bool CanMoveDiagonally()
    {
        return canMoveDiagonally;
    }
    public bool IsValidMove(Vector3 endPosition)
    {
        return cachedPaths != null && cachedPaths.ContainsKey(endPosition);
    }
    //public void DeselectThisPawn()
    //{
    //    pawnCollider.enabled = true;
    //}
    //public void SelectThisPawn()
    //{
    //    pawnCollider.enabled = false;
    //}
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
    public void SetMovementBuff(int buff)
    {
        movementBuff = Mathf.Max(0, buff);
    }

    public bool IsMoving()
    {
        return isMoving;
    }
    #endregion

    #region IPushable Implementation
    public bool CanBePushed(Vector3 direction)
    {
        // Check if there's a valid cell or empty space in the push direction
        Vector3 targetPosition = transform.position + direction * cellSize;
        return gridController.CellExists(targetPosition) || GameManager.Instance.GetPawnAtPosition(targetPosition) == null;
    }

    public void Push(Vector3 direction)
    {
        Vector3 targetPosition = transform.position + direction * cellSize;
        StartCoroutine(PushCoroutine(targetPosition));
    }
    #endregion

    #region Pushing Methods
    private IEnumerator PushCoroutine(Vector3 targetPosition)
    {
        isPushing = true;

        // Deoccupy the starting position
        UpdateCubeOccupation(transform.position, false);

        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition);
        float elapsedTime = 0f;

        while (elapsedTime < journeyLength / pushSpeed)
        {
            float t = elapsedTime / (journeyLength / pushSpeed);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        // Occupy the new position
        UpdateCubeOccupation(targetPosition, true);

        isPushing = false;

        // Check if the pawn is now off the grid
        if (!gridController.CellExists(targetPosition))
        {
            // Handle pawn being pushed off the grid
            HandlePushedOffGrid();
        }
    }

    private void HandlePushedOffGrid()
    {
        // Implement logic for when a pawn is pushed off the grid
        GameManager.Instance.RemovePawn(this);
        //Destroy(gameObject);
        GetComponent<Rigidbody>().isKinematic = false;

        Debug.Log($"Pawn {this.name} was pushed out of cliff");
    }

    public bool CanPushChain(Vector3 direction)
    {
        if (!canChainPush)
        {
            // If chain pushing is disabled, check only the immediate next object
            Vector3 nextPosition = transform.position + direction * cellSize;
            if (!gridController.CellExists(nextPosition)) return true; // Can push off grid
            IPushable nextPushable = GetPushableAtPosition(nextPosition);
            return nextPushable == null || nextPushable.CanBePushed(direction);
        }

        Vector3 currentPosition = transform.position;
        HashSet<IPushable> pushedObjects = new HashSet<IPushable>();

        while (true)
        {
            Vector3 nextPosition = currentPosition + direction * cellSize;
            if (!gridController.CellExists(nextPosition)) return true; // Can push off grid

            IPushable nextPushable = GetPushableAtPosition(nextPosition);

            if (nextPushable == null)
            {
                return true;
            }

            if (pushedObjects.Contains(nextPushable))
            {
                return false;
            }

            if (!nextPushable.CanBePushed(direction))
            {
                return false;
            }

            pushedObjects.Add(nextPushable);
            currentPosition = nextPosition;
        }
    }

    public bool TryPush(Vector3 direction)
    {
        if (!canPush || isMoving || isPushing) return false;

        if (!CanPushChain(direction)) return false;

        if (canChainPush)
        {
            // Execute chain push
            Vector3 currentPosition = transform.position;
            while (true)
            {
                Vector3 nextPosition = currentPosition + direction * cellSize;
                IPushable nextPushable = GetPushableAtPosition(nextPosition);

                if (nextPushable == null)
                {
                    break;
                }

                nextPushable.Push(direction);
                currentPosition = nextPosition;
            }
        }
        else
        {
            // Execute single-object push
            Vector3 nextPosition = transform.position + direction * cellSize;
            IPushable nextPushable = GetPushableAtPosition(nextPosition);
            if (nextPushable != null)
            {
                //TODO: NOW is to change the push that it occupies new cell
                nextPushable.Push(direction);
            }
        }

        return true;
    }

    private IPushable GetPushableAtPosition(Vector3 position)
    {
        // Check if there's a cell at the position
        CubeController targetCube = gridController.GetCellAtPosition(position)?.GetComponent<CubeController>();
        if (targetCube != null && targetCube.IsOccupied())
        {
            return targetCube.GetOccupant().GetComponent<IPushable>();
        }

        // If there's no cell or the cell is not occupied, check if there's a pawn at the position
        PawnMovement pawn = GameManager.Instance.GetPawnAtPosition(position);
        return pawn as IPushable;
    }

    private List<Vector3> GetPushMoves()
    {
        List<Vector3> pushMoves = new List<Vector3>();
        Vector3 currentPosition = transform.position;

        // Define possible push directions
        Vector3[] directions = new Vector3[]
        {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
        };

        foreach (Vector3 direction in directions)
        {
            Vector3 pushPosition = currentPosition + direction * cellSize;
            if (CanPushChain(direction))
            {
                pushMoves.Add(pushPosition);
            }
        }

        return pushMoves;
    }
    #endregion
}