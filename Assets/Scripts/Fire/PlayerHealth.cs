using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vie")]
    public float maxHealth = 3f;

    [Header("Degats du Feu")]
    public float fireDamageDistance = 2f;
    public float damagePerSecond    = 1f;
    public float fireCellSize       = 1f;

    [Header("Couleurs de la barre")]
    public Color healthyColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color damagedColor = new Color(0.8f, 0.2f, 0.1f, 1f);

    [Header("Camera Shake")]
    public float shakePositionIntensity = 0.08f;
    public float shakeRotationIntensity = 2f;
    public float shakeDuration          = 0.15f;

    [Header("Mort")]
    public float collapseDuration  = 1.2f;
    public float collapseAngle     = 80f;
    public float collapseDropHeight = 0.8f;
    public float eyeCloseDelay     = 1.5f;
    public float eyeCloseDuration  = 1.5f;

    [Header("Barre de vie (taille)")]
    public float barWidth  = 200f;
    public float barHeight = 10f;

    private float currentHealth;

    // Programmatic health bar
    private Canvas    healthBarCanvas;
    private Image     healthBarBG;
    private Image     healthBarFill;

    private Transform cameraTransform;
    private float     shakeTimer             = 0f;
    private float     currentShakeIntensity  = 0f;
    private Vector3   originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private bool      isShaking             = false;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        CreateHealthBar();
        UpdateHealthBar();

        Camera cam = GetComponent<Camera>() ?? GetComponentInChildren<Camera>() ?? Camera.main;
        if (cam != null)
            cameraTransform = cam.transform;
        else
            Debug.LogWarning("[PlayerHealth] Aucune camera trouvee pour le camera shake.");
    }

    private void CreateHealthBar()
    {
        // Root canvas — not parented to player so death sequence doesn't disable it
        GameObject canvasObj = new GameObject("HealthBar_Canvas");
        healthBarCanvas = canvasObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        healthBarCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();

        // Background (dark semi-transparent)
        GameObject bgObj = new GameObject("HealthBar_BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        healthBarBG              = bgObj.AddComponent<Image>();
        healthBarBG.color        = new Color(0f, 0f, 0f, 0.5f);
        healthBarBG.raycastTarget = false;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 1f);
        bgRect.anchorMax        = new Vector2(0f, 1f);
        bgRect.pivot            = new Vector2(0f, 1f);
        bgRect.anchoredPosition = new Vector2(16f, -16f);
        bgRect.sizeDelta        = new Vector2(barWidth, barHeight);

        // Fill (green → red)
        GameObject fillObj = new GameObject("HealthBar_Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        healthBarFill              = fillObj.AddComponent<Image>();
        healthBarFill.color        = healthyColor;
        healthBarFill.raycastTarget = false;
        healthBarFill.type         = Image.Type.Filled;
        healthBarFill.fillMethod   = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin   = (int)Image.OriginHorizontal.Left;
        healthBarFill.fillAmount   = 1f;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);   // 1px inner padding
        fillRect.offsetMax = new Vector2(-1f, -1f);
    }

    void Update()
    {
        if (isDead) return;

        if (Fire_propagation.ActiveFireCount > 0)
        {
            float distToFire = Fire_propagation.GetClosestFireDistance(transform.position, fireCellSize);
            if (distToFire <= fireDamageDistance)
            {
                currentHealth = Mathf.Max(currentHealth - damagePerSecond * Time.deltaTime, 0f);
                TriggerShake();
                UpdateHealthBar();

                if (currentHealth <= 0f) OnPlayerDeath();
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null || isDead) return;

        if (isShaking)
        {
            shakeTimer -= Time.deltaTime;

            if (shakeTimer > 0f)
            {
                float intensity = currentShakeIntensity * (shakeTimer / shakeDuration);

                cameraTransform.localPosition = originalCameraLocalPos + new Vector3(
                    Random.Range(-1f, 1f) * shakePositionIntensity * intensity,
                    Random.Range(-1f, 1f) * shakePositionIntensity * intensity,
                    0f);

                cameraTransform.localRotation = originalCameraLocalRot * Quaternion.Euler(
                    Random.Range(-1f, 1f) * shakeRotationIntensity * intensity,
                    Random.Range(-1f, 1f) * shakeRotationIntensity * intensity,
                    Random.Range(-1f, 1f) * shakeRotationIntensity * intensity * 0.5f);
            }
            else
            {
                cameraTransform.localPosition = originalCameraLocalPos;
                cameraTransform.localRotation = originalCameraLocalRot;
                isShaking = false;
            }
        }
    }

    private void TriggerShake()
    {
        if (cameraTransform == null) return;

        if (!isShaking)
        {
            originalCameraLocalPos = cameraTransform.localPosition;
            originalCameraLocalRot = cameraTransform.localRotation;
        }

        shakeTimer            = shakeDuration;
        currentShakeIntensity = 1f;
        isShaking             = true;
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        float ratio = currentHealth / maxHealth;
        healthBarFill.fillAmount = ratio;
        healthBarFill.color      = Color.Lerp(damagedColor, healthyColor, ratio);
    }

    private void OnPlayerDeath()
    {
        if (isDead) return;
        isDead    = true;
        isShaking = false;

        FireProximityVision fireVision =
            GetComponent<FireProximityVision>()
            ?? GetComponentInChildren<FireProximityVision>()
            ?? FindObjectOfType<FireProximityVision>();
        fireVision?.LockEffect();

        GameObject playerRoot = transform.root.gameObject;

        CharacterController cc = playerRoot.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        foreach (MonoBehaviour s in playerRoot.GetComponentsInChildren<MonoBehaviour>())
        {
            if (s != this && !(s is FireProximityVision) && !(s is Volume))
                s.enabled = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalCameraLocalPos;
            cameraTransform.localRotation = originalCameraLocalRot;

            Vector3    startPos = cameraTransform.localPosition;
            Quaternion startRot = cameraTransform.localRotation;
            Vector3    endPos   = startPos + new Vector3(0f, -collapseDropHeight, 0f);
            Quaternion endRot   = startRot * Quaternion.Euler(10f, 0f, collapseAngle);

            float elapsed = 0f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                float eased = Mathf.Pow(elapsed / collapseDuration, 2f);
                cameraTransform.localPosition = Vector3.Lerp(startPos, endPos, eased);
                cameraTransform.localRotation = Quaternion.Slerp(startRot, endRot, eased);
                yield return null;
            }

            cameraTransform.localPosition = endPos;
            cameraTransform.localRotation = endRot;
        }

        FindObjectOfType<FireProximityVision>()?.StartDeathBlur();

        yield return new WaitForSeconds(eyeCloseDelay);

        GameObject canvasObj = new GameObject("DeathOverlay_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform topRect    = CreateEyelid(canvasObj.transform, "TopEyelid",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        RectTransform bottomRect = CreateEyelid(canvasObj.transform, "BottomEyelid", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));

        float halfScreen   = Screen.height / 2f + 20f;
        float closeElapsed = 0f;

        while (closeElapsed < eyeCloseDuration)
        {
            closeElapsed += Time.deltaTime;
            float eased  = Mathf.SmoothStep(0f, 1f, closeElapsed / eyeCloseDuration);
            float h      = Mathf.Lerp(0f, halfScreen, eased);
            topRect.sizeDelta    = new Vector2(0f, h);
            bottomRect.sizeDelta = new Vector2(0f, h);
            yield return null;
        }

        topRect.sizeDelta    = new Vector2(0f, halfScreen);
        bottomRect.sizeDelta = new Vector2(0f, halfScreen);
    }

    private static RectTransform CreateEyelid(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>().color = Color.black;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin  = anchorMin;
        r.anchorMax  = anchorMax;
        r.pivot      = pivot;
        r.sizeDelta  = Vector2.zero;
        return r;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateHealthBar();
        if (currentHealth <= 0f) OnPlayerDeath();
    }

    void OnDestroy()
    {
        if (healthBarCanvas != null)
            Destroy(healthBarCanvas.gameObject);
    }
}
