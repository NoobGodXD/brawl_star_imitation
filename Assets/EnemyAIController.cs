using UnityEngine;

/// <summary>
/// 敵人專用控制層 - 高難度智慧、獨立彈藥裝填與資料驅動版
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

    [Header("【AI 基本移動數值】")]
    public float moveSpeed = 3f;
    [Tooltip("設定兩次連發開火之間的最小物理冷卻時間")]
    public float attackCooldown = 0.5f;

    [Header("【AI 獨立彈藥與裝填系統】（僅修改本腳本實現）")]
    public int maxAmmo = 3;
    [SerializeField] private int currentAmmo = 0; // 一開始初始化彈藥為 0
    [Tooltip("如果武器資料卡沒設定裝彈時間，則以此數值作為 Fallback")]
    public float defaultReloadTime = 2.0f;
    private float reloadTimer = 0f;

    private Transform targetPlayer;
    private Rigidbody rb;
    private PlayerAttackHandler dummyController;

    private float nextAttackTime;
    private float attackRange; 
    private WeaponFireBase cachedFireLogic; 

    // 智慧走位相關變數
    private float strafeFactor = 1f; 
    private float strafeDirectionTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // 強制同步對手的血量系統陣營設定
        HealthSystem myHealth = GetComponent<HealthSystem>();
        if (myHealth != null)
        {
            myHealth.isBlueTeam = false; 
            gameObject.tag = "RedTeam";  
        }

        dummyController = GetComponent<PlayerAttackHandler>();
        if (dummyController == null)
        {
            dummyController = gameObject.AddComponent<PlayerAttackHandler>();
        }
        
        dummyController.firePoint = this.firePoint;

        // 初始化生成發射邏輯
        if (attackLogicPrefab != null && firePoint != null)
        {
            GameObject logicObj = Instantiate(attackLogicPrefab, firePoint);
            logicObj.transform.localPosition = Vector3.zero;
            logicObj.transform.localRotation = Quaternion.identity;
            
            cachedFireLogic = logicObj.GetComponent<WeaponFireBase>();
        }

        // 資料驅動射程
        if (enemyWeaponData != null)
        {
            attackRange = Mathf.Max(1f, enemyWeaponData.attackRange - 0.3f);
        }
        else
        {
            attackRange = 5f; 
        }

        // 🌟 核心增補：監聽遊戲管理器狀態，用於回合重置時將彈藥歸零（解耦設計）
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnStateChanged += HandleGameStateChanged;
        }

        FindTarget();
    }

    private void OnDestroy()
    {
        // 釋放事件監聽，防止記憶體殘留
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }
    }

    private void Update()
    {
        if (targetPlayer == null) FindTarget();
        UpdateStrafeDirection();

        // 🌟 核心增補：AI 本地自動裝彈計時器
        HandleReloading();
    }

    private void FixedUpdate()
    {
        // 如果不是 Playing 狀態，強制暫停 AI 所有行動
        if (KnockoutGameManager.Instance != null && 
            KnockoutGameManager.Instance.CurrentState != KnockoutGameManager.MatchState.Playing)
        {
            StopMovement();
            return;
        }

        if (targetPlayer == null) 
        {
            StopMovement();
            return;
        }

        Vector3 playerPosHorizontal = new Vector3(targetPlayer.position.x, transform.position.y, targetPlayer.position.z);
        float distanceToPlayer = Vector3.Distance(transform.position, playerPosHorizontal);
        Vector3 direction = (playerPosHorizontal - transform.position).normalized;

        Vector3 predictedShootDirection = GetPredictedShootDirection(direction);
        if (predictedShootDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(predictedShootDirection);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * 15f));
        }

        if (distanceToPlayer > attackRange)
        {
            Vector3 moveVelocity = direction * moveSpeed;
            moveVelocity.y = rb.linearVelocity.y; 
            rb.linearVelocity = moveVelocity;
        }
        else
        {
            Vector3 perpendicularDirection = new Vector3(-direction.z, 0, direction.x); 
            Vector3 strafeVelocity = perpendicularDirection * (moveSpeed * 0.7f) * strafeFactor; 
            
            strafeVelocity.y = rb.linearVelocity.y; 
            rb.linearVelocity = strafeVelocity;

            if (Time.time >= nextAttackTime)
            {
                ExecuteAttack(predictedShootDirection);
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

    private void UpdateStrafeDirection()
    {
        if (Time.time >= strafeDirectionTimer)
        {
            strafeFactor = Random.Range(0, 2) == 0 ? 1f : -1f;
            strafeDirectionTimer = Time.time + Random.Range(1.2f, 2.5f);
        }
    }

    private Vector3 GetPredictedShootDirection(Vector3 directToPlayer)
    {
        if (targetPlayer == null) return directToPlayer;

        Rigidbody playerRb = targetPlayer.GetComponent<Rigidbody>();
        if (playerRb != null && enemyWeaponData != null && enemyWeaponData.bulletSpeed > 0)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            float bulletTravelTime = distance / enemyWeaponData.bulletSpeed;

            Vector3 playerVelocity = playerRb.linearVelocity;
            playerVelocity.y = 0; 

            Vector3 predictedPos = targetPlayer.position + (playerVelocity * bulletTravelTime);
            predictedPos.y = transform.position.y; 

            return (predictedPos - transform.position).normalized;
        }

        return directToPlayer;
    }

    /// <summary>
    /// AI 本地自動裝彈邏輯
    /// </summary>
    private void HandleReloading()
    {
        // 非戰鬥狀態中，不執行裝彈計時
        if (KnockoutGameManager.Instance != null && 
            KnockoutGameManager.Instance.CurrentState != KnockoutGameManager.MatchState.Playing)
        {
            return;
        }

        if (currentAmmo < maxAmmo)
        {
            // 🌟 資料驅動：自動讀取武器資料卡中的裝彈時間
            float reloadTime = (enemyWeaponData != null) ? enemyWeaponData.reloadTime : defaultReloadTime;

            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadTime)
            {
                currentAmmo++;
                reloadTimer = 0f;
            }
        }
        else
        {
            reloadTimer = 0f;
        }
    }

    /// <summary>
    /// 當遊戲狀態重置或進入下一局時，自動歸零彈藥（同步機制） [1]
    /// </summary>
    private void HandleGameStateChanged(KnockoutGameManager.MatchState newState)
    {
        if (newState == KnockoutGameManager.MatchState.Intro)
        {
            currentAmmo = 0;
            reloadTimer = 0f;
        }
    }

    private void ExecuteAttack(Vector3 shootDirection)
    {
        if (cachedFireLogic == null || enemyWeaponData == null) return;

        // 🌟 核心修正：檢查 AI 的獨立彈藥是否足夠！
        if (currentAmmo <= 0)
        {
            return; // 彈藥不足，不開火，繼續走位裝彈
        }

        // 🌟 核心修正：消耗 1 格獨立彈藥（會自動啟動 HandleReloading 裝彈計時）
        currentAmmo--;

        // 普攻連射之間的最小間隔（建議設為 0.3 ~ 0.5 秒左右，實現爆發三連發）
        float minRequiredCooldown = 0.3f; 
        float actualCooldown = Mathf.Max(attackCooldown, minRequiredCooldown);
        nextAttackTime = Time.time + actualCooldown;

        // 執行開火
        cachedFireLogic.Fire(dummyController, firePoint.position, shootDirection, enemyWeaponData, false);
    }
}