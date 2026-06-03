using UnityEngine;

public class Bullet : MonoBehaviour
{
    private PlayerAttackHandler playerHandler;
    private int damage;
    private float chargeAmount;

    // 🌟 新增：動態敵人標籤，依據發射者隊伍決定
    private string targetEnemyTag = "RedTeam";

    public void Init(PlayerAttackHandler player, float lifeTime, int initDamage, float initCharge)
    {
        playerHandler = player;
        damage = initDamage;
        chargeAmount = initCharge;

        // 🌟 新增：根據發射者的隊伍，動態決定攻擊對手 (RedTeam 或 BlueTeam)
        if (playerHandler != null)
        {
            HealthSystem shooterHealth = playerHandler.GetComponent<HealthSystem>();
            if (shooterHealth != null)
            {
                targetEnemyTag = shooterHealth.isBlueTeam ? "RedTeam" : "BlueTeam";
            }
        }

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // 🌟 修正：不使用寫死的 "Enemy"，改用動態的 targetEnemyTag
        if (other.CompareTag(targetEnemyTag))
        {
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth == null)
            {
                enemyHealth = other.GetComponentInParent<HealthSystem>();
            }

            if (enemyHealth != null)
            {
                string attackerName = playerHandler != null ? playerHandler.gameObject.name : "對手";

                // 呼叫雙參數的 TakeDamage，完整支援擊殺資訊系統
                enemyHealth.TakeDamage(damage, attackerName);

                Debug.Log($"💥 硬幣擊中敵人！造成 {damage} 點傷害");
            }

            if (playerHandler != null)
            {
                playerHandler.AddUltCharge(chargeAmount);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
    
}