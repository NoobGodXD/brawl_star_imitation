using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic; // 必須引入

public class CharacterRotator : MonoBehaviour
{
    [Header("滑鼠拖曳靈敏度")]
    public float dragSensitivity = 5f;

    [Header("最高旋轉速度上限 (度/秒)")]
    public float maxSpeed = 1500f; 

    [Header("慣性阻力 (數值越大停越快)")]
    public float damping = 5f;

    [Header("甩動判定時間 (秒)")]
    [Tooltip("在放開滑鼠前多久時間內的移動會被算入甩動速度。建議 0.05 ~ 0.1")]
    public float flickSampleTime = 0.05f;

    private float currentVelocity = 0f;
    private bool isDragging = false;

    // 實作一個簡單的結構來儲存歷史速度記錄
    private struct VelocitySample
    {
        public float time;
        public float velocity;
    }
    private Queue<VelocitySample> velocityHistory = new Queue<VelocitySample>();

    void Update()
    {
        // === 狀態 1：按下滑鼠的瞬間 ===
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                isDragging = true;
                currentVelocity = 0f;
                velocityHistory.Clear(); // 點下時清空歷史
            }
        }

        // === 狀態 2：按住滑鼠拖曳時 ===
        if (isDragging && Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxisRaw("Mouse X");
            float rotationThisFrame = -mouseX * dragSensitivity;

            // 1. 拖曳時，強制角色跟著滑鼠轉動
            transform.Rotate(Vector3.up, rotationThisFrame, Space.World);

            // 2. 計算這一幀的瞬時速度
            float instantVelocity = rotationThisFrame / Time.deltaTime;
            currentVelocity = Mathf.Clamp(instantVelocity, -maxSpeed, maxSpeed);

            // 3. 【核心核心】：將這一幀的速度和時間存入歷史紀錄
            velocityHistory.Enqueue(new VelocitySample { time = Time.time, velocity = currentVelocity });

            // 移除掉太舊的紀錄 (只保留最近 flickSampleTime 秒內的)
            while (velocityHistory.Count > 0 && Time.time - velocityHistory.Peek().time > flickSampleTime)
            {
                velocityHistory.Dequeue();
            }
        }

        // === 狀態 3：放開滑鼠的瞬間 (處理慣性初速度) ===
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            // 【核心修正】：計算歷史紀錄中的平均速度，作為慣性的初速度
            if (velocityHistory.Count > 0)
            {
                float sumVelocity = 0;
                foreach (var sample in velocityHistory)
                {
                    sumVelocity += sample.velocity;
                }
                // 使用平均速度，這樣即使最後一幀滑鼠慢下來了，前幾幀的高速依然會被計算進去
                currentVelocity = sumVelocity / velocityHistory.Count;
            }
            else
            {
                currentVelocity = 0f;
            }
        }

        // === 狀態 4：依靠慣性滑行 (isDragging 為 false 時) ===
        if (!isDragging)
        {
            if (Mathf.Abs(currentVelocity) > 0.1f) // 微調停止閾值
            {
                // 為了保險起見，慣性旋轉時也確保不超過最高速度
                currentVelocity = Mathf.Clamp(currentVelocity, -maxSpeed, maxSpeed);
                
                // 根據留下來的角速度繼續旋轉
                transform.Rotate(Vector3.up, currentVelocity * Time.deltaTime, Space.World);

                // 慣性衰減 (使用更物理的 Damp)
                currentVelocity = Mathf.Lerp(currentVelocity, 0, Time.deltaTime * damping);
            }
            else
            {
                currentVelocity = 0f;
            }
        }
    }
}