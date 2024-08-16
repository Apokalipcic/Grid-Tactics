using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData ", menuName = "Game/Level/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Information")]
    public string levelName;
    public int levelNumber;
    //[TextArea(3, 5)]
    //public string levelDescription;

    [Header("Action Points")]
    [Range(1,99)]
    public int playerMaxActionPoints = 90;
    [Range(1,99)]
    public int playerActionPoints = 3;
    [Range(1,99)]
    public int enemyActionPoints = 2;

    [Header("Win Conditions")]
    public List<WinCondition> winConditions = new List<WinCondition>();

    //[Header("Level Challenges")]

    [Header("Rewards")]
    public int starsForCompletion = 3;
    //public int coinsReward = 100;

    [Header("Visual Settings")]
    public string backgroundTheme; // This could be an enum if you have a fixed set of themes
    //public Color primaryColor = Color.white;
    //public Color secondaryColor = Color.gray;
}
