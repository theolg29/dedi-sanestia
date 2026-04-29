using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float drainRate  = 25f;   // 100 / 25 = 4 seconds of sprint
    public float regenRate  = 15f;

    [Header("Exhaustion")]
    public float exhaustionThreshold = 0.2f; // Must regen to 20% before sprinting again

    [Header("Barre de stamina (taille)")]
    public float barWidth  = 200f;
    public float barHeight = 10f;

    [Header("Fade")]
    public float fadeOutDuration = 2f;

    public bool canSprint   = true;
    public bool IsSprinting { get; private set; }

    private float currentStamina;
    private bool  isExhausted = false;

    // Programmatic UI
    private Canvas      staminaCanvas;
    private CanvasGroup staminaCanvasGroup;
    private Image       staminaBarFill;

    private bool  wasSprinting    = false;
    private Coroutine fadeCoroutine;

    void Awake() => instance = this;

    void Start()
    {
        currentStamina = maxStamina;
        CreateStaminaBar();

        // Disable built-in sprint bar on FirstPersonController if present
        FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
        if (fpc != null)
        {
            fpc.useSprintBar = false;
            if (fpc.sprintBarBG != null) fpc.sprintBarBG.gameObject.SetActive(false);
            if (fpc.sprintBar   != null) fpc.sprintBar.gameObject.SetActive(false);
        }
    }

    private void CreateStaminaBar()
    {
        // Root canvas
        GameObject canvasObj = new GameObject("StaminaBar_Canvas");
        staminaCanvas = canvasObj.AddComponent<Canvas>();
        staminaCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        staminaCanvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();

        staminaCanvasGroup                = canvasObj.AddComponent<CanvasGroup>();
        staminaCanvasGroup.alpha          = 0f; // Starts invisible
        staminaCanvasGroup.blocksRaycasts = false;
        staminaCanvasGroup.interactable   = false;

        // Background (dark semi-transparent)
        GameObject bgObj = new GameObject("StaminaBar_BG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage      = bgObj.AddComponent<Image>();
        bgImage.color        = new Color(0f, 0f, 0f, 0.5f);
        bgImage.raycastTarget = false;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0.5f, 0f);
        bgRect.anchorMax        = new Vector2(0.5f, 0f);
        bgRect.pivot            = new Vector2(0.5f, 0f);
        bgRect.anchoredPosition = new Vector2(0f, 32f); // 32px from bottom
        bgRect.sizeDelta        = new Vector2(barWidth, barHeight);

        // Fill (cyan/blue tint)
        GameObject fillObj = new GameObject("StaminaBar_Fill");
        fillObj.transform.SetParent(bgObj.transform, false);
        staminaBarFill              = fillObj.AddComponent<Image>();
        staminaBarFill.color        = new Color(0.3f, 0.7f, 1f, 1f); // Light blue
        staminaBarFill.raycastTarget = false;
        staminaBarFill.type         = Image.Type.Filled;
        staminaBarFill.fillMethod   = Image.FillMethod.Horizontal;
        staminaBarFill.fillOrigin   = (int)Image.OriginHorizontal.Left;
        staminaBarFill.fillAmount   = 1f;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);   // 1px inner padding
        fillRect.offsetMax = new Vector2(-1f, -1f);
    }

    void Update()
    {
        // Exhaustion logic
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

        // Detect sprinting
        bool moving = Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0;
        IsSprinting = Input.GetKey(KeyCode.LeftShift) && canSprint && moving;

        // Drain or regen
        currentStamina += (IsSprinting ? -drainRate : regenRate) * Time.deltaTime;
        currentStamina  = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // Update fill
        if (staminaBarFill != null)
            staminaBarFill.fillAmount = currentStamina / maxStamina;

        // Visibility logic
        if (IsSprinting && !wasSprinting)
        {
            // Started sprinting — show instantly
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            staminaCanvasGroup.alpha = 1f;
        }
        else if (!IsSprinting && wasSprinting)
        {
            // Stopped sprinting — fade out over 2 seconds
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
