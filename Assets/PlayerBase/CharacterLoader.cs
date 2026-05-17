using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    [Header("目前選擇的英雄資料卡")]
    public CharacterData currentHeroData; 

    [Header("要進行資料綁定的系統")]
    // 🌟 注意：這裡我們已經升級成新的大腦 PlayerAttackHandler 了！
    public PlayerAttackHandler playerAttackHandler; 
    public HealthSystem healthSystem;
    
    [Tooltip("用來放 3D 模型的空物件節點")]
    public Transform modelContainer; 

    void Start()
    {
        // 遊戲一開始，執行讀取英雄的程序
        LoadCharacter();
    }

    public void LoadCharacter()
    {
        if (currentHeroData == null) return;

        // 🌟 1. 加上 if 保護傘：如果有模型，才進行生成與綁定動畫
        if (currentHeroData.modelPrefab != null && modelContainer != null)
        {
            GameObject heroModel = Instantiate(currentHeroData.modelPrefab, modelContainer);
            Animator anim = heroModel.GetComponent<Animator>();
            if (anim != null && currentHeroData.animatorController != null)
            {
                anim.runtimeAnimatorController = currentHeroData.animatorController;
            }
        }
        else
        {
            Debug.LogWarning("⚠️ 英雄資料卡缺少 Model Prefab，將略過模型生成，但繼續載入武器。");
        }

        // 2. 灌入 UI 貼圖
        if (playerAttackHandler != null)
        {
            if (playerAttackHandler.gadgetIcon != null && currentHeroData.gadgetIcon != null) 
            {
                playerAttackHandler.gadgetIcon.sprite = currentHeroData.gadgetIcon;
            }
            
            // 🌟 3. 因為上面沒當機，現在這行終於可以順利執行了！
            playerAttackHandler.SetupWeaponModules(currentHeroData);
        }

        // 4. 灌入生命值
        if (healthSystem != null)
        {
            healthSystem.maxHealth = currentHeroData.maxHealth;
            healthSystem.currentHealth = currentHeroData.maxHealth;
        }

        Debug.Log("✅ 成功載入英雄：" + currentHeroData.characterName);
    }
}