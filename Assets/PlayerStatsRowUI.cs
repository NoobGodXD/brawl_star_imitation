using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsCardUI : MonoBehaviour
{
    [Header("基礎視覺")]
    public Image portraitImage;           // 角色立繪/大頭照
    public TextMeshProUGUI nameText;      // 玩家名稱 (如 NoobGod)
    public TextMeshProUGUI killsText;     // 擊殺數 (藍槍 icon 旁)
    public TextMeshProUGUI deathsText;    // 死亡數 (骷髏 icon 旁)
    public TextMeshProUGUI damageText;    // 總傷害量 (鑽石/數值旁)
    public TextMeshProUGUI achievementText;// 底部特殊榮譽 (如 "Most Takedowns", "Most Healing")
    public GameObject starPlayerRibbon;   // "STAR PLAYER" 金色絲帶/底座

    [Header("二級選單：點讚彈出面板 (🌟 你的核心需求)")]
    public Button cardTriggerButton;      // 點擊整張卡片或立繪，用來彈出點讚按鈕
    public GameObject kudosPopupPanel;    // 二級點讚彈出面板 (預設隱藏)
    public Button kudosConfirmButton;     // 彈出面板中的「大拇指確認按鈕」
    public TextMeshProUGUI kudosCountText;// 點讚數
    public Image kudosIcon;               // 點讚手勢
    public Sprite kudosNormalSprite;      // 普通灰色大拇指
    public Sprite kudosGlowSprite;        // 發光大拇指
    public GameObject fireEffect;         // 冒火特效

    [Header("對等卡片背景")]
    public Image cardBg;
    public Sprite blueBgSprite;
    public Sprite redBgSprite;

    private PlayerEndGameStats currentStats;

    public void SetupCard(PlayerEndGameStats stats, bool isWinner)
    {
        if (stats == null) return;
        currentStats = stats;

        if (portraitImage != null) portraitImage.sprite = stats.characterPortrait;
        if (nameText != null) nameText.text = stats.playerName;
        if (killsText != null) killsText.text = stats.kills.ToString();
        if (deathsText != null) deathsText.text = stats.deaths.ToString();
        if (damageText != null) damageText.text = stats.damageDealt.ToString("N0");

        // 特殊榮譽判定
        if (achievementText != null)
        {
            if (stats.isStarPlayer) achievementText.text = "Most Takedowns";
            else if (stats.damageDealt > 30000) achievementText.text = "Most Damage";
            else achievementText.text = "";
        }

        // 明星球員絲帶
        if (starPlayerRibbon != null) starPlayerRibbon.SetActive(stats.isStarPlayer);

        if (cardBg != null)
        {
            cardBg.sprite = stats.isBlueTeam ? blueBgSprite : redBgSprite;
        }

        // 🌟 預設隱藏二級點讚選單
        if (kudosPopupPanel != null) kudosPopupPanel.SetActive(false);

        // 註冊卡片點擊事件：點擊後彈出/關閉二級點讚選單
        if (cardTriggerButton != null)
        {
            cardTriggerButton.onClick.RemoveAllListeners();
            cardTriggerButton.onClick.AddListener(OnCardClicked);
        }

        // 註冊二級選單中的「大拇指點讚」事件
        if (kudosConfirmButton != null)
        {
            kudosConfirmButton.onClick.RemoveAllListeners();
            kudosConfirmButton.onClick.AddListener(OnKudosConfirmed);
        }

        UpdateKudosVisuals();
    }

    private void OnCardClicked()
    {
        // 點擊卡片時，反轉二級點讚面板的顯示狀態
        if (kudosPopupPanel != null)
        {
            kudosPopupPanel.SetActive(!kudosPopupPanel.activeSelf);
        }
    }

    private void OnKudosConfirmed()
    {
        // 點擊大拇指確認點讚
        currentStats.kudosCount++;
        UpdateKudosVisuals();

        // 點完讚後自動隱藏二級菜單，並禁用確認按鈕防止洗讚
        if (kudosPopupPanel != null) kudosPopupPanel.SetActive(false);
        if (kudosConfirmButton != null) kudosConfirmButton.interactable = false;
    }

    private void UpdateKudosVisuals()
    {
        if (currentStats == null) return;

        if (kudosCountText != null)
        {
            kudosCountText.text = currentStats.kudosCount > 0 ? currentStats.kudosCount.ToString() : "";
        }

        if (currentStats.kudosCount == 0)
        {
            if (kudosIcon != null) kudosIcon.sprite = kudosNormalSprite;
            if (fireEffect != null) fireEffect.SetActive(false);
        }
        else if (currentStats.kudosCount == 1)
        {
            if (kudosIcon != null) kudosIcon.sprite = kudosGlowSprite;
            if (fireEffect != null) fireEffect.SetActive(false);
        }
        else if (currentStats.kudosCount >= 2)
        {
            if (kudosIcon != null) kudosIcon.sprite = kudosGlowSprite;
            if (fireEffect != null) fireEffect.SetActive(true);
        }
    }
}