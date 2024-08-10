using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PawnMovement : MonoBehaviour
{
    #region Variables
    [Header("Pawn Properties")]
    public PawnType pawnType;
    [Range(1,10)]
    [SerializeField] private int movementRange = 1;
    [SerializeField] private int movementBuff = 0;

    [Header("Grid Properties")]
    [SerializeField] private float cellSize = 1f;

    [Header("Height Adjustment")]
    //[SerializeField] private float offsetY = 0f;

    [Header("Movement Properties")]
    [SerializeField] private float jumpHeight = 0.5f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Origin Reset Values")]
    [Tooltip("How fast the reset is")]
    [SerializeField] private float resetSpeed = 0.5f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private bool isMoving = false;
    private Coroutine currentMovement;
    private Collider pawnCollider;

    public enum PawnType
    {
        Normal,
        Diagonal,
        Queen
    }
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        originPosition = transform.position;
        originRotation = transform.rotation;

        pawnCollider = GetComponent<Collider>();
    }
    #endregion

    #region Movement Methods
    public void MovePath(Vector3 path)
    {
        if (!isMoving)
        {
            StoreCurrentState();
            currentMovement = StartCoroutine(MoveToPosition(path));
            // AudioManager.Instance.Play("PawnMove");
        }
    }

    private IEnumerator MoveToPosition(Vector3 destination)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, destination);
        float journeyTime = journeyLength / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < journeyTime)
        {
            float t = elapsedTime / journeyTime;

            // Parabolic jump
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            Vector3 newPosition = Vector3.Lerp(startPosition, destination, t);
            newPosition.y += height;

            transform.position = newPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the pawn ends exactly at the destination
        transform.position = destination;
        isMoving = false;
        currentMovement = null;
        pawnCollider.enabled = true;

        // AudioManager.Instance.Play("PawnLand");
    }

    public void DeselectThisPawn()
    {
        pawnCollider.enabled = true;
    }

    public void OriginReset()
    {
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }
        currentMovement = StartCoroutine(ResetToOrigin());
        // AudioManager.Instance.Play("PawnResetStart");
    }

    private IEnumerator ResetToOrigin()
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

        transform.position = originPosition;
        transform.rotation = originRotation;
        isMoving = false;
        pawnCollider.enabled = true;
        currentMovement = null;

        // AudioManager.Instance.Play("PawnResetComplete");
    }

    public bool IsValidMove(Vector3 startPosition, Vector3 endPosition)
    {
        if (startPosition == endPosition) return false; // Immediately return false if start and end are the same

        pawnCollider.enabled = false;

        Vector3 delta = endPosition - startPosition;
        int deltaX = Mathf.RoundToInt(delta.x / cellSize);
        int deltaZ = Mathf.RoundToInt(delta.z / cellSize);

        int totalMovement = movementRange + movementBuff;

        switch (pawnType)
        {
            case PawnType.Normal:
                return IsValidNormalMove(deltaX, deltaZ, totalMovement);
            case PawnType.Diagonal:
                return IsValidDiagonalMove(deltaX, deltaZ, totalMovement);
            case PawnType.Queen:
                return IsValidQueenMove(deltaX, deltaZ, totalMovement);
            default:
                return false;
        }
    }

    private bool IsValidNormalMove(int deltaX, int deltaZ, int totalMovement)
    {
        return (Mathf.Abs(deltaX) == 0 || Mathf.Abs(deltaZ) == 0) &&
               (Mathf.Abs(deltaX) + Mathf.Abs(deltaZ) <= totalMovement);
    }

    private bool IsValidDiagonalMove(int deltaX, int deltaZ, int totalMovement)
    {
        return (Mathf.Abs(deltaX) == Mathf.Abs(deltaZ)) &&
               (Mathf.Abs(deltaX) <= totalMovement);
    }

    private bool IsValidQueenMove(int deltaX, int deltaZ, int totalMovement)
    {
        // Allow diagonal movement
        bool isDiagonal = Mathf.Abs(deltaX) == Mathf.Abs(deltaZ);

        // Allow straight movement (horizontal, vertical, or diagonal)
        bool isStraightLine = deltaX == 0 || deltaZ == 0 || isDiagonal;

        // Check if the move is within the total movement range
        bool isWithinRange = Mathf.Max(Mathf.Abs(deltaX), Mathf.Abs(deltaZ)) <= totalMovement;

        return isStraightLine && isWithinRange;
    }

    public List<Vector3> GetValidMoves(Vector3 startPosition)
    {
        //startPosition.y = offsetY;

        List<Vector3> validMoves = new List<Vector3>();
        int totalMovement = movementRange + movementBuff;

        for (int x = -totalMovement; x <= totalMovement; x++)
        {
            for (int z = -totalMovement; z <= totalMovement; z++)
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