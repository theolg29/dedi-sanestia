using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapeZone : MonoBehaviour
{
    [Header("Joueur")]
    public Transform player;

    [Header("Fondu au noir")]
    public float maxFadeDistance = 20f;
    public float fadeSpeed = 3f;

    [Header("Scene de fin")]
    [Tooltip("Nom exact de la scene (doit etre dans Build Settings)")]
    public string endSceneName = "EndScene";

    private BoxCollider zone;
    private bool active  = false;
    private bool loading = false;
    private CanvasGroup overlayGroup;

    void Start()
    {
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
        CreateOverlay();

        if (player == null)
            Debug.LogWarning("[EscapeZone] Aucun joueur assigne dans l'Inspector !");
    }

    private bool IsPlayer(Collider other)
    {
        if (player == null) return false;
        Transform t = other.transform;
        while (t != null)
        {
            if (t == player) return true;
            t = t.parent;
        }
        return false;
    }

    private void CreateOverlay()
    {
        GameObject canvasObj = new GameObject("EscapeZone_FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9998;
        CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720);
        cs.matchWidthOrHeight  = 0.5f;

        GameObject imgObj = new GameObject("FadeOverlay");
        imgObj.transform.SetParent(canvasObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color         = Color.black;
        img.raycastTarget = false;

        RectTransform rect = imgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayGroup                = canvasObj.AddComponent<CanvasGroup>();
        overlayGroup.alpha          = 0f;
        overlayGroup.blocksRaycasts = false;
        overlayGroup.interactable   = false;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || loading) return;
        active = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other) || loading) return;
        active             = false;
        overlayGroup.alpha = 0f;
    }

    void Update()
    {
        if (!active || loading || player == null) return;

        Vector3 closest = zone.ClosestPoint(player.position);
        float   dist    = Vector3.Distance(player.position, closest);
        float   target  = Mathf.Clamp01(dist / maxFadeDistance);

        overlayGroup.alpha = Mathf.MoveTowards(overlayGroup.alpha, target, fadeSpeed * Time.deltaTime);

        if (overlayGroup.alpha >= 0.99f)
        {
            loading = true;
            StartCoroutine(LoadEndScene());
        }
    }

    private IEnumerator LoadEndScene()
    {
        overlayGroup.alpha = 1f;
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(endSceneName);
    }

    void OnDestroy()
    {
        if (overlayGroup != null && overlayGroup.transform.parent != null)
            Destroy(overlayGroup.transform.parent.gameObject);
    }
}
