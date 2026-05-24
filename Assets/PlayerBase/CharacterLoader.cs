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

        // 1. 加上 if 保護傘：如果有模型，才進行生成與綁定動畫
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

            // 3. 執行大招與武裝設定
            playerAttackHandler.SetupWeaponModules(currentHeroData);
        }

        // 4. 灌入生命值 與 🌟 展示卡需要的英雄資訊
        if (healthSystem != null)
        {
            healthSystem.maxHealth = currentHeroData.maxHealth;
            healthSystem.currentHealth = currentHeroData.maxHealth;

            // 🌟 新增：將資料卡中的英雄名字灌入血量系統（對應展示卡）
            healthSystem.characterName = currentHeroData.characterName;

            // 🌟 新增：將資料卡中的頭像圖片灌入血量系統（對應展示卡）
            // 提示：請確保您的 CharacterData 腳本（ScriptableObject）中有宣告 `public Sprite characterPortrait;`
            if (currentHeroData.characterPortrait != null)
            {
                healthSystem.characterPortrait = currentHeroData.characterPortrait;
            }
            else
            {
                // 如果沒有頭像，暫時用武裝配件的圖示當作備用頭像，避免展示卡完全空白
                healthSystem.characterPortrait = currentHeroData.gadgetIcon;
                Debug.LogWarning($"⚠️ 英雄 [{currentHeroData.characterName}] 的資料卡缺少頭像 (characterPortrait)，將以配件圖示替代。");
            }
        }

        Debug.Log("✅ 成功載入英雄並完成 UI 資料綁定：" + currentHeroData.characterName);
    }
}