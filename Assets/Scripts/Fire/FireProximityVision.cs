using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class FireProximityVision : MonoBehaviour
{
    [Header("Distances")]
    public float maxEffectDistance  = 7f;
    public float fullEffectDistance = 1.5f;

    [Header("Teinte Orange (Overlay UI)")]
    public Color orangeOverlayColor = new Color(1f, 0.4f, 0f, 0.35f);

    [Header("Flou (Depth of Field)")]
    public bool  enableBlur    = true;
    public float blurNearStart = 0.1f;
    public float blurNearEnd   = 3f;
    public float blurMaxRadius = 1f;

    [Header("Vignette")]
    public float maxVignetteIntensity = 0.5f;
    public Color vignetteColor        = new Color(0.8f, 0.3f, 0f, 1f);

    [Header("Flou de Mort")]
    public float deathBlurDuration     = 2.5f;
    public float deathBlurMaxIntensity = 1f;

    [Header("Transition")]
    public float smoothSpeed = 3f;

    [Header("Cellule (doit correspondre à Fire_propagation)")]
    public float fireCellSize = 1f;

    private Canvas      overlayCanvas;
    private Image       overlayImage;
    private CanvasGroup overlayCanvasGroup;

    private Volume       volume;
    private VolumeProfile profile;
    private DepthOfField  depthOfField;
    private Vignette      vignette;
    private bool          postProcessingAvailable = false;

    private float currentIntensity = 0f;
    private bool  isLocked         = false;

    void Start()
    {
        CreateUIOverlay();
        TrySetupPostProcessing();
    }

    private void CreateUIOverlay()
    {
        // Canvas must NOT be parented to the player — WakeUpCinematic disables all child MonoBehaviours on death
        GameObject canvasObj = new GameObject("FireVision_Canvas");
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 900;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        GameObject imgObj = new GameObject("FireVision_Overlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        overlayImage              = imgObj.AddComponent<Image>();
        overlayImage.color        = orangeOverlayColor;
        overlayImage.raycastTarget = false;

        RectTransform rect = imgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayCanvasGroup                = canvasObj.AddComponent<CanvasGroup>();
        overlayCanvasGroup.alpha          = 0f;
        overlayCanvasGroup.blocksRaycasts = false;
        overlayCanvasGroup.interactable   = false;
    }

    private void TrySetupPostProcessing()
    {
        Camera cam = GetComponent<Camera>() ?? GetComponentInChildren<Camera>() ?? Camera.main;
        if (cam != null)
        {
            var cameraData = cam.GetUniversalAdditionalCameraData();
            if (cameraData != null) cameraData.renderPostProcessing = true;
        }

        volume          = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100;
        volume.weight   = 0f;

        profile        = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        if (enableBlur)
        {
            depthOfField = profile.Add<DepthOfField>(false);
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(100f);
            depthOfField.gaussianEnd.Override(200f);
            depthOfField.gaussianMaxRadius.Override(0f);
        }

        vignette = profile.Add<Vignette>(false);
        vignette.active = true;
        vignette.intensity.Override(0f);
        vignette.color.Override(vignetteColor);
        vignette.smoothness.Override(0.4f);

        postProcessingAvailable = (cam != null);
    }

    void Update()
    {
        if (isLocked) return;

        if (Fire_propagation.ActiveFireCount == 0)
        {
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * smoothSpeed);
            ApplyEffect(currentIntensity);
            return;
        }

        float distToFire      = Fire_propagation.GetClosestFireDistance(transform.position, fireCellSize);
        float targetIntensity = 0f;

        if (distToFire <= fullEffectDistance)
        {
            targetIntensity = 1f;
        }
        else if (distToFire < maxEffectDistance)
        {
            float t = 1f - Mathf.InverseLerp(fullEffectDistance, maxEffectDistance, distToFire);
            targetIntensity = t * t;
        }

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);
        ApplyEffect(currentIntensity);
    }

    private void ApplyEffect(float intensity)
    {
        if (overlayCanvasGroup != null)
            overlayCanvasGroup.alpha = intensity;

        if (volume != null)
        {
            volume.weight = intensity > 0.001f ? 1f : 0f;

            if (depthOfField != null)
            {
                depthOfField.gaussianStart.Override(Mathf.Lerp(100f, blurNearStart, intensity));
                depthOfField.gaussianEnd.Override(Mathf.Lerp(200f, blurNearEnd, intensity));
                depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0f, blurMaxRadius, intensity));
            }

            if (vignette != null)
            {
                vignette.intensity.Override(Mathf.Lerp(0f, maxVignetteIntensity, intensity));
                vignette.color.Override(vignetteColor);
            }
        }
    }

    public void LockEffect()
    {
        isLocked = true;
    }

    public void StartDeathBlur()
    {
        isLocked = true;

        if (depthOfField == null && profile != null)
        {
            depthOfField = profile.Add<DepthOfField>(false);
            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(100f);
            depthOfField.gaussianEnd.Override(200f);
            depthOfField.gaussianMaxRadius.Override(0f);
        }

        StartCoroutine(DeathBlurCoroutine());
    }

    private IEnumerator DeathBlurCoroutine()
    {
        float startIntensity = currentIntensity;
        float elapsed        = 0f;

        while (elapsed < deathBlurDuration)
        {
            elapsed += Time.deltaTime;
            float t     = elapsed / deathBlurDuration;
            float eased = t * t * (3f - 2f * t);
            ApplyDeathBlur(Mathf.Lerp(startIntensity, deathBlurMaxIntensity, eased));
            yield return null;
        }

        ApplyDeathBlur(deathBlurMaxIntensity);
    }

    private void ApplyDeathBlur(float intensity)
    {
        if (volume == null) return;
        volume.weight = 1f;

        if (depthOfField != null)
        {
            depthOfField.gaussianStart.Override(Mathf.Lerp(100f, 0f, intensity));
            depthOfField.gaussianEnd.Override(Mathf.Lerp(200f, 0.01f, intensity));
            depthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0f, 1.5f, intensity));
        }

        if (vignette != null)
            vignette.intensity.Override(Mathf.Lerp(0f, maxVignetteIntensity, intensity));
    }

    void OnDestroy()
    {
        if (profile != null) DestroyImmediate(profile);
        if (overlayCanvas != null) Destroy(overlayCanvas.gameObject);
    }
}
