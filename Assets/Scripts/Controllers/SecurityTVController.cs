using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class SecurityTVController : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip switchSound;
    [Range(0f, 1f)]
    public float switchVolume = 1f;

    [Header("Transition")]
    [Tooltip("Durée du grésillement entre deux caméras")]
    public float glitchDuration = 0.35f;

    [Header("Flux caméras")]
    public RenderTexture[] cameraFeeds;
    [Tooltip("Index de la caméra défaillante (0-based, ex: 3 pour cam 4/4)")]
    public int brokenCameraIndex = 3;

    [Header("Écran TV")]
    public Renderer screenRenderer;
    [Tooltip("Propriété du matériau de l'écran (URP Lit = _BaseMap)")]
    public string textureProperty  = "_BaseMap";
    public string emissionProperty = "_EmissionMap";

    [Header("UI (optionnel)")]
    public TextMeshProUGUI cameraLabel;

    [Header("Objectif")]
    [Tooltip("Message affiché quand le joueur découvre la caméra défaillante")]
    public string objectiveMessage = "OBJECTIF - Aller vérifier la caméra défaillante";
    public float  objectiveDuration = 4f;

    [Header("Dialogue — 1ère interaction")]
    public DialogueLine[] firstInteractionDialogue;

    [Header("Dialogue — Caméra défaillante")]
    public DialogueLine[] brokenCameraDialogue;

    [Range(0f, 1f)]
    public float dialogueVolume    = 1f;
    public float pauseBetweenLines = 0.5f;

    public int CurrentIndex  => _currentIndex;
    public bool IsBrokenCamera => _currentIndex == brokenCameraIndex;

    private int        _currentIndex = 0;
    private Texture2D  _staticTex;
    private Material   _screenMat;
    private Coroutine  _staticCoroutine;
    private Coroutine  _transitionCoroutine;
    private float      _lastInteractTime   = -1f;
    private bool       _firstInteractDone  = false;
    private bool       _objectiveShown     = false;

    private TextMeshProUGUI _objectiveText;
    private Coroutine       _objectiveCoroutine;

    private TextMeshProUGUI _subtitleText;
    private Image           _subtitleBg;
    private AudioSource     _dialogueSource;
    private Coroutine       _dialogueCoroutine;

    void Start()
    {
        _staticTex = GenerateStatic(256, 144);

        if (screenRenderer != null)
        {
            _screenMat = screenRenderer.material;
            _screenMat.EnableKeyword("_EMISSION");
            _screenMat.SetColor("_EmissionColor", Color.white);
        }

        _dialogueSource              = gameObject.AddComponent<AudioSource>();
        _dialogueSource.spatialBlend = 0f;
        _dialogueSource.volume       = dialogueVolume;

        BuildUI();
        UpdateScreen();
    }

    public void CutPower()
    {
        if (_staticCoroutine     != null) StopCoroutine(_staticCoroutine);
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);

        if (screenRenderer != null)
            screenRenderer.gameObject.SetActive(false);
    }

    public void Interact()
    {
        if (Time.time - _lastInteractTime < 0.3f) return;
        _lastInteractTime = Time.time;

        if (!_firstInteractDone)
        {
            _firstInteractDone = true;
            if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = StartCoroutine(PlayDialogue(firstInteractionDialogue));
        }

        _currentIndex = (_currentIndex + 1) % cameraFeeds.Length;
        if (switchSound != null)
            AudioSource.PlayClipAtPoint(switchSound, transform.position, switchVolume);

        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(GlitchTransition());
    }

    private IEnumerator GlitchTransition()
    {
        if (_staticCoroutine != null) { StopCoroutine(_staticCoroutine); _staticCoroutine = null; }

        float elapsed = 0f;
        while (elapsed < glitchDuration)
        {
            elapsed += Time.deltaTime;
            RefreshStatic();

            float brightness = Random.value > 0.35f ? Random.Range(0.7f, 1.3f) : Random.Range(0f, 0.2f);
            if (_screenMat.HasProperty("_EmissionColor"))
                _screenMat.SetColor("_EmissionColor", Color.white * brightness);

            yield return null;
        }

        if (_screenMat.HasProperty("_EmissionColor"))
            _screenMat.SetColor("_EmissionColor", Color.white);

        UpdateScreen();
    }

    private void UpdateScreen()
    {
        if (_screenMat == null) return;

        if (_staticCoroutine != null)
        {
            StopCoroutine(_staticCoroutine);
            _staticCoroutine = null;
        }

        bool broken = IsBrokenCamera;
        Texture tex = broken ? (Texture)_staticTex : (Texture)cameraFeeds[_currentIndex];
        ApplyTexture(tex);

        if (broken)
        {
            _staticCoroutine = StartCoroutine(AnimateStatic());
            if (!_objectiveShown)
            {
                _objectiveShown = true;
                if (_objectiveCoroutine != null) StopCoroutine(_objectiveCoroutine);
                _objectiveCoroutine = StartCoroutine(ShowObjective());

                if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
                _dialogueCoroutine = StartCoroutine(PlayDialogue(brokenCameraDialogue));
            }
        }

        if (cameraLabel != null)
            cameraLabel.text = broken ? "CAM - SIGNAL PERDU" : $"CAM {_currentIndex + 1}";
    }

    // ─── Dialogue ───────────────────────────────────────────────────────────

    private IEnumerator PlayDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0) yield break;

        for (int i = 0; i < lines.Length; i++)
        {
            DialogueLine line = lines[i];

            if (line.clip != null)
            {
                _dialogueSource.clip = line.clip;
                _dialogueSource.Play();
            }

            if (!string.IsNullOrEmpty(line.subtitle))
            {
                _subtitleText.text = line.subtitle;
                yield return FadeSubtitle(0f, 1f, 0.18f);
            }

            yield return new WaitForSeconds(line.clip != null ? line.clip.length : 2f);

            yield return FadeSubtitle(1f, 0f, 0.18f);
            _subtitleText.text = "";

            if (i < lines.Length - 1)
                yield return new WaitForSeconds(pauseBetweenLines);
        }
    }

    private IEnumerator FadeSubtitle(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetSubtitleAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetSubtitleAlpha(to);
    }

    private void SetSubtitleAlpha(float alpha)
    {
        Color tc = _subtitleText.color; tc.a = alpha;        _subtitleText.color = tc;
        Color bc = _subtitleBg.color;   bc.a = alpha * 0.4f; _subtitleBg.color   = bc;
    }

    // ─── Écran ──────────────────────────────────────────────────────────────

    private void ApplyTexture(Texture tex)
    {
        if (_screenMat.HasProperty(textureProperty))
            _screenMat.SetTexture(textureProperty, tex);
        if (_screenMat.HasProperty(emissionProperty))
            _screenMat.SetTexture(emissionProperty, tex);
    }

    private IEnumerator AnimateStatic()
    {
        while (true)
        {
            RefreshStatic();
            yield return new WaitForSeconds(0.07f);
        }
    }

    private void RefreshStatic()
    {
        Color32[] pixels = _staticTex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            byte v = (byte)Random.Range(0, 255);
            pixels[i] = new Color32(v, v, v, 255);
        }
        _staticTex.SetPixels32(pixels);
        _staticTex.Apply(false);
        ApplyTexture(_staticTex);
    }

    // ─── UI ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowObjective()
    {
        if (_objectiveText == null) yield break;
        _objectiveText.transform.parent.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            SetObjectiveAlpha(elapsed / 0.4f);
            yield return null;
        }
        SetObjectiveAlpha(1f);
        yield return new WaitForSeconds(objectiveDuration);
        elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            SetObjectiveAlpha(1f - elapsed / 0.6f);
            yield return null;
        }
        _objectiveText.transform.parent.gameObject.SetActive(false);
    }

    private void SetObjectiveAlpha(float a)
    {
        if (_objectiveText == null) return;
        Color tc = _objectiveText.color; tc.a = a; _objectiveText.color = tc;
        Image bg = _objectiveText.transform.parent.GetComponent<Image>();
        if (bg != null) { Color bc = bg.color; bc.a = 0.65f * a; bg.color = bc; }
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("SecurityTV_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 202;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        // Objectif — haut
        GameObject panel = new GameObject("ObjectivePanel");
        panel.transform.SetParent(canvasObj.transform, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin        = new Vector2(0.5f, 1f);
        pr.anchorMax        = new Vector2(0.5f, 1f);
        pr.pivot            = new Vector2(0.5f, 1f);
        pr.anchoredPosition = new Vector2(0f, -60f);
        pr.sizeDelta        = new Vector2(480f, 44f);

        GameObject textObj = new GameObject("ObjectiveText");
        textObj.transform.SetParent(panel.transform, false);
        _objectiveText = textObj.AddComponent<TextMeshProUGUI>();
        _objectiveText.fontSize         = 17;
        _objectiveText.fontStyle        = FontStyles.Bold;
        _objectiveText.characterSpacing = 1.5f;
        _objectiveText.alignment        = TextAlignmentOptions.Center;
        _objectiveText.color            = new Color(1f, 0.85f, 0.2f, 0f);
        _objectiveText.text             = objectiveMessage;
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(8f, 4f);
        tr.offsetMax = new Vector2(-8f, -4f);
        panel.SetActive(false);

        // Sous-titres — bas
        GameObject bgObj = new GameObject("SubtitleBg");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _subtitleBg = bgObj.AddComponent<Image>();
        _subtitleBg.color = new Color(0f, 0f, 0f, 0f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0.2f, 0.04f);
        bgR.anchorMax = new Vector2(0.8f, 0.10f);
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;

        GameObject subTextObj = new GameObject("SubtitleText");
        subTextObj.transform.SetParent(bgObj.transform, false);
        _subtitleText = subTextObj.AddComponent<TextMeshProUGUI>();
        _subtitleText.fontSize         = 19;
        _subtitleText.characterSpacing = 0.5f;
        _subtitleText.alignment        = TextAlignmentOptions.Center;
        _subtitleText.color            = new Color(1f, 1f, 1f, 0f);
        _subtitleText.text             = "";
        RectTransform stR = subTextObj.GetComponent<RectTransform>();
        stR.anchorMin = Vector2.zero;
        stR.anchorMax = Vector2.one;
        stR.offsetMin = new Vector2(6f, 3f);
        stR.offsetMax = new Vector2(-6f, -3f);
    }

    private Texture2D GenerateStatic(int w, int h)
    {
        var tex    = new Texture2D(w, h, TextureFormat.RGB24, false);
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte v = (byte)Random.Range(0, 255);
            pixels[i] = new Color32(v, v, v, 255);
        }
        tex.SetPixels32(pixels);
        tex.Apply(false);
        return tex;
    }
}
