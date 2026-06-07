using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class KnockoutUIManager : MonoBehaviour
{
    public static KnockoutUIManager Instance { get; private set; }

    public static bool IsBattleStarting { get; private set; } = false;

    [Header("三大核心狀態面板 (CanvasGroup)")] 
    public CanvasGroup introPanel;
    public CanvasGroup hudPanel;
    public CanvasGroup roundEndPanel;

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

        HidePanelImmediate(hudPanel);
        HidePanelImmediate(roundEndPanel);
    }

    void Start()
    {
        ShowPanelImmediate(introPanel);

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
                StartCoroutine(FadePanel(hudPanel, 0f, 0.2f));
                StartCoroutine(FadePanel(roundEndPanel, 0f, 0.2f));
                if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 0f, 0.2f));

                int currentRound = KnockoutGameManager.Instance != null ? KnockoutGameManager.Instance.CurrentRound : 1;

                if (roundStartTitleText != null)
                {
                    roundStartTitleText.gameObject.SetActive(false);
                }

                // 🌟 核心修正：將 introPanel 的淡入淡出完全收入 currentRound 判斷中！
                if (currentRound == 1)
                {
                    // 只有在第一局倒數時才淡入展示卡
                    StartCoroutine(FadePanel(introPanel, 1f, 0.2f)); 
                    if (countdownText != null)
                    {
                        countdownText.gameObject.SetActive(true);
                    }
                    StartCoroutine(IntroCountdownRoutine());
                }
                else
                {
                    // 🌟 第二局以上，直接徹底關閉展示卡，不論它在編輯器中是否被開啟，都強制將其 Active 設為 false！
                    HidePanelImmediate(introPanel);
                    if (countdownText != null)
                    {
                        countdownText.text = "";
                        countdownText.gameObject.SetActive(false);
                    }
                }
                break;

            case KnockoutGameManager.MatchState.Playing:
                HidePanelImmediate(introPanel); 
                
                StartCoroutine(FadePanel(hudPanel, 1f, 0.3f));
                if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 1f, 0.3f));
                
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
                StartCoroutine(FadePanel(roundEndPanel, 1f, 0.2f));
                break;
        }
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

    // 🌟 修正：大亂鬥風格雙頭像擊殺廣播
    public void SpawnKillFeed(bool isBlueVictim, string killer, string victim)
    {
        if (killFeedPool.Count == 0) return;
        GameObject item = killFeedPool.Dequeue();
        item.SetActive(true);
        item.transform.SetAsLastSibling();

        KillFeedItem feedScript = item.GetComponent<KillFeedItem>();
        if (feedScript != null)
        {
            // 🌟 修正：動態在場景中尋找擊殺者與被擊殺者的頭像 Sprite
            HealthSystem killerHealth = FindPlayerByName(killer);
            HealthSystem victimHealth = FindPlayerByName(victim);
            Sprite killerPortrait = killerHealth != null ? killerHealth.characterPortrait : null;
            Sprite victimPortrait = victimHealth != null ? victimHealth.characterPortrait : null;

            // 呼叫升級版 Setup 進行對齊與顯示
            feedScript.Setup(isBlueVictim, killer, killerPortrait, victim, victimPortrait, this);
        }
    }

    // 🌟 修正：輔助尋找函數，利用玩家名字在場景中定位其 HealthSystem
    private HealthSystem FindPlayerByName(string name)
    {
        HealthSystem[] all = FindObjectsByType<HealthSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var p in all)
        {
            if (p != null && p.playerName == name)
            {
                return p;
            }
        }
        return null;
    }

    public void ReturnToPool(GameObject item)
    {
        item.SetActive(false);
        killFeedPool.Enqueue(item);
    }
}