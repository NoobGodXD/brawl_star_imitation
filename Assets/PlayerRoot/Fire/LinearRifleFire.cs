using UnityEngine;

using System.Collections; // 必須要有這行才能用協程 (Coroutine)

public class LinearRifleFire : WeaponFireBase
{
    // 用來記錄目前是否還在連發中，避免玩家狂按導致子彈數量重疊
    private bool isFiring = false;

    public override void Fire(PlayerAttackHandler owner, Vector3 origin, Vector3 direction, WeaponData data, bool gadgetBuff)
    {
        // 如果還在連發上一波的子彈，就忽略這次開火指令
        if (isFiring) return; 

        // 啟動協程來達成連發效果
        StartCoroutine(FireSequence(owner, origin, direction, data, gadgetBuff));
    }

    private IEnumerator FireSequence(PlayerAttackHandler owner, Vector3 origin, Vector3 direction, WeaponData data, bool gadgetBuff)
    {
        isFiring = true;
        
        int bulletCount = data.bulletCount;
        float timeDelay = data.timeBetweenShots;

        for (int i = 0; i < bulletCount; i++)
        {
            if (data.bulletPrefab != null)
            {
                // 生成子彈
                GameObject bullet = Instantiate(data.bulletPrefab, origin, Quaternion.LookRotation(direction));
                
                // 1. 計算壽命 (射程 / 速度 = 時間)
                float lifeTime = data.attackRange / data.bulletSpeed;
                
                // 2. 計算最終傷害 (武裝配件加成)
                int finalDamage = gadgetBuff ? data.baseDamage + 30 : data.baseDamage;

                // 3. 傳送動力、傷害與壽命給子彈
                Bullet bulletScript = bullet.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Init(owner, data.bulletSpeed, finalDamage, lifeTime);
                }
            }
            
            // 🌟 【關鍵】等待設定好的時間 (例如 0.1秒) 後，再執行迴圈射下一發
            yield return new WaitForSeconds(timeDelay); 
        }

        // 所有子彈都射完後，解除開火狀態
        isFiring = false;
    }
}