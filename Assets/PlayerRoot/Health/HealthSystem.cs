using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    [Header("血量設定")]
    public int maxHealth = 100;

    // 保持為 public 供 CharacterLoader 與 EnemyAIController 正常讀寫
    public int currentHealth;

    public string playerName = "Player";
    public bool IsDead { get; private set; }

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

    [Header("結算數據統計")]
    public int currentKills;         
    public int currentDeaths;        
    public float totalDamageDealt;   

    private KnockoutGameManager gameManager;

    void Awake()
    {
        ApplyTeamPhysicsSettings();
    }

    void Start()
    {
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

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, "未知的傷害來源");
    }

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

        if (KnockoutUIManager.Instance != null)
        {
            KnockoutUIManager.Instance.SpawnKillFeed(isBlueTeam, killerName, playerName);
        }

        // 🌟 如果死掉的是 AI，立刻關閉大腦
        EnemyAIController ai = GetComponent<EnemyAIController>();
        if (ai != null) ai.canAct = false;

        // 🌟 拔除陣營 Tag，防止敵方 AI 對屍體鞭屍
        gameObject.tag = "Untagged";

        DisableCharacterPhysicsAndVisuals();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        IsDead = false;

        // 🌟 重生時，重新貼回對應的陣營 Tag 供對手鎖定
        gameObject.tag = isBlueTeam ? "BlueTeam" : "RedTeam";

        UpdateHealthUI();
        EnableCharacterPhysicsAndVisuals();

        PlayerAttackHandler attackHandler = GetComponent<PlayerAttackHandler>();
        if (attackHandler != null)
        {
            attackHandler.ResetAmmo();
        }

        // 🌟 如果是 AI 角色，重生時重新啟動大腦
        EnemyAIController ai = GetComponent<EnemyAIController>();
        if (ai != null) ai.canAct = true;
    }

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