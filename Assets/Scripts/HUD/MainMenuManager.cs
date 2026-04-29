using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene de jeu")]
    public string nomSceneJeu = "SceneDeGame";

    [Header("Fade entree")]
    public float fadeInDuration  = 1.5f;
    public float fadeInDelay     = 0.2f;

    [Header("Fade sortie (Jouer)")]
    public float fadeOutDuration = 1f;

    private Image fadeOverlay;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        CreateFadeOverlay();
        StartCoroutine(FadeIn());
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
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = true;
        RectTransform r = overlayObj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeIn()
    {
        if (fadeInDelay > 0f) yield return new WaitForSeconds(fadeInDelay);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, elapsed / fadeInDuration));
            yield return null;
        }

        fadeOverlay.color         = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = false;
        fadeOverlay.enabled       = false;
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
        fadeOverlay.enabled       = true;
        fadeOverlay.color         = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.raycastTarget = true;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration));
            yield return null;
        }

        SceneManager.LoadScene(nomSceneJeu);
    }
}
