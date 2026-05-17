using UnityEngine;
using System.Collections; // 🌟 記得要有這個才能用協程 (IEnumerator)

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

    // 🌟 新增：決定要在最遠處停留多久？
    [Tooltip("在最遠處停留旋轉的時間 (秒)")]
    public float hoverDuration = 0.5f;

    private PlayerAttackHandler playerHandler;
    private Vector3 startPosition;
    private int bulletDamage;
    private float chargeAmount;
    private float currentFlyTime = 0f;

    // 🌟 核心升級：定義撲克牌的三個狀態
    private enum FlyState { FlyingOut, Hovering, Returning }
    private FlyState currentState;

    public void InitBoomerang(PlayerAttackHandler player, Vector3 direction, int baseDamage, float charge)
    {
        playerHandler = player;
        startPosition = transform.position;
        bulletDamage = baseDamage;
        chargeAmount = charge;
        currentFlyTime = 0f;

        // 一出生就設定為「飛出狀態」
        currentState = FlyState.FlyingOut;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    void Update()
    {
        // 視覺旋轉 (不管在哪個狀態，撲克牌都要一直轉！)
        if (visualModel != null)
        {
            visualModel.Rotate(Vector3.forward * spinSpeed * Time.deltaTime);
        }

        // 狀態機：根據現在的狀態決定要做什麼動作
        switch (currentState)
        {
            case FlyState.FlyingOut:
                currentFlyTime += Time.deltaTime;
                transform.Translate(Vector3.forward * flySpeed * Time.deltaTime, Space.Self);

                // 如果達到最遠距離，或飛太久，就進入「滯空停留」狀態
                if (Vector3.Distance(startPosition, transform.position) >= maxFlyDistance || currentFlyTime >= maxFlyTime)
                {
                    StartCoroutine(HoverRoutine());
                }
                break;

            case FlyState.Hovering:
                // 停留狀態中：位移停止，只在原地旋轉
                break;

            case FlyState.Returning:
                // 🌟 核心修改：不再追蹤玩家，而是精準導航飛回【原始發射地點 (startPosition)】
                transform.position = Vector3.MoveTowards(transform.position, startPosition, returnSpeed * Time.deltaTime);

                // 🌟 當飛回距離發射點不到 1.0f 的時候，自我銷毀
                if (Vector3.Distance(transform.position, startPosition) <= 1.0f)
                {
                    Destroy(gameObject);
                }
                break;
        }
    }

    // 🌟 滯空計時器
    private IEnumerator HoverRoutine()
    {
        currentState = FlyState.Hovering;       // 切換到停留狀態
        yield return new WaitForSeconds(hoverDuration); // 等待設定的秒數 (例如 0.5 秒)
        currentState = FlyState.Returning;      // 時間到，切換到飛回狀態！
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(bulletDamage);
                Debug.Log($"🎴 撲克牌切中敵人！造成 {bulletDamage} 點傷害！");

                if (playerHandler != null)
                {
                    playerHandler.AddUltCharge(chargeAmount);
                }
            }
        }
    }
}