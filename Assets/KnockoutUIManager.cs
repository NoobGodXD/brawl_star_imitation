using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class KnockoutUIManager : MonoBehaviour
{
    public static KnockoutUIManager Instance { get; private set; }

    // 靜態屬性，用來通知其他腳本此時是否正在播放開戰動畫（用來鎖定玩家移動/攻擊）
    public static bool IsBattleStarting { get; private set; } = false;

    [Header("四大核心狀態面板 (CanvasGroup)")]
    public CanvasGroup introPanel;
    public CanvasGroup hudPanel;
    public CanvasGroup roundEndPanel;
    public CanvasGroup victoryPanel;
    public CanvasGroup highlightsPanel;

    [Header("首局展示卡 (Showcase Slots - 3v3)")]
    public Image[] blueShowcasePortraits;
    public TextMeshProUGUI[] blueShowcasePlayerNames;

    public Image[] redShowcasePortraits;
    public TextMeshProUGUI[] redShowcasePlayerNames;

    [Header("比分燈號組件 (獨立置於 Canvas 下)")]
    public ScoreBoardUI scoreBoard;
    private CanvasGroup scoreBoardCG;

    [Header("狀態與倒數文字")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI roundStartTitleText;
    public TextMeshProUGUI roundEndStatusText;   

    [Header("🌟 開戰藝術字圖片動畫 (Brawl Stars Style)")]
    public Image battleStartImage;
    public float scaleDuration = 0.25f;
    [Tooltip("藝術字在中央停留展示時間（已修改為 0.5 秒左右）")]
    public float battleShowDuration = 0.5f; 

    [Header("擊殺提示 (Object Pooling)")]
    public Transform killFeedContainer;
    public GameObject killFeedPrefab;
    public int poolSize = 5;

    private Queue<GameObject> killFeedPool = new Queue<GameObject>();
    private Coroutine battleStartCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 🌟 修正：移至 Awake 立即隱藏，比 Start() 更早執行，完全防止遊戲啟動時第一影格閃現任何白色背景
        HidePanelImmediate(hudPanel);
        HidePanelImmediate(roundEndPanel);
        HidePanelImmediate(victoryPanel);
        HidePanelImmediate(highlightsPanel);
    }

    void Start()
    {
        // 確保第一幀就強制顯示開場展示面板
        ShowPanelImmediate(introPanel);

        // 初始化防禦：清空倒數與結束文字
        if (countdownText != null) countdownText.text = "";
        if (roundEndStatusText != null) roundEndStatusText.text = "";

        if (battleStartImage != null) battleStartImage.gameObject.SetActive(false);

        if (scoreBoard != null)
        {
            scoreBoardCG = scoreBoard.GetComponent<CanvasGroup>();
            if (scoreBoardCG == null)
            {
                scoreBoardCG = scoreBoard.gameObject.AddComponent<CanvasGroup>();
            }
            scoreBoardCG.alpha = 0f;
        }

        InitializeKillFeedPool();

        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnStateChanged += HandleStateChanged;
            KnockoutGameManager.Instance.OnScoreUpdated += UpdateScoreboard;
            KnockoutGameManager.Instance.OnShowcaseUpdated += UpdateShowcaseUI;

            HandleStateChanged(KnockoutGameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnStateChanged -= HandleStateChanged;
            KnockoutGameManager.Instance.OnScoreUpdated -= UpdateScoreboard;
            KnockoutGameManager.Instance.OnShowcaseUpdated -= UpdateShowcaseUI;
        }
    }

    private void HandleStateChanged(KnockoutGameManager.MatchState newState)
    {
        switch (newState)
        {
            case KnockoutGameManager.MatchState.Intro:
                // 獲取當前回合數
                int currentRound = GetCurrentRoundFromManager();

                // 🌟 修正：只有在第一局 (Round 1) 有倒數時，才顯示首局展示卡面板 (introPanel)！
                if (currentRound == 1)
                {
                    StartCoroutine(FadePanel(introPanel, 1f, 0.2f));
                    if (countdownText != null)
                    {
                        countdownText.gameObject.SetActive(true);
                    }
                    StartCoroutine(IntroCountdownRoutine());
                }
                else
                {
                    // 🌟 第二局以上（中間局與局之間），直接徹底隱藏展示卡與倒數文字！
                    HidePanelImmediate(introPanel);
                    if (countdownText != null)
                    {
                        countdownText.text = "";
                        countdownText.gameObject.SetActive(false);
                    }
                }

                StartCoroutine(FadePanel(hudPanel, 0f, 0.2f));
                StartCoroutine(FadePanel(roundEndPanel, 0f, 0.2f));
                if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 0f, 0.2f));

                // 完全關閉並隱藏「ROUND X」文字，不再顯示第幾次的 ROUND
                if (roundStartTitleText != null)
                {
                    roundStartTitleText.gameObject.SetActive(false);
                }
                break;

            case KnockoutGameManager.MatchState.Playing:
                        // 🌟 修正：倒數完 5 秒進入 Playing 的瞬間，立刻且徹底關閉介紹面板（0幀延遲），保證開戰藝術字彈出時畫面是乾淨的！
                        HidePanelImmediate(introPanel); 
                        
                        StartCoroutine(FadePanel(hudPanel, 1f, 0.3f));
                        if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 1f, 0.3f));
                        
                        // 徹底隱藏「ROUND X」與「倒數文字」物件
                        if (roundStartTitleText != null) roundStartTitleText.gameObject.SetActive(false);
                        if (countdownText != null) countdownText.gameObject.SetActive(false);

                        if (battleStartCoroutine != null) StopCoroutine(battleStartCoroutine);
                        battleStartCoroutine = StartCoroutine(PlayBattleStartAnimationRoutine());
                        break;

            case KnockoutGameManager.MatchState.RoundEnd:
                StartCoroutine(FadePanel(hudPanel, 0f, 0.2f));
                StartCoroutine(FadePanel(roundEndPanel, 1f, 0.2f));
                if (scoreBoardCG != null) scoreBoardCG.alpha = 1f;
                break;

            case KnockoutGameManager.MatchState.MatchEnd:
                if (roundEndStatusText != null)
                {
                    roundEndStatusText.text = "MATCH OVER!";
                    roundEndStatusText.color = Color.white;
                }
                StartCoroutine(MatchEndSequenceRoutine());
                break;
        }
    }

    // 讓 MATCH OVER! 停留 3 秒再切換勝利面板
    private IEnumerator MatchEndSequenceRoutine()
    {
        yield return new WaitForSeconds(3.0f);

        StartCoroutine(FadePanel(roundEndPanel, 0f, 0.2f));
        StartCoroutine(FadePanel(victoryPanel, 1f, 0.3f));
        if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 0f, 0.2f));
    }

    // 自動反射獲取當前回合數的防編譯報錯函數
    private int GetCurrentRoundFromManager()
    {
        if (KnockoutGameManager.Instance == null) return 1;
        try
        {
            System.Type type = KnockoutGameManager.Instance.GetType();
            
            var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (prop.Name.ToLower().Contains("round") && prop.PropertyType == typeof(int))
                {
                    return (int)prop.GetValue(KnockoutGameManager.Instance);
                }
            }

            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.Name.ToLower().Contains("round") && field.FieldType == typeof(int))
                {
                    return (int)field.GetValue(KnockoutGameManager.Instance);
                }
            }
        }
        catch {}
        return 1; // 預設回傳 1
    }

    private void UpdateShowcaseUI(HealthSystem[] bluePlayers, HealthSystem[] redPlayers)
    {
        for (int i = 0; i < blueShowcasePortraits.Length; i++)
        {
            if (i < bluePlayers.Length && bluePlayers[i] != null)
            {
                blueShowcasePortraits[i].gameObject.SetActive(true);
                blueShowcasePortraits[i].sprite = bluePlayers[i].characterPortrait;

                if (blueShowcasePlayerNames[i] != null)
                    blueShowcasePlayerNames[i].text = bluePlayers[i].playerName;
            }
            else
            {
                if (blueShowcasePortraits[i] != null)
                    blueShowcasePortraits[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < redShowcasePortraits.Length; i++)
        {
            if (i < redPlayers.Length && redPlayers[i] != null)
            {
                redShowcasePortraits[i].gameObject.SetActive(true);
                redShowcasePortraits[i].sprite = redPlayers[i].characterPortrait;

                if (redShowcasePlayerNames[i] != null)
                    redShowcasePlayerNames[i].text = redPlayers[i].playerName;
            }
            else
            {
                if (redShowcasePortraits[i] != null)
                    redShowcasePortraits[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator IntroCountdownRoutine()
    {
        if (countdownText == null) yield break;

        float timer = 5f;
        while (timer > 0)
        {
            countdownText.text = $"STARTING IN: {Mathf.CeilToInt(timer)}";
            yield return null;
            timer -= Time.deltaTime;
        }
        countdownText.text = "";
    }

    private void UpdateScoreboard(int[] roundWinners, int activeRound)
    {
        if (scoreBoard != null)
        {
            scoreBoard.UpdateScoreDots(roundWinners, activeRound);
        }

        int lastRoundWinner = roundWinners[activeRound - 1];
        if (roundEndStatusText != null)
        {
            if (lastRoundWinner == 1) 
            {
                roundEndStatusText.text = "ROUND WON!";
                roundEndStatusText.color = Color.white; 
            }
            else if (lastRoundWinner == 2) 
            {
                roundEndStatusText.text = "ROUND LOST!";
                roundEndStatusText.color = Color.white; 
            }
            else
            {
                roundEndStatusText.text = "DRAW!";
                roundEndStatusText.color = Color.white;
            }
        }
    }

    private IEnumerator PlayBattleStartAnimationRoutine()
    {
        if (battleStartImage == null) yield break;

        IsBattleStarting = true;

        battleStartImage.gameObject.SetActive(true);
        RectTransform rect = battleStartImage.GetComponent<RectTransform>();
        CanvasGroup cg = battleStartImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = battleStartImage.gameObject.AddComponent<CanvasGroup>();

        rect.localScale = Vector3.one * 0.1f;
        cg.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleDuration;
            rect.localScale = Vector3.Lerp(Vector3.one * 0.1f, Vector3.one * 1.15f, t);
            yield return null;
        }
        rect.localScale = Vector3.one; 

        yield return new WaitForSeconds(battleShowDuration);

        elapsed = 0f;
        float fadeDuration = 0.25f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        battleStartImage.gameObject.SetActive(false);

        IsBattleStarting = false;
    }

    public void OnVictoryContinueClicked()
    {
        StartCoroutine(FadePanel(victoryPanel, 0f, 0.2f));
        StartCoroutine(FadePanel(highlightsPanel, 1f, 0.3f));
    }

    public void OnHighlightsContinueClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void InitializeKillFeedPool()
    {
        if (killFeedPrefab == null || killFeedContainer == null) return;
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(killFeedPrefab, killFeedContainer);
            obj.SetActive(false);
            killFeedPool.Enqueue(obj);
        }
    }

    public void SpawnKillFeed(bool isBlueVictim, string killer, string victim)
    {
        if (killFeedPool.Count == 0) return;
        GameObject item = killFeedPool.Dequeue();
        item.SetActive(true);
        item.transform.SetAsLastSibling();

        KillFeedItem feedScript = item.GetComponent<KillFeedItem>();
        if (feedScript != null)
        {
            feedScript.Setup(isBlueVictim, killer, victim, this);
        }
    }

    public void ReturnToPool(GameObject item)
    {
        item.SetActive(false);
        killFeedPool.Enqueue(item);
    }

    private IEnumerator FadePanel(CanvasGroup group, float targetAlpha, float duration)
    {
        if (group == null) yield break;
        float startAlpha = group.alpha;
        float elapsed = 0f;

        if (targetAlpha > 0f)
        {
            group.gameObject.SetActive(true);
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            group.blocksRaycasts = false;
            group.interactable = false;
            group.gameObject.SetActive(false);
        }
    }

    private void ShowPanelImmediate(CanvasGroup group)
    {
        if (group == null) return;
        group.gameObject.SetActive(true); 
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;
    }

    private void HidePanelImmediate(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        group.gameObject.SetActive(false); 
    }
}