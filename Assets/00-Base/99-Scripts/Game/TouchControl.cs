using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Recorder.Input;

public class TouchController : MonoBehaviour
{
    #region Variables
    [Header("Layer Masks")]
    [SerializeField] private LayerMask pawnLayer;
    [SerializeField] private LayerMask cellLayer;

    [Header("Grid Reference")]
    [SerializeField] private GridController gridController;

    [Header("Highlighting")]
    [SerializeField] private Color regularMoveColor = Color.blue;
    [SerializeField] private Color pushMoveColor = Color.red;

    private PawnMovement selectedPawn;
    private Dictionary<Transform, Color> highlightedCells = new Dictionary<Transform, Color>();

    private Camera mainCamera;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleTouch(Input.GetTouch(0).position);
        }
    }
    #endregion

    #region Touch Handling
    private void HandleTouch(Vector2 touchPosition)
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.PlayerAction)
        {
            return; // Only allow interactions during the player's turn
        }

        Ray ray = mainCamera.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, pawnLayer))
        {
            SelectPawn(hit.collider.GetComponent<PawnMovement>());
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, cellLayer))
        {
            TryMovePawn(hit.point);
        }
    }

    private void SelectPawn(PawnMovement pawn)
    {
        if (pawn != null && !pawn.IsMoving() && pawn.CompareTag("Player"))
        {
            GameManager.Instance.SetAllPlayerPawnCollider(false);
            selectedPawn = pawn;
            selectedPawn.CalculateReachableCells();
            HighlightValidMoves();
        }
    }

    private void TryMovePawn(Vector3 targetPosition)
    {
        if (selectedPawn == null || selectedPawn.IsMoving())
        {
            Debug.Log($"TryMovePawn: Invalid state. SelectedPawn={selectedPawn}, IsMoving={selectedPawn?.IsMoving()}");
            return;
        }

        targetPosition = gridController.SnapToGrid(targetPosition);

        if (selectedPawn.IsValidMove(targetPosition))
        {
            Debug.Log("TryMovePawn: Valid move, executing MovePath");
            selectedPawn.MovePath(targetPosition);
        }
        else
        {
            Debug.Log("TryMovePawn: Invalid move");
        }

        ResetPawnSelection();
    }

    private void ResetPawnSelection()
    {
        ClearHighlightedCells();
        selectedPawn = null;
        GameManager.Instance.SetAllPlayerPawnCollider(true);
    }
    #endregion

    #region Helper Methods
    private void HighlightValidMoves()
    {
        ClearHighlightedCells();

        if (selectedPawn == null)
        {
            Debug.LogError("HighlightValidMoves: No pawn selected");
            return;
        }

        List<Vector3> validMoves = selectedPawn.GetValidMoves();
        List<Vector3> pushableMoves = selectedPawn.GetPushMoves();

        foreach (Vector3 move in validMoves)
        {
            HighlightCell(move, regularMoveColor);
        }

        foreach (Vector3 move in pushableMoves)
        {
            HighlightCell(move, pushMoveColor);
        }
    }
    private void HighlightCell(Vector3 position, Color color)
    {
        Transform cell = gridController.GetCellAtPosition(position);
        if (cell != null)
        {
            CubeController cubeController = cell.GetComponent<CubeController>();
            if (cubeController != null)
            {
                cubeController.ChangeHighlightVFX(true, color);
                highlightedCells[cell] = color;
            }
        }
        else
        {
            Debug.LogWarning($"No cell found at position: {position}");
        }
    }

    private void ClearHighlightedCells()
    {
        foreach (var cellPair in highlightedCells)
        {
            CubeController cubeController = cellPair.Key.GetComponent<CubeController>();
            if (cubeController != null)
            {
                cubeController.ChangeHighlightVFX(false);
            }
        }
        highlightedCells.Clear();
    }
    #endregion
}