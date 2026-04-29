using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene de jeu")]
    public string nomSceneJeu = "SceneDeGame";

    [Header("Fade")]
    public float fadeDuration = 1f;

    private Image fadeOverlay;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        CreateFadeOverlay();
    }

    private void CreateFadeOverlay()
    {
        GameObject canvasObj = new GameObject("MainMenu_FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
        RectTransform r = overlayObj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    public void BoutonJouer()
    {
        StartCoroutine(FadeAndLoad());
    }

    public void BoutonQuitter()
    {
        Application.Quit();
    }

    private IEnumerator FadeAndLoad()
    {
        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }

        SceneManager.LoadScene(nomSceneJeu);
    }
}
