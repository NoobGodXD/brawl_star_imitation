using UnityEngine;

public class Bullet : MonoBehaviour
{
    // ==========================================
    // 🌟 核心記憶體 (全數設為 private，保持 Inspector 乾淨)
    // ==========================================
    private PlayerAttackHandler playerHandler; // 記住是誰發射了這顆子彈
    private int damage;                        // 記住自己要扣敵人多少血
    private float chargeAmount;                // 記住自己打中人可以回多少大招能量


    // ==========================================
    // 🌟 初始化 (由發射器在子彈生成的瞬間呼叫)
    // ==========================================
    public void Init(PlayerAttackHandler player, float lifeTime, int initDamage, float initCharge)
    {
        // 將外部傳來的數值，存進自己的私有記憶體中
        playerHandler = player;
        damage = initDamage;
        chargeAmount = initCharge;

        // 保險機制：設定子彈的壽命，時間到自動銷毀，避免飛到無限遠的宇宙吃效能
        Destroy(gameObject, lifeTime);
    }


    // ==========================================
    // 🌟 碰撞與傷害結算
    // ==========================================
    void OnTriggerEnter(Collider other)
    {
        // 🎯 情況一：撞到敵人
        if (other.CompareTag("Enemy"))
        {
            // 💥 1. 造成傷害 (雙重保險尋找血量系統)
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth == null)
            {
                enemyHealth = other.GetComponentInParent<HealthSystem>();
            }

            if (enemyHealth != null)
            {
                // 把存好的傷害值灌進去！
                enemyHealth.TakeDamage(damage);
                Debug.Log($"💥 硬幣擊中敵人！造成 {damage} 點傷害");
            }

            // 🔋 2. 增加大招能量
            if (playerHandler != null)
            {
                // 把存好的充能值回傳給玩家大腦！
                playerHandler.AddUltCharge(chargeAmount);
            }

            // ❌ 3. 普攻硬幣不會穿透，打中敵人就立刻銷毀自己
            Destroy(gameObject);
        }
        // 🧱 情況二：撞到牆壁或障礙物 (假設你有設定 Wall 標籤)
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            // 撞到牆壁直接碎裂消失
            Destroy(gameObject);
        }
    }
}