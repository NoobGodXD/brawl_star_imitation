using UnityEngine;

/// <summary>
/// 敵人專用控制層 - 完美結合 WeaponFireBase.Fire() 規範
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAIController : MonoBehaviour
{
    [Header("【目標與陣營設定】")]
    public string targetTag = "BlueTeam";

    [Header("【資料層與邏輯層綁定】")]
    [Tooltip("請拖入該對手使用的武器資料 (WeaponData)")]
    public WeaponData enemyWeaponData; 
    
    [Tooltip("請拖入攻擊邏輯 Prefab (例如：Griff_Main_Logic)")]
    public GameObject attackLogicPrefab;
    
    [Tooltip("對手的子彈發射點物件 (FirePoint)")]
    public Transform firePoint;

    [Header("【AI 行為數值】")]
    public float moveSpeed = 3f;
    public float attackRange = 5f;
    public float attackCooldown = 1.5f;

    private Transform targetPlayer;
    private float nextAttackTime;
    private Rigidbody rb;

    // 假大腦，用來作為呼叫 Fire() 時的第一個參數
    private PlayerAttackHandler dummyController;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // 嘗試獲取假大腦（如果身上沒掛，程式會自動補上一個避免報錯）
        dummyController = GetComponent<PlayerAttackHandler>();
        if (dummyController == null)
        {
            dummyController = gameObject.AddComponent<PlayerAttackHandler>();
        }

        FindTarget();
    }

    private void Update()
    {
        if (targetPlayer == null) FindTarget();
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null) 
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0; 

        // 轉向主角
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * 12f));
        }

        // 決策：移動或攻擊
        if (distanceToPlayer > attackRange)
        {
            Vector3 moveVelocity = direction * moveSpeed;
            moveVelocity.y = rb.linearVelocity.y; 
            rb.linearVelocity = moveVelocity;
        }
        else
        {
            StopMovement();

            if (Time.time >= nextAttackTime)
            {
                ExecuteAttack(direction);
            }
        }
    }

    private void StopMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    private void FindTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
        }
    }

    private void ExecuteAttack(Vector3 shootDirection)
    {
        if (attackLogicPrefab == null || firePoint == null || enemyWeaponData == null) return;

        nextAttackTime = Time.time + attackCooldown;

        // 1. 生成發射邏輯物件
        GameObject logicObj = Instantiate(attackLogicPrefab, firePoint.position, firePoint.rotation);
        
        // 2. 獲取 WeaponFireBase 進行發射
        var fireLogic = logicObj.GetComponent<WeaponFireBase>();
        if (fireLogic != null)
        {
            // 完美對接！完全按照你的 Fire 函數規範填入 5 個參數：
            // (1) owner: 大腦參考
            // (2) origin: 發射點位置
            // (3) direction: 射擊方向 (朝向主角)
            // (4) data: 武器資料
            // (5) gadgetBuff: 對手預設為 false 
            fireLogic.Fire(dummyController, firePoint.position, shootDirection, enemyWeaponData, false);
        }
    }
}