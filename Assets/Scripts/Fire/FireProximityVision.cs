using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Effet visuel de proximité au feu.
/// À placer sur le joueur ou la caméra.
/// Quand le joueur s'approche du feu :
///   - Overlay UI orange semi-transparent (GARANTI de fonctionner)
///   - Flou via URP Depth of Field (bonus si Post Processing activé)
///   - Vignette orange
/// </summary>
public class FireProximityVision : MonoBehaviour
{
    [Header("Distances")]
    [Tooltip("Distance à partir de laquelle l'effet commence")]
    public float maxEffectDistance = 15f;

    [Tooltip("Distance à laquelle l'effet est à 100%")]
    public float fullEffectDistance = 2f;

    [Header("Teinte Orange (Overlay UI)")]
    [Tooltip("Couleur de l'overlay orange")]
    public Color orangeOverlayColor = new Color(1f, 0.4f, 0f, 0.35f);

    [Header("Flou (Depth of Field — nécessite Post Processing)")]
    [Tooltip("Activer le flou URP (nécessite Post Processing sur la caméra)")]
    public bool enableBlur = true;
    public float blurNearStart = 0.1f;
    public float blurNearEnd = 3f;
    [Tooltip("Rayon max du flou gaussien (plus = plus flou)")]
    public float blurMaxRadius = 1f;

    [Header("Vignette")]
    public float maxVignetteIntensity = 0.5f;
    public Color vignetteColor = new Color(0.8f, 0.3f, 0f, 1f);

    [Header("Flou de Mort (post-mortem)")]
    [Tooltip("Durée de la montée du flou après la mort (secondes)")]
    public float deathBlurDuration = 2.5f;

    [Tooltip("Intensité max du flou de mort (0-1)")]
    public float deathBlurMaxIntensity = 1f;

    [Header("Transition")]
    public float smoothSpeed = 3f;

    [Header("Cellule (doit correspondre à Fire_propagation)")]
    public float fireCellSize = 1f;

    // --- UI Overlay (garanti) ---
    private Canvas overlayCanvas;
    private Image overlayImage;
    private CanvasGroup overlayCanvasGroup;

    // --- URP Volume (bonus) ---
    private Volume volume;
    private VolumeProfile profile;
    private DepthOfField depthOfField;
    private Vignette vignette;
    private bool postProcessingAvailable = false;

    // État
    private float currentIntensity = 0f;
    private bool isLocked = false; // Si true, l'effet reste figé (mort du joueur)

    void Start()
    {
        // ============================================================
        // MÉTHODE 1 : OVERLAY UI (fonctionne TOUJOURS, sans config)
        // ============================================================
        CreateUIOverlay();

        // ============================================================
        // MÉTHODE 2 : URP VOLUME (flou + vignette, si dispo)
        // ============================================================
        TrySetupPostProcessing();

        Debug.Log("[FireProximityVision] ✅ Initialisé — Overlay UI : OK | Post Processing : " 
            + (postProcessingAvailable ? "✅ Activé" : "⚠️ Non disponible (flou désactivé)"));
    }

    private void CreateUIOverlay()
    {
        // Créer un Canvas ScreenSpace Overlay
        // IMPORTANT : NE PAS parenter au joueur !
        // Sinon OnPlayerDeath désactive les MonoBehaviours enfants (Image, CanvasScaler...)
        GameObject canvasObj = new GameObject("FireVision_Canvas");
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 900; // Au-dessus du jeu, sous les menus de mort
        canvasObj.AddComponent<CanvasScaler>();

        // Image plein écran orange semi-transparente
        GameObject imgObj = new GameObject("FireVision_Overlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        overlayImage = imgObj.AddComponent<Image>();
        overlayImage.color = orangeOverlayColor;
        overlayImage.raycastTarget = false; // Ne bloque pas les clics

        // Étirer sur tout l'écran
        RectTransform rect = imgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // CanvasGroup pour contrôler l'opacité globale
        overlayCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.interactable = false;
    }

    private void TrySetupPostProcessing()
    {
        // Essayer d'activer le Post Processing sur la caméra
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;

        if (cam != null)
        {
            var cameraData = cam.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = true;
            }
        }

        // Créer le Volume URP
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100;
        volume.weight = 0f;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        if (enableBlur)
        {
            depthOfField = profile.Add<DepthOfField>(false);
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(100f);
            depthOfField.gaussianEnd.Override(200f);
            depthOfField.gaussianMaxRadius.Override(0f); // Commence sans flou
        }

        vignette = profile.Add<Vignette>(false);
        vignette.active = true;
        vignette.intensity.Override(0f);
        vignette.color.Override(vignetteColor);
        vignette.smoothness.Override(0.4f);

        // Tester si le post-processing fonctionne réellement
        // (on le considère "disponible" s'il y a une caméra avec les données URP)
        postProcessingAvailable = (cam != null);
    }

    void Update()
    {
        // Si l'effet est figé (joueur mort), ne plus rien changer
        if (isLocked) return;

        // Pas de feu actif = pas d'effet
        if (Fire_propagation.ActiveFireCount == 0)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * smoothSpeed);
            ApplyEffect(currentIntensity);
            return;
        }

        // Distance au feu le plus proche
        float distToFire = Fire_propagation.GetClosestFireDistance(transform.position, fireCellSize);

        // Intensité cible (0 = loin, 1 = dans le feu)
        float targetIntensity = 0f;
        if (distToFire <= fullEffectDistance)
        {
            targetIntensity = 1f;
        }
        else if (distToFire < maxEffectDistance)
        {
            targetIntensity = 1f - Mathf.InverseLerp(fullEffectDistance, maxEffectDistance, distToFire);
            targetIntensity = targetIntensity * targetIntensity; // Courbe quadratique
        }

        // Lissage
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);

        ApplyEffect(currentIntensity);
    }

    private void ApplyEffect(float intensity)
    {
        // === OVERLAY UI (toujours visible) ===
        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = intensity;
        }

        // === URP VOLUME (flou + vignette, bonus) ===
        if (volume != null)
        {
            volume.weight = intensity > 0.001f ? 1f : 0f;

            if (depthOfField != null)
            {
                float gaussStart = Mathf.Lerp(100f, blurNearStart, intensity);
                float gaussEnd = Mathf.Lerp(200f, blurNearEnd, intensity);
                depthOfField.gaussianStart.Override(gaussStart);
                depthOfField.gaussianEnd.Override(gaussEnd);
                depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0f, blurMaxRadius, intensity));
            }

            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Lerp(0f, maxVignetteIntensity, intensity));
                vignette.color.Override(vignetteColor);
            }
        }
    }

    /// <summary>
    /// Fige l'effet à son intensité actuelle (appelé par PlayerHealth à la mort).
    /// L'overlay orange et le flou restent visibles indéfiniment.
    /// </summary>
    public void LockEffect()
    {
        isLocked = true;
        Debug.Log("[FireProximityVision] 🔒 Effet figé à intensité : " + currentIntensity.ToString("F2"));
    }

    /// <summary>
    /// Lance le flou progressif post-mortem.
    /// Le flou augmente graduellement de l'intensité actuelle jusqu'au maximum.
    /// L'overlay orange reste figé, seul le flou change.
    /// </summary>
    public void StartDeathBlur()
    {
        isLocked = true; // Empêcher l'Update de modifier quoi que ce soit

        // Créer le DepthOfField s'il n'existait pas (enableBlur décoché)
        if (depthOfField == null && profile != null)
        {
            depthOfField = profile.Add<DepthOfField>(false);
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(100f);
            depthOfField.gaussianEnd.Override(200f);
            depthOfField.gaussianMaxRadius.Override(0f);
            Debug.Log("[FireProximityVision] 🌫️ DepthOfField créé pour le flou de mort");
        }

        StartCoroutine(DeathBlurCoroutine());
    }

    private IEnumerator DeathBlurCoroutine()
    {
        Debug.Log("[FireProximityVision] 🌫️ Début du flou de mort progressif");

        float startIntensity = currentIntensity;
        float elapsed = 0f;

        while (elapsed < deathBlurDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathBlurDuration;

            // Courbe SmoothStep pour une progression naturelle
            float eased = t * t * (3f - 2f * t);

            float blurIntensity = Mathf.Lerp(startIntensity, deathBlurMaxIntensity, eased);

            // Appliquer UNIQUEMENT le flou (pas l'overlay orange)
            ApplyDeathBlur(blurIntensity);

            yield return null;
        }

        // S'assurer qu'on atteint le maximum
        ApplyDeathBlur(deathBlurMaxIntensity);
        Debug.Log("[FireProximityVision] 🌫️ Flou de mort au maximum");
    }

    private void ApplyDeathBlur(float intensity)
    {
        if (volume == null) return;

        volume.weight = 1f;

        if (depthOfField != null)
        {
            // Forcer le flou sur TOUT (near = très proche, end = très loin)
            depthOfField.gaussianStart.Override(Mathf.Lerp(100f, 0f, intensity));
            depthOfField.gaussianEnd.Override(Mathf.Lerp(200f, 0.01f, intensity));
            // gaussianMaxRadius contrôle la FORCE réelle du flou
            depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0f, 1.5f, intensity));
        }

        if (vignette != null)
        {
            vignette.intensity.Override(Mathf.Lerp(0f, maxVignetteIntensity, intensity));
        }
    }

    void OnDestroy()
    {
        if (profile != null)
        {
            DestroyImmediate(profile);
        }
        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
        }
    }
}
