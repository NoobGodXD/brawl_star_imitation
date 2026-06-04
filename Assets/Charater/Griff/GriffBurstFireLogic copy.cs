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
        for (int w = 0; w < waves; w++)
        {
            // 🌟 核心修正：動態取得發射者當前的 firePoint 位置！
            // 這樣不論玩家還是 AI 在移動，子彈都會從最新的發射點位置產生
            Vector3 currentOrigin = (player != null && player.firePoint != null) ? player.firePoint.position : origin;

            ExecuteWave(player, currentOrigin, direction, data);
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        // 🌟 核心修正：此處【不可以】寫 Destroy(gameObject)！
        // 因為本系統採用了「單次生成、重複使用（Cache）」的機制。
        // 如果銷毀了，玩家/AI 開第二槍時會報錯。
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