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
    [Range(0.05f,10)]
    public float resetDuration = 0.5f;

    [Header("Pawns")]
    [SerializeField] private int currentPlayerActions;
    [SerializeField] private int currentEnemyActions;
    public List<PawnMovement> PlayerPawns { get; private set; } = new List<PawnMovement>();
    public List<PawnMovement> EnemyPawns { get; private set; } = new List<PawnMovement>();
    public List<PawnMovement> NeutralPawns { get; private set; } = new List<PawnMovement>();

    [Header("Other elements")]
    [SerializeField] List<PushableObstacles> pusheableElements = new List<PushableObstacles>();

    [Header("Script References")]
    [SerializeField] UserInterface_Script userInterface;
    [SerializeField] WinConditionManager winConditionManager;

    [Header("Debug")]
    [SerializeField] bool debug = false;

    private int currentMoveNumber = 0;

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
        //StartPlayerTurn();
    }

    #region Grid System Functions

    private IEnumerator ActivateGrid()
    {
        // Grid Activation
        int totalCells = gridCellsHolder.childCount;
        float gridDelayBetweenSpawn = (timeToActivateWholeGrid/totalCells) / 2; // Divide by 2 since we're activating two cells at once
        int forwardIndex = 0;
        int backwardIndex = totalCells - 1;

        while (forwardIndex <= backwardIndex)
        {
            gridCellsHolder.GetChild(forwardIndex).GetComponent<CubeController>().ActivateThisCube();

            if (forwardIndex != backwardIndex) // Avoid activating the same cell twice for odd numbers
            {
                gridCellsHolder.GetChild(backwardIndex).GetComponent<CubeController>().ActivateThisCube();
            }

            forwardIndex++;
            backwardIndex--;
            yield return new WaitForSeconds(gridDelayBetweenSpawn);
        }

        // Wait for one frame to ensure all grid operations are complete
        yield return null;

        // Pawn Activation
        List<PawnMovement> allPawns = new List<PawnMovement>();
        allPawns.AddRange(PlayerPawns);
        allPawns.AddRange(EnemyPawns);
        allPawns.AddRange(NeutralPawns);

        float pawnDelayBetweenSpawn = timeToActivateWholeGrid / allPawns.Count;

        foreach (PawnMovement pawn in allPawns)
        {
            pawn.Initialize();
            yield return new WaitForSeconds(pawnDelayBetweenSpawn);
        }

        pawnDelayBetweenSpawn = timeToActivateWholeGrid / pusheableElements.Count;

        foreach (PushableObstacles pusheableObject in pusheableElements)
        {
            pusheableObject.Initialize();
            yield return new WaitForSeconds(pawnDelayBetweenSpawn);
        }

        StartPlayerTurn();
    }

    #endregion


    private void StartPlayerTurn()
    {
        CurrentState = GameState.PlayerAction;

        foreach (PawnMovement pawn in PlayerPawns)
        {
            pawn.ClearTurnActions();
        }

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
                userInterface.UpdatePlayerActionPoint(false);
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
        foreach (PawnMovement enemyPawn in EnemyPawns)
        {
            AIController aiController = enemyPawn.GetComponent<AIController>();
            if (aiController != null)
            {
                yield return StartCoroutine(aiController.CalculateAndExecuteMove());

                // Wait for a short delay between enemy turns for better visualization
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Debug.LogError($"AIController not attached to EnemyPawn [{enemyPawn.name}]");
            }

            // Check if we should end the enemy turn (e.g., out of action points)
            if (currentEnemyActions <= 0)
            {
                break;
            }
        }

        EndCurrentTurn();
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
                //aiController.ExecuteTurn();
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
        OnDebug($"OldState {oldState} ended.");
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
    public void ReturnActionPoints(int amount)
    {
        //Can Return Only Player AP
        if (CurrentState != GameState.PlayerAction || amount <= 0)
            return;

        currentPlayerActions += amount;
        userInterface.SetPlayerActionPonts(currentPlayerActions);
    }
    public void UseActionPoint(bool consume = true)
    {
        switch (CurrentState)
        {
            case GameState.PlayerAction:
                if (currentPlayerActions >= 0)
                {
                    currentPlayerActions -= consume ? 1: -1;
                    userInterface.UpdatePlayerActionPoint(consume);
                    if (currentPlayerActions == 0) EndCurrentTurn();
                }
                break;
            case GameState.EnemyAction:
                if (currentEnemyActions > 0)
                {
                    currentEnemyActions -= consume ? 1 : -1;
                    UpdateUI();

                    if (currentEnemyActions == 0) EndCurrentTurn();
                }
                break;
            case GameState.NeutralAction:
                break;
        }
        
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
                pawn.OriginReset(resetDuration);
            }
        }

        //if (EnemyPawns.Count != 0)
        //{
        //    foreach (PawnMovement pawn in EnemyPawns)
        //    {
        //        pawn.OriginReset(resetDuration);
        //    }
        //}

        //if (NeutralPawns.Count != 0)
        //{
        //    foreach (PawnMovement pawn in NeutralPawns)
        //    {
        //        pawn.OriginReset(resetDuration);
        //    }
        //}

        Debug.Log($"Reset Current Stage was pressed");
    }

    public void UndoMove()
    {
        Debug.Log($"Undo Move was pressed");
        //Removing everything as it was by 1 turn.

        if (currentMoveNumber < 0)
            return;

        if (PlayerPawns.Count != 0)
        {
            foreach (PawnMovement pawn in PlayerPawns)
            {
                pawn.UndoMove(resetDuration, currentMoveNumber);
            }
        }

        currentMoveNumber--;
    }
    public int GetCurrentMoveNumber()
    {
        return currentMoveNumber;
    }

    public void IncrementMoveNumber()
    {
        currentMoveNumber++;
    }
    #endregion

    #region Pusheable Management
    public void AddPusheable(PushableObstacles pusheableObj)
    {
        if(!pusheableElements.Contains(pusheableObj))
            pusheableElements.Add(pusheableObj);
    }
    #endregion

    #region Chest Management
    public void ActivateChest(WinConditionType winCondition)
    {
        winConditionManager.UpdateCondition(winCondition);
    }
    #endregion

    #region UI Management
    public void CallWinScreen()
    {
        userInterface.CallWinScreen();
    }

    public void CallLoseLoose()
    {
        userInterface.CallLoseScreen();
    }

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

    private void OnDebug(string message, string type = "Log")
    {
        if(debug)
            if(type == "Log")
                Debug.Log(message);
            else if(type == "Warning")
                Debug.LogWarning(message);
            else if(type == "Error")
                Debug.LogError(message);

    }
    #endregion
}