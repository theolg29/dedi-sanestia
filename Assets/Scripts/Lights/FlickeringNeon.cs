using UnityEngine;
using System.Collections;

public class FlickeringNeon : MonoBehaviour
{
    [Header("Composants à relier")]
    public Light lumiereNeon;
    public Renderer cylindreNeon;
    public AudioSource audioNeon;

    // Temps de clignotement (On/Off) 100% aléatoire
    private float tempsMin = 0.05f;
    private float tempsMax = 1.0f; 

    private Material materialNeon;
    private Color couleurEmissionInitiale;
    
    // Variables pour le tremblement d'intensité
    private float intensiteInitiale;
    private bool estAllume = true; 

    void Start()
    {
        // On mémorise la puissance d'origine réglée dans l'Inspector
        if (lumiereNeon != null) 
        {
            intensiteInitiale = lumiereNeon.intensity;
        }

        if (cylindreNeon != null)
        {
            materialNeon = cylindreNeon.material;
            couleurEmissionInitiale = materialNeon.GetColor("_EmissionColor");
        }

        StartCoroutine(ClignotementAleatoire());
    }

    void Update()
    {
        // --- LE TREMBLEMENT MASSIF ---
        // Si le néon est "allumé", on fait chuter son intensité de façon brutale.
        if (estAllume && lumiereNeon != null)
        {
            // MODIFICATION ICI : On choisit une valeur aléatoire entre 10% (0.1f) 
            // et 100% de l'intensité de base. La chute est énorme !
            lumiereNeon.intensity = Random.Range(intensiteInitiale * 0.1f, intensiteInitiale);
        }
    }

    IEnumerator ClignotementAleatoire()
    {
        while (true)
        {
            // 1. Attente aléatoire avant de changer d'état
            yield return new WaitForSeconds(Random.Range(tempsMin, tempsMax));

            // 2. Inversion de l'état
            estAllume = !estAllume;
            lumiereNeon.enabled = estAllume;

            // 3. Application du visuel (Matériau) et du son
            if (estAllume)
            {
                // On s'assure que la lumière repart sur une bonne base d'intensité
                lumiereNeon.intensity = intensiteInitiale; 
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