using UnityEngine;

public class Bullet : MonoBehaviour
{
    private PlayerAttackHandler playerHandler;
    private int damage;
    private float chargeAmount;

    // 動態敵人標籤，依據發射者隊伍決定
    private string targetEnemyTag = "RedTeam";

    public void Init(PlayerAttackHandler player, float lifeTime, int initDamage, float initCharge)
    {
        playerHandler = player;
        damage = initDamage;
        chargeAmount = initCharge;

        // 根據發射者的隊伍，動態決定攻擊對手 (RedTeam 或 BlueTeam)
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
         
        // 🌟 核心修正 1：先向上尋找受擊者身上（或其父物件中）的 HealthSystem
        HealthSystem enemyHealth = other.GetComponentInParent<HealthSystem>();

        // 🌟 核心修正 2：如果找到了 HealthSystem，改為檢查該【扣血系統物件 (Root)】的 Tag
        if (enemyHealth != null)
        {
            if (enemyHealth.CompareTag(targetEnemyTag))
            {
                string attackerName = playerHandler != null ? playerHandler.gameObject.name : "對手";

                // 呼叫雙參數的 TakeDamage，完整支援擊殺資訊系統
                enemyHealth.TakeDamage(damage, attackerName);

                Debug.Log($"💥 硬幣擊中敵人！造成 {damage} 點傷害");

                if (playerHandler != null)
                {
                    playerHandler.AddUltCharge(chargeAmount);
                }

                Destroy(gameObject);
                return; // 擊中敵人後銷毀並跳出，避免執行下方的牆壁判定
            }
        }
        
        // 牆壁或障礙物判定
        if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }
}