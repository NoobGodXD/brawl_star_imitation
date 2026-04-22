using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("目標設定")]
    public Transform target;

    [Header("跟隨設定")]
    public float smoothTime = 0.15f; 
    public Vector3 offset = new Vector3(0f, 15f, -12f); 

    [Header("X軸 (左右) 傳統死區")]
    [Tooltip("左右移動時，超過死區攝影機會跟隨邊緣。")]
    public float deadZoneX = 1.0f;
    
    [Header("Z軸 (前後) 突破鎖定死區")]
    [Tooltip("前後移動的死區。超過這個範圍後，攝影機會立刻將角色拉回正中央的紅線！")]
    public float deadZoneZ = 1.5f; 
    [Tooltip("當角色Z軸移動速度低於此值，視為『停下腳步』並解除紅線鎖定，重新產生死區。")]
    public float stopSpeedThreshold = 0.05f;

    [Header("地圖邊界限制")]
    public bool useBoundaries = true;
    public float minX = -10f;
    public float maxX = 10f;
    public float minZ = -10f;
    public float maxZ = 10f;

    private Vector3 targetCameraPos;
    private Vector3 currentVelocity = Vector3.zero;

    // 用來判斷 Z 軸是否進入「緊緊鎖定在紅線」的狀態
    private bool isZLocked = false;
    private float lastTargetZ;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null) cam.orthographic = true;
        
        if (target != null)
        {
            lastTargetZ = target.position.z;
            targetCameraPos = CalculateClampedPosition(target.position + offset);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 找出攝影機目前注視的地平線中心點
        Vector3 currentFocusPoint = transform.position - offset;
        currentFocusPoint.y = target.position.y;
        Vector3 targetFocusPoint = currentFocusPoint;

        // --- 處理 X 軸 (左右)：傳統的「推邊緣」死區 ---
        float diffX = target.position.x - currentFocusPoint.x;
        if (Mathf.Abs(diffX) > deadZoneX)
        {
            targetFocusPoint.x = target.position.x - (Mathf.Sign(diffX) * deadZoneX);
        }

        // --- 處理 Z 軸 (前後)：突破後鎖定中央紅線 ---
        float diffZ = target.position.z - currentFocusPoint.z;
        
        // 計算玩家在 Z 軸的移動速度
        float currentSpeedZ = Mathf.Abs(target.position.z - lastTargetZ) / Time.deltaTime;
        lastTargetZ = target.position.z;

        // 核心邏輯 A：如果玩家停下腳步，解除鎖定，讓死區重新生效
        if (currentSpeedZ < stopSpeedThreshold)
        {
            isZLocked = false;
        }

        // 核心邏輯 B：如果玩家走出了 Z 軸死區，觸發「緊緊鎖定」狀態
        if (Mathf.Abs(diffZ) > deadZoneZ)
        {
            isZLocked = true;
        }

        // 根據狀態決定目標位置
        if (isZLocked)
        {
            // 鎖定狀態：無視死區，直接把注視目標設為角色的 Z 座標，將他強力拉回紅線上！
            targetFocusPoint.z = target.position.z;
        }
        else
        {
            // 未鎖定狀態：玩家在死區內閒晃，攝影機 Z 軸完全不動
            targetFocusPoint.z = currentFocusPoint.z;
        }

        // --- 最終座標計算與邊界限制 ---
        Vector3 desiredPos = targetFocusPoint + offset;
        targetCameraPos = CalculateClampedPosition(desiredPos);

        // 執行平滑移動
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            targetCameraPos, 
            ref currentVelocity, 
            smoothTime
        );
    }

    private Vector3 CalculateClampedPosition(Vector3 position)
    {
        if (!useBoundaries) return position;

        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        float clampedY = position.y;
        float clampedZ = Mathf.Clamp(position.z, minZ, maxZ);

        return new Vector3(clampedX, clampedY, clampedZ);
    }

    // 在 Unity 編輯器 Scene 視窗中繪製輔助線 (整合成一個)
    void OnDrawGizmos()
    {
        // --- 1. 繪製死區 (黃色/紅色長方形) ---
        if (target != null)
        {
            Vector3 currentFocusPoint = transform.position - offset;
            currentFocusPoint.y = target.position.y;
            
            // 根據是否鎖定切換顏色
            Gizmos.color = isZLocked ? Color.red : Color.yellow;
            Vector3 deadzoneSize = new Vector3(deadZoneX * 2, 0.1f, deadZoneZ * 2);
            Gizmos.DrawWireCube(currentFocusPoint, deadzoneSize);
        }

        // --- 2. 繪製地圖邊界 (白色大方框) ---
        if (useBoundaries)
        {
            Gizmos.color = Color.white;

            // 計算邊界的中心點
            float centerX = (minX + maxX) / 2f;
            float centerZ = (minZ + maxZ) / 2f;
            // 讓框框顯示在玩家的高度
            float yPos = target != null ? target.position.y : 0f;
            
            Vector3 boundaryCenter = new Vector3(centerX, yPos, centerZ);
            
            // 計算邊界的尺寸
            float sizeX = maxX - minX;
            float sizeZ = maxZ - minZ;
            Vector3 boundarySize = new Vector3(sizeX, 0.2f, sizeZ);

            // 繪製主邊界框
            Gizmos.DrawWireCube(boundaryCenter, boundarySize);
            
            // 繪製一個稍微高一點點的框，增加視覺辨識度
            Gizmos.DrawWireCube(boundaryCenter + Vector3.up * 0.1f, boundarySize);

            // 在四個角落畫小球，方便確認頂點
            Gizmos.DrawSphere(new Vector3(minX, yPos, minZ), 0.2f);
            Gizmos.DrawSphere(new Vector3(maxX, yPos, minZ), 0.2f);
            Gizmos.DrawSphere(new Vector3(minX, yPos, maxZ), 0.2f);
            Gizmos.DrawSphere(new Vector3(maxX, yPos, maxZ), 0.2f);
        }
    }
}