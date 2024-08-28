using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(PawnMovement))]
public class AIController : MonoBehaviour
{
    #region Variables
    [Header("Destination")]
    [SerializeField] private Vector3 targetDestination;

    [Header("Variables")]
    [SerializeField] private int ActionPointsToUse = 3;
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool isPatrolling = false;
    
    [Header("Components")]
    [SerializeField] private PawnMovement pawnMovement;
    [SerializeField] private GridController gridController;

    private Vector3 originPosition;
    private int freeActionPoints = 0;
    private Vector3 currentPreferableDestination = Vector3.zero;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (pawnMovement == null)
        {
            pawnMovement = GetComponent<PawnMovement>();
        }

        if(gridController == null)
            gridController = FindAnyObjectByType<GridController>();

        originPosition = gridController.SnapToGrid(transform.position);

        targetDestination = gridController.SnapToGrid(targetDestination);

        currentPreferableDestination = targetDestination;
        
        pawnMovement.SetMovementRange(ActionPointsToUse);
    }
    #endregion

    #region Public Methods
    public int ExecuteTurn()
    {
        Debug.Log($"Trying to move AI pawn [{this.name}]");

        if (targetDestination == gridController.SnapToGrid(this.transform.position) && isPatrolling)
            currentPreferableDestination = originPosition;
       
        if (pawnMovement.IsValidMove(currentPreferableDestination))
        {
            Debug.Log("(AI) TryMovePawn: Valid move, executing MovePath");
            pawnMovement.MovePath(currentPreferableDestination);
        }

        return ActionPointsToUse;
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


}