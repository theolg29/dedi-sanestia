using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vie")]
    public float maxHealth = 3f;

    [Header("Dégâts du Feu")]
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

    [Header("Références (auto-détectées si vides)")]
    public Slider healthBarSlider;

    private float currentHealth;
    private Image fillImage;

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

        if (healthBarSlider == null)
        {
            GameObject sliderObj = GameObject.Find("HealthBar");
            if (sliderObj != null) healthBarSlider = sliderObj.GetComponent<Slider>();
        }

        if (healthBarSlider == null)
        {
            Debug.LogError("[PlayerHealth] Aucun Slider 'HealthBar' trouvé dans la scène.");
            return;
        }

        healthBarSlider.minValue    = 0f;
        healthBarSlider.maxValue    = maxHealth;
        healthBarSlider.value       = currentHealth;
        healthBarSlider.interactable = false;

        Transform fillArea = healthBarSlider.transform.Find("Fill Area");
        Transform fill     = fillArea?.Find("Fill");
        fillImage = fill != null
            ? fill.GetComponent<Image>()
            : healthBarSlider.fillRect?.GetComponent<Image>();

        if (fillImage == null)
            Debug.LogWarning("[PlayerHealth] Image de remplissage (Fill) non trouvée sur le Slider.");
        else
            fillImage.color = healthyColor;

        UpdateHealthBar();

        Camera cam = GetComponent<Camera>() ?? GetComponentInChildren<Camera>() ?? Camera.main;
        if (cam != null)
            cameraTransform = cam.transform;
        else
            Debug.LogWarning("[PlayerHealth] Aucune caméra trouvée pour le camera shake.");
    }

    void Update()
    {
        if (healthBarSlider == null || isDead) return;

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
        healthBarSlider.value = currentHealth;
        if (fillImage != null)
            fillImage.color = Color.Lerp(damagedColor, healthyColor, currentHealth / maxHealth);
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
}
