using UnityEngine;
using System.Collections;

public class FlickeringNeon : MonoBehaviour
{
    [Header("Composants à relier")]
    public Light lumiereNeon;
    public Renderer cylindreNeon;

    [Header("Couleurs")]
    public Color  normalColor       = Color.white;
    public float  normalIntensity   = 1f;
    public Color  securityColor     = new Color(0.2f, 0.5f, 1f);
    public float  securityIntensity = 0.8f;
    public float  transitionDuration = 2f;

    [Header("Urgence (apres remise du courant)")]
    public Color emergencyColor     = new Color(1f, 0.05f, 0.05f);
    public float emergencyIntensity = 0.5f;
    public float flickerSpeedMin    = 0.04f;
    public float flickerSpeedMax    = 0.18f;

    private Material materialNeon;

    void Start()
    {
        if (lumiereNeon != null)
        {
            lumiereNeon.color     = normalColor;
            lumiereNeon.intensity = normalIntensity;
        }

        if (cylindreNeon != null)
        {
            materialNeon = cylindreNeon.material;
            materialNeon.EnableKeyword("_EMISSION");
            materialNeon.SetColor("_EmissionColor", normalColor);
        }
    }

    public void SwitchToSecurity()
    {
        if (lumiereNeon == null) return;
        StopAllCoroutines();
        StartCoroutine(TransitionToSecurity());
    }

    public void SwitchOff()
    {
        if (lumiereNeon == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeOff());
    }

    public void SwitchToEmergency()
    {
        if (lumiereNeon == null) return;
        StopAllCoroutines();
        StartCoroutine(EmergencyRoutine());
    }

    private IEnumerator FadeOff()
    {
        float elapsed        = 0f;
        float startIntensity = lumiereNeon.intensity;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            lumiereNeon.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / 1f);
            yield return null;
        }

        lumiereNeon.enabled = false;
        if (materialNeon != null)
            materialNeon.SetColor("_EmissionColor", Color.black);
    }

    private IEnumerator EmergencyRoutine()
    {
        lumiereNeon.enabled   = true;
        lumiereNeon.color     = emergencyColor;
        lumiereNeon.intensity = 0f;

        if (materialNeon != null)
            materialNeon.SetColor("_EmissionColor", emergencyColor * 0.3f);

        // Fade in
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            lumiereNeon.intensity = Mathf.Lerp(0f, emergencyIntensity, elapsed / 0.6f);
            yield return null;
        }

        // Flicker en boucle 5% -> 50% -> 5% ...
        float speed = 1f / Mathf.Max(0.001f, flickerSpeedMax);
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * speed;
            float flicker = Mathf.Lerp(0.05f, 0.5f, Mathf.PingPong(t, 1f));
            lumiereNeon.intensity = emergencyIntensity * flicker;
            if (materialNeon != null)
                materialNeon.SetColor("_EmissionColor", emergencyColor * (flicker * 0.4f));
            yield return null;
        }
    }

    private IEnumerator TransitionToSecurity()
    {
        float elapsed = 0f;
        Color startColor     = lumiereNeon.color;
        float startIntensity = lumiereNeon.intensity;
        Color startEmission  = materialNeon != null ? materialNeon.GetColor("_EmissionColor") : normalColor;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;

            lumiereNeon.color     = Color.Lerp(startColor, securityColor, t);
            lumiereNeon.intensity = Mathf.Lerp(startIntensity, securityIntensity, t);

            if (materialNeon != null)
                materialNeon.SetColor("_EmissionColor", Color.Lerp(startEmission, securityColor, t));

            yield return null;
        }

        lumiereNeon.color     = securityColor;
        lumiereNeon.intensity = securityIntensity;
        if (materialNeon != null) materialNeon.SetColor("_EmissionColor", securityColor);
    }
}
