using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerEndGameStats
{
    public string playerName;
    public Sprite characterPortrait;
    public string characterName;
    public bool isBlueTeam;
    
    // 戰績數據
    public int kills;
    public int deaths;
    public float damageDealt;
    public bool isStarPlayer;
    
    // 點讚計數器 (初始為 0)
    public int kudosCount = 0;
}

public static class MatchResultData
{
    public static bool IsBlueTeamWinner = true;
    public static List<PlayerEndGameStats> BlueTeamStats = new List<PlayerEndGameStats>();
    public static List<PlayerEndGameStats> RedTeamStats = new List<PlayerEndGameStats>();

    public static void Clear()
    {
        BlueTeamStats.Clear();
        RedTeamStats.Clear();
    }
}