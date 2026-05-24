using UnityEngine;
using System.Collections;

public class BoomerangBullet : MonoBehaviour
{
    [Header("【視覺模型】")]
    public Transform visualModel;
    public float spinSpeed = 1500f;

    [Header("【飛行參數】")]
    public float flySpeed = 20f;
    public float returnSpeed = 25f;
    public float maxFlyDistance = 10f;
    public float maxFlyTime = 0.8f;

    [Tooltip("在最遠處停留旋轉的時間 (秒)")]
    public float hoverDuration = 0.5f;

    private PlayerAttackHandler playerHandler;
    private Vector3 startPosition;
    private int bulletDamage;
    private float chargeAmount;
    private float currentFlyTime = 0f;

    // 🌟 新增：動態敵人標籤，依據發射者隊伍而定
    private string targetEnemyTag = "RedTeam";

    private enum FlyState { FlyingOut, Hovering, Returning }
    private FlyState currentState;

    public void InitBoomerang(PlayerAttackHandler player, Vector3 direction, int baseDamage, float charge)
    {
        playerHandler = player;
        startPosition = transform.position;
        bulletDamage = baseDamage;
        chargeAmount = charge;
        currentFlyTime = 0f;
        currentState = FlyState.FlyingOut;

        // 🌟 新增：動態設定子彈的敵人標籤
        if (playerHandler != null)
        {
            HealthSystem shooterHealth = playerHandler.GetComponent<HealthSystem>();
            if (shooterHealth != null)
            {
                targetEnemyTag = shooterHealth.isBlueTeam ? "RedTeam" : "BlueTeam";
            }
        }

        transform.rotation = Quaternion.LookRotation(direction);
    }

    void Update()
    {
        if (visualModel != null)
        {
            visualModel.Rotate(Vector3.forward * spinSpeed * Time.deltaTime);
        }

        switch (currentState)
        {
            case FlyState.FlyingOut:
                currentFlyTime += Time.deltaTime;
                transform.Translate(Vector3.forward * flySpeed * Time.deltaTime, Space.Self);

                if (Vector3.Distance(startPosition, transform.position) >= maxFlyDistance || currentFlyTime >= maxFlyTime)
                {
                    StartCoroutine(HoverRoutine());
                }
                break;

            case FlyState.Hovering:
                break;

            case FlyState.Returning:
                transform.position = Vector3.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, startPosition) <= 1.0f)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    private IEnumerator HoverRoutine()
    {
        currentState = FlyState.Hovering;
        yield return new WaitForSeconds(hoverDuration);
        currentState = FlyState.Returning;
    }

    void OnTriggerEnter(Collider other)
    {
        // 🌟 修正：不使用寫死的 "Enemy"，改用動態的 targetEnemyTag
        if (other.CompareTag(targetEnemyTag))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health == null)
            {
                health = other.GetComponentInParent<HealthSystem>();
            }

            if (health != null)
            {
                // 🌟 修正：傳入發射者的名稱作為傷害來源，解決編譯錯誤
                string attackerName = playerHandler != null ? playerHandler.gameObject.name : "對手";
                health.TakeDamage(bulletDamage, attackerName);

                Debug.Log($"🎴 撲克牌切中敵人！造成 {bulletDamage} 點傷害！");

                if (playerHandler != null)
                {
                    playerHandler.AddUltCharge(chargeAmount);
                }
            }
        }
        // 🧱 牆壁碰撞優化：迴力鏢打到牆壁，不應該直接碎裂銷毀，而是立刻開始「飛回」
        else if (other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            if (currentState == FlyState.FlyingOut)
            {
                StopAllCoroutines();
                currentState = FlyState.Returning;
            }
        }
    }
}