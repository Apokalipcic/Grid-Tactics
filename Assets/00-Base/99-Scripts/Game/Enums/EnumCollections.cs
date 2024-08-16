public enum WinConditionType
{
    CollectChests,
    EliminateEnemies,
    CollectCrowns,
    // Add more as needed
}

[System.Serializable]
public class WinCondition
{
    public WinConditionType Type;
    public int RequiredAmount;
    public int CurrentAmount;

    public bool IsMet => CurrentAmount >= RequiredAmount;

    public void IncrementProgress(int amount = 1)
    {
        CurrentAmount = UnityEngine.Mathf.Min(CurrentAmount + amount, RequiredAmount);
    }
}