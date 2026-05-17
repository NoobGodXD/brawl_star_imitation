using UnityEngine;

// 所有武器發射邏輯的基類
public abstract class WeaponFireBase : MonoBehaviour
{
    // 供核心管理器呼叫，執行發射邏輯
    public abstract void Fire(PlayerAttackHandler owner, Vector3 origin, Vector3 direction, WeaponData data, bool gadgetBuff);
}