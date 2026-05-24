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

    void Start()
    {
        mainCam = Camera.main;

        // 🌟 修正：一開始初始化時子彈為 0
        currentAmmo = 0;

        if (whiteFlashOverlay != null) whiteFlashOverlay.canvasRenderer.SetAlpha(0f);
        if (pulseRingCG != null) pulseRingCG.alpha = 0f;

        UpdateChargeUI();
    }

    // 🌟 新增：供回合重置時調用，將子彈歸零，重新累積
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
                ultWeaponFire = obj.GetComponent<WeaponFireBase>();
            }
        }

        UpdateChargeUI();
    }

    void Update()
    {
        // 🌟 核心防線：如果不是 Playing 狀態，關閉普通與大招瞄準線，並不處理任何戰鬥操作
        if (KnockoutGameManager.Instance != null &&
            KnockoutGameManager.Instance.CurrentState != KnockoutGameManager.MatchState.Playing)
        {
            if (normalAimIndicator != null) normalAimIndicator.gameObject.SetActive(false);
            if (ultAimIndicator != null) ultAimIndicator.gameObject.SetActive(false);
            return;
        }

        AimAtMouse();
        HandleReloading();
        HandleGadgetInput();
        HandleAttackInput();
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

    private void HandleAttackInput()
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
                ultAimIndicator.UpdateAiming(firePoint.position, transform.forward, ultWeapon.attackRange, ultWeapon.spreadAngle, currentColor);
            }
        }
        else
        {
            if (ultAimIndicator != null) ultAimIndicator.gameObject.SetActive(false);
            if (normalAimIndicator != null)
            {
                normalAimIndicator.gameObject.SetActive(true);
                normalAimIndicator.UpdateAiming(firePoint.position, transform.forward, normalWeapon.attackRange, normalWeapon.spreadAngle, currentColor);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isAimingUlt)
            {
                if (currentCharge >= (maxCharge - 0.1f))
                {
                    if (ultWeaponFire != null)
                    {
                        ultWeaponFire.Fire(this, firePoint.position, transform.forward, ultWeapon, false);
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
                    if (normalWeaponFire != null) normalWeaponFire.Fire(this, firePoint.position, transform.forward, normalWeapon, hasGadgetBuff);
                    if (hasGadgetBuff) { hasGadgetBuff = false; gadgetCooldownTimer = gadgetCooldown; }
                }
            }
        }
    }

    private void AimAtMouse()
    {
        if (mainCam == null) return;
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            Vector3 lookDir = hitPoint - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }
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
}