using UnityEngine;
using UnityEngine.UI;
using TMPro; // 如果使用 TextMeshPro 顯示文字需要這行

public class HealthSystem : MonoBehaviour
{
    [Header("血量設定")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("陣營與 UI 綁定")]
    public bool isTeammate = true;             // 打勾代表隊友(綠)，不打勾代表敵人(紅)
    public Image healthFillBar;                // 血條的填充層 (Image Type: Filled)
    public TextMeshProUGUI healthText;         // 顯示數字的文字元件

    [Header("顏色設定")]
    public Color allyColor = new Color(0.2f, 0.8f, 0.2f, 1f); // 綠色
    public Color enemyColor = new Color(0.9f, 0.2f, 0.2f, 1f); // 紅色

    void Start()
    {
        currentHealth = maxHealth;
        
        // 根據陣營設定初始顏色
        if (healthFillBar != null)
        {
            healthFillBar.color = isTeammate ? allyColor : enemyColor;
        }

        UpdateHealthUI();
    }

    // 受到傷害時呼叫這個函數
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        // 更新血條長度
        if (healthFillBar != null)
        {
            healthFillBar.fillAmount = (float)currentHealth / maxHealth;
        }

        // 更新數字顯示
        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + " 死亡！");
        // 這裡可以加入死亡動畫或銷毀物件
        // Destroy(gameObject);
    }
}
