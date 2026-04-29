using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public GameObject requiredItem;

    [Header("Son")]
    public AudioClip sonOuverture;
    public AudioClip sonVerrouille;

    [Header("Son d'interaction")]
    [Tooltip("Son joué après interaction avec la porte (avec délai)")]
    public AudioClip sonInteraction;

    [Tooltip("Délai en secondes avant de jouer le son d'interaction")]
    public float delaiSonInteraction = 1f;

    [TextArea(1, 4)]
    [Tooltip("Sous-titres affichés pendant le son d'interaction")]
    public string sousTitresInteraction;

    private const float openAngle    = 90f;
    private const float animDuration = 0.6f;

    // Style DIALOGUE — TEXT_STYLES.md
    private const float SUBTITLE_FONT_SIZE       = 19f;
    private const float SUBTITLE_CHAR_SPACING    = 0.5f;
    private const float SUBTITLE_FADE_DURATION   = 0.18f;
    private const int   SUBTITLE_SORT_ORDER      = 203;

    private bool isOpen      = false;
    private bool isAnimating = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private AudioSource     audioSource;
    private AudioSource     interactionAudioSource;
    private TextMeshProUGUI _subtitleText;
    private Image           _subtitleBg;
    private Coroutine       _interactionCoroutine;
    private bool            _interactionPlayed;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation   = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        audioSource    = GetComponent<AudioSource>();

        // AudioSource dédié au son d'interaction (2D, non-spatialisé)
        interactionAudioSource              = gameObject.AddComponent<AudioSource>();
        interactionAudioSource.spatialBlend = 0f;
        interactionAudioSource.playOnAwake  = false;

        if (sonInteraction != null && !string.IsNullOrEmpty(sousTitresInteraction))
            BuildSubtitleUI();
    }

    public bool IsOpen => isOpen;
    public bool IsLocked => requiredItem != null && PlayerInventory.instance?.GetItem() != requiredItem.name;

    public bool TryToggle()
    {
        if (isAnimating) return false;

        // Son d'interaction joué à chaque pression de E
        LancerSonInteraction();

        if (isOpen)
        {
            isOpen = false;
            JouerSon();
            StartCoroutine(AnimateRotation(openRotation, closedRotation));
            return true;
        }

        if (requiredItem == null)
        {
            Ouvrir();
            return true;
        }

        if (PlayerInventory.instance == null) return false;

        if (PlayerInventory.instance.GetItem() == requiredItem.name)
        {
            Ouvrir();
            return true;
        }

        return false;
    }

    private void Ouvrir()
    {
        isOpen = true;
        JouerSon();
        StartCoroutine(AnimateRotation(closedRotation, openRotation));
    }

    // ─── Son d'interaction + sous-titres (style DIALOGUE) ───────────────────

    private void LancerSonInteraction()
    {
        if (sonInteraction == null || _interactionPlayed) return;
        _interactionPlayed = true;

        if (_interactionCoroutine != null)
            StopCoroutine(_interactionCoroutine);

        _interactionCoroutine = StartCoroutine(JouerSonInteractionRoutine());
    }

    private IEnumerator JouerSonInteractionRoutine()
    {
        // Délai avant de jouer le son
        if (delaiSonInteraction > 0f)
            yield return new WaitForSeconds(delaiSonInteraction);

        // Jouer le son
        interactionAudioSource.PlayOneShot(sonInteraction);

        // Afficher les sous-titres si texte renseigné
        bool hasSubs = !string.IsNullOrEmpty(sousTitresInteraction) && _subtitleText != null;

        if (hasSubs)
        {
            _subtitleText.text = sousTitresInteraction;
            yield return FadeSubtitle(0f, 1f, SUBTITLE_FADE_DURATION);
        }

        // Attendre la fin du clip audio
        yield return new WaitForSeconds(sonInteraction.length);

        // Fade out des sous-titres
        if (hasSubs)
        {
            yield return FadeSubtitle(1f, 0f, SUBTITLE_FADE_DURATION);
            _subtitleText.text = "";
        }
    }

    // ─── Fade sous-titres (TEXT_STYLES: fade 0.18s) ─────────────────────────

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
            // FOND_DIALOGUE = rgba(0,0,0,0.40)
            Color bc = _subtitleBg.color; bc.a = alpha * 0.40f; _subtitleBg.color = bc;
        }
    }

    // ─── Construction UI sous-titres (style DIALOGUE — TEXT_STYLES.md) ──────

    private void BuildSubtitleUI()
    {
        // Canvas ScreenSpaceOverlay, sortingOrder 203
        GameObject canvasObj = new GameObject("DoorDialogue_Canvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas       = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SUBTITLE_SORT_ORDER;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        // Fond — ancres (0.2, 0.04) → (0.8, 0.10)
        GameObject bgObj = new GameObject("SubtitleBg");
        bgObj.transform.SetParent(canvasObj.transform, false);
        _subtitleBg       = bgObj.AddComponent<Image>();
        _subtitleBg.color = new Color(0f, 0f, 0f, 0f); // Invisible au départ
        RectTransform bgR = bgObj.GetComponent<RectTransform>();
        bgR.anchorMin     = new Vector2(0.2f, 0.04f);
        bgR.anchorMax     = new Vector2(0.8f, 0.10f);
        bgR.offsetMin     = Vector2.zero;
        bgR.offsetMax     = Vector2.zero;

        // Texte — 19pt, blanc, centré, characterSpacing 0.5
        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(bgObj.transform, false);
        _subtitleText                  = textObj.AddComponent<TextMeshProUGUI>();
        _subtitleText.fontSize         = SUBTITLE_FONT_SIZE;
        _subtitleText.characterSpacing = SUBTITLE_CHAR_SPACING;
        _subtitleText.alignment        = TextAlignmentOptions.Center;
        _subtitleText.color            = new Color(1f, 1f, 1f, 0f); // Invisible au départ
        _subtitleText.text             = "";

        // Padding 6px H, 3px V
        RectTransform tR = textObj.GetComponent<RectTransform>();
        tR.anchorMin     = Vector2.zero;
        tR.anchorMax     = Vector2.one;
        tR.offsetMin     = new Vector2(6f, 3f);
        tR.offsetMax     = new Vector2(-6f, -3f);
    }

    // ─── Animation porte ────────────────────────────────────────────────────

    private IEnumerator AnimateRotation(Quaternion from, Quaternion to)
    {
        isAnimating = true;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(from, to, elapsed / animDuration);
            yield return null;
        }

        transform.rotation = to;
        isAnimating = false;
    }

    // ─── Sons porte ─────────────────────────────────────────────────────────

    public void JouerSonVerrouille()
    {
        if (sonVerrouille == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(sonVerrouille);
        else
            AudioSource.PlayClipAtPoint(sonVerrouille, transform.position);
    }

    private void JouerSon()
    {
        if (sonOuverture == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(sonOuverture);
        else
            AudioSource.PlayClipAtPoint(sonOuverture, transform.position);
    }
}
