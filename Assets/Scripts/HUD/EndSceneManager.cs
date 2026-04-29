using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EndSceneManager : MonoBehaviour
{
    [Header("Intro")]
    public AudioClip introSound;
    public float fadeDuration = 2f;
    public float fadeDelay    = 0.3f;

    private Image fadeOverlay;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;

        CreateFadeOverlay();

        if (introSound != null)
            AudioSource.PlayClipAtPoint(introSound, Vector3.zero);

        StartCoroutine(FadeIn());
    }

    private void CreateFadeOverlay()
    {
        GameObject canvasObj = new GameObject("EndScene_FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = false;
        RectTransform r = overlayObj.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeIn()
    {
        if (fadeDelay > 0f) yield return new WaitForSeconds(fadeDelay);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
            yield return null;
        }

        fadeOverlay.color   = new Color(0f, 0f, 0f, 0f);
        fadeOverlay.enabled = false;
    }
}
