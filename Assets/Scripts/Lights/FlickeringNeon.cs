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
        StartCoroutine(TransitionToSecurity());
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
