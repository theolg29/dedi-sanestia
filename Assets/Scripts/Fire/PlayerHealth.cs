using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
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
    public Color healthyColor = new Color(0.15f, 0.85f, 0.25f, 1f);
    public Color damagedColor = new Color(0.95f, 0.15f, 0.1f,  1f);

    [Header("Camera Shake")]
    public float shakePositionIntensity = 0.08f;
    public float shakeRotationIntensity = 2f;
    public float shakeDuration          = 0.15f;

    [Header("Mort")]
    public float collapseDuration   = 1.2f;
    public float collapseAngle      = 80f;
    public float collapseDropHeight = 0.8f;
    public float eyeCloseDelay      = 1.5f;
    public float eyeCloseDuration   = 1.5f;

    [Header("Barre de vie (taille)")]
    public float barWidth  = 220f;
    public float barHeight = 16f;

    private float currentHealth;

    private Canvas        healthBarCanvas;
    private Image         healthBarFill;
    private RectTransform healthFillRect;

    private Transform  cameraTransform;
    private float      shakeTimer            = 0f;
    private float      currentShakeIntensity = 0f;
    private Vector3    originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private bool       isShaking             = false;

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
        float panelW = barWidth + 20f;
        float panelH = 52f;

        GameObject canvasObj = new GameObject("HealthBar_Canvas");
        healthBarCanvas = canvasObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        healthBarCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();

        // Panel conteneur
        GameObject panelObj   = new GameObject("HealthBar_Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImg         = panelObj.AddComponent<Image>();
        panelImg.color         = new Color(0.04f, 0.04f, 0.04f, 0.78f);
        panelImg.raycastTarget = false;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0f, 1f);
        panelRect.anchorMax        = new Vector2(0f, 1f);
        panelRect.pivot            = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(14f, -14f);
        panelRect.sizeDelta        = new Vector2(panelW, panelH);

        // Label "VIE"
        GameObject labelObj    = new GameObject("HealthBar_Label");
        labelObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI label  = labelObj.AddComponent<TextMeshProUGUI>();
        label.text             = "VIE";
        label.fontSize         = 11f;
        label.color            = new Color(0.85f, 0.85f, 0.85f, 1f);
        label.fontStyle        = FontStyles.Bold;
        label.raycastTarget    = false;
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin        = new Vector2(0f, 1f);
        labelRect.anchorMax        = new Vector2(0f, 1f);
        labelRect.pivot            = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(10f, -8f);
        labelRect.sizeDelta        = new Vector2(barWidth, 16f);

        // Fond de barre
        GameObject bgObj   = new GameObject("HealthBar_BG");
        bgObj.transform.SetParent(panelObj.transform, false);
        Image bgImg         = bgObj.AddComponent<Image>();
        bgImg.color         = new Color(0.12f, 0.12f, 0.12f, 1f);
        bgImg.raycastTarget = false;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 0f);
        bgRect.anchorMax        = new Vector2(0f, 0f);
        bgRect.pivot            = new Vector2(0f, 0f);
        bgRect.anchoredPosition = new Vector2(10f, 8f);
        bgRect.sizeDelta        = new Vector2(barWidth, barHeight);

        // Fill — taille mise a jour via sizeDelta (pas de sprite requis)
        GameObject fillObj     = new GameObject("HealthBar_Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        healthBarFill              = fillObj.AddComponent<Image>();
        healthBarFill.color        = healthyColor;
        healthBarFill.raycastTarget = false;
        healthFillRect             = fillObj.GetComponent<RectTransform>();
        healthFillRect.anchorMin        = new Vector2(0f, 0f);
        healthFillRect.anchorMax        = new Vector2(0f, 0f);
        healthFillRect.pivot            = new Vector2(0f, 0f);
        healthFillRect.anchoredPosition = new Vector2(2f, 2f);
        healthFillRect.sizeDelta        = new Vector2(barWidth - 4f, barHeight - 4f);
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
        if (healthFillRect == null) return;
        float ratio = currentHealth / maxHealth;
        healthFillRect.sizeDelta = new Vector2(Mathf.Max(0f, (barWidth - 4f) * ratio), barHeight - 4f);
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
