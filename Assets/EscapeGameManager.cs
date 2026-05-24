using UnityEngine;
using System.Collections;
using System;

public class KnockoutGameManager : MonoBehaviour
{
    // 單例模式，方便其他控制腳本與 UI 隨時讀取狀態
    public static KnockoutGameManager Instance { get; private set; }

    public enum MatchState { Intro, Playing, RoundEnd, MatchEnd }

    [Header("狀態機監控")]
    [SerializeField] private MatchState currentState;
    public MatchState CurrentState => currentState;

    // 🌟 狀態改變事件，供 UIManager 進行解耦
    public event Action<MatchState> OnStateChanged;

    // 🌟 3v3 展示卡順序事件，傳遞重生點 0, 1, 2 對應的玩家給 UI
    public event Action<HealthSystem[], HealthSystem[]> OnShowcaseUpdated;

    // 🌟 比分圓點更新事件：傳遞 3 回合勝負狀態 (int[]) 與 當前局數 (int)
    public event Action<int[], int> OnScoreUpdated;

    [Header("單局設定 (Data-Driven)")]
    public int winsNeeded = 2;
    [SerializeField] private int currentRound = 1;
    [SerializeField] private int blueTeamWins = 0;
    [SerializeField] private int redTeamWins = 0;

    // 🌟 記錄 3 局比分燈號 (0:未打, 1:藍勝, 2:紅勝)
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
        OnStateChanged?.Invoke(currentState); // 廣播 Intro 狀態

        ResetPlayersAndApplyPhysicsSetup(); // 重置位置、排序、復活並重置彈藥
        SetPlayerInputLock(true); // 鎖定操作並清除物理慣性

        if (currentRound == 1)
        {
            Debug.Log("第一局：開場展示中 (5 秒)");
            yield return new WaitForSeconds(5f);
        }
        else
        {
            Debug.Log($"第 {currentRound} 局：準備時間 (1.5 秒)");
            yield return new WaitForSeconds(1.5f);
        }
    }

    private IEnumerator RoundPlayingRoutine()
    {
        currentState = MatchState.Playing;
        OnStateChanged?.Invoke(currentState); // 廣播 Playing 狀態

        SetPlayerInputLock(false); // 解除鎖定
        Debug.Log("戰鬥開始！");

        RecalculateAlivePlayers();

        while (blueTeamAlive > 0 && redTeamAlive > 0)
        {
            yield return null;
        }
    }

    private IEnumerator RoundEndRoutine()
    {
        currentState = MatchState.RoundEnd;
        OnStateChanged?.Invoke(currentState); // 廣播 RoundEnd 狀態

        SetPlayerInputLock(true);

        int winnerOfThisRound = 0; // 0:平手, 1:藍隊勝, 2:紅隊勝
        if (redTeamAlive <= 0 && blueTeamAlive > 0)
        {
            blueTeamWins++;
            winnerOfThisRound = 1;
            Debug.Log("本局結束：藍隊獲勝！");
        }
        else if (blueTeamAlive <= 0 && redTeamAlive > 0)
        {
            redTeamWins++;
            winnerOfThisRound = 2;
            Debug.Log("本局結束：紅隊獲勝！");
        }
        else
        {
            Debug.Log("本局結束：平手");
        }

        // 記錄目前局數的結果
        if (currentRound - 1 < 3)
        {
            roundWinners[currentRound - 1] = winnerOfThisRound;
        }

        // 發送 3 局狀態與當前局數
        OnScoreUpdated?.Invoke(roundWinners, currentRound);

        yield return new WaitForSeconds(3f);

        if (blueTeamWins < winsNeeded && redTeamWins < winsNeeded)
        {
            currentRound++;
        }
    }

    private IEnumerator MatchEndRoutine()
    {
        currentState = MatchState.MatchEnd;
        OnStateChanged?.Invoke(currentState); // 廣播 MatchEnd 狀態

        Debug.Log("比賽結束！");
        OnMatchEnd?.Invoke();
        yield break;
    }

    public void OnPlayerDied(bool isBlueTeam, string killerName, string victimName)
    {
        if (currentState != MatchState.Playing) return;

        Debug.Log($"【擊殺資訊】{killerName} 擊敗了 {victimName}");
        RecalculateAlivePlayers();
    }

    private void RecalculateAlivePlayers()
    {
        // 尋找場景中包含隱藏物件在內的所有玩家
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
        HealthSystem[] allPlayers = FindObjectsByType<HealthSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        HealthSystem[] orderedBlue = new HealthSystem[3];
        HealthSystem[] orderedRed = new HealthSystem[3];

        int blueIndex = 0;
        int redIndex = 0;

        foreach (var player in allPlayers)
        {
            player.ResetHealth(); // 復活、重置血量、顯示視覺、清空彈藥

            if (player.isBlueTeam)
            {
                if (blueIndex < blueTeamSpawns.Length)
                {
                    player.transform.position = blueTeamSpawns[blueIndex].position;
                    player.transform.rotation = blueTeamSpawns[blueIndex].rotation;
                    orderedBlue[blueIndex] = player; // 儲存重生點 0, 1, 2 順序的玩家
                    blueIndex++;
                }
            }
            else
            {
                if (redIndex < redTeamSpawns.Length)
                {
                    player.transform.position = redTeamSpawns[redIndex].position;
                    player.transform.rotation = redTeamSpawns[redIndex].rotation;
                    orderedRed[redIndex] = player; // 儲存重生點 0, 1, 2 順序的玩家
                    redIndex++;
                }
            }
        }

        // 廣播最新排序給動態展示卡
        OnShowcaseUpdated?.Invoke(orderedBlue, orderedRed);

        // 復活重置後，立刻更新一次比分圓圈縮放，確保開場即完成燈號定位
        OnScoreUpdated?.Invoke(roundWinners, currentRound);

        RecalculateAlivePlayers();
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
                rb.linearVelocity = Vector3.zero; // Unity 6.3 LTS 推薦 API
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