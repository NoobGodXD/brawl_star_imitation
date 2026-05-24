using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("角色的最大移動速度")]
    public float maxSpeed = 5f;

    [Tooltip("從靜止到最大速度的加速時間（秒）。50ms = 0.05f")]
    public float accelerationTime = 0.05f;

    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 smoothVelocityReference = Vector3.zero;

    void Update()
    {
        // 🌟 核心防線：如果遊戲不處於「遊玩中」狀態，強制清除所有速度並退出移動邏輯
        if (KnockoutGameManager.Instance != null &&
            KnockoutGameManager.Instance.CurrentState != KnockoutGameManager.MatchState.Playing)
        {
            currentVelocity = Vector3.zero;
            smoothVelocityReference = Vector3.zero;
            return;
        }

        // 1. 獲取無預設平滑的純粹輸入值 (-1, 0, 1)
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // 2. 計算目標方向並標準化，避免對角線移動時速度疊加變快
        Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // 3. 計算玩家應該要達到的目標速度
        Vector3 targetVelocity = inputDirection * maxSpeed;

        // 4. 使用 SmoothDamp 讓當前速度平滑過渡到目標速度
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