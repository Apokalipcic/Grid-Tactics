using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

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
        CurrentAmount = Mathf.Min(CurrentAmount + amount, RequiredAmount);
    }
}
[System.Serializable]

public class PawnAction
{
    public Vector3 LastPosition { get; set; }
    public Quaternion LastRotation { get; set; }
    public int ActionPointsSpent { get; set; }
    public List<PawnMovement> KilledPawns { get; set; } = new List<PawnMovement>();
}