using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class KnockoutGameManager : MonoBehaviour
{
    // 單例模式，方便其他控制腳本與 UI 隨時讀取狀態
    public static KnockoutGameManager Instance { get; private set; }

    public enum MatchState { Intro, Playing, RoundEnd, MatchEnd }

    [Header("狀態機監控")]
    [SerializeField] private MatchState currentState;
    public MatchState CurrentState => currentState;

    // 狀態改變事件，供 UIManager 進行解耦
    public event Action<MatchState> OnStateChanged;

    // 3v3 展示卡順序事件，傳遞重生點 0, 1, 2 對應的玩家給 UI
    public event Action<HealthSystem[], HealthSystem[]> OnShowcaseUpdated;

    // 比分圓點更新事件：傳遞 3 回合勝負狀態 (int[]) 與 當前局數 (int)
    public event Action<int[], int> OnScoreUpdated;

    [Header("單局設定 (Data-Driven)")]
    public int winsNeeded = 2;
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int blueTeamWins = 0;
    [SerializeField] private int redTeamWins = 0;

    // 公開屬性：讓 UI 管理器能 100% 精準讀取當前回合
    public int CurrentRound => currentRound;

    // 記錄 3 局比分燈號 (0:未打, 1:藍勝, 2:紅勝)
    private int[] roundWinners = new int[3];

    [Header("重生點配置")]
    public Transform[] blueTeamSpawns;
    public Transform[] redTeamSpawns;

    private int blueTeamAlive;
    private int redTeamAlive;

    public event Action OnMatchEnd;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(MatchFlowRoutine());
    }

    // 僅在 Unity 編輯器測試時生效的快捷測試功能
#if UNITY_EDITOR
    void Update()
    {
        // 🌟 修正：改用鍵盤 "T" 鍵 (Test)，避開 Unity 編輯器內建的 F10 暫停快捷鍵
        if (Input.GetKeyDown(KeyCode.T))
        {
            TriggerMockMatchEnd();
        }
    }

    private void TriggerMockMatchEnd()
    {
        Debug.Log("🎯【測試啟動】正在生成模擬戰績數據並立刻加載結算場景...");

        MatchResultData.Clear();
        MatchResultData.IsBlueTeamWinner = true; // 模擬玩家（藍隊）獲勝

        // 模擬藍隊數據 (3 人)
        for (int i = 0; i < 3; i++)
        {
            MatchResultData.BlueTeamStats.Add(new PlayerEndGameStats
            {
                playerName = i == 0 ? "Player (你)" : $"Ally_Bot_{i}",
                characterPortrait = GetMockPortrait(i),
                characterName = "Griff",
                isBlueTeam = true,
                kills = UnityEngine.Random.Range(3, 9),
                deaths = UnityEngine.Random.Range(0, 2),
                damageDealt = UnityEngine.Random.Range(18000, 52000),
                isStarPlayer = (i == 0) // 模擬你是榮譽玩家
            });
        }

        // 模擬紅隊數據 (3 人)
        for (int i = 0; i < 3; i++)
        {
            MatchResultData.RedTeamStats.Add(new PlayerEndGameStats
            {
                playerName = $"Enemy_Bot_{i}",
                characterPortrait = GetMockPortrait(i + 3),
                characterName = "Enemy",
                isBlueTeam = false,
                kills = UnityEngine.Random.Range(0, 3),
                deaths = UnityEngine.Random.Range(3, 8),
                damageDealt = UnityEngine.Random.Range(4000, 15000),
                isStarPlayer = false
            });
        }

        // 跳轉到結算場景
        UnityEngine.SceneManagement.SceneManager.LoadScene("SettlementScene");
    }

    private Sprite GetMockPortrait(int index)
    {
        HealthSystem[] all = FindObjectsByType<HealthSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (all != null && all.Length > 0)
        {
            int safeIndex = index % all.Length;
            return all[safeIndex].characterPortrait;
        }
        return null;
    }
#endif

    private IEnumerator MatchFlowRoutine()
    {
        while (blueTeamWins < winsNeeded && redTeamWins < winsNeeded)
        {
            yield return StartCoroutine(RoundIntroRoutine());
            yield return StartCoroutine(RoundPlayingRoutine());
            yield return StartCoroutine(RoundEndRoutine());
        }

        yield return StartCoroutine(MatchEndRoutine());
    }

    private IEnumerator RoundIntroRoutine()
    {
        currentState = MatchState.Intro;
        OnStateChanged?.Invoke(currentState);

        ResetPlayersAndApplyPhysicsSetup(); 
        SetPlayerInputLock(true); 

        if (currentRound == 1)
        {
            yield return new WaitForSeconds(5f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }
    }

    private IEnumerator RoundPlayingRoutine()
    {
        currentState = MatchState.Playing;
        OnStateChanged?.Invoke(currentState);

        SetPlayerInputLock(false); 
        RecalculateAlivePlayers();

        while (blueTeamAlive > 0 && redTeamAlive > 0)
        {
            yield return null;
        }
    }

    private IEnumerator RoundEndRoutine()
    {
        currentState = MatchState.RoundEnd;
        OnStateChanged?.Invoke(currentState);

        SetPlayerInputLock(true);

        int winnerOfThisRound = 0; 
        if (redTeamAlive <= 0 && blueTeamAlive > 0)
        {
            blueTeamWins++;
            winnerOfThisRound = 1;
        }
        else if (blueTeamAlive <= 0 && redTeamAlive > 0)
        {
            redTeamWins++;
            winnerOfThisRound = 2;
        }

        if (currentRound - 1 < 3)
        {
            roundWinners[currentRound - 1] = winnerOfThisRound;
        }

        OnScoreUpdated?.Invoke(roundWinners, currentRound);

        yield return new WaitForSeconds(3f);

        if (blueTeamWins < winsNeeded && redTeamWins < winsNeeded)
        {
            currentRound++;
        }
    }

    // 打包戰績到 MatchResultData，並於 1.5 秒後加載結算場景
    private IEnumerator MatchEndRoutine()
    {
        currentState = MatchState.MatchEnd;
        OnStateChanged?.Invoke(currentState); 

        Debug.Log("比賽結束，開始打包戰績並加載結算場景...");

        MatchResultData.Clear();
        MatchResultData.IsBlueTeamWinner = (blueTeamWins >= winsNeeded);

        HealthSystem[] allPlayers = FindObjectsByType<HealthSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        HealthSystem mvpPlayer = DetermineMVP(allPlayers);

        foreach (var player in allPlayers)
        {
            PlayerEndGameStats pStat = new PlayerEndGameStats
            {
                playerName = player.playerName,
                characterPortrait = player.characterPortrait,
                characterName = player.characterName,
                isBlueTeam = player.isBlueTeam,
                kills = player.currentKills,         
                deaths = player.currentDeaths,       
                damageDealt = player.totalDamageDealt, 
                isStarPlayer = (player == mvpPlayer)   
            };

            if (player.isBlueTeam) MatchResultData.BlueTeamStats.Add(pStat);
            else MatchResultData.RedTeamStats.Add(pStat);
        }

        OnMatchEnd?.Invoke();

        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SettlementScene"); 
    }

    private HealthSystem DetermineMVP(HealthSystem[] players)
    {
        HealthSystem bestPlayer = null;
        float highestScore = -9999f;

        foreach (var p in players)
        {
            float score = (p.currentKills * 10f) + (p.totalDamageDealt * 0.05f) - (p.currentDeaths * 5f);
            if (score > highestScore)
            {
                highestScore = score;
                bestPlayer = p;
            }
        }
        return bestPlayer;
    }

    public void OnPlayerDied(bool isBlueTeam, string killerName, string victimName)
    {
        if (currentState != MatchState.Playing) return;
        RecalculateAlivePlayers();
    }

    private void RecalculateAlivePlayers()
    {
        HealthSystem[] allPlayers = FindObjectsByType<HealthSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        int tempBlue = 0;
        int tempRed = 0;

        foreach (var player in allPlayers)
        {
            if (!player.IsDead)
            {
                if (player.isBlueTeam) tempBlue++;
                else tempRed++;
            }
        }

        blueTeamAlive = tempBlue;
        redTeamAlive = tempRed;
    }

    private void ResetPlayersAndApplyPhysicsSetup()
    {
        if (blueTeamSpawns == null || blueTeamSpawns.Length < 3)
        {
            Debug.LogError($"⚠️【出生點配置錯誤】藍隊重生點數量不足，請指派 3 個！");
        }
        if (redTeamSpawns == null || redTeamSpawns.Length < 3)
        {
            Debug.LogError($"⚠️【出生點配置錯誤】紅隊重生點數量不足，請指派 3 個！");
        }

        HealthSystem[] allPlayers = FindObjectsByType<HealthSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        HealthSystem[] orderedBlue = new HealthSystem[3];
        HealthSystem[] orderedRed = new HealthSystem[3];

        int blueIndex = 0;
        int redIndex = 0;

        foreach (var player in allPlayers)
        {
            player.ResetHealth(); 

            if (player.isBlueTeam)
            {
                if (blueIndex < blueTeamSpawns.Length)
                {
                    TeleportPlayer(player, blueTeamSpawns[blueIndex]);
                    orderedBlue[blueIndex] = player; 
                    blueIndex++;
                }
            }
            else
            {
                if (redIndex < redTeamSpawns.Length)
                {
                    TeleportPlayer(player, redTeamSpawns[redIndex]);
                    orderedRed[redIndex] = player; 
                    redIndex++;
                }
            }
        }

        OnShowcaseUpdated?.Invoke(orderedBlue, orderedRed);
        OnScoreUpdated?.Invoke(roundWinners, currentRound);
        RecalculateAlivePlayers();
    }

    private void TeleportPlayer(HealthSystem player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null) return;

        var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(spawnPoint.position); 
            player.transform.rotation = spawnPoint.rotation;
            return;
        }

        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false; 
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            controller.enabled = true;  
            return;
        }

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = spawnPoint.position; 
            rb.rotation = spawnPoint.rotation; 
            rb.linearVelocity = Vector3.zero;  
            rb.angularVelocity = Vector3.zero; 
            return;
        }

        var rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            rb2d.position = spawnPoint.position;
            rb2d.rotation = spawnPoint.rotation.eulerAngles.z;
            rb2d.linearVelocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
            return;
        }

        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;
    }

    private void SetPlayerInputLock(bool isLocked)
    {
        var inputs = FindObjectsByType<UnityEngine.InputSystem.PlayerInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var input in inputs)
        {
            if (isLocked) input.actions.Disable();
            else input.actions.Enable();
        }

        var rbs = FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rb in rbs)
        {
            if (isLocked)
            {
                rb.linearVelocity = Vector3.zero; 
                rb.angularVelocity = Vector3.zero;
            }
        }
        var rb2ds = FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var rb2d in rb2ds)
        {
            if (isLocked)
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
        }

        var agents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var agent in agents)
        {
            if (agent.isActiveAndEnabled)
            {
                agent.isStopped = isLocked;
            }
        }
    }
}