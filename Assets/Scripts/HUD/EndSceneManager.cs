using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndSceneManager : MonoBehaviour
{
    [Header("Scene du menu principal")]
    public string mainMenuSceneName = "MainMenu";

    private Canvas endCanvas;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;
        CreateEndScreen();
    }

    public void BoutonMenuPrincipal()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void BoutonQuitter()
    {
        Debug.Log("[EndScene] Quitter le jeu");
        Application.Quit();
    }

    private void CreateEndScreen()
    {
        // Canvas
        GameObject canvasObj = new GameObject("EndScene_Canvas");
        endCanvas = canvasObj.AddComponent<Canvas>();
        endCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        endCanvas.sortingOrder = 1000;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark background
        GameObject bgObj = new GameObject("EndBG");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.02f, 0.02f, 0.05f, 1f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Thanks text
        CreateLabel(canvasObj.transform, "Merci d'avoir joue !", 56, new Vector2(0f, 100f), FontStyles.Bold);

        // Subtitle
        CreateLabel(canvasObj.transform, "Sanestia - Escape The Office", 28, new Vector2(0f, 30f), FontStyles.Italic);

        // Buttons
        CreateButton(canvasObj.transform, "Menu Principal", new Vector2(0f, -80f), BoutonMenuPrincipal);
        CreateButton(canvasObj.transform, "Quitter", new Vector2(0f, -150f), BoutonQuitter);
    }

    private void CreateLabel(Transform parent, string text, float size, Vector2 pos, FontStyles style)
    {
        GameObject obj = new GameObject("Label_" + text);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = style;
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 0.5f);
        r.anchorMax        = new Vector2(0.5f, 0.5f);
        r.pivot            = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta        = new Vector2(800f, 80f);
    }

    private void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject("Btn_" + label);
        btnObj.transform.SetParent(parent, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        cb.highlightedColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        cb.pressedColor     = new Color(0.1f, 0.1f, 0.15f, 1f);
        btn.colors = cb;

        RectTransform br = btnObj.GetComponent<RectTransform>();
        br.anchorMin        = new Vector2(0.5f, 0.5f);
        br.anchorMax        = new Vector2(0.5f, 0.5f);
        br.pivot            = new Vector2(0.5f, 0.5f);
        br.anchoredPosition = pos;
        br.sizeDelta        = new Vector2(300f, 55f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI t = textObj.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 26;
        t.alignment = TextAlignmentOptions.Center;
        t.color     = Color.white;
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        btn.onClick.AddListener(action);
    }

    void OnDestroy()
    {
        if (endCanvas != null)
            Destroy(endCanvas.gameObject);
    }
}
