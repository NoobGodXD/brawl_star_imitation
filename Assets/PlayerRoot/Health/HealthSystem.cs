using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    [Header("血量設定")]
    public int maxHealth = 100;

    // 保持為 public 供 CharacterLoader 正常讀寫，解決編譯錯誤
    public int currentHealth;

    public string playerName = "Player";
    public bool IsDead { get; private set; }

    // 🌟 解決 CS1061 錯誤：新增開場展示所需的資料欄位
    [Header("角色展示卡設定 (Data-Driven)")]
    [Tooltip("英雄的類別名稱（例如：雪莉、柯爾特）")]
    public string characterName = "雪莉";
    [Tooltip("英雄的展示頭像圖片")]
    public Sprite characterPortrait;

    [Header("陣營與 UI 綁定")]
    public bool isBlueTeam = true;
    public Image healthFillBar;
    public TextMeshProUGUI healthText;

    [Header("顏色設定")]
    public Color blueTeamColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color redTeamColor = new Color(1f, 0.2f, 0.2f, 1f);

    private KnockoutGameManager gameManager;

    void Awake()
    {
        ApplyTeamPhysicsSettings();
    }

    void Start()
    {
        // 尋找場景中的管理器
        gameManager = FindFirstObjectByType<KnockoutGameManager>();
        ResetHealth();
    }

    private void ApplyTeamPhysicsSettings()
    {
        gameObject.tag = isBlueTeam ? "BlueTeam" : "RedTeam";
        string layerName = isBlueTeam ? "BlueTeamPlayer" : "RedTeamPlayer";
        int targetLayer = LayerMask.NameToLayer(layerName);
        if (targetLayer != -1) gameObject.layer = targetLayer;
    }

    // 多載 1：支援舊子彈或一般扣血調用 (解決舊編譯錯誤)
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, "未知的傷害來源");
    }

    // 多載 2：支援新子彈調用，附帶擊殺者姓名
    public void TakeDamage(int damage, string attackerName)
    {
        if (IsDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die(attackerName);
        }
    }

    private void UpdateHealthUI()
    {
        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = (float)currentHealth / maxHealth;
            healthFillBar.color = isBlueTeam ? blueTeamColor : redTeamColor;
        }

        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }

    private void Die(string killerName)
    {
        IsDead = true;
        Debug.Log(gameObject.name + " 死亡！ 擊殺者: " + killerName);

        if (gameManager != null)
        {
            gameManager.OnPlayerDied(isBlueTeam, killerName, playerName);
        }

        // 向全域 UIManager 送出通知，彈出擊殺提示
        if (KnockoutUIManager.Instance != null)
        {
            KnockoutUIManager.Instance.SpawnKillFeed(isBlueTeam, killerName, playerName);
        }

        DisableCharacterPhysicsAndVisuals();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;
        UpdateHealthUI();
        EnableCharacterPhysicsAndVisuals();

        // 🌟 新增：回合重置時，一併將子彈清空，讓玩家在起跑點重新累積
        PlayerAttackHandler attackHandler = GetComponent<PlayerAttackHandler>();
        if (attackHandler != null)
        {
            attackHandler.ResetAmmo();
        }
    }

    // 雙重防禦隱藏視覺：若無 Visual 節點，直接關閉所有 Renderer 確保一定消失
    private void DisableCharacterPhysicsAndVisuals()
    {
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null) collider2D.enabled = false;

        Transform visualNode = transform.Find("Visual");
        if (visualNode != null)
        {
            visualNode.gameObject.SetActive(false);
        }
        else
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;

            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in spriteRenderers) sr.enabled = false;
        }
    }

    private void EnableCharacterPhysicsAndVisuals()
    {
        var collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = true;
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null) collider2D.enabled = true;

        Transform visualNode = transform.Find("Visual");
        if (visualNode != null)
        {
            visualNode.gameObject.SetActive(true);
        }
        else
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = true;

            var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in spriteRenderers) sr.enabled = true;
        }
    }
}