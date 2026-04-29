using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class ElecRoomPanel : MonoBehaviour
{
    [Header("Dialogue - entree dans la salle electrique")]
    public DialogueLine[] knobDialogue;
    [Header("Dialogue - courant remis (joue quand TOUS les panneaux sont actives)")]
    public DialogueLine[] powerRestoredDialogue;

    [Range(0f, 1f)]
    public float dialogueVolume    = 1f;
    public float dialogueDelay     = 0.5f;
    public float pauseBetweenLines = 0.5f;
    [Tooltip("Distance de declenchement du dialogue (quand le joueur s'approche du panneau)")]
    public float dialogueTriggerDistance = 6f;

    [Header("Porte")]
    [Tooltip("Le trou de vis a viser avec E - doit avoir un Collider")]
    public Transform screwHoleTransform;
    [Tooltip("La porte a animer quand elle s'ouvre")]
    public Transform doorTransform;
    [Tooltip("Item requis pour demonter la porte")]
    public GameObject requiredItem;
    public AudioClip sonChute;
    [Tooltip("Axe local sur lequel la porte se penche")]
    public RotationAxis doorFallAxis = RotationAxis.X;
    [Tooltip("Angle de chute en degres (ex: 90 = completement a plat)")]
    public float doorFallAngle = 85f;
    [Tooltip("Duree de l'animation en secondes")]
    public float doorFallDuration = 0.6f;
    [Tooltip("Son joue quand le joueur appuie sur E sans avoir l'item requis")]
    public AudioClip sonLocked;
    [Tooltip("Sous-titre affiche en bas ecran quand la porte est verrouillee (style DIALOGUE)")]
    [TextArea(1, 3)]
    public string sonLockedSubtitle;

    [Header("Knob")]
    [Tooltip("Le knob a faire pivoter - doit avoir un Collider")]
    public Transform knobTransform;
    [Tooltip("Axe de rotation du knob")]
    public RotationAxis knobAxis = RotationAxis.Y;
    [Tooltip("Angle cible a atteindre")]
    public float targetAngle = 80f;
    [Tooltip("Sensibilite souris - degres par unite Mouse Y")]
    public float mouseSensitivity = 120f;
    [Tooltip("Son joue quand le knob atteint sa position finale")]
    public AudioClip sonKnobActivated;

    public enum RotationAxis { X, Y, Z }

    [Header("Parametres communs")]
    public float interactDistance = 3f;
    public Color highlightColor = new Color(1f, 0.85f, 0f);
    public Camera playerCamera;

    // --- compteurs statiques : partagés entre toutes les instances ---
    private static int s_totalPanels    = 0;
    private static int s_activatedCount = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        s_totalPanels    = 0;
        s_activatedCount = 0;
    }

    // --- etat porte ---
    private bool _doorOpen;
    private bool _doorHighlighted;
    private Renderer[] _doorRenderers;
    private Color[] _doorEmissions;
    private bool[] _doorKeywords;

    // --- etat knob ---
    private bool _knobActivated;
    private bool _isDragging;
    private float _startAngle;
    private float _currentAngle;
    private bool _knobHighlighted;
    private Renderer[] _knobRenderers;
    private Color[] _knobEmissions;
    private bool[] _knobKeywords;

    // --- dialogue ---
    private bool _dialoguePlayed;

    // --- UI ---
    private TextMeshProUGUI _promptText;
    private GameObject _promptPanel;
    private TextMeshProUGUI _notifText;
    private GameObject _notifPanel;
    private Coroutine _notifCoroutine;

    private AudioSource     _dialogueSource;
    private TextMeshProUGUI _subtitleText;
    private Image           _subtitleBg;
    private Coroutine       _dialogueCoroutine;

    private FirstPersonController _fpc;

    void Start()
    {
        s_totalPanels++;

        if (playerCamera == null) playerCamera = Camera.main;
        _fpc = FindObjectOfType<FirstPersonController>();

        _dialogueSource              = gameObject.AddComponent<AudioSource>();
        _dialogueSource.spatialBlend = 0f;
        _dialogueSource.volume       = dialogueVolume;

        if (powerRestoredDialogue == null || powerRestoredDialogue.Length == 0)
        {
            AudioClip clip = Resources.Load<AudioClip>("courant_remis");
            powerRestoredDialogue = new DialogueLine[]
            {
                new DialogueLine
                {
                    clip     = clip,
                    subtitle = "Super, j'ai reactive le courant d'urgence !"
                }
            };
        }

        ValidateAndInit();
        BuildUI();
    }

    void OnDestroy()
    {
        s_totalPanels = Mathf.Max(0, s_totalPanels - 1);
    }

    void Update()
    {
        if (!_dialoguePlayed && PowerCutTrigger.PowerIsCut && playerCamera != null)
        {
            float dist = Vector3.Distance(playerCamera.transform.position, transform.position);
            if (dist <= dialogueTriggerDistance)
            {
                _dialoguePlayed = true;
                if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
                _dialogueCoroutine = StartCoroutine(PlaySpecificDialogue(knobDialogue));
            }
        }

        if (!_doorOpen)
            HandleDoor();
        else if (!_knobActivated)
            HandleKnob();
    }

    // ─── PORTE ────────────────────────────────────────────────────────────────

    private void HandleDoor()
    {
        bool looking = IsLooking(screwHoleTransform);

        SetHighlight(ref _doorHighlighted, _doorRenderers, _doorEmissions, _doorKeywords, looking);

        bool locked = requiredItem != null
                   && PlayerInventory.instance?.GetItem() != requiredItem.name;

        if (looking)
        {
            ShowPrompt(true, locked
                ? $"Necessite : {requiredItem.name}"
                : "E - Demonter le panneau");

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (locked)
                {
                    ShowNotif($"Il vous faut : {requiredItem.name}");
                    if (sonLocked != null || !string.IsNullOrEmpty(sonLockedSubtitle))
                    {
                        var line = new DialogueLine { clip = sonLocked, subtitle = sonLockedSubtitle };
                        if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
                        _dialogueCoroutine = StartCoroutine(PlaySpecificDialogue(new[] { line }));
                    }
                }
                else        OpenDoor();
            }
        }
        else
        {
            ShowPrompt(false, "");
        }
    }

    private void OpenDoor()
    {
        _doorOpen = true;
        SetHighlight(ref _doorHighlighted, _doorRenderers, _doorEmissions, _doorKeywords, false);
        ShowPrompt(false, "");

        if (doorTransform == null) return;

        if (sonChute != null)
            AudioSource.PlayClipAtPoint(sonChute, doorTransform.position);

        StartCoroutine(TiltDoor());
    }

    private IEnumerator TiltDoor()
    {
        Quaternion startRot = doorTransform.localRotation;
        Vector3 axis = doorFallAxis switch
        {
            RotationAxis.Y => Vector3.up,
            RotationAxis.Z => Vector3.forward,
            _              => Vector3.right,
        };
        Quaternion endRot = startRot * Quaternion.AngleAxis(doorFallAngle, axis);

        float elapsed = 0f;
        while (elapsed < doorFallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / doorFallDuration);
            doorTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        doorTransform.localRotation = endRot;
    }

    // ─── KNOB ─────────────────────────────────────────────────────────────────

    private void HandleKnob()
    {
        bool looking = IsLooking(knobTransform);

        SetHighlight(ref _knobHighlighted, _knobRenderers, _knobEmissions, _knobKeywords,
                     looking || _isDragging);

        if (looking || _isDragging)
            ShowPrompt(true, _isDragging ? "Remontez la souris..." : "Clic gauche - Tourner");
        else
            ShowPrompt(false, "");

        if (looking && Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            if (_fpc != null) _fpc.cameraCanMove = false;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging && _fpc != null) _fpc.cameraCanMove = true;
            _isDragging = false;
            if (!looking) SetHighlight(ref _knobHighlighted, _knobRenderers, _knobEmissions, _knobKeywords, false);
        }

        if (_isDragging)
        {
            // Inverse le delta si targetAngle est inferieur au point de depart (ex: -80)
            float rawDelta = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            float delta    = targetAngle >= _startAngle ? rawDelta : -rawDelta;

            float min = Mathf.Min(_startAngle, targetAngle);
            float max = Mathf.Max(_startAngle, targetAngle);
            _currentAngle = Mathf.Clamp(_currentAngle + delta, min, max);

            Vector3 e = knobTransform.localEulerAngles;
            switch (knobAxis)
            {
                case RotationAxis.X: e.x = _currentAngle; break;
                case RotationAxis.Z: e.z = _currentAngle; break;
                default:             e.y = _currentAngle; break;
            }
            knobTransform.localEulerAngles = e;

            // Verifie l'atteinte de l'angle cible dans les deux sens
            bool reached = targetAngle >= _startAngle
                ? _currentAngle >= targetAngle
                : _currentAngle <= targetAngle;

            if (reached)
            {
                _knobActivated = true;
                _isDragging    = false;
                if (_fpc != null) _fpc.cameraCanMove = true;
                SetHighlight(ref _knobHighlighted, _knobRenderers, _knobEmissions, _knobKeywords, false);
                ShowPrompt(false, "");
                OnKnobActivated();
            }
        }
    }

    private void OnKnobActivated()
    {
        s_activatedCount++;
        Debug.Log($"[ElecRoomPanel] Knob active ({s_activatedCount}/{s_totalPanels})");

        if (sonKnobActivated != null)
            AudioSource.PlayClipAtPoint(sonKnobActivated, knobTransform.position);

        // Le son joue uniquement quand TOUS les panneaux sont actives
        if (s_activatedCount >= s_totalPanels)
        {
            if (_dialogueCoroutine != null) StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = StartCoroutine(PlaySpecificDialogue(powerRestoredDialogue));
        }
    }

    // ─── DIALOGUE ─────────────────────────────────────────────────────────────

    private IEnumerator PlaySpecificDialogue(DialogueLine[] dialogueLines)
    {
        if (dialogueLines == null || dialogueLines.Length == 0) yield break;
        yield return new WaitForSeconds(dialogueDelay);

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
                yield return FadeSubtitle(0f, 1f, 0.18f);
            }

            yield return new WaitForSeconds(line.clip != null ? line.clip.length : 2f);
            while (_dialogueSource.isPlaying) yield return null;

            yield return FadeSubtitle(1f, 0f, 0.18f);
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

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    private bool IsLooking(Transform target)
    {
        if (target == null) return false;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, ~0, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == target
             || hit.transform.IsChildOf(target)
             || target.IsChildOf(hit.transform))
                return true;
        }
        return false;
    }

    private void SetHighlight(ref bool state, Renderer[] renderers, Color[] emissions, bool[] keywords, bool active)
    {
        if (state == active || renderers == null) return;
        state = active;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material mat = renderers[i].material;
            if (active)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", highlightColor * 0.15f);
            }
            else
            {
                if (!keywords[i]) mat.DisableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", emissions[i]);
            }
        }
    }

    private void ValidateAndInit()
    {
        if (screwHoleTransform != null)
        {
            if (screwHoleTransform.GetComponent<Collider>() == null)
                Debug.LogError("[ElecRoomPanel] screwHoleTransform n'a pas de Collider !", screwHoleTransform);
            InitRenderers(screwHoleTransform, out _doorRenderers, out _doorEmissions, out _doorKeywords);
        }
        else Debug.LogError("[ElecRoomPanel] screwHoleTransform non assigne !", this);

        if (knobTransform != null)
        {
            if (knobTransform.GetComponent<Collider>() == null)
                Debug.LogError("[ElecRoomPanel] knobTransform n'a pas de Collider !", knobTransform);
            InitRenderers(knobTransform, out _knobRenderers, out _knobEmissions, out _knobKeywords);

            Vector3 e = knobTransform.localEulerAngles;
            _currentAngle = knobAxis switch
            {
                RotationAxis.X => e.x,
                RotationAxis.Z => e.z,
                _              => e.y,
            };
            if (_currentAngle > 180f) _currentAngle -= 360f;
            _startAngle = _currentAngle;
        }
        else Debug.LogError("[ElecRoomPanel] knobTransform non assigne !", this);
    }

    private void InitRenderers(Transform t, out Renderer[] rends, out Color[] emissions, out bool[] keywords)
    {
        rends     = t.GetComponentsInChildren<Renderer>();
        emissions = new Color[rends.Length];
        keywords  = new bool[rends.Length];

        for (int i = 0; i < rends.Length; i++)
        {
            Material mat  = rends[i].material;
            keywords[i]   = mat.IsKeywordEnabled("_EMISSION");
            emissions[i]  = mat.HasProperty("_EmissionColor")
                ? mat.GetColor("_EmissionColor") : Color.black;
        }
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    private void ShowPrompt(bool visible, string text)
    {
        if (_promptPanel == null) return;
        _promptPanel.SetActive(visible);
        if (visible && _promptText != null) _promptText.text = text;
    }

    private void ShowNotif(string message)
    {
        if (_notifCoroutine != null) StopCoroutine(_notifCoroutine);
        _notifCoroutine = StartCoroutine(NotifCoroutine(message));
    }

    private IEnumerator NotifCoroutine(string message)
    {
        if (_notifPanel == null) yield break;
        _notifText.text = message;
        _notifPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        _notifPanel.SetActive(false);
    }

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("ElecPanel_Canvas");
        Canvas canvas        = canvasObj.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 202;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        _promptPanel = BuildPanel(canvasObj.transform, new Vector2(0f, -90f), new Vector2(280f, 40f),
                                  new Color(0f, 0f, 0f, 0.55f), out _promptText, 20);
        _promptPanel.SetActive(false);

        _notifPanel  = BuildPanel(canvasObj.transform, new Vector2(0f, -155f), new Vector2(300f, 40f),
                                  new Color(0.8f, 0.2f, 0.2f, 0.75f), out _notifText, 17);
        _notifPanel.SetActive(false);

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

    private GameObject BuildPanel(Transform parent, Vector2 pos, Vector2 size, Color bgColor,
                                   out TextMeshProUGUI label, int fontSize)
    {
        GameObject panel    = new GameObject("Panel");
        panel.transform.SetParent(parent, false);
        Image bg            = panel.AddComponent<Image>();
        bg.color            = bgColor;
        RectTransform rt    = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        GameObject textObj  = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);
        label               = textObj.AddComponent<TextMeshProUGUI>();
        label.fontSize      = fontSize;
        label.alignment     = TextAlignmentOptions.Center;
        label.color         = Color.white;
        RectTransform tr    = textObj.GetComponent<RectTransform>();
        tr.anchorMin        = Vector2.zero;
        tr.anchorMax        = Vector2.one;
        tr.offsetMin        = new Vector2(8f, 4f);
        tr.offsetMax        = new Vector2(-8f, -4f);

        return panel;
    }
}
