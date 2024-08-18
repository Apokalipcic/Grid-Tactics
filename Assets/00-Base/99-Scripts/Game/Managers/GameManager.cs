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
    [Range(1,25)]
    [SerializeField] private int maxPlayerActions = 3;
    [Range(1,25)]
    [SerializeField] private int maxEnemyActions = 3;
    //[Range(1,5)]
    //[SerializeField] private int maxNeutralActions = 3;


    //private int currentNeutralActions;
    [Header("Reset Properties")]
    [Range(1,10)]
    [SerializeField] private float resetSpeed = 10;

    [Header("Pawns")]
    private int currentPlayerActions;
    private int currentEnemyActions;
    public List<PawnMovement> PlayerPawns { get; private set; } = new List<PawnMovement>();
    public List<PawnMovement> EnemyPawns { get; private set; } = new List<PawnMovement>();
    public List<PawnMovement> NeutralPawns { get; private set; } = new List<PawnMovement>();

    [Header("Script References")]
    [SerializeField] UserInterface_Script userInterface;
    [SerializeField] WinConditionManager winConditionManager;

    [Header("Debug")]
    [SerializeField] bool debug = false;

    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if(!winConditionManager)
            winConditionManager = GetComponent<WinConditionManager>();

        SetupGame();
    }
    #endregion

    #region Game State Management
    private void SetupGame()
    {
        CurrentState = GameState.Setup;
        //ResetActionPoints();

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
            gridCellsHolder.GetChild(forwardIndex).GetComponent<CubeController>().ActivateThisCube();
            gridCellsHolder.GetChild(backwardIndex).GetComponent<CubeController>().ActivateThisCube();


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
        //currentPlayerActions = maxPlayerActions;
        StartCoroutine(IncreaseActionPointsPool());
        // Additional logic for starting player turn    
    }

    private IEnumerator IncreaseActionPointsPool()
    {
        if (CurrentState == GameState.PlayerAction)
        {
            while (currentPlayerActions != maxPlayerActions)
            {
                currentPlayerActions++;
                userInterface.UpdatePlayerActionPoint(true);
                float delayBetweenSpawn = timeToActivateWholeGrid / maxPlayerActions;


                yield return new WaitForSeconds(delayBetweenSpawn);
            }
        }
        else if (CurrentState == GameState.EnemyAction)
        {
            while (currentEnemyActions != maxEnemyActions)
            {
                currentEnemyActions++;
                //userInterface.UpdatePlayerActionPoint(true);
                float delayBetweenSpawn = timeToActivateWholeGrid / maxEnemyActions;


                yield return new WaitForSeconds(delayBetweenSpawn);
            }
        }
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
        //currentNeutralActions = maxNeutralActions;
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
                while (pawn.IsMoving())
                {
                    yield return null;
                }
            }
        }

        EndCurrentTurn(); // Move to next turn if actions remain
    }

    public void EndCurrentTurn()
    {
        GameState oldState = CurrentState;

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

        OnDebug($"OldState {oldState} ended, new state {CurrentState} started");
    }
    #endregion

    #region Action Point Management
    public void ReturnActionPoints(int amount)
    {
        //Can Return Only Player AP
        if (CurrentState != GameState.PlayerAction || amount <= 0)
            return;

        currentPlayerActions += amount;
        userInterface.SetPlayerActionPonts(currentPlayerActions);
    }
    public bool UseActionPoint()
    {
        switch (CurrentState)
        {
            case GameState.PlayerAction:
                if (currentPlayerActions > 0)
                {
                    currentPlayerActions--;
                    userInterface.UpdatePlayerActionPoint(false);
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
                break;
        }
        return false;
    }

    public int GetAmountOfAvailableActionPoints()
    {
        int amount = CurrentState == GameState.EnemyAction ? currentEnemyActions : currentPlayerActions;
        
        OnDebug($"Request Amount of Available Points [{amount}] in state {CurrentState}");

        return amount;
    }

    private void ResetActionPoints()
    {
        currentPlayerActions = maxPlayerActions;
        currentEnemyActions = maxEnemyActions;
        //currentNeutralActions = maxNeutralActions;
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
        if(PlayerPawns.Contains(pawn))
            PlayerPawns.Remove(pawn);
        else if(EnemyPawns.Contains(pawn))
            EnemyPawns.Remove(pawn);
        else if(NeutralPawns.Contains(pawn))
            NeutralPawns.Remove(pawn);
    }

    public void SetAllPlayerPawnCollider(bool state)
    {
        foreach (var player in PlayerPawns)
        {
            player.GetComponent<Collider>().enabled = state;
        }
    }

    public PawnMovement GetPawnAtPosition(Vector3 position)
    {
        foreach (var pawn in PlayerPawns)
        {
            if (Vector3.Distance(pawn.transform.position, position) < 0.1f)
                return pawn;
        }
        foreach (var pawn in EnemyPawns)
        {
            if (Vector3.Distance(pawn.transform.position, position) < 0.1f)
                return pawn;
        }
        foreach (var pawn in NeutralPawns)
        {
            if (Vector3.Distance(pawn.transform.position, position) < 0.1f)
                return pawn;
        }
        return null;
    }

    public void ResetCurrentStage()
    {
        //Reseting origin for pawns and returning everything how it was

        if (PlayerPawns.Count != 0)
        {
            foreach (PawnMovement pawn in PlayerPawns)
            {
                pawn.OriginReset(resetSpeed);
            }
        }

        if (EnemyPawns.Count != 0)
        {
            foreach (PawnMovement pawn in EnemyPawns)
            {
                pawn.OriginReset(resetSpeed);
            }
        }

        if (NeutralPawns.Count != 0)
        {
            foreach (PawnMovement pawn in NeutralPawns)
            {
                pawn.OriginReset(resetSpeed);
            }
        }

        Debug.Log($"Reset Current Stage was pressed");
    }

    public void UndoMove()
    {
        //Removing everything as it was by 1 turn.
        if (PlayerPawns.Count != 0)
        {
            foreach (PawnMovement pawn in PlayerPawns)
            {
                pawn.UndoMove(resetSpeed);
            }
        }

        if (EnemyPawns.Count != 0)
        {
            foreach (PawnMovement pawn in EnemyPawns)
            {
                pawn.UndoMove(resetSpeed);
            }
        }

        if (NeutralPawns.Count != 0)
        {
            foreach (PawnMovement pawn in NeutralPawns)
            {
                pawn.UndoMove(resetSpeed);
            }
        }

        Debug.Log($"Undo Move was pressed");
    }

    #endregion

    #region Chest Management
    public void ActivateChest(WinConditionType winCondition)
    {
        winConditionManager.UpdateCondition(winCondition);
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
    public void EndGame(bool playerWon)
    {
        CurrentState = GameState.GameOver;
        
        if (playerWon)
        {
            userInterface.CallWinScreen();
        }
        else
        {
            userInterface.CallLoseScreen();
        }
    }
    #endregion

    #region Debug
    public void DebugLogGameState()
    {
        Debug.Log($"Current State: {CurrentState}");
        Debug.Log($"Player Actions: {currentPlayerActions}/{maxPlayerActions}");
        Debug.Log($"Enemy Actions: {currentEnemyActions}/{maxEnemyActions}");
        //Debug.Log($"Neutral Actions: {currentNeutralActions}/{maxNeutralActions}");
        Debug.Log($"Player Pawns: {PlayerPawns.Count}");
        Debug.Log($"Enemy Pawns: {EnemyPawns.Count}");
        Debug.Log($"Neutral Pawns: {NeutralPawns.Count}");
    }

    private void OnDebug(string message)
    {
        if(debug)
            Debug.Log(message);
    }
    #endregion
}