using UnityEngine;
using System.Collections.Generic;

public class TouchController : MonoBehaviour
{
    #region Variables
    [Header("Layer Masks")]
    [SerializeField] private LayerMask pawnLayer;
    [SerializeField] private LayerMask cellLayer;

    [Header("Grid Reference")]
    [SerializeField] private GridController gridController;

    private PawnMovement selectedPawn;
    private List<Transform> highlightedCells = new List<Transform>();

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
        if (pawn != null && !pawn.IsMoving())
        {
            selectedPawn = pawn;
            selectedPawn.SelectThisPawn();
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

        Vector3 currentPosition = selectedPawn.transform.position;
        if (selectedPawn.IsValidMove(currentPosition, targetPosition))
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
        selectedPawn.DeselectThisPawn();
        selectedPawn = null;
        // Optionally, you can add some visual or audio feedback here
        // to indicate that the selection has been reset
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

        List<Vector3> validMoves = selectedPawn.GetValidMoves(selectedPawn.transform.position);
        Debug.Log($"HighlightValidMoves: ValidMovesCount={validMoves.Count}");

        foreach (Vector3 move in validMoves)
        {
            Transform cell = gridController.GetCellAtPosition(move);
            if (cell != null)
            {
                CubeController cubeController = cell.GetComponent<CubeController>();
                if (cubeController != null)
                {
                    cubeController.ChangeHighlightVFX(true);
                    highlightedCells.Add(cell);
                    Debug.Log($"Highlighted cell at position: {move}");
                }
            }
            else
            {
                Debug.LogWarning($"No cell found at position: {move}");
            }
        }
    }

    private void ClearHighlightedCells()
    {
        foreach (Transform cell in highlightedCells)
        {
            CubeController cubeController = cell.GetComponent<CubeController>();
            if (cubeController != null)
            {
                cubeController.ChangeHighlightVFX(false);
            }
        }
        highlightedCells.Clear();
    }

    private CubeController GetCellAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f, cellLayer);
        return colliders.Length > 0 ? colliders[0].GetComponent<CubeController>() : null;
    }

    private PawnMovement GetPawnAtPosition(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f, pawnLayer);
        return colliders.Length > 0 ? colliders[0].GetComponent<PawnMovement>() : null;
    }

    private bool IsCellValid(Vector3 position)
    {
        return GetCellAtPosition(position) != null;
    }
    #endregion
}