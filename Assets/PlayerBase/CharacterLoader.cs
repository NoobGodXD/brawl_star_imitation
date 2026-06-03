using UnityEngine;

public class CharacterLoader : MonoBehaviour
{
    [Header("目前選擇的英雄資料卡")]
    public CharacterData currentHeroData;

    [Header("要進行資料綁定的系統")]
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

        // 1. 生成模型與綁定動畫
        if (currentHeroData.modelPrefab != null && modelContainer != null)
        {
            GameObject heroModel = Instantiate(currentHeroData.modelPrefab, modelContainer);
            
            // ⭐ 核心修復：將生出來的模型，相對於 modelContainer 的位置與旋轉歸零
            heroModel.transform.localPosition = Vector3.zero;
            heroModel.transform.localRotation = Quaternion.Euler(currentHeroData.modelRotationOffset);

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

            // 3. 執行大招與武裝設定
            playerAttackHandler.SetupWeaponModules(currentHeroData);
        }

        // 4. 灌入生命值與展示卡資訊
        if (healthSystem != null)
        {
            healthSystem.maxHealth = currentHeroData.maxHealth;
            healthSystem.currentHealth = currentHeroData.maxHealth;
            healthSystem.characterName = currentHeroData.characterName;

            if (currentHeroData.characterPortrait != null)
            {
                healthSystem.characterPortrait = currentHeroData.characterPortrait;
            }
            else
            {
                healthSystem.characterPortrait = currentHeroData.gadgetIcon;
                Debug.LogWarning($"⚠️ 英雄 [{currentHeroData.characterName}] 缺少頭像，將以配件圖示替代。");
            }
        }

        Debug.Log("✅ 成功載入英雄並完成 UI 資料綁定：" + currentHeroData.characterName);
    }
    
}