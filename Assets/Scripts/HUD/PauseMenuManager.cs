using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager instance;

    [Header("Scene du menu principal")]
    public string mainMenuSceneName = "MainMenu";

    private Canvas      pauseCanvas;
    private CanvasGroup pauseCanvasGroup;
    private bool        isPaused = false;

    void Awake() => instance = this;

    void Start() => CreatePauseMenu();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseCanvasGroup.alpha          = 1f;
        pauseCanvasGroup.blocksRaycasts = true;
        pauseCanvasGroup.interactable   = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseCanvasGroup.alpha          = 0f;
        pauseCanvasGroup.blocksRaycasts = false;
        pauseCanvasGroup.interactable   = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    public void OnMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnQuitter()
    {
        Debug.Log("[PauseMenu] Quitter le jeu");
        Application.Quit();
    }

    private void CreatePauseMenu()
    {
        GameObject canvasObj = new GameObject("PauseMenu_Canvas");
        pauseCanvas = canvasObj.AddComponent<Canvas>();
        pauseCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        pauseCanvas.sortingOrder = 5000;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasObj.AddComponent<GraphicRaycaster>();

        pauseCanvasGroup                = canvasObj.AddComponent<CanvasGroup>();
        pauseCanvasGroup.alpha          = 0f;
        pauseCanvasGroup.blocksRaycasts = false;
        pauseCanvasGroup.interactable   = false;

        // Dark overlay
        GameObject bgObj = new GameObject("PauseBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        CreateLabel(canvasObj.transform, "PAUSE", 48, new Vector2(0f, 120f));

        // Buttons
        CreateButton(canvasObj.transform, "Menu Principal", new Vector2(0f, 0f), OnMenuPrincipal);
        CreateButton(canvasObj.transform, "Quitter", new Vector2(0f, -70f), OnQuitter);
    }

    private void CreateLabel(Transform parent, string text, float size, Vector2 pos)
    {
        GameObject obj = new GameObject("Label_" + text);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 0.5f);
        r.anchorMax        = new Vector2(0.5f, 0.5f);
        r.pivot            = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta        = new Vector2(400f, 60f);
    }

    private void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        cb.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        cb.pressedColor     = new Color(0.15f, 0.15f, 0.15f, 1f);
        btn.colors = cb;

        RectTransform br = btnObj.GetComponent<RectTransform>();
        br.anchorMin        = new Vector2(0.5f, 0.5f);
        br.anchorMax        = new Vector2(0.5f, 0.5f);
        br.pivot            = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = pos;
        br.sizeDelta        = new Vector2(300f, 50f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI t = textObj.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 24;
        t.alignment = TextAlignmentOptions.Center;
        t.color     = Color.white;
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        btn.onClick.AddListener(action);
    }
}
