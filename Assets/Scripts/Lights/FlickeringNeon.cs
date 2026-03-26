using UnityEngine;
using System.Collections;

public class FlickeringNeon : MonoBehaviour
{
    [Header("Composants à relier")]
    public Light lumiereNeon;
    public Renderer cylindreNeon;
    public AudioSource audioNeon;

    // Le temps minimum et maximum avant de changer d'état (allumé ou éteint)
    private float tempsMin = 0.05f;
    private float tempsMax = 1.0f; 

    private Material materialNeon;
    private Color couleurEmissionInitiale;

    void Start()
    {
        if (cylindreNeon != null)
        {
            materialNeon = cylindreNeon.material;
            couleurEmissionInitiale = materialNeon.GetColor("_EmissionColor");
        }

        StartCoroutine(ClignotementAleatoire());
    }

    IEnumerator ClignotementAleatoire()
    {
        while (true)
        {
            // 1. On attend un temps 100% aléatoire
            yield return new WaitForSeconds(Random.Range(tempsMin, tempsMax));

            // 2. On inverse l'état (si c'était allumé, on éteint, et inversement)
            bool estAllume = !lumiereNeon.enabled;
            lumiereNeon.enabled = estAllume;

            // 3. On applique la couleur et le son
            if (estAllume)
            {
                if (materialNeon != null) materialNeon.SetColor("_EmissionColor", couleurEmissionInitiale);
                if (audioNeon != null) audioNeon.mute = false;
            }
            else
            {
                if (materialNeon != null) materialNeon.SetColor("_EmissionColor", Color.black);
                if (audioNeon != null) audioNeon.mute = true;
            }
        }
    }
}