using UnityEngine;

public class ShotgunFire : WeaponFireBase
{
    public override void Fire(PlayerAttackHandler owner, Vector3 origin, Vector3 direction, WeaponData data, bool gadgetBuff)
    {
        if (data.bulletPrefab == null) return;

        int bulletCount = data.bulletCount;
        float sAngle = data.spreadAngle;
        
        // 防呆機制：確保子彈數大於 1 才做除法，避免錯誤
        float angleStep = bulletCount > 1 ? sAngle / (bulletCount - 1) : 0;
        float startAngle = -sAngle / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            
            Vector3 shootDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * direction;
            shootDirection.Normalize();

            GameObject bullet = Instantiate(data.bulletPrefab, origin, Quaternion.LookRotation(shootDirection));
            
            // 1. 計算這發子彈能活多久 (射程 / 速度 = 時間)
            float lifeTime = data.attackRange / data.bulletSpeed;
            
            // 2. 計算最終傷害 (如果有武裝配件加持，傷害 +30)
            int finalDamage = gadgetBuff ? data.baseDamage + 30 : data.baseDamage;

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                // 🌟 核心解封！把速度、傷害和壽命正式傳給子彈
                bulletScript.Init(owner, data.bulletSpeed, finalDamage, lifeTime);
            }
        }
        
        Debug.Log("💥 散彈已射出！");
    }
}