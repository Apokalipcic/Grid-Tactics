using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PushableObstacles : MonoBehaviour, IPushable
{
    #region Variables
    [Header("Movement Properties")]
    [SerializeField] private float pushSpeed = 2f;
    [SerializeField] private float cellSize = 1f;

    [Header("Grid Reference")]
    [SerializeField] private GridController gridController;

    [Header("Components")]
    [SerializeField] private Animator anim;

    private Vector3 originPosition;
    private Vector3 lastPosition;
    private bool isPushing = false;
    private Coroutine currentMovement;
    private CubeController currentOccupiedCell;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        GameManager.Instance.AddPusheable(this);
        this.gameObject.SetActive(false);
    }

    public void Initialize()
    {
        originPosition = transform.position;
        lastPosition = originPosition;

        if (gridController == null)
        {
            gridController = FindObjectOfType<GridController>();
        }
        if (gridController)
        {
            cellSize = gridController.cellSize;
        }
        else
        {
            Debug.LogError("GridController not found for PushableObstacle: " + gameObject.name);
        }

        if (!anim)
            anim = GetComponent<Animator>();

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

    #region IPushable Implementation
    public bool CanBePushed(Vector3 direction)
    {
        Vector3 targetPosition = transform.position + direction * cellSize;
        if (!gridController.CellExists(targetPosition))
        {
            // Allow pushing off the grid
            return true;
        }
        return !gridController.GetCellAtPosition(targetPosition).GetComponent<CubeController>().IsOccupied();
    }

    public void Push(Vector3 direction)
    {
        if (isPushing) return;

        lastPosition = transform.position;
        Vector3 targetPosition = transform.position + direction * cellSize;
        StartCoroutine(PushCoroutine(targetPosition));
    }

    private IEnumerator PushCoroutine(Vector3 targetPosition)
    {
        isPushing = true;

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

        if (gridController.CellExists(targetPosition))
        {
            UpdateCubeOccupation(targetPosition, true);
        }
        else
        {
            HandlePushedOffGrid();
        }

        isPushing = false;
    }

    private void HandlePushedOffGrid()
    {
        // Implement off-grid behavior (e.g., deactivate, destroy, or special effect)
        anim.SetBool("DecreaseSize", true);
        Debug.Log($"PushableObstacle {name} was pushed off the grid");
    }

    public void UndoPushObject()
    {
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        float resetDuration = GameManager.Instance.resetDuration;
        currentMovement = StartCoroutine(ResetPosition(resetDuration, transform.position, lastPosition));
    }

    public void ReturnPushObjectOrigin()
    {
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
        }

        float resetDuration = GameManager.Instance.resetDuration;
        currentMovement = StartCoroutine(ResetPosition(resetDuration, transform.position, originPosition));
    }

    private IEnumerator ResetPosition(float resetDuration, Vector3 startPosition, Vector3 targetPosition)
    {
        if (gridController.CellExists(startPosition))
        {
            UpdateCubeOccupation(startPosition, false);
        }

        float elapsedTime = 0f;

        while (elapsedTime < resetDuration)
        {
            float t = elapsedTime / resetDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        if (gridController.CellExists(targetPosition))
        {
            UpdateCubeOccupation(targetPosition, true);
        }

        if (anim.GetBool("DecreaseSize"))
        {
            anim.SetBool("DecreaseSize", false);
        }
    }

    public string GetPushableName()
    {
        return gameObject.name;
    }
    #endregion

    #region Helper Methods
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
            Debug.LogWarning($"Cannot Update Cube Occupation for {name}, because cube doesn't exist at {position}");
        }
    }
    #endregion
}