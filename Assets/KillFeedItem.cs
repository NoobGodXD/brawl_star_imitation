using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class KillFeedItem : MonoBehaviour
{
    public TextMeshProUGUI feedText;
    public Image backgroundImage;

    [Header("配色與動畫設定")]
    public Color allyKillColor = new Color(0.2f, 0.6f, 1f, 0.9f); // 藍色背景 (己方得手)
    public Color enemyKillColor = new Color(0.9f, 0.2f, 0.2f, 0.9f); // 紅色背景 (敵方得手)

    public float slideDuration = 0.25f;  // 滑動花費時間
    public float showDuration = 1.5f;     // 在中央停留時間 (1.5秒)

    private KnockoutUIManager uiManager;
    private RectTransform rectTransform;
    private Coroutine slideCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(bool isBlueVictim, string killer, string victim, KnockoutUIManager manager)
    {
        uiManager = manager;

        if (feedText != null)
        {
            feedText.text = $"<b>{killer}</b> 擊倒了 <b>{victim}</b>";
        }

        // 🌟 顏色判定：若是己方擊敗敵方（敵方死亡，即 isBlueVictim 為 false）顯示藍色
        if (backgroundImage != null)
        {
            backgroundImage.color = isBlueVictim ? enemyKillColor : allyKillColor;
        }

        // 啟動滑動與生命週期協程
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideAnimationRoutine());
    }

    private IEnumerator SlideAnimationRoutine()
    {
        if (rectTransform == null) yield break;

        // 1. 初始位置：螢幕左側外面 (X = -500f)
        Vector2 startPos = new Vector2(-500f, rectTransform.anchoredPosition.y);
        // 2. 目標位置：滑入位置 (X = 30f)
        Vector2 targetPos = new Vector2(30f, rectTransform.anchoredPosition.y);

        rectTransform.anchoredPosition = startPos;
        float elapsed = 0f;

        // 🌟 滑入動畫 (邊緣往中心移動)
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / slideDuration);
            yield return null;
        }
        rectTransform.anchoredPosition = targetPos;

        // 🌟 顯示並停留約 1.5 秒
        yield return new WaitForSeconds(showDuration);

        // 🌟 滑出動畫 (往左邊緣退回)
        elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(targetPos, startPos, elapsed / slideDuration);
            yield return null;
        }
        rectTransform.anchoredPosition = startPos;

        // 將自己放回物件池
        if (uiManager != null)
        {
            uiManager.ReturnToPool(gameObject);
        }
    }
}