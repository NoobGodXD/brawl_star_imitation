using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("核心綁定")]
    [Tooltip("請放入做好的子彈 Prefab")]
    public GameObject bulletPrefab;
    [Tooltip("請放入玩家前方代表槍口的空物件")]
    public Transform firePoint;

    [Header("射擊數值設定")]
    public float bulletSpeed = 20f;       // 子彈飛行速度
    public float spreadAngle = 30f;       // 總射擊範圍角度 (例如 30度)
    public float angleStep = 5f;          // 每隔幾度發射一顆子彈 (例如 5度)
    public int bulletCount = 7;           // 一次射擊發射的子彈數量

    [Header("場景整理 (可選)")]
    [Tooltip("請放入場景中的一個空物件，用來收納生成的子彈，保持畫面乾淨")]
    // 總射擊範圍角度
    public Transform bulletContainer;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. 每一幀都讓玩家面向滑鼠
        AimAtMouse();

        // 2. 偵測滑鼠左鍵，按下時觸發射擊
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    /// <summary>
    /// 處理 3D 空間中的滑鼠瞄準邏輯 (利用射線與虛擬平面交點)
    /// </summary>
    private void AimAtMouse()
    {
        // 從攝影機發射一條射線到滑鼠在螢幕上的位置
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // 建立一個與玩家同高的虛擬水平面 (Y軸鎖定)
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        // 計算射線與水平面的交點
        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);

            // 計算玩家朝向交點的方向向量
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0; // 絕對鎖定 Y 軸，防止玩家模型傾斜

            if (lookDir != Vector3.zero)
            {
                // 將方向向量轉為旋轉值，並套用到玩家身上
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    /// <summary>
    /// 處理完美的扇形多重射擊邏輯
    /// </summary>
    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 如果只設定 1 顆子彈，直接朝正前方發射
        if (bulletCount <= 1)
        {
            FireBullet(0f);
            return;
        }

        // 精準數學：計算每顆子彈之間的間隔角度 (絕對不會有浮點數誤差)
        float angleStep = spreadAngle / (bulletCount - 1);

        // 算出最左邊的起始角度
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            FireBullet(currentAngle);
        }
    }

    // 將發射單顆子彈的邏輯獨立出來，程式碼更乾淨
        // 將發射單顆子彈的邏輯獨立出來，使用「世界空間絕對數學」防彈寫法
    private void FireBullet(float angle)
    {
        // 1. 【核武級防禦】：不依賴子彈的旋轉，直接用數學算出世界座標中的絕對方向向量！
        // 以槍口的正前方 (firePoint.forward) 為基準，繞著絕對的向上軸 (Vector3.up) 旋轉指定角度
        Vector3 shootDirection = Quaternion.AngleAxis(angle, Vector3.up) * firePoint.forward;
        
        // 確保方向向量的長度為 1 (標準化)，保證每顆子彈速度絕對一致
        shootDirection.Normalize();

        // 2. 生成子彈，並讓它看向這個絕對方向
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(shootDirection));

        // 3. 收納子彈 (為了防止資料夾帶有奇怪的 Scale 扭曲子彈，我們這裡做個保險)
        if (bulletContainer != null) 
        {
            bullet.transform.SetParent(bulletContainer, true); // true 代表保持世界座標不變
        }

        // 4. 賦予物理速度
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 直接把世界座標的方向向量 乘上 速度，完全無視子彈自身的 Transform！
            rb.linearVelocity = shootDirection * bulletSpeed;
        }
    }
    }