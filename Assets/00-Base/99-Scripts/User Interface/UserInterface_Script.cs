using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UserInterface_Script : MonoBehaviour
{
    [Header("Action Points")]
    [SerializeField] Transform playerActionPointsHolder;
    private int currentAmountOfActivatedAP = 0;

    [Header("Debug")]
    [SerializeField] bool debug;

    private void Start()
    {
        ResetPlayerActionPoints();
    }
    public void HardRestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #region Update Methods

    #region Player Action Points
    public void ResetPlayerActionPoints()
    {
        currentAmountOfActivatedAP = 0;

        for (int i = 0; i < playerActionPointsHolder.childCount; i++)
        {
            playerActionPointsHolder.GetChild(i).gameObject.SetActive(false);
        }
    }
    public void SetPlayerActionPonts(int amount)
    {
        if (amount >= playerActionPointsHolder.childCount)
            OnDebug($"Amount Request to Fill Player AP [{amount}] is greater than UI child count [{playerActionPointsHolder.childCount}]");

        currentAmountOfActivatedAP = amount < playerActionPointsHolder.childCount ? amount : playerActionPointsHolder.childCount;

        for (int i = 0; i < currentAmountOfActivatedAP; i++)
        {
            playerActionPointsHolder.GetChild(i).gameObject.SetActive(true);
        }
    }

    public void UpdatePlayerActionPoint(bool consume)
    {
        currentAmountOfActivatedAP = consume ? currentAmountOfActivatedAP-1: currentAmountOfActivatedAP+1;

        playerActionPointsHolder.GetChild(currentAmountOfActivatedAP+1).gameObject.SetActive(!consume);
    }
    #endregion

    #endregion

    #region Windows
    public void CallWinScreen()
    {

    }

    public void CallLoseScreen()
    {

    }
    #endregion

    #region Debug

    private void OnDebug(string message)
    {
        if(debug)
            Debug.Log(message);
    }
    #endregion
}
