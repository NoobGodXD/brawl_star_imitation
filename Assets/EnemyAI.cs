using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform player;      // 拖入玩家物件
    public float attackRange = 1.5f; // 攻擊距離（建議比導航半徑稍微大一點點）
    public float attackCooldown = 1.0f; // 攻擊冷卻（秒）
    public int damageAmount = 10;    // 每次攻擊傷害量
    public string enemyName = "邪惡球體"; // 對手的名字，會顯示在擊殺訊息

    private NavMeshAgent agent;
    private float nextAttackTime = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (player == null) return;

        // 計算距離
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            // 距離太遠：繼續追蹤
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            // 距離夠近：停止移動並嘗試攻擊
            agent.isStopped = true;

            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    void AttackPlayer()
    {
        // 🌟 關鍵步驟：獲取玩家身上的 HealthSystem 組件
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();

        if (playerHealth != null)
        {
            // 呼叫你寫好的 TakeDamage 函數
            playerHealth.TakeDamage(damageAmount, enemyName);
            Debug.Log("對手攻擊了玩家！扣除 " + damageAmount + " 滴血");
        }
        else
        {
            Debug.LogWarning("在玩家物件上找不到 HealthSystem 腳本！");
        }
    }
}