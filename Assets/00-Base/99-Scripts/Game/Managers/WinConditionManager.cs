using UnityEngine;
using System.Collections.Generic;


public class WinConditionManager : MonoBehaviour
{
    [SerializeField] private List<WinCondition> winConditions = new List<WinCondition>();

    public void AddWinCondition(WinConditionType type, int requiredAmount)
    {
        winConditions.Add(new WinCondition { Type = type, RequiredAmount = requiredAmount });
    }

    public void UpdateCondition(WinConditionType type, int amount = 1)
    {
        WinCondition condition = winConditions.Find(c => c.Type == type);
        if (condition != null)
        {
            condition.IncrementProgress(amount);
            CheckWinCondition();
        }
    }

    private void CheckWinCondition()
    {
        if (winConditions.TrueForAll(c => c.IsMet))
        {
            Win();
        }
    }

    private void Win()
    {
        Debug.Log("All win conditions met! You win!");
        // Implement win logic here

        GameManager.Instance.CallWinScreen();
    }

    // Optional: Method to reset all conditions
    public void ResetConditions()
    {
        foreach (var condition in winConditions)
        {
            condition.CurrentAmount = 0;
        }
    }
}