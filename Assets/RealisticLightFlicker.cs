using UnityEngine;

[RequireComponent(typeof(Light))] // Force l'objet à avoir une lumière
public class RealisticLightFlicker : MonoBehaviour
{
    [Header("Configuration Lumière")]
    [Tooltip("L'intensité normale quand la lumière fonctionne bien")]
    [SerializeField] private float maxIntensity = 1.0f;
    [Tooltip("L'intensité minimum (quand ça grésille)")]
    [SerializeField] private float minIntensity = 0.0f;

    [Header("Vitesse du scintillement")]
    [SerializeField] private float speed = 0.1f; // Vitesse de fluctuation

    [Header("Matériau (Optionnel)")]
    [Tooltip("Glisse ici le MeshRenderer de l'ampoule pour qu'elle s'allume/s'éteigne visuellement")]
    [SerializeField] private Renderer bulbRenderer;
    private Color baseEmissionColor;
    private Material bulbMaterial; // Instance locale pour éviter de tout modifier

    [Header("Audio (Optionnel)")]
    [Tooltip("Un son de buzz électrique en boucle")]
    [SerializeField] private AudioSource buzzSound;

    private Light myLight;
    private float targetIntensity;
    private float currentVelocity; // Pour le lissage (Mathf.SmoothDamp)

    void Start()
    {
        myLight = GetComponent<Light>();

        // Configuration initiale du matériau (Emission)
        if (bulbRenderer != null)
        {
            // On crée une instance du material pour ne pas changer toutes les ampoules en même temps
            bulbMaterial = bulbRenderer.material;

            // On vérifie si le shader a bien une propriété d'émission
            if (bulbMaterial.HasProperty("_EmissionColor"))
            {
                baseEmissionColor = bulbMaterial.GetColor("_EmissionColor");
            }
            else
            {
                Debug.LogWarning("Le matériau assigné n'a pas de propriété _EmissionColor activée !");
            }
        }

        // On démarre avec une intensité aléatoire
        targetIntensity = maxIntensity;
    }

    void Update()
    {
        // 1. Calcul de l'intensité aléatoire
        // On utilise un bruit de Perlin pour un effet plus "électrique" que le pur Random
        float noise = Mathf.PerlinNoise(Time.time * (10f / speed), transform.position.x);

        // Si le bruit est élevé, on reste proche du max, sinon on chute vers le min (effet de court-circuit)
        // J'ajoute un seuil : si le bruit est > 0.4, la lumière est stable, sinon elle déconne.
        float finalIntensity = (noise > 0.4f) ? maxIntensity : Random.Range(minIntensity, maxIntensity * 0.5f);

        // On applique l'intensité à la lumière
        myLight.intensity = finalIntensity;

        // 2. Gestion du Matériau (L'ampoule elle-même)
        if (bulbRenderer != null && bulbMaterial != null)
        {
            // On calcule la couleur d'émission basée sur l'intensité actuelle
            float emissionRatio = finalIntensity / maxIntensity;
            Color finalColor = baseEmissionColor * emissionRatio;

            bulbMaterial.SetColor("_EmissionColor", finalColor);

            // Pour que l'émission se mette à jour en temps réel dans le Global Illumination (optionnel mais joli)
            DynamicGI.SetEmissive(bulbRenderer, finalColor);
        }

        // 3. Gestion du Son
        if (buzzSound != null)
        {
            // Si la lumière est très faible, on coupe le son ou on baisse le volume
            // Ça donne l'impression que le courant ne passe plus
            buzzSound.volume = (finalIntensity / maxIntensity);

            // Optionnel : varier le pitch pour un effet plus dramatique
            buzzSound.pitch = 0.8f + (finalIntensity / maxIntensity) * 0.2f;
        }
    }
}