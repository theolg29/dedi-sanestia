using UnityEngine;

public class MinimapToggle : MonoBehaviour
{
    [Header("Minimap")]
    public RectTransform minimapRect;
    public Camera minimapCamera;

    [Header("Taille agrandie")]
    public Vector2 expandedSize = new Vector2(500f, 500f);
    public float expandedZoom = 15f;

    private Vector2 smallSize;
    private Vector2 smallAnchoredPosition;
    private Vector2 smallAnchorMin;
    private Vector2 smallAnchorMax;
    private Vector2 smallPivot;
    private float smallZoom;

    private bool isExpanded = false;

    void Start()
    {
        if (minimapRect == null)
        {
            Debug.LogError("[MinimapToggle] ❌ Minimap Rect non assigné dans l'Inspector !");
            return;
        }

        smallSize             = minimapRect.sizeDelta;
        smallAnchoredPosition = minimapRect.anchoredPosition;
        smallAnchorMin        = minimapRect.anchorMin;
        smallAnchorMax        = minimapRect.anchorMax;
        smallPivot            = minimapRect.pivot;

        if (minimapCamera == null)
        {
            MinimapFollow follow = FindFirstObjectByType<MinimapFollow>();
            if (follow != null) minimapCamera = follow.GetComponent<Camera>();
        }

        if (minimapCamera != null)
            smallZoom = minimapCamera.orthographicSize;
    }

    void Update()
    {
        if (Input.inputString == "m" || Input.inputString == "M")
            Toggle();
    }

    private void Toggle()
    {
        isExpanded = !isExpanded;

        if (isExpanded)
        {
            minimapRect.anchorMin        = new Vector2(0.5f, 0.5f);
            minimapRect.anchorMax        = new Vector2(0.5f, 0.5f);
            minimapRect.pivot            = new Vector2(0.5f, 0.5f);
            minimapRect.anchoredPosition = Vector2.zero;
            minimapRect.sizeDelta        = expandedSize;

            if (minimapCamera != null)
                minimapCamera.orthographicSize = expandedZoom;
        }
        else
        {
            minimapRect.anchorMin        = smallAnchorMin;
            minimapRect.anchorMax        = smallAnchorMax;
            minimapRect.pivot            = smallPivot;
            minimapRect.anchoredPosition = smallAnchoredPosition;
            minimapRect.sizeDelta        = smallSize;

            if (minimapCamera != null)
                minimapCamera.orthographicSize = smallZoom;
        }
    }
}
