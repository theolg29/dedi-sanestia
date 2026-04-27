using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class ElecRoomPanel : MonoBehaviour
{
    [Header("Porte")]
    [Tooltip("Le trou de vis a viser avec E - doit avoir un Collider")]
    public Transform screwHoleTransform;
    [Tooltip("La porte a faire tomber - doit avoir un Rigidbody kinematic + Collider")]
    public Transform doorTransform;
    [Tooltip("Item requis pour demonter la porte")]
    public GameObject requiredItem;
    public AudioClip sonChute;

    [Header("Knob")]
    [Tooltip("Le knob a faire pivoter - doit avoir un Collider")]
    public Transform knobTransform;
    [Tooltip("Axe de rotation du knob - verifie dans l'Inspector du modele")]
    public RotationAxis knobAxis = RotationAxis.Y;
    [Tooltip("Angle cible a atteindre (l'angle de depart est lu automatiquement depuis la scene)")]
    public float targetAngle = 80f;
    [Tooltip("Sensibilite souris - degres par unite Mouse Y")]
    public float mouseSensitivity = 120f;

    public enum RotationAxis { X, Y, Z }

    [Header("Parametres communs")]
    public float interactDistance = 3f;
    public Color highlightColor = new Color(1f, 0.85f, 0f);
    public Camera playerCamera;

    // --- etat ---
    private bool _doorOpen;
    private bool _knobActivated;
    private bool _isDragging;

    // --- highlight porte ---
    private bool _doorHighlighted;
    private Renderer[] _doorRenderers;
    private Color[] _doorEmissions;
    private bool[] _doorKeywords;

    // --- highlight knob ---
    private float _startAngle;
    private float _currentAngle;
    private bool _knobHighlighted;
    private Renderer[] _knobRenderers;
    private Color[] _knobEmissions;
    private bool[] _knobKeywords;

    // --- UI ---
    private TextMeshProUGUI _promptText;
    private GameObject _promptPanel;
    private TextMeshProUGUI _notifText;
    private GameObject _notifPanel;
    private Coroutine _notifCoroutine;

    private FirstPersonController _fpc;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        _fpc = FindObjectOfType<FirstPersonController>();

        ValidateAndInit();
        BuildUI();
    }

    void Update()
    {
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
                if (locked) ShowNotif($"Il vous faut : {requiredItem.name}");
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

        Rigidbody rb = doorTransform.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[ElecRoomPanel] Aucun Rigidbody sur la porte - ajoute un Rigidbody kinematic dessus dans l'Inspector.", doorTransform);
            rb = doorTransform.gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity  = true;

        if (sonChute != null)
            AudioSource.PlayClipAtPoint(sonChute, doorTransform.position);
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
            float delta = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
            _currentAngle = Mathf.Clamp(_currentAngle + delta, _startAngle, targetAngle);

            Vector3 e = knobTransform.localEulerAngles;
            switch (knobAxis)
            {
                case RotationAxis.X: e.x = _currentAngle; break;
                case RotationAxis.Z: e.z = _currentAngle; break;
                default:             e.y = _currentAngle; break;
            }
            knobTransform.localEulerAngles = e;

            if (_currentAngle >= targetAngle)
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
        Debug.Log("[ElecRoomPanel] Knob en position - panneau active !");
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    private bool IsLooking(Transform target)
    {
        if (target == null) return false;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);
        foreach (RaycastHit hit in hits)
        {
            // Correspond si le hit est target, un enfant de target, ou un parent de target
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

            // Lire la rotation actuelle de la scene comme point de depart
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
        canvasObj.AddComponent<CanvasScaler>();

        _promptPanel = BuildPanel(canvasObj.transform, new Vector2(0f, -90f), new Vector2(280f, 40f),
                                  new Color(0f, 0f, 0f, 0.55f), out _promptText, 20);
        _promptPanel.SetActive(false);

        _notifPanel  = BuildPanel(canvasObj.transform, new Vector2(0f, -155f), new Vector2(300f, 40f),
                                  new Color(0.8f, 0.2f, 0.2f, 0.75f), out _notifText, 17);
        _notifPanel.SetActive(false);
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
