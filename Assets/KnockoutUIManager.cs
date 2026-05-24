using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class KnockoutUIManager : MonoBehaviour
{
    public static KnockoutUIManager Instance { get; private set; }

    [Header("四大核心狀態面板 (CanvasGroup)")]
    public CanvasGroup introPanel;
    public CanvasGroup hudPanel;
    public CanvasGroup roundEndPanel;
    public CanvasGroup victoryPanel;
    public CanvasGroup highlightsPanel;

    [Header("首局展示卡 (Showcase Slots - 3v3)")]
    // 藍隊三個重生點對應的展示卡槽 (Slot 0, 1, 2)
    public Image[] blueShowcasePortraits;
    public TextMeshProUGUI[] blueShowcasePlayerNames;
    public TextMeshProUGUI[] blueShowcaseCharacterNames;

    // 紅隊三個重生點對應的展示卡槽 (Slot 0, 1, 2)
    public Image[] redShowcasePortraits;
    public TextMeshProUGUI[] redShowcasePlayerNames;
    public TextMeshProUGUI[] redShowcaseCharacterNames;

    [Header("比分燈號組件 (獨立置於 Canvas 下)")]
    public ScoreBoardUI scoreBoard;
    private CanvasGroup scoreBoardCG;

    [Header("狀態與倒數文字")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI roundStartTitleText;
    public TextMeshProUGUI roundEndStatusText;   // 此文字會被程式碼動態覆寫

    [Header("🌟 開戰藝術字圖片動畫 (Brawl Stars Style)")]
    [Tooltip("指派大大的「開戰」藝術字 Image 物件")]
    public Image battleStartImage;
    [Tooltip("放大動畫所需時間")]
    public float scaleDuration = 0.25f;
    [Tooltip("藝術字在中央停留展示時間")]
    public float battleShowDuration = 1.0f;

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
    }

    void Start()
    {
        ShowPanelImmediate(introPanel);
        HidePanelImmediate(hudPanel);
        HidePanelImmediate(roundEndPanel);
        HidePanelImmediate(victoryPanel);
        HidePanelImmediate(highlightsPanel);

        // 確保開場時開戰藝術字是隱藏的
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
                StartCoroutine(FadePanel(introPanel, 1f, 0.2f));
                StartCoroutine(FadePanel(hudPanel, 0f, 0.2f));
                StartCoroutine(FadePanel(roundEndPanel, 0f, 0.2f));
                if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 0f, 0.2f));
                StartCoroutine(IntroCountdownRoutine());
                break;

            case KnockoutGameManager.MatchState.Playing:
                StartCoroutine(FadePanel(introPanel, 0f, 0.3f));
                StartCoroutine(FadePanel(hudPanel, 1f, 0.3f));
                if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 1f, 0.3f));

                // 狀態切換為 Playing 時，立刻播放「開戰」藝術字放大與淡出動畫
                if (battleStartCoroutine != null) StopCoroutine(battleStartCoroutine);
                battleStartCoroutine = StartCoroutine(PlayBattleStartAnimationRoutine());
                break;

            case KnockoutGameManager.MatchState.RoundEnd:
                StartCoroutine(FadePanel(hudPanel, 0f, 0.2f));
                StartCoroutine(FadePanel(roundEndPanel, 1f, 0.2f));
                if (scoreBoardCG != null) scoreBoardCG.alpha = 1f;
                break;

            case KnockoutGameManager.MatchState.MatchEnd:
                StartCoroutine(FadePanel(roundEndPanel, 0f, 0.2f));
                StartCoroutine(FadePanel(victoryPanel, 1f, 0.3f));
                if (scoreBoardCG != null) StartCoroutine(FadePanel(scoreBoardCG, 0f, 0.2f));

                // 比賽結束時，將懸浮文字更改為英文的 "MATCH OVER!"
                if (roundEndStatusText != null)
                {
                    roundEndStatusText.text = "MATCH OVER!";
                    roundEndStatusText.color = Color.white;
                }
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

                if (blueShowcaseCharacterNames[i] != null)
                    blueShowcaseCharacterNames[i].text = bluePlayers[i].characterName;
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

                if (redShowcaseCharacterNames[i] != null)
                    redShowcaseCharacterNames[i].text = redPlayers[i].characterName;
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
            // 展示卡底層文字設為英文
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

        // 根據最後一回合的勝者，自動決定要對玩家顯示什麼文字
        int lastRoundWinner = roundWinners[activeRound - 1];
        if (roundEndStatusText != null)
        {
            if (lastRoundWinner == 1) // 藍隊（玩家/友軍）獲勝
            {
                roundEndStatusText.text = "ROUND WON!";
                roundEndStatusText.color = Color.white; // 🌟 更改處：改為白色
            }
            else if (lastRoundWinner == 2) // 紅隊（對手）獲勝
            {
                roundEndStatusText.text = "ROUND LOST!";
                roundEndStatusText.color = Color.white; // 🌟 更改處：改為白色
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

        battleStartImage.gameObject.SetActive(true);
        RectTransform rect = battleStartImage.GetComponent<RectTransform>();
        CanvasGroup cg = battleStartImage.GetComponent<CanvasGroup>();
        if (cg == null) cg = battleStartImage.gameObject.AddComponent<CanvasGroup>();

        // 1. 初始化狀態：極小 (Scale = 0.1)、完全不透明 (Alpha = 1)
        rect.localScale = Vector3.one * 0.1f;
        cg.alpha = 1f;

        // 2. 由小放大的插值動畫 (0.1 放大到 1.15，再縮回 1.0，產生彈性震動感)
        float elapsed = 0f;
        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleDuration;

            // 使用 Lerp 計算縮放，最高點放大至 1.15 倍
            rect.localScale = Vector3.Lerp(Vector3.one * 0.1f, Vector3.one * 1.15f, t);
            yield return null;
        }
        rect.localScale = Vector3.one; // 恢復正常 1.0 倍大小

        // 3. 在中央停留展示
        yield return new WaitForSeconds(battleShowDuration);

        // 4. 平滑淡出 (Alpha 1 降到 0)
        elapsed = 0f;
        float fadeDuration = 0.25f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        battleStartImage.gameObject.SetActive(false);
    }

    public void OnVictoryContinueClicked()
    {
        StartCoroutine(FadePanel(victoryPanel, 0f, 0.2f));
        StartCoroutine(FadePanel(highlightsPanel, 1f, 0.3f));
    }

    public void OnHighlightsContinueClicked()
    {
        Debug.Log("載入大廳場景...");
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
        }
    }

    private void ShowPanelImmediate(CanvasGroup group)
    {
        if (group == null) return;
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
    }
}