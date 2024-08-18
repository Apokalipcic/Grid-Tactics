using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

public class PawnMovement : MonoBehaviour, IPushable
{
    #region Variables
    [Header("Pawn Properties")]
    [Range(1, 10)]
    [SerializeField] private int movementRange = 1;
    [SerializeField] private int movementBuff = 0;
    [SerializeField] bool isDead = false;

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
    [SerializeField] private bool canChainPush = true;
    [SerializeField] private float pushSpeed = 2f;

    [Header("Components")]
    [SerializeField] private Animator anim;

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
    private HashSet<Vector3> pushableMoves;
    private bool moved = false;
    private CubeController currentOccupiedCell;

    [Header("Booster Properties")]
    private string currentBooster = "None";
    #endregion

    #region Initialization
    private void Start()
    {
        GameManager.Instance.AddPawn(this, this.tag);
        this.gameObject.SetActive(false);
    }

    public void Initialize()
    {
        originPosition = transform.position;
        originRotation = transform.rotation;

        pushSpeed = moveSpeed;

        pawnCollider = GetComponent<Collider>();

        if (gridController)
            cellSize = gridController.cellSize;
        else
            Debug.LogError($"Grid Controller in pawn {this.name} not found");

        if (!anim)
            anim = this.GetComponent<Animator>();

        GameManager.Instance.AddPawn(this, this.tag);
        pathfinder = new AStarPathfinder(gridController, this);

        currentOccupiedCell = gridController.GetCellAtPosition(originPosition)?.GetComponent<CubeController>();

        if (currentOccupiedCell != null)
        {
            currentOccupiedCell.OnOccupy(this.gameObject);
        }
        else
            Debug.Log($"Current Occupied Cell doesn't exist at position {originPosition}");

        this.gameObject.SetActive(true);
    }

    #endregion

    #region Movement Methods
    public void CalculateReachableCells()
    {
        int maxDistance = Mathf.Min(movementRange + movementBuff, GameManager.Instance.GetAmountOfAvailableActionPoints());
        cachedPaths = pathfinder.FindAllReachableCells(transform.position, maxDistance);
        pushableMoves = new HashSet<Vector3>(GetPushMoves());
    }


    public List<Vector3> GetValidMoves()
    {
        if (cachedPaths == null || pushableMoves == null)
        {
            CalculateReachableCells();
        }

        List<Vector3> validMoves = new List<Vector3>(cachedPaths.Keys);
        validMoves.AddRange(pushableMoves);
        return validMoves;
    }

    public void MovePath(Vector3 destination)
    {
        if (isMoving || isPushing)
        {
            Debug.LogWarning("Cannot move: Pawn is already moving.");
            return;
        }

        if (pushableMoves.Contains(destination))
        {
            Vector3 direction = (destination - transform.position).normalized;
            if (TryPush(direction))
            {
                StoreCurrentState();
                amountOfActionPointsUsed = 1; // Pushing always costs 1 action point
                GameManager.Instance.UseActionPoint();
            }
            else
            {
                Debug.LogWarning("Push failed.");
            }
            return;
        }

        if (cachedPaths == null || !cachedPaths.ContainsKey(destination))
        {
            Debug.LogError("Invalid move: Path not found in cached paths");
            return;
        }

        StoreCurrentState();
        amountOfActionPointsUsed = 0;

        List<Vector3> path = cachedPaths[destination];
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }
        currentMovement = StartCoroutine(MoveAlongPath(path));

        // Clear the cache after starting movement
        cachedPaths = null;
        pushableMoves = null;
    }

    private IEnumerator MoveAlongPath(List<Vector3> path)
    {
        isMoving = true;
        pawnCollider.enabled = false;

        // Deoccupy the starting position
        UpdateCubeOccupation(transform.position, false);

        bool isFirstStep = true;

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

            CubeController currentCube = gridController.GetCellAtPosition(cellPosition)?.GetComponent<CubeController>();
            if (currentCube != null && currentCube.IsOccupied())
            {
                GameObject occupant = currentCube.GetOccupant();
                if (occupant.CompareTag("Enemy"))
                {
                    PawnMovement enemyAI = occupant.GetComponent<PawnMovement>();
                    if (enemyAI != null)
                    {
                        enemyAI.DeathEvent();
                    }
                    // Clear the cell after killing the enemy
                    //currentCube.OnDeoccupy();
                }
            }


            // Consume Action Point, but not for the first step
            if (!isFirstStep)
            {
                GameManager.Instance.UseActionPoint();
                amountOfActionPointsUsed++;
            }
            isFirstStep = false;
        }

        // Occupy the new position
        UpdateCubeOccupation(transform.position, true);

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
        {
            Debug.LogWarning($"Cannot Update Cube Occupation, because cube doesn't exist at {position}");
        }
    }
    #endregion

    #region Pushing Methods
    public bool CanPushChain(Vector3 direction)
    {
        if (!canPush) return false;

        Vector3 currentPosition = transform.position;
        HashSet<IPushable> pushedObjects = new HashSet<IPushable>();

        while (true)
        {
            Vector3 nextPosition = currentPosition + direction * cellSize;
            if (!gridController.CellExists(nextPosition)) return true; // Can push off grid

            CubeController nextCube = gridController.GetCellAtPosition(nextPosition)?.GetComponent<CubeController>();
            if (nextCube == null) return false;

            if (!nextCube.IsOccupied()) return true;
            //if (!nextCube.isWalkable) return false //TODO: I'm not sure I need this


            GameObject occupant = nextCube.GetOccupant();
            // Check if the occupant is Neutral or has the same tag as the pushing pawn
            if (occupant.CompareTag("Neutral") || occupant.CompareTag(this.gameObject.tag))
            {
                IPushable nextPushable = occupant.GetComponent<IPushable>();

                if (nextPushable == null) return false;

                if (pushedObjects.Contains(nextPushable)) return false;

                if (!nextPushable.CanBePushed(direction)) return false;

                pushedObjects.Add(nextPushable);

                if (!canChainPush) return true; // If chain pushing is disabled, stop after checking the first object

                currentPosition = nextPosition;
            }
            else
            {
                return false; // Cannot push objects that are not Neutral or the same tag
            }
        }
    }

public bool TryPush(Vector3 direction)
{
    if (!canPush || isMoving || isPushing) return false;

    if (!CanPushChain(direction)) return false;

    Vector3 currentPosition = transform.position;
    List<IPushable> objectsToPush = new List<IPushable>();

    while (true)
    {
        Vector3 nextPosition = currentPosition + direction * cellSize;
        CubeController nextCube = gridController.GetCellAtPosition(nextPosition)?.GetComponent<CubeController>();

        if (nextCube == null) break;
        if (!nextCube.IsOccupied()) break;

        // Remove the check for isWalkable here

        GameObject occupant = nextCube.GetOccupant();
        
        // Check if the occupant is Neutral or has the same tag as the pushing pawn
        if (occupant.CompareTag("Neutral") || occupant.CompareTag(this.gameObject.tag))
        {

            IPushable nextPushable = occupant.GetComponent<IPushable>();

            if (nextPushable == null) break;

            objectsToPush.Add(nextPushable);

            if (!canChainPush) break;

            currentPosition = nextPosition;
        }
        else
        {
            break; // Cannot push objects that are not Neutral or the same tag
        }
    }

    // Push objects in reverse order
    for (int i = objectsToPush.Count - 1; i >= 0; i--)
    {
        objectsToPush[i].Push(direction);
    }

    return objectsToPush.Count > 0;
}

    public List<Vector3> GetPushMoves()
    {
        List<Vector3> pushMoves = new List<Vector3>();
        Vector3 currentPosition = transform.position;

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
            CubeController nextCube = gridController.GetCellAtPosition(pushPosition)?.GetComponent<CubeController>();

            // Only consider it a push move if the cell is occupied and can be pushed
            if (nextCube != null && nextCube.IsOccupied() && CanPushChain(direction))
            {
                pushMoves.Add(pushPosition);
            }
        }

        return pushMoves;
    }
    #endregion

    #region IPushable Implementation
    public bool CanBePushed(Vector3 direction)
    {
        Vector3 targetPosition = transform.position + direction * cellSize;
        return gridController.CellExists(targetPosition) || GameManager.Instance.GetPawnAtPosition(targetPosition) == null;
    }

    public void Push(Vector3 direction)
    {
        Vector3 targetPosition = transform.position + direction * cellSize;
        StartCoroutine(PushCoroutine(targetPosition));
    }

    private IEnumerator PushCoroutine(Vector3 targetPosition)
    {
        isPushing = true;

        StoreCurrentState();

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

        UpdateCubeOccupation(targetPosition, true);

        isPushing = false;

        if (!gridController.CellExists(targetPosition))
        {
            HandlePushedOffGrid();
        }
    }

    private void HandlePushedOffGrid()
    {
        DeathEvent();
        //GameManager.Instance.RemovePawn(this);
        GetComponent<Rigidbody>().isKinematic = false;
        Debug.Log($"Pawn {this.name} was pushed off the grid");
    }
    #endregion

    #region Public Events
    public void DeathEvent()
    {
        if(isDead) return;

        currentOccupiedCell.OnDeoccupy();

        Debug.Log($"Name [{this.name}] Tag [{this.tag}] - pawn died!");

        // Example: Disable the enemy GameObject
        //gameObject.SetActive(false);

        anim.SetBool("DecreaseSize", true);

        //GameManager.Instance.RemovePawn(this);

        isDead = true;
    }

    public void RessurectEvent()
    {
        if (!isDead) return;

        isDead = false;

        GetComponent<Rigidbody>().isKinematic = true;

        anim.SetBool("DecreaseSize", false);

        currentOccupiedCell.OnOccupy(this.gameObject);

        //GameManager.Instance.AddPawn(this, this.tag);

        Debug.Log($"Name [{this.name}] Tag [{this.tag}] - pawn ressurected!");
    }

    #endregion

    #region Helper Methods
    public bool CanMoveDiagonally()
    {
        return canMoveDiagonally;
    }

    public bool IsValidMove(Vector3 endPosition)
    {
        if (cachedPaths == null || pushableMoves == null)
        {
            CalculateReachableCells();
        }
        return cachedPaths.ContainsKey(endPosition) || pushableMoves.Contains(endPosition);
    }

    public void OriginReset(float resedDuration)
    {
        if (isDead)
        {
            RessurectEvent();
        }

        if (!moved)
            return;

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        UpdateCubeOccupation(this.transform.position, false);

        currentMovement = StartCoroutine(ResetPosition(resedDuration, originPosition, originRotation));
    }

    private IEnumerator ResetPosition(float resetDuration, Vector3 targetPosition, Quaternion targetRotation)
    {
        isMoving = true;
        pawnCollider.enabled = false;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < resetDuration)
        {
            float t = elapsedTime / resetDuration;

            // Use SmoothStep for a more natural easing
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final position and rotation are exact
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        lastPosition = targetPosition;
        lastRotation = targetRotation;

        isMoving = false;
        pawnCollider.enabled = true;

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

    private void StoreCurrentState()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;

        moved = true;
    }

    public void Reset()
    {
        if (!moved) return;

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
        UpdateCubeOccupation(lastPosition, true);
        Debug.Log($"Undo move by {this.name} && LastPosition is {lastPosition}");
    }

    public void UndoMove(float resetDuration)
    {
        if (isDead)
        {
            RessurectEvent();
        }

        if (!moved) return;

        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        UpdateCubeOccupation(this.transform.position, false);

        currentMovement = StartCoroutine(ResetPosition(resetDuration, lastPosition, lastRotation));
    }
    #endregion
}