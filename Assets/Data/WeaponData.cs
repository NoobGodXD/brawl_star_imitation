using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("戰鬥數值")]
    public int baseDamage;
    public float reloadTime;       // 普攻需要裝填時間，大招的話可以設為 0
    public float attackRange;      // 攻擊距離
    public float bulletSpeed;      // 子彈速度
    public float spreadAngle;      // 散彈角度 (直線則設為 0)
    public int bulletCount;        // 子彈數量
    public float timeBetweenShots; // 連發間隔時間 (散彈設為 0)

    [Header("核心 Prefab 綁定")]
    public GameObject bulletPrefab;       // 專屬子彈外觀與特效
    public GameObject aimIndicatorPrefab; // 專屬瞄準框外觀 (扇形或長方形)
    public GameObject weaponFirePrefab;    // 專屬發射邏輯 (ShotgunFire 或 LinearRifleFire)

    
    // 🌟 新增：讓每張武器卡可以自己決定「打中一發回多少能量」
    [Tooltip("打中敵人時，可以增加多少大招能量？")]
    public float ultChargeAmount = 15f;
}