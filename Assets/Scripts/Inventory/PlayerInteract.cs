using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public Camera playerCamera;

    [Header("Affordance")]
    [Tooltip("Prompt UI - créé automatiquement si vide")]
    public TextMeshProUGUI interactPromptText;
    [Tooltip("Couleur d'émission appliquée sur l'objet visé")]
    public Color highlightColor = new Color(1f, 0.85f, 0f);

    [Header("Notification")]
    [Tooltip("Durée d'affichage de la notif porte verrouillée")]
    public float notifDuration = 2f;
    private TextMeshProUGUI _notifText;
    private Coroutine _notifCoroutine;

    private Collider[] playerColliders;

    private GameObject currentTarget;
    private Renderer[] targetRenderers;
    private Color[] savedEmissions;
    private bool[] savedEmissionKeyword;

    void Start()
    {
        playerColliders = transform.root.GetComponentsInChildren<Collider>();

        if (interactPromptText == null)
            BuildPromptUI();

        BuildNotifUI();
        ShowPrompt(false, "");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            Vector3 spawnPos = playerCamera.transform.position + playerCamera.transform.forward * 1.5f;
            PlayerInventory.instance?.Throw(spawnPos, playerCamera.transform.forward);
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (GetFirstHit(ray, out RaycastHit hit))
        {
            GameObject itemObj = FindTaggedParent(hit.collider.gameObject, "Item");
            bool isItem   = itemObj != null;
            bool isCarton = hit.collider.CompareTag("Carton") || hit.collider.CompareTag("Movable");
            DoorController door = hit.collider.GetComponent<DoorController>()
                               ?? hit.collider.GetComponentInParent<DoorController>();
            SecurityTVController tv = hit.collider.GetComponent<SecurityTVController>()
                                   ?? hit.collider.GetComponentInParent<SecurityTVController>();

            if (isItem || isCarton || door != null || tv != null)
            {
                if (currentTarget != hit.collider.gameObject)
                {
                    ClearHighlight();
                    ApplyHighlight(hit.collider.gameObject);
                }

                string prompt;
                if (isItem)            prompt = "E - Prendre";
                else if (isCarton)     prompt = "Clic gauche - Déplacer";
                else if (door != null) prompt = door.IsOpen ? "E - Fermer" : "E - Ouvrir";
                else                   prompt = $"E - Caméra suivante  ({tv.CurrentIndex + 1}/{tv.cameraFeeds.Length})";
                ShowPrompt(true, prompt);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (isItem)
                    {
                        ClearHighlight();
                        PlayerInventory.instance?.PickUp(itemObj);
                    }
                    else if (door != null)
                    {
                        if (!door.TryToggle() && door.IsLocked)
                        {
                            door.JouerSonVerrouille();
                            ShowNotif("Impossible d'ouvrir cette porte");
                        }
                    }
                    else
                    {
                        tv.Interact();
                    }
                }
                return;
            }
        }

        ClearHighlight();
        ShowPrompt(false, "");
    }

    private bool GetFirstHit(Ray ray, out RaycastHit validHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (System.Array.Exists(playerColliders, c => c == hit.collider))
                continue;

            validHit = hit;
            return true;
        }

        validHit = default;
        return false;
    }

    private void ApplyHighlight(GameObject obj)
    {
        currentTarget = obj;
        targetRenderers = obj.GetComponentsInChildren<Renderer>();
        savedEmissions = new Color[targetRenderers.Length];
        savedEmissionKeyword = new bool[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Material mat = targetRenderers[i].material;
            savedEmissionKeyword[i] = mat.IsKeywordEnabled("_EMISSION");
            savedEmissions[i] = mat.HasProperty("_EmissionColor")
                ? mat.GetColor("_EmissionColor")
                : Color.black;

            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", highlightColor * 0.15f);
        }
    }

    private void ClearHighlight()
    {
        if (currentTarget == null) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            Material mat = targetRenderers[i].material;

            if (!savedEmissionKeyword[i])
                mat.DisableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", savedEmissions[i]);
        }

        currentTarget = null;
        targetRenderers = null;
    }

    private void ShowPrompt(bool visible, string text)
    {
        if (interactPromptText == null) return;
        interactPromptText.transform.parent.gameObject.SetActive(visible);
        if (visible) interactPromptText.text = text;
    }

    private void ShowNotif(string message)
    {
        if (_notifCoroutine != null) StopCoroutine(_notifCoroutine);
        _notifCoroutine = StartCoroutine(NotifCoroutine(message));
    }

    private System.Collections.IEnumerator NotifCoroutine(string message)
    {
        if (_notifText == null) yield break;
        _notifText.transform.parent.gameObject.SetActive(true);
        _notifText.text = message;
        yield return new WaitForSeconds(notifDuration);
        _notifText.transform.parent.gameObject.SetActive(false);
    }

    private void BuildNotifUI()
    {
        GameObject canvasObj = new GameObject("Notif_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 201;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        GameObject panel = new GameObject("NotifPanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.8f, 0.2f, 0.2f, 0.75f);  // FOND_ERREUR

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot     = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -155f);
        panelRect.sizeDelta = new Vector2(300f, 40f);

        GameObject textObj = new GameObject("NotifText");
        textObj.transform.SetParent(panel.transform, false);
        _notifText = textObj.AddComponent<TextMeshProUGUI>();
        _notifText.fontSize  = 17;
        _notifText.alignment = TextAlignmentOptions.Center;
        _notifText.color     = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);

        panel.SetActive(false);
    }

    private static GameObject FindTaggedParent(GameObject obj, string tag)
    {
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag(tag)) return t.gameObject;
            t = t.parent;
        }
        return null;
    }

    private void BuildPromptUI()
    {
        GameObject canvasObj = new GameObject("InteractPrompt_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        GameObject panel = new GameObject("PromptPanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -90f);
        panelRect.sizeDelta = new Vector2(240f, 40f);

        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(panel.transform, false);
        interactPromptText = textObj.AddComponent<TextMeshProUGUI>();
        interactPromptText.fontSize = 20;
        interactPromptText.alignment = TextAlignmentOptions.Center;
        interactPromptText.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
    }
}
