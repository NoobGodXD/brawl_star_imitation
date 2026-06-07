using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class KillFeedItem : MonoBehaviour
{
    [Header("大亂鬥風格雙頭像排版")]
    public Image backgroundImage;        // 橫條背景
    
    [Header("擊殺者 (Killer)")]
    public Image killerPortraitImage;    // 擊殺者頭像 Image
    public TextMeshProUGUI killerNameText;// 擊殺者名稱

    [Header("擊殺標誌")]
    public Image weaponIconImage;        // 擊殺圖示 (如小槍/雙劍圖案)

    [Header("被擊殺者 (Victim)")]
    public Image victimPortraitImage;    // 被擊殺者頭像 Image
    public TextMeshProUGUI victimNameText;// 被擊殺者名稱
    public GameObject skullIcon;         // 被擊殺者頭像右上角的「小骷髏頭」物件

    [Header("顏色與時間參數")]
    public Color allyKillBgColor = new Color(0.12f, 0.45f, 0.9f, 0.85f);  // 我方擊敗對手 (藍底)
    public Color enemyKillBgColor = new Color(0.9f, 0.12f, 0.12f, 0.85f); // 敵方擊敗我方 (紅底)
    public float showDuration = 3f;      // 停留時間

    private KnockoutUIManager manager;

    // 🌟 升級版初始化：接收頭像 Sprite，動態渲染排版
    public void Setup(bool isBlueVictim, string killerName, Sprite killerPortrait, string victimName, Sprite victimPortrait, KnockoutUIManager uiManager)
    {
        this.manager = uiManager;

        // 1. 設定文字
        if (killerNameText != null) killerNameText.text = killerName;
        if (victimNameText != null) victimNameText.text = victimName;

        // 2. 設定雙方頭像
        if (killerPortraitImage != null)
        {
            killerPortraitImage.sprite = killerPortrait;
            killerPortraitImage.gameObject.SetActive(killerPortrait != null);
        }

        if (victimPortraitImage != null)
        {
            victimPortraitImage.sprite = victimPortrait;
            victimPortraitImage.gameObject.SetActive(victimPortrait != null);
        }

        // 3. 顯示小骷髏頭
        if (skullIcon != null)
        {
            skullIcon.SetActive(true);
        }

        // 4. 根據是誰死亡，自動調整底圖顏色
        // 藍隊隊員死亡為紅底（Enemy Kill），紅隊隊員死亡為藍底（Ally Kill）
        if (backgroundImage != null)
        {
            backgroundImage.color = isBlueVictim ? enemyKillBgColor : allyKillBgColor;
        }

        // 5. 啟動自動回收協程
        StopAllCoroutines();
        StartCoroutine(AutoDestroyRoutine());
    }

    private IEnumerator AutoDestroyRoutine()
    {
        yield return new WaitForSeconds(showDuration);
        if (manager != null)
        {
            manager.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}