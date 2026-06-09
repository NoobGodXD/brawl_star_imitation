using UnityEngine;
using System.Collections;

public class GriffBurstFireLogic : WeaponFireBase
{
    [Header("【葛瑞夫】連發與散佈設定")]
    public int waves = 3; 
    public float timeBetweenWaves = 0.15f; 
    public int bulletsPerWave = 3; 

    public override void Fire(PlayerAttackHandler player, Vector3 origin, Vector3 direction, WeaponData data, bool isGadget)
    {
        StartCoroutine(FireBurstRoutine(player, origin, direction, data));
    }

    private IEnumerator FireBurstRoutine(PlayerAttackHandler player, Vector3 origin, Vector3 direction, WeaponData data)
    {
        // 🌟【核心修正】：在第一波發射前，精算出發射點相對於角色中心點的相對偏移向量
        Vector3 relativeOffset = Vector3.zero;
        if (player != null)
        {
            relativeOffset = origin - player.transform.position;
        }

        for (int w = 0; w < waves; w++)
        {
            // 預設防呆回退值
            Vector3 currentOrigin = origin;

            if (player != null)
            {
                // 優先順序 1：檢查 AI 控制器上有沒有直接掛載現成的 firePoint
                var ai = player.GetComponent<EnemyAIController>();
                if (ai != null && ai.firePoint != null)
                {
                    currentOrigin = ai.firePoint.position;
                }
                // 優先順序 2：直接在物件子層級中自動搜尋名為 "FirePoint" 的物件
                else
                {
                    Transform fp = player.transform.Find("FirePoint");
                    if (fp == null) fp = player.transform.Find("firePoint");

                    if (fp != null)
                    {
                        currentOrigin = fp.position;
                    }
                    // 優先順序 3：【終極動態追蹤】直接用角色當前世界座標 + 初始發射偏移量
                    // 如此一來，不論大腦有沒有設定 firePoint，子彈發射點都會完美跟隨身體移動！
                    else
                    {
                        currentOrigin = player.transform.position + relativeOffset;
                    }
                }
            }

            ExecuteWave(player, currentOrigin, direction, data);
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

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
            SpawnSingleBullet(player, origin, direction, data, currentAngle);
        }
    }

    private void SpawnSingleBullet(PlayerAttackHandler player, Vector3 origin, Vector3 baseDirection, WeaponData data, float angleOffset)
    {
        Vector3 shootDirection = Quaternion.AngleAxis(angleOffset, Vector3.up) * baseDirection;
        shootDirection.Normalize();

        GameObject bullet = Instantiate(data.bulletPrefab, origin, Quaternion.LookRotation(shootDirection));

        BoomerangBullet boomerang = bullet.GetComponent<BoomerangBullet>();
        if (boomerang != null)
        {
            boomerang.InitBoomerang(player, shootDirection, data.baseDamage, data.ultChargeAmount);
            return; 
        }

        Bullet normalBullet = bullet.GetComponent<Bullet>();
        if (normalBullet != null)
        {
            float lifeTime = data.attackRange / data.bulletSpeed;
            normalBullet.Init(player, lifeTime, data.baseDamage, data.ultChargeAmount); 
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * data.bulletSpeed;
        }

        float bulletLifeTime = data.attackRange / data.bulletSpeed;
        Destroy(bullet, bulletLifeTime);
    }
}