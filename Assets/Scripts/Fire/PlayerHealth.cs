using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// Système de vie du joueur avec dégâts du feu.
/// À placer sur le joueur (ou la caméra).
/// Cherche automatiquement le slider "HealthBar" dans la scène.
/// 
/// - 3 PV max
/// - Perd 1 PV par seconde passée dans le feu
/// - Barre verte (pleine) → rouge (vide)
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Vie")]
    [Tooltip("Points de vie maximum")]
    public float maxHealth = 3f;

    [Header("Dégâts du Feu")]
    [Tooltip("Distance à laquelle le joueur subit des dégâts (touche le feu)")]
    public float fireDamageDistance = 2f;

    [Tooltip("Dégâts par seconde dans le feu")]
    public float damagePerSecond = 1f;

    [Tooltip("Taille de cellule du système de feu (doit correspondre à Fire_propagation)")]
    public float fireCellSize = 1f;

    [Header("Couleurs de la barre")]
    public Color healthyColor = new Color(0.2f, 0.8f, 0.2f, 1f);  // Vert
    public Color damagedColor = new Color(0.8f, 0.2f, 0.1f, 1f);   // Rouge

    [Header("Tremblement (Camera Shake)")]
    [Tooltip("Amplitude max du tremblement de position")]
    public float shakePositionIntensity = 0.08f;

    [Tooltip("Amplitude max du tremblement de rotation (degrés)")]
    public float shakeRotationIntensity = 2f;

    [Tooltip("Durée du tremblement après chaque tick de dégât")]
    public float shakeDuration = 0.15f;

    [Header("Mort du joueur")]
    [Tooltip("Durée de la chute de la caméra sur le côté (secondes)")]
    public float collapseDuration = 1.2f;

    [Tooltip("Angle de rotation Z quand le joueur s'effondre (degrés)")]
    public float collapseAngle = 80f;

    [Tooltip("Hauteur de chute de la caméra (mètres)")]
    public float collapseDropHeight = 0.8f;

    [Tooltip("Délai avant que les yeux se ferment (secondes)")]
    public float eyeCloseDelay = 1.5f;

    [Tooltip("Durée de la fermeture des yeux (secondes)")]
    public float eyeCloseDuration = 1.5f;

    [Header("Références (auto-détectées si vides)")]
    [Tooltip("Le Slider UI nommé 'HealthBar'. Laissez vide pour auto-détection.")]
    public Slider healthBarSlider;

    // État interne
    private float currentHealth;
    private Image fillImage;

    // Camera shake
    private Transform cameraTransform;
    private float shakeTimer = 0f;
    private float currentShakeIntensity = 0f;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private bool isShaking = false;
    private float damageAccumulator = 0f;

    // Death state
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;

        // --- Auto-détection du Slider "HealthBar" ---
        if (healthBarSlider == null)
        {
            GameObject sliderObj = GameObject.Find("HealthBar");
            if (sliderObj != null)
            {
                healthBarSlider = sliderObj.GetComponent<Slider>();
            }
        }

        if (healthBarSlider == null)
        {
            Debug.LogError("[PlayerHealth] ❌ Aucun Slider 'HealthBar' trouvé dans la scène !");
            return;
        }

        // --- Configurer le Slider ---
        healthBarSlider.minValue = 0f;
        healthBarSlider.maxValue = maxHealth;
        healthBarSlider.value = currentHealth;
        healthBarSlider.interactable = false; // Le joueur ne doit pas pouvoir le glisser

        // --- Récupérer l'image de remplissage (Fill) pour changer sa couleur ---
        Transform fillArea = healthBarSlider.transform.Find("Fill Area");
        if (fillArea != null)
        {
            Transform fill = fillArea.Find("Fill");
            if (fill != null)
            {
                fillImage = fill.GetComponent<Image>();
            }
        }

        if (fillImage == null)
        {
            // Fallback : chercher dans fillRect directement
            if (healthBarSlider.fillRect != null)
            {
                fillImage = healthBarSlider.fillRect.GetComponent<Image>();
            }
        }

        if (fillImage != null)
        {
            fillImage.color = healthyColor;
            Debug.Log("[PlayerHealth] ✅ Barre de vie initialisée : " + maxHealth + " PV");
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] ⚠️ Image de remplissage (Fill) non trouvée sur le Slider.");
        }

        UpdateHealthBar();

        // --- Auto-détection de la caméra pour le shake ---
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            cameraTransform = cam.transform;
            Debug.Log("[PlayerHealth] ✅ Caméra détectée pour le shake : " + cam.gameObject.name);
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] ⚠️ Aucune caméra trouvée pour le camera shake.");
        }
    }

    void Update()
    {
        if (healthBarSlider == null) return;
        if (isDead) return; // Séquence de mort en cours

        // --- Vérifier si le joueur est dans le feu ---
        if (Fire_propagation.ActiveFireCount > 0)
        {
            float distToFire = Fire_propagation.GetClosestFireDistance(transform.position, fireCellSize);

            if (distToFire <= fireDamageDistance)
            {
                // Le joueur touche le feu → perd de la vie
                float damage = damagePerSecond * Time.deltaTime;
                currentHealth -= damage;
                currentHealth = Mathf.Max(currentHealth, 0f);

                // Déclencher le shake en continu tant qu'on prend des dégâts
                TriggerShake();

                UpdateHealthBar();

                if (currentHealth <= 0f)
                {
                    OnPlayerDeath();
                }
            }
        }
    }

    void LateUpdate()
    {
        // --- CAMERA SHAKE (désactivé pendant la mort) ---
        if (cameraTransform == null || isDead) return;

        if (isShaking)
        {
            shakeTimer -= Time.deltaTime;

            if (shakeTimer > 0f)
            {
                float progress = shakeTimer / shakeDuration;
                float intensity = currentShakeIntensity * progress;

                Vector3 posOffset = new Vector3(
                    Random.Range(-1f, 1f) * shakePositionIntensity * intensity,
                    Random.Range(-1f, 1f) * shakePositionIntensity * intensity,
                    0f
                );

                Vector3 rotOffset = new Vector3(
                    Random.Range(-1f, 1f) * shakeRotationIntensity * intensity,
                    Random.Range(-1f, 1f) * shakeRotationIntensity * intensity,
                    Random.Range(-1f, 1f) * shakeRotationIntensity * intensity * 0.5f
                );

                cameraTransform.localPosition = originalCameraLocalPos + posOffset;
                cameraTransform.localRotation = originalCameraLocalRot * Quaternion.Euler(rotOffset);
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

        // Sauvegarder la position/rotation d'origine seulement si pas déjà en shake
        if (!isShaking)
        {
            originalCameraLocalPos = cameraTransform.localPosition;
            originalCameraLocalRot = cameraTransform.localRotation;
        }

        shakeTimer = shakeDuration;
        currentShakeIntensity = 1f;
        isShaking = true;
    }

    private void UpdateHealthBar()
    {
        // Mettre à jour la valeur du slider
        healthBarSlider.value = currentHealth;

        // Interpoler la couleur : vert (plein) → rouge (vide)
        if (fillImage != null)
        {
            float healthRatio = currentHealth / maxHealth;
            fillImage.color = Color.Lerp(damagedColor, healthyColor, healthRatio);
        }
    }

    private void OnPlayerDeath()
    {
        if (isDead) return;
        isDead = true;
        isShaking = false;

        Debug.Log("[PlayerHealth] 💀 Le joueur est mort !");

        // --- Figer l'overlay orange (le flou progressif sera lancé après la chute) ---
        FireProximityVision fireVision = GetComponent<FireProximityVision>();
        if (fireVision == null) fireVision = GetComponentInChildren<FireProximityVision>();
        if (fireVision == null) fireVision = FindObjectOfType<FireProximityVision>();
        if (fireVision != null) fireVision.LockEffect();

        // --- Désactiver TOUS les contrôles du joueur ---
        GameObject playerRoot = transform.root.gameObject;

        // Désactiver le CharacterController (mouvement)
        CharacterController cc = playerRoot.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Désactiver tous les MonoBehaviours sauf PlayerHealth, FireProximityVision et Volume (flou)
        MonoBehaviour[] allScripts = playerRoot.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour script in allScripts)
        {
            if (script != this && !(script is FireProximityVision) && !(script is Volume)) script.enabled = false;
        }

        // Bloquer et rendre invisible le curseur
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // --- Lancer la séquence de mort ---
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // === PHASE 1 : La caméra s'effondre sur le côté ===
        if (cameraTransform != null)
        {
            // Restaurer la position propre (arrêter le shake)
            cameraTransform.localPosition = originalCameraLocalPos;
            cameraTransform.localRotation = originalCameraLocalRot;

            Vector3 startPos = cameraTransform.localPosition;
            Quaternion startRot = cameraTransform.localRotation;

            // Position finale : plus bas + tourné sur le côté
            Vector3 endPos = startPos + new Vector3(0f, -collapseDropHeight, 0f);
            Quaternion endRot = startRot * Quaternion.Euler(10f, 0f, collapseAngle);

            float elapsed = 0f;
            while (elapsed < collapseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / collapseDuration;

                // Courbe EaseInQuad : accélération naturelle (comme une vraie chute)
                float eased = t * t;

                cameraTransform.localPosition = Vector3.Lerp(startPos, endPos, eased);
                cameraTransform.localRotation = Quaternion.Slerp(startRot, endRot, eased);

                yield return null;
            }

            cameraTransform.localPosition = endPos;
            cameraTransform.localRotation = endRot;
        }

        // === PHASE 1.5 : Lancer le flou progressif post-mortem ===
        FireProximityVision deathVision = FindObjectOfType<FireProximityVision>();
        if (deathVision != null)
        {
            deathVision.StartDeathBlur();
            Debug.Log("[PlayerHealth] 🌫️ Flou de mort lancé");
        }

        // === PHASE 2 : Attendre avant la fermeture des yeux ===
        yield return new WaitForSeconds(eyeCloseDelay);

        // === PHASE 3 : Les yeux se ferment (deux barres noires haut/bas) ===
        // Créer le Canvas UI pour l'overlay
        GameObject canvasObj = new GameObject("DeathOverlay_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // Au-dessus de tout
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // --- Barre du HAUT (paupière supérieure) ---
        GameObject topBar = new GameObject("TopEyelid");
        topBar.transform.SetParent(canvasObj.transform, false);
        Image topImage = topBar.AddComponent<Image>();
        topImage.color = Color.black;
        RectTransform topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);  // Ancré en haut
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);    // Pivot en haut
        topRect.sizeDelta = new Vector2(0f, 0f);   // Commence à hauteur 0

        // --- Barre du BAS (paupière inférieure) ---
        GameObject bottomBar = new GameObject("BottomEyelid");
        bottomBar.transform.SetParent(canvasObj.transform, false);
        Image bottomImage = bottomBar.AddComponent<Image>();
        bottomImage.color = Color.black;
        RectTransform bottomRect = bottomBar.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f); // Ancré en bas
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);   // Pivot en bas
        bottomRect.sizeDelta = new Vector2(0f, 0f);  // Commence à hauteur 0

        // Animer les deux barres qui se rejoignent au centre
        float halfScreen = Screen.height / 2f + 20f; // +20 pour s'assurer qu'elles se chevauchent
        float closeElapsed = 0f;

        while (closeElapsed < eyeCloseDuration)
        {
            closeElapsed += Time.deltaTime;
            float t = closeElapsed / eyeCloseDuration;

            // Courbe smooth : lent au début, accélère, puis ralentit à la fin
            float eased = t * t * (3f - 2f * t); // SmoothStep

            float barHeight = Mathf.Lerp(0f, halfScreen, eased);
            topRect.sizeDelta = new Vector2(0f, barHeight);
            bottomRect.sizeDelta = new Vector2(0f, barHeight);

            yield return null;
        }

        // S'assurer que l'écran est complètement noir
        topRect.sizeDelta = new Vector2(0f, halfScreen);
        bottomRect.sizeDelta = new Vector2(0f, halfScreen);

        Debug.Log("[PlayerHealth] 🖤 Écran noir — Game Over");
    }

    /// <summary>
    /// Permet de soigner le joueur depuis un autre script.
    /// </summary>
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    /// <summary>
    /// Permet d'infliger des dégâts depuis un autre script.
    /// </summary>
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        UpdateHealthBar();
        if (currentHealth <= 0f) OnPlayerDeath();
    }
}
