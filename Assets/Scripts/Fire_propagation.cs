using UnityEngine;
using System.Collections.Generic;

public class Fire_propagation : MonoBehaviour
{
    [Header("Settings")]
    public float propagationInterval = 3f; // Temps entre chaque propagation
    public float cellSize = 1f; // Taille d'une case (1x1x1)

    [Header("Visual Evolution")]
    public float minScale = 0.1f;
    public float maxScale = 1f;
    public float durationToMax = 3f; // Temps pour atteindre sa taille max

    [Header("FX Globaux (Fumée & Lumière)")]
    public float flamesForMaxSmoke = 50f; 
    public float maxLightRange = 50f; // Portée maximum de la lumière globale
    public float maxLightIntensity = 10f; // Intensité maximum de la lumière globale

    private float currentScale;
    private float timer = 0f;

    // Références locales
    private Transform hearth;
    private Transform volumeOut;
    private Transform trailOut;
    private Transform particleOut;

    // Références statiques/globales
    private static ParticleSystem sharedSmoke;
    private static Light globalLight;

    // --- SYSTEME DE GRILLE GLOBALE ---
    private static HashSet<Vector3Int> burningCells = new HashSet<Vector3Int>();

    void Start()
    {
        currentScale = minScale;

        // On cherche uniquement les éléments visuels du feu lui-même
        hearth = transform.Find("Hearth");
        volumeOut = transform.Find("Volume_Out");
        trailOut = transform.Find("Trail_Out");
        particleOut = transform.Find("Particle_Out");

        // Enregistre la grille
        Vector3Int gridPos = new Vector3Int(
            Mathf.RoundToInt(transform.position.x / cellSize),
            Mathf.RoundToInt(transform.position.y / cellSize),
            Mathf.RoundToInt(transform.position.z / cellSize)
        );
        burningCells.Add(gridPos);

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            ps.Play();
        }

        UpdateScale();

        // --- RECHERCHE DES FX GLOBAUX ---
        if (sharedSmoke == null)
        {
            GameObject smokeObj = GameObject.Find("FX_Smoke");
            if (smokeObj != null) sharedSmoke = smokeObj.GetComponent<ParticleSystem>();
            else
            {
                ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
                foreach (ParticleSystem ps in allPS) { if (ps.gameObject.name.Contains("FX_Smoke")) { sharedSmoke = ps; break; } }
            }
        }

        // --- RECHERCHE DE LA LUMIÈRE GLOBALE ---
        if (globalLight == null)
        {
            GameObject lightObj = GameObject.Find("Point Light");
            if (lightObj != null) globalLight = lightObj.GetComponent<Light>();
            else
            {
                Light[] allLights = FindObjectsOfType<Light>();
                foreach (Light l in allLights) { if (l.gameObject.name.Contains("Point Light")) { globalLight = l; break; } }
            }
        }

        // --- SECURITE FUMÉE PRINCIPALE ---
        if (sharedSmoke != null)
        {
            var main = sharedSmoke.main;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;
        }
    }

    void OnDestroy()
    {
        Vector3Int gridPos = new Vector3Int(
            Mathf.RoundToInt(transform.position.x / cellSize),
            Mathf.RoundToInt(transform.position.y / cellSize),
            Mathf.RoundToInt(transform.position.z / cellSize)
        );
        burningCells.Remove(gridPos);
    }

    void Update()
    {
        // 1. Évolution de l'échelle des particules de FEU
        if (currentScale < maxScale)
        {
            float scaleSpeed = (maxScale - minScale) / durationToMax;
            currentScale += scaleSpeed * Time.deltaTime;

            if (currentScale >= maxScale) currentScale = maxScale;
            UpdateScale();
        }

        // 2. Propagation
        timer += Time.deltaTime;
        if (timer >= propagationInterval)
        {
            timer = 0f;
            TryPropagate();
        }

        // 3. --- MISE À JOUR CUMULÉE (LUMIÈRE & FUMÉE) ---
        float ratio = (float)burningCells.Count / flamesForMaxSmoke; 

        if (sharedSmoke != null)
        {
            var main = sharedSmoke.main;
            main.startSize = Mathf.Lerp(0.1f, 5f, Mathf.Clamp01(ratio));
        }

        if (globalLight != null)
        {
            globalLight.range = Mathf.Lerp(10f, maxLightRange, Mathf.Clamp01(ratio));
            globalLight.intensity = Mathf.Lerp(1f, maxLightIntensity, Mathf.Clamp01(ratio));
        }
    }

    private void TryPropagate()
    {
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward, Vector3.back,
            Vector3.left, Vector3.right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 temp = directions[i];
            int randomIndex = Random.Range(i, directions.Length);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        foreach (Vector3 dir in directions)
        {
            Vector3 targetPos = transform.position + (dir * cellSize);

            if (IsValidPropagationPosition(targetPos))
            {
                GameObject newFire = Instantiate(gameObject, targetPos, transform.rotation);
                newFire.name = "VFX_FullOpaqueFire_Clone_" + Time.frameCount;
                break; 
            }
        }
    }

    private bool IsValidPropagationPosition(Vector3 pos)
    {
        Vector3Int targetGridPos = new Vector3Int(
            Mathf.RoundToInt(pos.x / cellSize),
            Mathf.RoundToInt(pos.y / cellSize),
            Mathf.RoundToInt(pos.z / cellSize)
        );
        return !burningCells.Contains(targetGridPos);
    }

    private void UpdateScale()
    {
        Vector3 newScale = new Vector3(currentScale, currentScale, currentScale);

        if (hearth != null) hearth.localScale = newScale;
        if (volumeOut != null) volumeOut.localScale = newScale;
        if (trailOut != null) trailOut.localScale = newScale;
        if (particleOut != null) particleOut.localScale = newScale;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticData()
    {
        burningCells.Clear();
    }
}
