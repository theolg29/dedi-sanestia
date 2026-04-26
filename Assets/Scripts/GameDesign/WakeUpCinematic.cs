using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public AudioClip clip;
    [TextArea(1, 3)] public string subtitle;
}

public class WakeUpCinematic : MonoBehaviour
{
    [Header("Caméra")]
    [Tooltip("Auto-détectée si vide")]
    public Transform playerCamera;

    [Header("Pose initiale (doit correspondre à PlayerHealth)")]
    public float sleepDropHeight = 0.8f;
    public float sleepAngle      = 80f;

    [Header("Audio")]
    [Tooltip("Clip voix radio — créé sur ElevenLabs")]
    public AudioClip voiceAlert;
    [Range(0f, 1f)]
    public float voiceVolume = 0.85f;
    [Tooltip("Son de bâillement — joué juste avant le premier clignotement")]
    public AudioClip yawnClip;
    [Range(0f, 1f)]
    public float yawnVolume = 1f;

    [Header("Dialogue")]
    public DialogueLine[] dialogueLines;
    [Range(0f, 1f)]
    public float dialogueVolume = 1f;
    public float pauseBetweenLines = 0.5f;

    [Header("Timings")]
    public float silenceAtStart       = 1.0f;
    public float delayAfterVoiceStart = 1.4f;
    public float delayBetweenBlinks   = 0.9f;
    public float delayBeforeFullOpen  = 0.7f;
    public float flashDuration        = 2.0f;
    public float getUpDuration        = 2.2f;

    private Image           _blackOverlay;
    private Image           _flashOverlay;
    private TextMeshProUGUI _subtitleText;
    private Image           _subtitleBg;
    private AudioSource _dialogueSource;

    private Vector3    _origPos;
    private Quaternion _origRot;
    private readonly List<MonoBehaviour> _disabledScripts = new List<MonoBehaviour>();

    void Start()
    {
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogError("[WakeUpCinematic] Aucune caméra trouvée.");
            return;
        }

        _origPos = playerCamera.localPosition;
        _origRot = playerCamera.localRotation;

        playerCamera.localPosition = _origPos + new Vector3(0f, -sleepDropHeight, 0f);
        playerCamera.localRotation = _origRot * Quaternion.Euler(10f, 0f, sleepAngle);

        _dialogueSource              = gameObject.AddComponent<AudioSource>();
        _dialogueSource.spatialBlend = 0f;
        _dialogueSource.volume       = dialogueVolume;

        DisablePlayerControls();
        BuildUI();
        StartCoroutine(WakeUpSequence());
    }

    // ─── Séquence principale ────────────────────────────────────────────────

    private IEnumerator WakeUpSequence()
    {
        yield return new WaitForSeconds(silenceAtStart);

        if (voiceAlert != null)
        {
            AudioSource radio = gameObject.AddComponent<AudioSource>();
            radio.spatialBlend         = 0f;
            radio.ignoreListenerVolume = true;
            radio.volume               = voiceVolume;
            radio.clip                 = voiceAlert;
            radio.Play();
        }

        yield return new WaitForSeconds(delayAfterVoiceStart);

        yield return Blink(targetAlpha: 0.50f, openTime: 0.45f, holdTime: 0.08f, closeTime: 0.35f);
        yield return new WaitForSeconds(delayBetweenBlinks);

        yield return Blink(targetAlpha: 0.80f, openTime: 0.50f, holdTime: 0.10f, closeTime: 0.40f);
        yield return new WaitForSeconds(delayBeforeFullOpen);

        if (yawnClip != null)
            AudioSource.PlayClipAtPoint(yawnClip, playerCamera.position, yawnVolume);
        yield return OpenFull();

        yield return GetUp();

        yield return PlayDialogue();

        EnablePlayerControls();
        Destroy(this);
    }

    // ─── Dialogue ───────────────────────────────────────────────────────────

    private IEnumerator PlayDialogue()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) yield break;

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            DialogueLine line = dialogueLines[i];

            if (line.clip != null)
            {
                _dialogueSource.clip = line.clip;
                _dialogueSource.Play();
            }

            if (!string.IsNullOrEmpty(line.subtitle))
            {
                _subtitleText.text = line.subtitle;
                yield return FadeSubtitle(0f, 1f, 0.2f);
            }

            yield return new WaitForSeconds(line.clip != null ? line.clip.length : 2f);

            yield return FadeSubtitle(1f, 0f, 0.2f);
            _subtitleText.text = "";

            if (i < dialogueLines.Length - 1)
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

    // ─── Coroutines d'animation ─────────────────────────────────────────────

    private IEnumerator Blink(float targetAlpha, float openTime, float holdTime, float closeTime)
    {
        float t = 0f;
        while (t < openTime)
        {
            t += Time.deltaTime;
            SetBlack(Mathf.Lerp(1f, targetAlpha, Mathf.SmoothStep(0f, 1f, t / openTime)));
            yield return null;
        }
        yield return new WaitForSeconds(holdTime);
        t = 0f;
        while (t < closeTime)
        {
            t += Time.deltaTime;
            SetBlack(Mathf.Lerp(targetAlpha, 1f, Mathf.SmoothStep(0f, 1f, t / closeTime)));
            yield return null;
        }
        SetBlack(1f);
    }

    private IEnumerator OpenFull()
    {
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, t / 0.4f);
            SetBlack(1f - eased);
            _flashOverlay.color = new Color(1f, 1f, 1f, eased * 0.90f);
            yield return null;
        }
        SetBlack(0f);

        t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float eased = Mathf.SmoothStep(0f, 1f, t / flashDuration);
            _flashOverlay.color = new Color(1f, 1f, 1f, (1f - eased) * 0.90f);
            yield return null;
        }
        _flashOverlay.color = Color.clear;
    }

    private IEnumerator GetUp()
    {
        Vector3    startPos = playerCamera.localPosition;
        Quaternion startRot = playerCamera.localRotation;

        float t = 0f;
        while (t < getUpDuration)
        {
            t += Time.deltaTime;
            float norm  = t / getUpDuration;
            float eased = 1f - (1f - norm) * (1f - norm);
            float sway  = Mathf.Sin(norm * Mathf.PI * 2.8f) * (1f - norm) * 3f;

            playerCamera.localPosition = Vector3.Lerp(startPos, _origPos, eased);
            playerCamera.localRotation = Quaternion.Slerp(startRot, _origRot, eased)
                                       * Quaternion.Euler(0f, sway, sway * 0.35f);
            yield return null;
        }

        playerCamera.localPosition = _origPos;
        playerCamera.localRotation = _origRot;
    }

    // ─── UI ─────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("WakeUpOverlay_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();

        _blackOverlay = CreateFullscreenImage(canvasObj.transform, "BlackOverlay", Color.black);
        _flashOverlay = CreateFullscreenImage(canvasObj.transform, "FlashOverlay", Color.clear);

        BuildSubtitleUI(canvasObj.transform);
    }

    private void BuildSubtitleUI(Transform canvasParent)
    {
        GameObject bgObj = new GameObject("SubtitleBg");
        bgObj.transform.SetParent(canvasParent, false);
        _subtitleBg = bgObj.AddComponent<Image>();
        _subtitleBg.color = new Color(0f, 0f, 0f, 0f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0.2f, 0.04f);
        bgR.anchorMax = new Vector2(0.8f, 0.10f);
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(bgObj.transform, false);
        _subtitleText = textObj.AddComponent<TextMeshProUGUI>();
        _subtitleText.fontSize         = 19;
        _subtitleText.characterSpacing = 0.5f;
        _subtitleText.alignment        = TextAlignmentOptions.Center;
        _subtitleText.color            = new Color(1f, 1f, 1f, 0f);
        _subtitleText.text             = "";

        RectTransform tR = textObj.GetComponent<RectTransform>();
        tR.anchorMin = Vector2.zero;
        tR.anchorMax = Vector2.one;
        tR.offsetMin = new Vector2(6f, 3f);
        tR.offsetMax = new Vector2(-6f, -3f);
    }

    private static Image CreateFullscreenImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        return img;
    }

    private void SetBlack(float alpha) =>
        _blackOverlay.color = new Color(0f, 0f, 0f, alpha);

    // ─── Contrôles joueur ───────────────────────────────────────────────────

    private void DisablePlayerControls()
    {
        GameObject root = transform.root.gameObject;
        CharacterController cc = root.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        foreach (MonoBehaviour s in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (s != this && s.enabled)
            {
                s.enabled = false;
                _disabledScripts.Add(s);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void EnablePlayerControls()
    {
        GameObject root = transform.root.gameObject;
        CharacterController cc = root.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = true;

        foreach (MonoBehaviour s in _disabledScripts)
            if (s != null) s.enabled = true;

        _disabledScripts.Clear();
    }
}
