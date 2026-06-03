using UnityEngine;
using System.Collections;

public class GriffBurstFireLogic : WeaponFireBase
{
    [Header("【葛瑞夫】連發與散佈設定")]
    public int waves = 3; 
    public float timeBetweenWaves = 0.15f; 
    public int bulletsPerWave = 3; 

    // 1. 大腦呼叫開火，並把「玩家自己 (player)」傳進來
    public override void Fire(PlayerAttackHandler player, Vector3 origin, Vector3 direction, WeaponData data, bool isGadget)
    {
        // 🌟 核心修正：把 player 傳進協程中
        StartCoroutine(FireBurstRoutine(player, origin, direction, data));
    }

    // 2. 協程接收 player 參數
    private IEnumerator FireBurstRoutine(PlayerAttackHandler player, Vector3 origin, Vector3 direction, WeaponData data)
    {
        for (int w = 0; w < waves; w++)
        {
            // 🌟 核心修正：把 player 傳給下一層
            ExecuteWave(player, origin, direction, data);
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    // 3. 每波發射接收 player 參數
    private void ExecuteWave(PlayerAttackHandler player, Vector3 origin, Vector3 direction, WeaponData data)
    {
        if (data.bulletPrefab == null) return;

        if (bulletsPerWave <= 1)
        {
            SpawnSingleBullet(player, origin, direction, data, 0f);
            return;
        }

        float angleStep = data.spreadAngle / (bulletsPerWave - 1);
        float startAngle = -data.spreadAngle / 2f;

        for (int i = 0; i < bulletsPerWave; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            // 🌟 核心修正：把 player 傳給最終生成子彈的函數
            SpawnSingleBullet(player, origin, direction, data, currentAngle);
        }
    }

    // 4. 最終生成子彈，正式將玩家綁定給子彈！
    private void SpawnSingleBullet(PlayerAttackHandler player, Vector3 origin, Vector3 baseDirection, WeaponData data, float angleOffset)
    {
        Vector3 shootDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * baseDirection;
        shootDirection.Normalize();

        GameObject bullet = Instantiate(data.bulletPrefab, origin, Quaternion.LookRotation(shootDirection));

       // --- 🎯 處理大招：迴旋鏢紙鈔 ---
        BoomerangBullet boomerang = bullet.GetComponent<BoomerangBullet>();
        if (boomerang != null)
        {
            // 🌟 這裡改成 data.baseDamage
            boomerang.InitBoomerang(player, shootDirection, data.baseDamage, data.ultChargeAmount);
            return; 
        }

        // --- 🎯 處理普攻：普通硬幣子彈 ---
        Bullet normalBullet = bullet.GetComponent<Bullet>();
        if (normalBullet != null)
        {
            float lifeTime = data.attackRange / data.bulletSpeed;
            
            // 🌟 這裡也改成 data.baseDamage
            normalBullet.Init(player, lifeTime, data.baseDamage, data.ultChargeAmount); 
        }

        // 賦予一般子彈物理速度
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * data.bulletSpeed;
        }

        float bulletLifeTime = data.attackRange / data.bulletSpeed;
        Destroy(bullet, bulletLifeTime);
    }
}