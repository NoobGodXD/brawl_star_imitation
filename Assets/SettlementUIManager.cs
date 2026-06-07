using UnityEngine;

public class SettlementUIManager : MonoBehaviour
{
    public static SettlementUIManager Instance { get; private set; }

    [Header("戰績展示卡預製物 (直向卡片)")]
    public GameObject statsCardPrefab; 

    [Header("雙隊排版容器 (水平 Layout Group)")]
    public Transform blueTeamContainer; // 藍隊 3 人水平排列
    public Transform redTeamContainer;  // 紅隊 3 人水平排列

    [Header("場景切換設定")]
    [Tooltip("按下退出按鈕時，要跳轉返回的場景名稱")]
    public string exitSceneName = "Mainstage"; // 🌟 預設為你的主要舞台場景 "Mainstage"

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        PopulateSettlementUI();
    }

    private void PopulateSettlementUI()
    {
        // 清空舊物件防止重複渲染
        foreach (Transform child in blueTeamContainer) Destroy(child.gameObject);
        foreach (Transform child in redTeamContainer) Destroy(child.gameObject);

        // 1. 生成藍隊（左側）戰績卡片
        bool isBlueWinner = MatchResultData.IsBlueTeamWinner;
        foreach (var stat in MatchResultData.BlueTeamStats)
        {
            GameObject card = Instantiate(statsCardPrefab, blueTeamContainer);
            PlayerStatsCardUI cardUI = card.GetComponent<PlayerStatsCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(stat, isBlueWinner);
            }
        }

        // 2. 生成紅隊（右側）戰績卡片
        bool isRedWinner = !MatchResultData.IsBlueTeamWinner;
        foreach (var stat in MatchResultData.RedTeamStats)
        {
            GameObject card = Instantiate(statsCardPrefab, redTeamContainer);
            PlayerStatsCardUI cardUI = card.GetComponent<PlayerStatsCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(stat, isRedWinner);
            }
        }
    }

    // 🌟 修正：按下 ExitButton 時，自動跳轉載入名為 exitSceneName (預設為 Mainstage) 的場景
    public void ExitToLobby()
    {
        Debug.Log($"正在返回主要舞台場景: {exitSceneName}...");
        UnityEngine.SceneManagement.SceneManager.LoadScene(exitSceneName); 
    }
}