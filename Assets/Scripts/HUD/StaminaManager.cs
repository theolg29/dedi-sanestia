using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float drainRate  = 25f;
    public float regenRate  = 15f;

    [Header("Exhaustion")]
    public float exhaustionThreshold = 0.2f;

    [Header("Barre de stamina (taille)")]
    public float barWidth  = 220f;
    public float barHeight = 16f;

    [Header("Fade")]
    public float fadeOutDuration = 2f;

    public bool canSprint   = true;
    public bool IsSprinting { get; private set; }

    private float currentStamina;
    private bool  isExhausted = false;

    private Canvas        staminaCanvas;
    private CanvasGroup   staminaCanvasGroup;
    private Image         staminaBarFill;
    private RectTransform staminaFillRect;

    private bool      wasSprinting = false;
    private Coroutine fadeCoroutine;
    private Rigidbody playerRb;

    void Awake() => instance = this;

    void Start()
    {
        currentStamina = maxStamina;

        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.useSprintBar = false;
            if (fpc.sprintBarBG != null) fpc.sprintBarBG.gameObject.SetActive(false);
            if (fpc.sprintBar   != null) fpc.sprintBar.gameObject.SetActive(false);
            playerRb = fpc.GetComponent<Rigidbody>();
        }

        CreateStaminaBar();
    }

    private void CreateStaminaBar()
    {
        float panelW = barWidth + 20f;
        float panelH = 52f;

        GameObject canvasObj = new GameObject("StaminaBar_Canvas");
        staminaCanvas = canvasObj.AddComponent<Canvas>();
        staminaCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        staminaCanvas.sortingOrder = 100;
        CanvasScaler staminaScaler = canvasObj.AddComponent<CanvasScaler>();
        staminaScaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        staminaScaler.referenceResolution = new Vector2(1280, 720);
        staminaScaler.matchWidthOrHeight  = 0.5f;

        staminaCanvasGroup                = canvasObj.AddComponent<CanvasGroup>();
        staminaCanvasGroup.alpha          = 0f;
        staminaCanvasGroup.blocksRaycasts = false;
        staminaCanvasGroup.interactable   = false;

        // Panel conteneur
        GameObject panelObj    = new GameObject("StaminaBar_Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImg          = panelObj.AddComponent<Image>();
        panelImg.color          = new Color(0.04f, 0.04f, 0.04f, 0.78f);
        panelImg.raycastTarget  = false;
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0.5f, 0f);
        panelRect.anchorMax        = new Vector2(0.5f, 0f);
        panelRect.pivot            = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 20f);
        panelRect.sizeDelta        = new Vector2(panelW, panelH);

        // Label "SPRINT"
        GameObject labelObj    = new GameObject("StaminaBar_Label");
        labelObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI label  = labelObj.AddComponent<TextMeshProUGUI>();
        label.text             = "SPRINT";
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
        GameObject bgObj   = new GameObject("StaminaBar_BG");
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
        GameObject fillObj     = new GameObject("StaminaBar_Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        staminaBarFill              = fillObj.AddComponent<Image>();
        staminaBarFill.color        = new Color(0.15f, 0.75f, 1f, 1f);
        staminaBarFill.raycastTarget = false;
        staminaFillRect             = fillObj.GetComponent<RectTransform>();
        staminaFillRect.anchorMin        = new Vector2(0f, 0f);
        staminaFillRect.anchorMax        = new Vector2(0f, 0f);
        staminaFillRect.pivot            = new Vector2(0f, 0f);
        staminaFillRect.anchoredPosition = new Vector2(2f, 2f);
        staminaFillRect.sizeDelta        = new Vector2(barWidth - 4f, barHeight - 4f);
    }

    void Update()
    {
        // Exhaustion
        if (currentStamina <= 0f && !isExhausted)
        {
            isExhausted = true;
            canSprint   = false;
        }

        if (isExhausted)
        {
            canSprint = false;
            if (currentStamina >= maxStamina * exhaustionThreshold)
                isExhausted = false;
        }
        else if (currentStamina >= maxStamina * exhaustionThreshold)
        {
            canSprint = true;
        }

        // Detection du mouvement via le Rigidbody du FPC
        bool moving;
        if (playerRb != null)
        {
            Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
            moving = flatVel.magnitude > 0.4f;
        }
        else
        {
            moving = Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f;
        }

        IsSprinting = Input.GetKey(KeyCode.LeftShift) && canSprint && moving;

        // Drain ou regen
        currentStamina += (IsSprinting ? -drainRate : regenRate) * Time.deltaTime;
        currentStamina  = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // Mise a jour visuelle
        if (staminaFillRect != null)
        {
            float ratio = currentStamina / maxStamina;
            staminaFillRect.sizeDelta = new Vector2(Mathf.Max(0f, (barWidth - 4f) * ratio), barHeight - 4f);

            // Bleu -> orange quand stamina est basse
            staminaBarFill.color = ratio < 0.3f
                ? Color.Lerp(new Color(1f, 0.35f, 0.1f, 1f), new Color(0.15f, 0.75f, 1f, 1f), ratio / 0.3f)
                : new Color(0.15f, 0.75f, 1f, 1f);
        }

        // Visibilite (fade)
        if (IsSprinting && !wasSprinting)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            staminaCanvasGroup.alpha = 1f;
        }
        else if (!IsSprinting && wasSprinting)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOut());
        }

        wasSprinting = IsSprinting;
    }

    private IEnumerator FadeOut()
    {
        float startAlpha = staminaCanvasGroup.alpha;
        float elapsed    = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            staminaCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        staminaCanvasGroup.alpha = 0f;
        fadeCoroutine = null;
    }

    void OnDestroy()
    {
        if (staminaCanvas != null)
            Destroy(staminaCanvas.gameObject);
    }
}
