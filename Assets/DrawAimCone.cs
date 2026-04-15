using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawAimCone : MonoBehaviour
{
    [Header("範圍設定")]
    public float radius = 5f;       // 瞄準線的長度
    public float angle = 30f;       // 射擊角度 (需與你射擊腳本的數值一致)
    public int segments = 20;       // 圓弧的平滑度 (分段數)

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        // 陣列長度 = 分段數 + 2 (包含原點與圓弧起點)
        line.positionCount = segments + 2; 
        
        // 設為 false，讓線條的座標系跟隨這個物件(也就是跟著玩家旋轉)
        line.useWorldSpace = false; 
    }

    void Update()
    {
        // 將第一個點固定在原點 (玩家位置)
        line.SetPosition(0, Vector3.zero);

        float halfAngle = angle / 2f;
        float angleStep = angle / segments;

        // 計算圓弧上的每一個點
        for (int i = 0; i <= segments; i++)
        {
            // 將當前角度從度數轉換為弧度 (Rad)
            float currentAngle = (-halfAngle + (i * angleStep)) * Mathf.Deg2Rad;
            
            // 利用三角函數計算 X 與 Z 座標
            float x = Mathf.Sin(currentAngle) * radius;
            float z = Mathf.Cos(currentAngle) * radius;

            // 將計算出的點加入 Line Renderer，注意 Y 軸設為 0.1f 稍微浮空，避免與地板重疊閃爍 (Z-Fighting)
            line.SetPosition(i + 1, new Vector3(x, 0.1f, z));
        }
    }
}