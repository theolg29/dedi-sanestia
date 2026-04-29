using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Singleton gérant l'affichage des sous-titres de dialogue
/// Style basé strictement sur TEXT_STYLES.md (style: DIALOGUE)
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    [Range(0f, 1f)]
    public float globalDialogueVolume = 1f;

    private AudioSource     _dialogueSource;
    private TextMeshProUGUI _subtitleText;
    private Image           _subtitleBg;
    private Coroutine       _dialogueCoroutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        _dialogueSource              = gameObject.AddComponent<AudioSource>();
        _dialogueSource.spatialBlend = 0f;
        _dialogueSource.volume       = globalDialogueVolume;

        BuildUI();
    }

    public void PlayDialogue(DialogueLine[] lines, float delayBeforeStart = 0f, float pauseBetweenLines = 0.5f)
    {
        if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
        _dialogueCoroutine = StartCoroutine(PlayRoutine(lines, delayBeforeStart, pauseBetweenLines));
    }

    private IEnumerator PlayRoutine(DialogueLine[] lines, float delay, float pause)
    {
        if (lines == null || lines.Length == 0) yield break;

        if (delay > 0f) yield return new WaitForSeconds(delay);

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
                yield return FadeSubtitle(0f, 1f, 0.18f); // Fade in spécifié dans TEXT_STYLES
            }

            // Attendre la fin de l'audio, ou 2s par défaut
            yield return new WaitForSeconds(line.clip != null ? line.clip.length : 2f);

            // Attendre la fin du clip si toujours en cours
            while (_dialogueSource.isPlaying) yield return null;

            yield return FadeSubtitle(1f, 0f, 0.18f); // Fade out
            _subtitleText.text = "";

            if (i < lines.Length - 1)
                yield return new WaitForSeconds(pause);
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
        if (_subtitleText != null)
        {
            Color tc = _subtitleText.color; tc.a = alpha; _subtitleText.color = tc;
        }
        if (_subtitleBg != null)
        {
            Color bc = _subtitleBg.color; bc.a = alpha * 0.40f; _subtitleBg.color = bc; // FOND_DIALOGUE = rgba(0,0,0,0.40)
        }
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("DialogueManager_Canvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 203; // Ordre spécifié dans TEXT_STYLES
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        // Background
        GameObject bgObj = new GameObject("SubtitleBg");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _subtitleBg = bgObj.AddComponent<Image>();
        _subtitleBg.color = new Color(0f, 0f, 0f, 0f);
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0.2f, 0.04f); // Spécifié dans TEXT_STYLES
        bgR.anchorMax = new Vector2(0.8f, 0.10f);
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;

        // Texte
        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(bgObj.transform, false);
        _subtitleText = textObj.AddComponent<TextMeshProUGUI>();
        _subtitleText.fontSize         = 19; // Spécifié dans TEXT_STYLES
        _subtitleText.characterSpacing = 0.5f; // Spécifié dans TEXT_STYLES
        _subtitleText.alignment        = TextAlignmentOptions.Center;
        _subtitleText.color            = new Color(1f, 1f, 1f, 0f);
        _subtitleText.text             = "";
        
        RectTransform tR = textObj.GetComponent<RectTransform>();
        tR.anchorMin = Vector2.zero;
        tR.anchorMax = Vector2.one;
        tR.offsetMin = new Vector2(6f, 3f);  // Padding 6px H, 3px V
        tR.offsetMax = new Vector2(-6f, -3f);
    }
}
