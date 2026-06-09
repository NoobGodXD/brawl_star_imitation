using UnityEngine;
using System.Collections;

/// <summary>
/// 敵人專用控制層 - 支援回合倒數與暫停開關版
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyAIController : MonoBehaviour
{
    [Header("【遊戲狀態控制】")]
    [Tooltip("如果為 false，AI 會原地立正且不攻擊（由 GameManager 或 Player 控制）")]
    public bool canAct = true; 

    [Header("【目標與陣營設定】")]
    public string targetTag = "BlueTeam";

    [Header("【資料層與邏輯層綁定】")]
    public WeaponData enemyWeaponData; 
    public GameObject attackLogicPrefab;
    public Transform firePoint;

    [Header("【AI 基礎行為數值】")]
    public float moveSpeed = 3.5f;
    public float attackRange = 6f;
    public float attackCooldown = 1.5f;

    [Header("【荒野亂鬥高級走位設定】")]
    public float comfortableRange = 4.5f;
    public float jukeIntensity = 2f;
    public float jukeFrequency = 0.6f;
    public int retreatHealthThreshold = 300;

    private Transform targetPlayer;
    private float nextAttackTime;
    private Rigidbody rb;
    private HealthSystem myHealth;

    private float jukeTimer;
    private float currentJukeSign = 1f; 

    private float personalMoveSpeed;
    private float personalJukeFrequency;
    private float personalJukeIntensity;
    private float dynamicComfortableRange; 

    private PlayerAttackHandler dummyController;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        myHealth = GetComponent<HealthSystem>();
        
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
        
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

        personalMoveSpeed = moveSpeed + Random.Range(-0.4f, 0.4f);
        personalJukeFrequency = jukeFrequency + Random.Range(-0.15f, 0.15f);
        personalJukeIntensity = jukeIntensity + Random.Range(-0.5f, 0.5f);

        UpdateDynamicComfortableRange();
        FindTarget();
        currentJukeSign = Random.Range(0, 2) == 0 ? 1f : -1f;
    }

    private void Update()
    {
        // 🌟 總開關：如果不允許行動，就不更新任何走位計時器
        if (!canAct) return;

        if (targetPlayer == null) FindTarget();

        jukeTimer += Time.deltaTime;
        if (jukeTimer >= personalJukeFrequency)
        {
            jukeTimer = 0f;
            currentJukeSign = Random.Range(0, 2) == 0 ? 1f : -1f; 
            UpdateDynamicComfortableRange();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        // 🌟 總開關：如果不允許行動、或是找不到玩家，就立刻煞車並停止運作
        if (!canAct || targetPlayer == null) 
        {
            StopMovement();
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        Vector3 baseDirection = (targetPlayer.position - transform.position).normalized;
        baseDirection.y = 0; 

        if (baseDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(baseDirection);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * 12f));
        }

        bool shouldRetreat = false;
        if (myHealth == null) myHealth = GetComponent<HealthSystem>();
        
        if (myHealth != null)
        {
            try 
            {
                if (myHealth.currentHealth < retreatHealthThreshold) shouldRetreat = true;
            }
            catch (System.NullReferenceException)
            {
                shouldRetreat = false;
            }
        }

        Vector3 finalMoveVelocity = Vector3.zero;

        if (shouldRetreat)
        {
            Vector3 retreatDir = -baseDirection;
            Vector3 jukeDir = Vector3.Cross(retreatDir, Vector3.up) * currentJukeSign * personalJukeIntensity * 0.5f;
            finalMoveVelocity = (retreatDir + jukeDir).normalized * (personalMoveSpeed * 1.2f);
        }
        else
        {
            Vector3 sideDirection = Vector3.Cross(baseDirection, Vector3.up) * currentJukeSign;
            Vector3 jukeMovement = sideDirection * personalJukeIntensity;

            if (distanceToPlayer > attackRange)
            {
                finalMoveVelocity = (baseDirection + jukeMovement).normalized * personalMoveSpeed;
            }
            else if (distanceToPlayer < dynamicComfortableRange)
            {
                finalMoveVelocity = (-baseDirection + jukeMovement).normalized * personalMoveSpeed;
            }
            else
            {
                finalMoveVelocity = jukeMovement.normalized * (personalMoveSpeed * 0.8f);
            }
        }

        if (finalMoveVelocity != Vector3.zero)
        {
            Vector3 randomWander = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized * 0.4f;
            finalMoveVelocity = (finalMoveVelocity + randomWander).normalized * finalMoveVelocity.magnitude;

            finalMoveVelocity.y = rb.linearVelocity.y; 
            rb.linearVelocity = finalMoveVelocity;
        }

        float effectiveAttackRange = Mathf.Max(attackRange, dynamicComfortableRange + 1.5f);
        if (distanceToPlayer <= effectiveAttackRange && !shouldRetreat)
        {
            if (Time.time >= nextAttackTime)
            {
                ExecuteAttack(baseDirection);
            }
        }
    }

    private void UpdateDynamicComfortableRange()
    {
        dynamicComfortableRange = comfortableRange + Random.Range(-1.5f, 1.5f);
        dynamicComfortableRange = Mathf.Max(1.5f, dynamicComfortableRange);
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

        GameObject logicObj = Instantiate(attackLogicPrefab, firePoint.position, firePoint.rotation);
        var fireLogic = logicObj.GetComponent<WeaponFireBase>();
        if (fireLogic != null)
        {
            fireLogic.Fire(dummyController, firePoint.position, shootDirection, enemyWeaponData, false);
        }

        Bullet[] spawnedBullets = UnityEngine.Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None);
    }
}