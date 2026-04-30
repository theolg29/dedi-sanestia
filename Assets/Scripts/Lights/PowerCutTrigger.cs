using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PowerCutTrigger : MonoBehaviour
{
    [Header("Son de coupure")]
    public AudioClip sonCoupure;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Speakers")]
    [Tooltip("Durée du fade out des speakers")]
    public float speakerFadeDuration = 2f;

    [Header("Dialogue")]
    public DialogueLine[] powerCutDialogue;
    [Range(0f, 1f)]
    public float dialogueVolume    = 1f;
    public float dialogueDelay     = 1f;
    public float pauseBetweenLines = 0.5f;

    [Header("Objectif")]
    public string objectiveMessage  = "OBJECTIF - Dirigez-vous vers la salle électrique";
    public float  objectiveDuration = 4f;

    public static bool PowerIsCut { get; private set; }

    private bool _triggered = false;

    private AudioSource     _dialogueSource;
    private TextMeshProUGUI _subtitleText;
    private Image           _subtitleBg;
    private TextMeshProUGUI _objectiveText;

    void Start()
    {
        _dialogueSource              = gameObject.AddComponent<AudioSource>();
        _dialogueSource.spatialBlend = 0f;
        _dialogueSource.volume       = dialogueVolume;

        BuildUI();
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        PowerIsCut = true;

        if (sonCoupure != null)
            AudioSource.PlayClipAtPoint(sonCoupure, transform.position, volume);

        foreach (FlickeringNeon neon in FindObjectsByType<FlickeringNeon>(FindObjectsSortMode.None))
            neon.SwitchOff();

        foreach (SurveillanceCameraController cam in FindObjectsByType<SurveillanceCameraController>(FindObjectsSortMode.None))
            cam.CutPower();

        foreach (SecurityTVController tv in FindObjectsByType<SecurityTVController>(FindObjectsSortMode.None))
            tv.CutPower();

        foreach (GameObject speaker in GameObject.FindGameObjectsWithTag("Speaker"))
        {
            AudioSource src = speaker.GetComponent<AudioSource>();
            if (src != null) StartCoroutine(FadeSpeaker(src));
        }

        StartCoroutine(PlayDialogue());
        StartCoroutine(ShowObjective());
    }

    // ─── Dialogue ───────────────────────────────────────────────────────────

    private IEnumerator PlayDialogue()
    {
        if (powerCutDialogue == null || powerCutDialogue.Length == 0) yield break;
        yield return new WaitForSeconds(dialogueDelay);

        for (int i = 0; i < powerCutDialogue.Length; i++)
        {
            DialogueLine line = powerCutDialogue[i];

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

            if (i < powerCutDialogue.Length - 1)
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

    // ─── Objectif ───────────────────────────────────────────────────────────

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

    // ─── UI ─────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("PowerCut_Canvas");
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

    // ─── Speaker fade ────────────────────────────────────────────────────────

    private IEnumerator FadeSpeaker(AudioSource src)
    {
        float startVolume = src.volume;
        float elapsed     = 0f;
        while (elapsed < speakerFadeDuration)
        {
            elapsed    += Time.deltaTime;
            src.volume  = Mathf.Lerp(startVolume, 0f, elapsed / speakerFadeDuration);
            yield return null;
        }
        src.Stop();
        src.volume = startVolume;
    }
}
