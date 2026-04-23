using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f;
    public Camera playerCamera;

    [Header("Affordance")]
    [Tooltip("Prompt UI — créé automatiquement si vide")]
    public TextMeshProUGUI interactPromptText;
    [Tooltip("Couleur d'émission appliquée sur l'objet visé")]
    public Color highlightColor = new Color(1f, 0.85f, 0f);

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

        ShowPrompt(false, "");
    }

    void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (GetFirstHit(ray, out RaycastHit hit))
        {
            bool isItem = hit.collider.CompareTag("Item");
            DoorController door = hit.collider.GetComponent<DoorController>();

            if (isItem || door != null)
            {
                if (currentTarget != hit.collider.gameObject)
                {
                    ClearHighlight();
                    ApplyHighlight(hit.collider.gameObject);
                }

                string prompt = isItem ? "E — Ramasser" : (door.IsOpen ? "E — Fermer" : "E — Ouvrir");
                ShowPrompt(true, prompt);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (isItem)
                    {
                        ClearHighlight();
                        if (PlayerInventory.instance != null)
                            PlayerInventory.instance.PickUp(hit.collider.gameObject.name);
                        Destroy(hit.collider.gameObject);
                    }
                    else
                    {
                        door.TryToggle();
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

    private void BuildPromptUI()
    {
        GameObject canvasObj = new GameObject("InteractPrompt_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject panel = new GameObject("PromptPanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -90f);
        panelRect.sizeDelta = new Vector2(220f, 40f);

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
