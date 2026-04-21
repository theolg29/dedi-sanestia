using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Séquence d'introduction : miroir exact de la séquence de mort (PlayerHealth.DeathSequence).
/// Mort  : debout → tombe sur le côté (EaseInQuad) → yeux se ferment (SmoothStep)
/// Réveil: couché sur le côté → yeux s'ouvrent (SmoothStep) → se redresse (EaseOutQuad)
///
/// Paramètres sleepDropHeight et sleepAngle doivent correspondre à
/// collapseDropHeight et collapseAngle de PlayerHealth pour que les deux animations
/// partent/arrivent exactement à la même pose.
/// </summary>
public class WakeUpCinematic : MonoBehaviour
{
    [Header("Caméra")]
    [Tooltip("Auto-détectée si vide")]
    public Transform playerCamera;

    [Header("Pose de départ (doit correspondre aux valeurs de mort dans PlayerHealth)")]
    [Tooltip("Hauteur de chute — même valeur que collapseDropHeight dans PlayerHealth")]
    public float sleepDropHeight = 0.8f;

    [Tooltip("Angle Z de la tête au sol — même valeur que collapseAngle dans PlayerHealth")]
    public float sleepAngle = 80f;

    [Header("Timings")]
    [Tooltip("Délai noir avant d'ouvrir les yeux")]
    public float delayBeforeOpeningEyes = 1f;

    [Tooltip("Durée de l'ouverture des paupières")]
    public float eyeOpenDuration = 2f;

    [Tooltip("Durée pour se redresser après l'ouverture des yeux")]
    public float getUpDuration = 1.5f;

    private Vector3 originalCameraPos;
    private Quaternion originalCameraRot;
    private readonly List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();

    void Start()
    {
        if (playerCamera == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null) playerCamera = cam.transform;
        }

        if (playerCamera == null)
        {
            Debug.LogError("[WakeUpCinematic] Aucune caméra trouvée.");
            return;
        }

        originalCameraPos = playerCamera.localPosition;
        originalCameraRot = playerCamera.localRotation;

        // Pose identique à la fin de DeathSequence dans PlayerHealth
        playerCamera.localPosition = originalCameraPos + new Vector3(0f, -sleepDropHeight, 0f);
        playerCamera.localRotation = originalCameraRot * Quaternion.Euler(10f, 0f, sleepAngle);

        // Couper tout le son au départ — il remonte progressivement pendant le réveil
        AudioListener.volume = 0f;

        DisablePlayerControls();
        StartCoroutine(WakeUpSequence());
    }

    private void DisablePlayerControls()
    {
        GameObject root = transform.root.gameObject;

        CharacterController cc = root.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Sauvegarder l'état et désactiver tous les scripts sauf celui-ci
        MonoBehaviour[] scripts = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour s in scripts)
        {
            if (s != this && s.enabled)
            {
                s.enabled = false;
                disabledScripts.Add(s);
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnablePlayerControls()
    {
        GameObject root = transform.root.gameObject;

        CharacterController cc = root.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = true;

        foreach (MonoBehaviour s in disabledScripts)
            if (s != null) s.enabled = true;

        disabledScripts.Clear();
    }

    private IEnumerator WakeUpSequence()
    {
        // === PHASE 1 : Noir total, couché (miroir de la fin de mort) ===
        GameObject canvasObj = new GameObject("WakeUpOverlay_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasObj.AddComponent<CanvasScaler>();

        float halfScreen = Screen.height / 2f + 20f;
        RectTransform topRect    = CreateEyelid(canvasObj.transform, top: true,  halfScreen);
        RectTransform bottomRect = CreateEyelid(canvasObj.transform, top: false, halfScreen);

        yield return new WaitForSeconds(delayBeforeOpeningEyes);

        // === PHASE 2 : Ouverture des yeux — SmoothStep (identique à la fermeture à la mort) ===
        float eyeElapsed = 0f;
        while (eyeElapsed < eyeOpenDuration)
        {
            eyeElapsed += Time.deltaTime;
            float t     = eyeElapsed / eyeOpenDuration;
            float eased = t * t * (3f - 2f * t); // SmoothStep

            topRect.sizeDelta    = new Vector2(0f, Mathf.Lerp(halfScreen, 0f, eased));
            bottomRect.sizeDelta = new Vector2(0f, Mathf.Lerp(halfScreen, 0f, eased));

            // Son qui arrive progressivement pendant l'ouverture des yeux
            AudioListener.volume = Mathf.Lerp(0f, 0.5f, eased);

            yield return null;
        }

        Destroy(canvasObj);

        // === PHASE 3 : Se redresser — EaseOutQuad (inverse de EaseInQuad de la chute) ===
        Vector3    startPos = playerCamera.localPosition;
        Quaternion startRot = playerCamera.localRotation;

        float getUpElapsed = 0f;
        while (getUpElapsed < getUpDuration)
        {
            getUpElapsed += Time.deltaTime;
            float t     = getUpElapsed / getUpDuration;
            float eased = 1f - (1f - t) * (1f - t); // EaseOutQuad

            playerCamera.localPosition = Vector3.Lerp(startPos, originalCameraPos, eased);
            playerCamera.localRotation = Quaternion.Slerp(startRot, originalCameraRot, eased);

            // Volume monte à 1 pendant que le joueur se redresse
            AudioListener.volume = Mathf.Lerp(0.5f, 1f, eased);

            yield return null;
        }

        playerCamera.localPosition = originalCameraPos;
        playerCamera.localRotation = originalCameraRot;

        AudioListener.volume = 1f;
        EnablePlayerControls();
        Destroy(this);
    }

    private static RectTransform CreateEyelid(Transform parent, bool top, float height)
    {
        GameObject bar = new GameObject(top ? "TopEyelid" : "BottomEyelid");
        bar.transform.SetParent(parent, false);
        bar.AddComponent<Image>().color = Color.black;

        RectTransform rect = bar.GetComponent<RectTransform>();
        if (top)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot     = new Vector2(0.5f, 0f);
        }
        rect.sizeDelta = new Vector2(0f, height);
        return rect;
    }
}
