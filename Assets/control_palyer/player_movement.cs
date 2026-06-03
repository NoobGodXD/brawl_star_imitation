using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float maxSpeed = 5f;
    public float accelerationTime = 0.05f;

    [Header("轉向設定")]
    public float rotationSpeed = 15f;
    public Transform visualModelContainer;

    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 smoothVelocityReference = Vector3.zero;
    
    // ⭐ 新增：用來記憶玩家最後一次輸入的方向
    private Vector3 lastLookDirection = Vector3.forward; 

    void Update()
    {
        if (KnockoutGameManager.Instance != null &&
            KnockoutGameManager.Instance.CurrentState != KnockoutGameManager.MatchState.Playing)
        {
            currentVelocity = Vector3.zero;
            smoothVelocityReference = Vector3.zero;
            return;
        }

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        // 取得當前的輸入方向
        Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // ⭐ 核心優化：只要玩家有按方向鍵，就更新「最後要看的方向」
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            lastLookDirection = inputDirection;
        }

        Vector3 targetVelocity = inputDirection * maxSpeed;

        currentVelocity = Vector3.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref smoothVelocityReference,
            accelerationTime
        );

        transform.position += currentVelocity * Time.deltaTime;
    }

    void LateUpdate()
    {
        // ⭐ 改為使用 lastLookDirection 來旋轉，這樣就算放開鍵盤，角色也會把身轉完！
        if (visualModelContainer != null && lastLookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lastLookDirection);
            
            visualModelContainer.rotation = Quaternion.Slerp(
                visualModelContainer.rotation, 
                targetRotation, 
                Time.deltaTime * rotationSpeed
            );
        }
    }
}