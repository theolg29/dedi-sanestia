using UnityEngine;

[DefaultExecutionOrder(-10)]
public class NeonManager : MonoBehaviour
{
    [Header("Lumière normale")]
    public Color normalColor     = Color.white;
    public float normalIntensity = 1f;

    [Header("Lumière de sécurité")]
    public Color securityColor     = new Color(0.2f, 0.5f, 1f);
    public float securityIntensity = 0.8f;

    void Awake()
    {
        foreach (FlickeringNeon neon in FindObjectsByType<FlickeringNeon>(FindObjectsSortMode.None))
        {
            neon.normalColor       = normalColor;
            neon.normalIntensity   = normalIntensity;
            neon.securityColor     = securityColor;
            neon.securityIntensity = securityIntensity;
        }
    }
}
