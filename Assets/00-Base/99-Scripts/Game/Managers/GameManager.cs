using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        //if (Instance == null)
        //{
        //    Instance = this;
        //    DontDestroyOnLoad(gameObject);
        //}
        //else
        //{
        //    Destroy(gameObject);
        //}

    }
    #endregion

    #region Variables
    [Header("Grid Values")]
    [SerializeField] float timeToActivateWholeGrid = 1.25f;
    [SerializeField] Transform gridCellsHolder;
    public enum GameState { Setup, PlayerAction, EnemyAction, NeutralAction, GameOver }
    public GameState CurrentState { get; private set; }

    [Header("Action Points")]
    [Range(1,5)]
    [SerializeField] private int maxPlayerActions = 3;
    [Range(1,5)]
    [SerializeField] private int maxEnemyActions = 3;
    [Range(1,5)]
    [SerializeField] private int maxNeutralActions = 3;

    [Header("Pawns")]
    private int currentPlayerActions;
    private int currentEnemyActions;
    private int currentNeutralActions;

    public List<PawnMovement> PlayerPawns { get; private set; } = new List<PawnMovement>();
    public List<PawnMovement> EnemyPawns { get; private set; } = new List<PawnMovement>();
    public List<PawnMovement> NeutralPawns { get; private set; } = new List<PawnMovement>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        SetupGame();
    }
    #endregion

    #region Game State Management
    private void SetupGame()
    {
        CurrentState = GameState.Setup;
        ResetActionPoints();

        for (int i = 0; i < gridCellsHolder.childCount; i++)
        {
            gridCellsHolder.transform.GetChild(i).gameObject.SetActive(false);
        }
        StartCoroutine(ActivateGrid());

        // Additional setup logic (e.g., spawning pawns, setting up the board)
        StartPlayerTurn();
    }

    #region Grid System Functions

    private IEnumerator ActivateGrid()
    {
        int totalCells = gridCellsHolder.childCount;

        float delayBetweenSpawn = timeToActivateWholeGrid / totalCells;

        int forwardIndex = 0;
        int backwardIndex = totalCells - 1;

        while (totalCells > 0)
        {
            gridCellsHolder.GetChild(forwardIndex).gameObject.SetActive(true);
            gridCellsHolder.GetChild(backwardIndex).gameObject.SetActive(true);

            forwardIndex++;
            backwardIndex--;

            yield return new WaitForSeconds(delayBetweenSpawn);

            totalCells = totalCells - 2;
        }
    }

    #endregion


    private void StartPlayerTurn()
    {
        CurrentState = GameState.PlayerAction;
        currentPlayerActions = maxPlayerActions;
        UpdateUI();
        // Additional logic for starting player turn    
    }

    private void StartEnemyTurn()
    {
        CurrentState = GameState.EnemyAction;
        currentEnemyActions = maxEnemyActions;
        UpdateUI();
        StartCoroutine(ExecuteEnemyTurns());
    }
    private IEnumerator ExecuteEnemyTurns()
    {
        foreach (PawnMovement pawn in EnemyPawns)
        {
            AIController aiController = pawn.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.ExecuteTurn();
                while (currentEnemyActions > 0 && pawn.IsMoving())
                {
                    yield return null;
                }
                if (currentEnemyActions <= 0) break;
            }
        }

        if (currentEnemyActions > 0)
        {
            EndCurrentTurn(); // Move to next turn if actions remain
        }
    }

    private void StartNeutralTurn()
    {
        CurrentState = GameState.NeutralAction;
        currentNeutralActions = maxNeutralActions;
        UpdateUI();
        StartCoroutine(ExecuteNeutralTurns());
    }

    private IEnumerator ExecuteNeutralTurns()
    {
        foreach (PawnMovement pawn in NeutralPawns)
        {
            AIController aiController = pawn.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.ExecuteTurn();
                while (currentNeutralActions > 0 && pawn.IsMoving())
                {
                    yield return null;
                }
                if (currentNeutralActions <= 0) break;
            }
        }

        if (currentNeutralActions > 0)
        {
            EndCurrentTurn(); // Move to next turn if actions remain
        }
    }

    public void EndCurrentTurn()
    {
        switch (CurrentState)
        {
            case GameState.PlayerAction:
                StartEnemyTurn();
                break;
            case GameState.EnemyAction:
                StartNeutralTurn();
                break;
            case GameState.NeutralAction:
                StartPlayerTurn();
                break;
        }
    }
    #endregion

    #region Action Point Management
    public bool UseActionPoint()
    {
        switch (CurrentState)
        {
            case GameState.PlayerAction:
                if (currentPlayerActions > 0)
                {
                    currentPlayerActions--;
                    UpdateUI();
                    if (currentPlayerActions == 0) EndCurrentTurn();
                    return true;
                }
                break;
            case GameState.EnemyAction:
                if (currentEnemyActions > 0)
                {
                    currentEnemyActions--;
                    UpdateUI();
                    if (currentEnemyActions == 0) EndCurrentTurn();
                    return true;
                }
                break;
            case GameState.NeutralAction:
                if (currentNeutralActions > 0)
                {
                    currentNeutralActions--;
                    UpdateUI();
                    if (currentNeutralActions == 0) EndCurrentTurn();
                    return true;
                }
                break;
        }
        return false;
    }

    private void ResetActionPoints()
    {
        currentPlayerActions = maxPlayerActions;
        currentEnemyActions = maxEnemyActions;
        currentNeutralActions = maxNeutralActions;
    }
    #endregion

    #region Pawn Management
    public void AddPawn(PawnMovement pawn, string tag)
    {
        if (tag == "Player")
            PlayerPawns.Add(pawn);
        else if (tag == "Enemy")
            EnemyPawns.Add(pawn);
        else if (tag == "Neutral")
            NeutralPawns.Add(pawn);
    }

    public void RemovePawn(PawnMovement pawn)
    {
        PlayerPawns.Remove(pawn);
        EnemyPawns.Remove(pawn);
        NeutralPawns.Remove(pawn);
    }

    public void ResetCurrentStage()
    {
        //Reseting origin for pawns and returning everything how it was

        if (PlayerPawns.Count == 0)
        {
            foreach (PawnMovement pawn in PlayerPawns)
            {
                pawn.Reset();
            }
        }

        if (EnemyPawns.Count == 0)
        {
            foreach (PawnMovement pawn in EnemyPawns)
            {
                pawn.Reset();
            }
        }

        if (NeutralPawns.Count == 0)
        {
            foreach (PawnMovement pawn in NeutralPawns)
            {
                pawn.Reset();
            }
        }

        Debug.Log($"Reset Current Stage was pressed");
    }

    public void UndoMove()
    {
        //Removing everything as it was by 1 turn.
        if (PlayerPawns.Count == 0)
        {
            foreach (PawnMovement pawn in PlayerPawns)
            {
                pawn.UndoMove();
            }
        }

        if (EnemyPawns.Count == 0)
        {
            foreach (PawnMovement pawn in EnemyPawns)
            {
                pawn.UndoMove();
            }
        }

        if (NeutralPawns.Count == 0)
        {
            foreach (PawnMovement pawn in NeutralPawns)
            {
                pawn.UndoMove();
            }
        }

        Debug.Log($"Undo Move was pressed");
    }

    #endregion

    #region UI Management
    private void UpdateUI()
    {
        // Update UI elements (action points, current turn, etc.)
        // This method would be called whenever the game state changes
    }
    #endregion

    #region Win/Lose Condition
    public void CheckWinCondition()
    {
        // Implement win condition logic
        // If win condition is met, call EndGame(true)
    }

    public void CheckLoseCondition()
    {
        // Implement lose condition logic
        // If lose condition is met, call EndGame(false)
    }

    private void EndGame(bool playerWon)
    {
        CurrentState = GameState.GameOver;
        // Implement game over logic (e.g., show win/lose screen)
    }
    #endregion

    #region Debug
    public void DebugLogGameState()
    {
        Debug.Log($"Current State: {CurrentState}");
        Debug.Log($"Player Actions: {currentPlayerActions}/{maxPlayerActions}");
        Debug.Log($"Enemy Actions: {currentEnemyActions}/{maxEnemyActions}");
        Debug.Log($"Neutral Actions: {currentNeutralActions}/{maxNeutralActions}");
        Debug.Log($"Player Pawns: {PlayerPawns.Count}");
        Debug.Log($"Enemy Pawns: {EnemyPawns.Count}");
        Debug.Log($"Neutral Pawns: {NeutralPawns.Count}");
    }
    #endregion
}