using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerAttackHandler : MonoBehaviour
{
    [Header("【核心】當前武器與發射點")]
    [HideInInspector] public WeaponData normalWeapon;
    [HideInInspector] public WeaponData ultWeapon;
    public Transform firePoint;

    private AimIndicatorBase normalAimIndicator;
    private WeaponFireBase normalWeaponFire;
    private AimIndicatorBase ultAimIndicator;
    private WeaponFireBase ultWeaponFire;

    [Header("【彈藥系統 (Ammo)】")]
    public int maxAmmo = 3;
    public int currentAmmo = 3;
    private float reloadTimer = 0f;
    public Image[] ammoFills;
    public Color loadingColor = Color.white;
    public Color readyColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("【武裝配件 (Gadget)】")]
    public KeyCode gadgetKey = KeyCode.F;
    public float gadgetCooldown = 10f;
    private float gadgetCooldownTimer = 0f;
    public bool hasGadgetBuff = false;
    public Image gadgetIcon;
    public Color gadgetReadyColor = Color.white;
    public Color gadgetCooldownColor = Color.gray;

    [Header("【大招能量參數】")]
    public float maxCharge = 100f;
    public float currentCharge = 0f;
    public Image chargeBarUI;

    [Header("【大招 UI 進階視覺】")]
    public Image ultMainIcon;
    [HideInInspector] public Sprite normalIcon;
    [HideInInspector] public Sprite ultReadyIcon;
    public Image whiteFlashOverlay;
    public RectTransform pulseRing;
    public CanvasGroup pulseRingCG;
    public Image[] uiElementsToFlash;

    [Header("【瞄準框視覺客製化】")]
    public Color normalIndicatorColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);
    public Color ultIndicatorColor = new Color(1f, 0.85f, 0f, 0.8f);

    [HideInInspector] public bool isAimingUlt = false;
    private bool isUltReady = false;

    private Camera mainCam;
    private Coroutine flashCoroutine;
    private Coroutine pulseCoroutine;

    // 狀態與快取控制（用於安全防禦與自我修復）
    private CharacterData cachedCharacterData;
    private bool isWeaponModulesInitialized = false; // 核心防線：用標記防止無限實例化循環

    void Start()
    {
        mainCam = Camera.main;

        // 一開始初始化時子彈為 0
        currentAmmo = 0;

        if (whiteFlashOverlay != null) whiteFlashOverlay.canvasRenderer.SetAlpha(0f);
        if (pulseRingCG != null) pulseRingCG.alpha = 0f;

        // 監聽遊戲管理器狀態，用於回合重置/復活時標記重新初始化
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnStateChanged += HandleGameStateChanged;
        }

        UpdateChargeUI();
    }

    private void OnDestroy()
    {
        // 釋放事件監聽
        if (KnockoutGameManager.Instance != null)
        {
            KnockoutGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }
    }

    // 供回合重置時調用，將子彈歸零，重新累積
    public void ResetAmmo()
    {
        currentAmmo = 0;
        reloadTimer = 0f;
        UpdateAmmoUI();
    }

    public void SetupWeaponModules(CharacterData data)
    {
        if (data == null) return;
        this.normalWeapon = data.normalAttack;
        this.ultWeapon = data.ultimateAttack;

        // 快取資料卡
        this.cachedCharacterData = data;

        // 將大招圖示重新指派
        this.normalIcon = data.ultNormalIcon;
        this.ultReadyIcon = data.ultReadyIcon;

        if (this.normalIcon == null) Debug.LogWarning("⚠️ 英雄資料卡缺少 '普通大招圖示'");
        if (this.ultReadyIcon == null) Debug.LogWarning("⚠️ 英雄資料卡缺少 '滿能發光圖示'");

        foreach (Transform child in transform) if (child.name.Contains("Aim_")) Destroy(child.gameObject);
        foreach (Transform child in firePoint) if (child.name.Contains("Aim_") || child.name.Contains("FireLogic_")) Destroy(child.gameObject);

        if (normalWeapon != null)
        {
            if (normalWeapon.aimIndicatorPrefab != null)
            {
                GameObject obj = Instantiate(normalWeapon.aimIndicatorPrefab, firePoint);
                obj.transform.localPosition = Vector3.zero; obj.transform.localRotation = Quaternion.identity;
                normalAimIndicator = obj.GetComponent<AimIndicatorBase>();
                normalAimIndicator.gameObject.SetActive(false);
            }
            if (normalWeapon.weaponFirePrefab != null)
            {
                GameObject obj = Instantiate(normalWeapon.weaponFirePrefab, firePoint);
                
                // 重置發射邏輯物件的本地座標與旋轉
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                
                normalWeaponFire = obj.GetComponent<WeaponFireBase>();
            }
        }

        if (ultWeapon != null)
        {
            if (ultWeapon.aimIndicatorPrefab != null)
            {
                GameObject obj = Instantiate(ultWeapon.aimIndicatorPrefab, firePoint);
                obj.transform.localPosition = Vector3.zero; obj.transform.localRotation = Quaternion.identity;
                ultAimIndicator = obj.GetComponent<AimIndicatorBase>();
                ultAimIndicator.gameObject.SetActive(false);
            }
            if (ultWeapon.weaponFirePrefab != null)
            {
                GameObject obj = Instantiate(ultWeapon.weaponFirePrefab, firePoint);
                
                // 重置大招發射邏輯物件的本地座標與旋轉
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                
                ultWeaponFire = obj.GetComponent<WeaponFireBase>();
            }
        }

        // 標記初始化已完成，防止 Update 重複調用
        isWeaponModulesInitialized = true;

        UpdateChargeUI();
    }

    void Update()
    {
        // 核心防線：如果不是 Playing 狀態，關閉普通與大招瞄準線，並不處理任何戰鬥操作
        if (KnockoutGameManager.Instance != null &&
            KnockoutGameManager.Instance.CurrentState != KnockoutGameManager.MatchState.Playing)
        {
            if (normalAimIndicator != null) normalAimIndicator.gameObject.SetActive(false);
            if (ultAimIndicator != null) ultAimIndicator.gameObject.SetActive(false);
            return;
        }

        // 核心防禦：如果進入戰鬥狀態後，發現尚未初始化或因重生模型重建需要重新加載
        if (!isWeaponModulesInitialized && normalWeapon != null)
        {
            RebuildWeaponModules();
        }

        // 1. 取得當前滑鼠指向方向（角色本體不進行旋轉）
        Vector3 mouseDirection = GetMouseDirection();

        HandleReloading();
        HandleGadgetInput();
        HandleAttackInput(mouseDirection);
    }

    public void AddUltCharge(float amount)
    {
        currentCharge = Mathf.Min(currentCharge + amount, maxCharge);
        UpdateChargeUI();

        if (amount > 0 && currentCharge < maxCharge) TriggerWhiteFlash();
    }

    private void UpdateChargeUI()
    {
        if (chargeBarUI != null)
        {
            chargeBarUI.fillAmount = currentCharge / maxCharge;
        }

        bool isCurrentlyReady = (currentCharge >= (maxCharge - 0.1f));

        if (ultMainIcon != null)
        {
            text_sprite_change(isCurrentlyReady);
        }

        if (isCurrentlyReady && !isUltReady)
        {
            isUltReady = true;
            Debug.Log("🔥 大招已就緒！圖片應已切換！瞄準框顏色已解鎖金黃色！");

            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(UltReadyPulseLoop());
        }
        else if (!isCurrentlyReady && isUltReady)
        {
            isUltReady = false;
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            if (pulseRingCG != null) pulseRingCG.alpha = 0f;
        }
    }

    private void text_sprite_change(bool isCurrentlyReady)
    {
        ultMainIcon.sprite = isCurrentlyReady ? ultReadyIcon : normalIcon;
    }

    private void HandleAttackInput(Vector3 mouseDirection)
    {
        if (normalWeapon == null) return;

        if (ultWeapon != null && currentCharge >= (maxCharge - 0.1f) && Input.GetKey(KeyCode.E))
            isAimingUlt = true;
        else
            isAimingUlt = false;

        Color currentColor = isAimingUlt ? ultIndicatorColor : normalIndicatorColor;

        if (isAimingUlt)
        {
            if (normalAimIndicator != null) normalAimIndicator.gameObject.SetActive(false);
            if (ultAimIndicator != null)
            {
                ultAimIndicator.gameObject.SetActive(true);

                // ⭐ 關鍵修復：我們只針對「大招瞄準框」物件進行旋轉，而不動到 transform 本身
                if (mouseDirection.sqrMagnitude > 0.01f)
                {
                    ultAimIndicator.transform.rotation = Quaternion.LookRotation(mouseDirection);
                }
                
                // 更新網格圖形時，將 transform.forward 改為傳入 mouseDirection
                ultAimIndicator.UpdateAiming(firePoint.position, mouseDirection, ultWeapon.attackRange, ultWeapon.spreadAngle, currentColor);
            }
        }
        else
        {
            if (ultAimIndicator != null) ultAimIndicator.gameObject.SetActive(false);
            if (normalAimIndicator != null)
            {
                normalAimIndicator.gameObject.SetActive(true);

                // ⭐ 關鍵修復：我們只針對「普攻瞄準框」物件進行旋轉，而不動到 transform 本身
                if (mouseDirection.sqrMagnitude > 0.01f)
                {
                    normalAimIndicator.transform.rotation = Quaternion.LookRotation(mouseDirection);
                }

                // 更新網格圖形時，將 transform.forward 改為傳入 mouseDirection
                normalAimIndicator.UpdateAiming(firePoint.position, mouseDirection, normalWeapon.attackRange, normalWeapon.spreadAngle, currentColor);
            }
        }

        // 手動與自動雙模開火判定
        bool wantsManualFire = Input.GetMouseButtonDown(0); // LMB 手動
        bool wantsAutoFire = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space); // RMB 或 空白鍵 自動

        if (wantsManualFire || wantsAutoFire)
        {
            if (isAimingUlt)
            {
                if (currentCharge >= (maxCharge - 0.1f))
                {
                    if (ultWeaponFire != null)
                    {
                        // 🌟 修正 1：大招自動瞄準傳入「滑鼠方向」作為 Fallback，避免沒人時朝正面空放
                        Vector3 fireDirection = wantsManualFire ? mouseDirection : GetAutoAimDirection(ultWeapon.attackRange, mouseDirection);

                        ultWeaponFire.Fire(this, firePoint.position, fireDirection, ultWeapon, false);
                        currentCharge = 0f;
                        UpdateChargeUI();
                    }
                }
            }
            else
            {
                if (currentAmmo > 0)
                {
                    currentAmmo--;
                    if (normalWeaponFire != null) 
                    {
                        // 🌟 修正 2：普攻自動瞄準傳入「滑鼠方向」作為 Fallback，避免沒人時朝正面空放
                        Vector3 fireDirection = wantsManualFire ? mouseDirection : GetAutoAimDirection(normalWeapon.attackRange, mouseDirection);

                        normalWeaponFire.Fire(this, firePoint.position, fireDirection, normalWeapon, hasGadgetBuff);
                    }
                    if (hasGadgetBuff) { hasGadgetBuff = false; gadgetCooldownTimer = gadgetCooldown; }
                }
            }
        }
    }

    private Vector3 GetMouseDirection()
    {
        if (mainCam == null) return transform.forward;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                return lookDir.normalized;
            }
        }
        return transform.forward;
    }

    private void HandleReloading()
    {
        if (normalWeapon == null) return;
        if (currentAmmo < maxAmmo)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= normalWeapon.reloadTime)
            {
                currentAmmo++;
                reloadTimer = 0f;
            }
        }
        else reloadTimer = 0f;
        UpdateAmmoUI();
    }

    private void UpdateAmmoUI()
    {
        if (normalWeapon == null) return;
        for (int i = 0; i < ammoFills.Length; i++)
        {
            if (ammoFills[i] != null)
            {
                if (i < currentAmmo) { ammoFills[i].fillAmount = 1f; ammoFills[i].color = readyColor; }
                else if (i == currentAmmo) { ammoFills[i].fillAmount = reloadTimer / normalWeapon.reloadTime; ammoFills[i].color = loadingColor; }
                else ammoFills[i].fillAmount = 0f;
            }
        }
    }

    private void HandleGadgetInput()
    {
        if (gadgetCooldownTimer > 0)
        {
            gadgetCooldownTimer -= Time.deltaTime;
            if (gadgetIcon != null) gadgetIcon.color = gadgetCooldownColor;
        }
        else
        {
            if (gadgetIcon != null) gadgetIcon.color = gadgetReadyColor;
        }
        if (Input.GetKeyDown(gadgetKey) && gadgetCooldownTimer <= 0 && !isAimingUlt)
        {
            hasGadgetBuff = true;
        }
    }

    private void TriggerWhiteFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (whiteFlashOverlay != null) whiteFlashOverlay.canvasRenderer.SetAlpha(1f);

        Color[] originalColors = new Color[0];
        if (uiElementsToFlash != null)
        {
            originalColors = new Color[uiElementsToFlash.Length];
            for (int i = 0; i < uiElementsToFlash.Length; i++)
            {
                if (uiElementsToFlash[i] != null)
                {
                    originalColors[i] = uiElementsToFlash[i].color;
                    uiElementsToFlash[i].color = Color.white;
                }
            }
        }

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (whiteFlashOverlay != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                whiteFlashOverlay.canvasRenderer.SetAlpha(alpha);
            }
            yield return null;
        }

        if (uiElementsToFlash != null)
        {
            for (int i = 0; i < uiElementsToFlash.Length; i++)
            {
                if (uiElementsToFlash[i] != null) uiElementsToFlash[i].color = originalColors[i];
            }
        }
    }

    private IEnumerator UltReadyPulseLoop()
    {
        while (isUltReady)
        {
            yield return StartCoroutine(SinglePulse());
            yield return new WaitForSeconds(1.2f);
        }
    }

    private IEnumerator SinglePulse()
    {
        if (pulseRing == null) yield break;

        float elapsed = 0f;
        float duration = 0.8f;
        pulseRing.localScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            pulseRing.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 2.5f, t);
            if (pulseRingCG != null) pulseRingCG.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        if (pulseRingCG != null) pulseRingCG.alpha = 0f;
    }

    // ==================== 🌟 自動尋敵與防範無限重複初始化 ====================

    private void HandleGameStateChanged(KnockoutGameManager.MatchState newState)
    {
        if (newState == KnockoutGameManager.MatchState.Intro)
        {
            isWeaponModulesInitialized = false; // 標記：下一局開始時需要重新尋找與建立 [1]
        }
    }

    private void RebuildWeaponModules()
    {
        if (cachedCharacterData != null)
        {
            // 如果在新模型加載過程中，原有的 firePoint 被物理銷毀（變為 null）
            // 我們自動在角色子物件中尋找最新生成的 "FirePoint"
            if (firePoint == null)
            {
                firePoint = FindFirePointInChildren(transform);
            }

            Debug.LogWarning("🛡️ [Self-Healing] 偵測到復活/重生，已重新綁定新模型的發機點與瞄準模組。");
            SetupWeaponModules(cachedCharacterData);
        }
    }

    private Transform FindFirePointInChildren(Transform parent)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLower().Contains("firepoint"))
            {
                return child;
            }
        }
        return null;
    }

    private string GetTargetEnemyTag()
    {
        HealthSystem myHealth = GetComponent<HealthSystem>();
        if (myHealth != null)
        {
            return myHealth.isBlueTeam ? "RedTeam" : "BlueTeam";
        }
        return "RedTeam"; 
    }

    /// <summary>
    /// 自動瞄準：在射程內尋找最近的敌方活著目標，並回傳方向 [1]
    /// </summary>
    private Vector3 GetAutoAimDirection(float range, Vector3 fallbackDirection)
    {
        string targetTag = GetTargetEnemyTag();
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(targetTag);
        GameObject nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;
        Vector3 myPosition = transform.position;

        foreach (GameObject enemy in enemies)
        {
            // 🌟 優化 1：使用 GetComponentInParent，防止打中骨骼或子物件時找不到 HealthSystem 導致目標遺失
            HealthSystem health = enemy.GetComponentInParent<HealthSystem>();
            if (health != null && !health.IsDead)
            {
                // 🌟 優化 2：將座標水平化 (XZ 平面)，忽略 Y 軸高度差，防止斜坡或 Pivot 造成射程判定誤差
                Vector3 enemyPosHorizontal = enemy.transform.position;
                enemyPosHorizontal.y = myPosition.y;

                float distance = Vector3.Distance(myPosition, enemyPosHorizontal);
                
                // 尋找在武器射程內，且距離最近的目標
                if (distance < nearestDistance && distance <= range)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        // 如果找到目標，回傳方向
        if (nearestEnemy != null)
        {
            Vector3 dir = (nearestEnemy.transform.position - myPosition).normalized;
            dir.y = 0; 
            return dir;
        }

        // 🌟 優化 3：如果射程內沒有任何敵人，Fallback 回傳滑鼠當前瞄準的方向，手感更流暢
        return fallbackDirection;
    }
}