using UnityEditor.Callbacks;
using UnityEngine;

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("角色的最大移動速度")]
    public float maxSpeed = 5f;
    
    [Tooltip("從靜止到最大速度的加速時間（秒）。50ms = 0.05f")]
    public float accelerationTime = 0.05f;

    // 儲存當前速度，用於應用在角色位移上
    private Vector3 currentVelocity = Vector3.zero;
    // SmoothDamp 需要的參考變數，用來記錄當前的速度變化率
    private Vector3 smoothVelocityReference = Vector3.zero;

    void Update()
    {
        // 1. 獲取無預設平滑的純粹輸入值 (-1, 0, 1)
        float moveX = Input.GetAxisRaw("Horizontal"); // A/D 或 左/右
        float moveZ = Input.GetAxisRaw("Vertical");   // W/S 或 上/下

        // 2. 計算目標方向並標準化，避免對角線移動時速度疊加變快
        Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // 3. 計算玩家應該要達到的目標速度
        Vector3 targetVelocity = inputDirection * maxSpeed;

        // 4. 核心邏輯：使用 SmoothDamp 讓當前速度平滑過渡到目標速度
        // 這裡創造了約 50ms 的微小起步延遲與加速感
        currentVelocity = Vector3.SmoothDamp(
            currentVelocity, 
            targetVelocity, 
            ref smoothVelocityReference, 
            accelerationTime
        );

        // 5. 應用位移 (速度 * 時間 = 距離)
        transform.position += currentVelocity * Time.deltaTime;
    }
}
