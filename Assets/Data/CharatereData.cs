using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("基本資料")]
    public string characterName;
    public GameObject modelPrefab; 
    public RuntimeAnimatorController animatorController; 

    [Header("生命值")]
    public int maxHealth;

    [Header("技能武器設定 (組合模式)")]
    public WeaponData normalAttack;    // 🎯 拖入普攻武器卡 (例如：雪莉的散彈)
    public WeaponData ultimateAttack;  // 🎯 拖入大招武器卡 (例如：雪莉的大散彈)

    [Header("專屬 UI 特效貼圖")]
    public Sprite gadgetIcon;          // 武裝配件圖示
    public Sprite ultNormalIcon;       // 🎯 大招未滿時的專屬圖示
    public Sprite ultReadyIcon;        // 🎯 大招滿能發光的專屬圖示
}