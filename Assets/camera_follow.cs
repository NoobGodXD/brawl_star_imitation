using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("要跟隨的玩家目標")]
    public Transform target;

    [Header("延遲與跟隨設定 (可在面板自由調整)")]
    
    [Tooltip("【時間延遲】角色離開死區後，攝影機要發呆幾秒才開始動？ (秒)")]
    public float startDelayTime = 0.5f;

    [Tooltip("【拖曳感】攝影機移動的平滑程度。數值越大，跟上的速度越慢、越滑順。")]
    public float smoothTime = 0.3f;
    
    [Tooltip("【死區半徑】玩家在此範圍內移動時，攝影機完全不會理他。(黃色圈圈)")]
    public float deadZoneRadius = 2f;
    
    [Tooltip("攝影機相對於玩家的偏移位置（正交投影通常設定在正上方 Y 軸）")]
    public Vector3 offset = new Vector3(0f, 5f, 0f);

    // 內部運算變數
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 currentTargetPos; // 攝影機目前「打算前往」的目標點
    private float delayTimer = 0f;    // 延遲計時器

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
        }

        // 遊戲開始時，先將目標點設為攝影機當前位置，避免一開始畫面亂飛
        currentTargetPos = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 將攝影機當前位置投影到與玩家相同的高度 (Y軸)
        Vector3 cameraGroundPos = new Vector3(transform.position.x, target.position.y, transform.position.z);
        
        // 2. 計算距離
        float distanceFromTarget = Vector3.Distance(cameraGroundPos, target.position);

        // 3. 核心邏輯：判斷是否在死區外，並計算時間延遲
        if (distanceFromTarget > deadZoneRadius)
        {
            // 玩家在死區外，開始計時
            delayTimer += Time.deltaTime;

            // 如果發呆的時間結束了，就開始更新目標位置
            if (delayTimer >= startDelayTime)
            {
                Vector3 directionToTarget = (target.position - cameraGroundPos).normalized;
                // 讓理想位置保持在死區邊緣
                Vector3 idealGroundPos = target.position - (directionToTarget * deadZoneRadius);
                currentTargetPos = idealGroundPos + offset;
            }
        }
        else
        {
            // 如果玩家走回死區內，計時器歸零
            delayTimer = 0f;
        }

        // 4. 無論如何，攝影機永遠朝著 currentTargetPos 平滑移動
        // 這樣即使玩家突然停下，攝影機也會順順地停在最後的目標點，不會有頓挫感
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            currentTargetPos, 
            ref currentVelocity, 
            smoothTime
        );
    }

    // 視覺化輔助：在 Scene 面板畫出死區範圍
    void OnDrawGizmos()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            // 以攝影機當前對應的地板位置為中心，畫出死區半徑
            Vector3 center = new Vector3(transform.position.x, target.position.y, transform.position.z);
            Gizmos.DrawWireSphere(center, deadZoneRadius);
        }
    }
}