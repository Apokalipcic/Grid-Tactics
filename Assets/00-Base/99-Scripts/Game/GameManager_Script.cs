using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager_Script : MonoBehaviour
{
    [Header("Grid Values")]
    [SerializeField] float timeToActivateWholeGrid = 1.25f;
    [SerializeField] Transform gridCellsHolder;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        InitializeThisScript();
    }

    private void Start()
    {
        StartGame();
    }

    public void InitializeThisScript()
    {

    }

    public void StartGame()
    {
        for (int i = 0; i < gridCellsHolder.childCount; i++)
        {
            gridCellsHolder.transform.GetChild(i).gameObject.SetActive(false);
        }
        StartCoroutine(ActivateGrid());
    }

    #region Grid System Functions

    private IEnumerator ActivateGrid()
    {
        int totalCells = gridCellsHolder.childCount;

        float delayBetweenSpawn = timeToActivateWholeGrid / totalCells;

        int forwardIndex = 0;
        int backwardIndex = totalCells-1;

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


}
