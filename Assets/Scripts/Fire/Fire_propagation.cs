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

    [Header("FX Globaux (Lumière)")]
    public float flamesForMaxEffects = 500f; // Nombre de feux pour atteindre le max global
    public float maxLightRange = 50f;
    public float maxLightIntensity = 10f;

    [Header("FX_Smoke (Traces noires / Suie au sol)")]
    public float flamesForMaxSoot = 500f; // La suie met LONGTEMPS à atteindre son max
    public float maxSootParticleSize = 0.8f; // Taille max très petite (c'est de la suie, pas un nuage)
    public float maxSootEmissionArea = 5f; // Reste collé au sol, petite zone
    public float maxSootParticlesRate = 10f; // Très peu de particules (traces subtiles)

    [Header("Smoke01 (Fumée atmosphérique qui se dissipe)")]
    public float flamesForMaxSmoke01 = 500f; // Même échelle que le feu
    public float minSmoke01Size = 3f; // Visible immédiatement, même avec 1 seul feu
    public float maxSmoke01Size = 200f; // Taille finale énorme
    public float maxSmoke01EmissionArea = 40f; // Grande zone d'étalement dans l'air
    public float minSmoke01ParticlesRate = 2f; // Quelques wisps de fumée au début
    public float maxSmoke01ParticlesRate = 60f; // Beaucoup de fumée à la fin

    private float currentScale;
    private float timer = 0f;
    private bool isEvaporated = false;

    // Références locales
    private Transform hearth;
    private Transform volumeOut;
    private Transform trailOut;
    private Transform particleOut;

    // Références statiques/globales
    private static ParticleSystem sharedSmoke;
    private static ParticleSystem sharedSmoke01;
    private static Light globalLight;

    // Valeurs de départ (sauvegardées automatiquement)
    private static float initialSmokeRate = -1f;
    private static Vector3 initialSmokeShapeScale;
    private static float initialSmoke01Rate = -1f;
    private static Vector3 initialSmoke01ShapeScale;
    private static float initialLightRange = 0f;
    private static float initialLightIntensity;

    // --- SYSTEME DE GRILLE GLOBALE ---
    private static HashSet<Vector3Int> burningCells = new HashSet<Vector3Int>();

    /// <summary>
    /// Retourne la distance au feu le plus proche depuis une position donnée.
    /// Utilisé par FireProximityVision pour l'effet visuel de proximité.
    /// </summary>
    public static float GetClosestFireDistance(Vector3 position, float cellSize = 1f)
    {
        float closestDist = float.MaxValue;
        foreach (Vector3Int cell in burningCells)
        {
            Vector3 fireWorldPos = new Vector3(cell.x * cellSize, cell.y * cellSize, cell.z * cellSize);
            float dist = Vector3.Distance(position, fireWorldPos);
            if (dist < closestDist) closestDist = dist;
        }
        return closestDist;
    }

    /// <summary>Nombre total de cellules en feu dans la scène.</summary>
    public static int ActiveFireCount => burningCells.Count;

    void Start()
    {
        currentScale = minScale;

        hearth = transform.Find("Hearth");
        volumeOut = transform.Find("Volume_Out");
        trailOut = transform.Find("Trail_Out");
        particleOut = transform.Find("Particle_Out");

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

            if (sharedSmoke != null)
            {
                var main = sharedSmoke.main;
                main.loop = true;
                main.stopAction = ParticleSystemStopAction.None;

                initialSmokeRate = sharedSmoke.emission.rateOverTime.constant;
                initialSmokeShapeScale = sharedSmoke.shape.scale;
            }
        }

        if (sharedSmoke01 == null)
        {
            GameObject smoke01Obj = GameObject.Find("Smoke01");
            if (smoke01Obj != null) sharedSmoke01 = smoke01Obj.GetComponent<ParticleSystem>();
            else
            {
                ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
                foreach (ParticleSystem ps in allPS) { if (ps.gameObject.name.Contains("Smoke01")) { sharedSmoke01 = ps; break; } }
            }

            if (sharedSmoke01 != null)
            {
                var main = sharedSmoke01.main;
                main.loop = true;
                sharedSmoke01.Clear(); // Supprime l'énorme particule de démarrage indésirable !
                main.stopAction = ParticleSystemStopAction.None;

                initialSmoke01Rate = sharedSmoke01.emission.rateOverTime.constant;
                initialSmoke01ShapeScale = sharedSmoke01.shape.scale;
            }
        }

        if (globalLight == null)
        {
            GameObject lightObj = GameObject.Find("Point Light");
            if (lightObj != null) globalLight = lightObj.GetComponent<Light>();
            else
            {
                Light[] allLights = FindObjectsOfType<Light>();
                foreach (Light l in allLights) { if (l.gameObject.name.Contains("Point Light")) { globalLight = l; break; } }
            }

            if (globalLight != null)
            {
                initialLightRange = globalLight.range;
                initialLightIntensity = globalLight.intensity;
            }
        }
    }

    void OnDestroy()
    {
        if (!isEvaporated)
        {
            Vector3Int gridPos = new Vector3Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.y / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize)
            );
            burningCells.Remove(gridPos);
        }
    }

    void Update()
    {
        if (currentScale < maxScale)
        {
            float scaleSpeed = (maxScale - minScale) / durationToMax;
            currentScale += scaleSpeed * Time.deltaTime;

            if (currentScale >= maxScale) currentScale = maxScale;
            UpdateScale();
        }

        timer += Time.deltaTime;
        if (timer >= propagationInterval)
        {
            timer = 0f;
            TryPropagate();

            if (currentScale >= maxScale && IsCompletelySurrounded())
            {
                isEvaporated = true; // Empêche OnDestroy d'effacer sa position de la grille
                Destroy(gameObject); // Optimisation
            }
        }

        // --- MISE À JOUR GLOBALE (FUMÉE & LUMIÈRE) ---

        // ============================================
        // FX_Smoke = SUIE / TRACES NOIRES AU SOL
        // Croissance TRES lente, TRES linéaire, reste petit et subtil
        // ============================================
        if (sharedSmoke != null && initialSmokeRate != -1f)
        {
            // Ratio linéaire pur, pas de courbe. La suie grossit régulièrement, sans surprise.
            float sootRatio = Mathf.Clamp01((float)burningCells.Count / flamesForMaxSoot);

            var main = sharedSmoke.main;
            main.startSize = Mathf.Lerp(0.05f, maxSootParticleSize, sootRatio);

            var shape = sharedSmoke.shape;
            shape.scale = Vector3.Lerp(initialSmokeShapeScale, new Vector3(maxSootEmissionArea, maxSootEmissionArea, maxSootEmissionArea), sootRatio);

            var emission = sharedSmoke.emission;
            emission.rateOverTime = Mathf.Lerp(initialSmokeRate, maxSootParticlesRate, sootRatio);
        }

        // ============================================
        // Smoke01 = FUMÉE ATMOSPHÉRIQUE QUI SE DISSIPE
        // Visible IMMÉDIATEMENT (sqrt = rapide au début, lent à la fin)
        // Grossit régulièrement et domine progressivement la scène
        // ============================================
        if (sharedSmoke01 != null && initialSmoke01Rate != -1f)
        {
            float ratio01 = Mathf.Clamp01((float)burningCells.Count / flamesForMaxSmoke01);
            
            // Courbe SQRT (racine carrée) = l'INVERSE du Pow(2).
            // Avec 1% du feu, on a déjà 10% de l'effet. Visible dès le départ !
            // Puis la croissance ralentit naturellement vers la fin = réaliste.
            float curvedRatio01 = Mathf.Sqrt(ratio01);
            
            var main01 = sharedSmoke01.main;
            main01.startSize = Mathf.Lerp(minSmoke01Size, maxSmoke01Size, curvedRatio01);

            // Zone d'étalement (la fumée se diffuse dans l'air)
            var shape01 = sharedSmoke01.shape;
            shape01.scale = Vector3.Lerp(initialSmoke01ShapeScale, new Vector3(maxSmoke01EmissionArea, maxSmoke01EmissionArea, maxSmoke01EmissionArea), curvedRatio01);

            // Débit de particules (de quelques wisps à un gros nuage)
            var emission01 = sharedSmoke01.emission;
            emission01.rateOverTime = Mathf.Lerp(minSmoke01ParticlesRate, maxSmoke01ParticlesRate, curvedRatio01);
        }

        // ============================================
        // LUMIÈRE GLOBALE
        // ============================================
        if (globalLight != null && initialLightRange != 0f)
        {
            float lightRatio = Mathf.Clamp01((float)burningCells.Count / flamesForMaxEffects);
            globalLight.range = Mathf.Lerp(initialLightRange, maxLightRange, lightRatio);
            globalLight.intensity = Mathf.Lerp(initialLightIntensity, maxLightIntensity, lightRatio);
        }
    }

    private void TryPropagate()
    {
        Vector3[] directions = new Vector3[]
        {
            Vector3.forward, Vector3.back,
            Vector3.left, Vector3.right,
            Vector3.up, Vector3.down
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
        if (burningCells.Contains(targetGridPos)) return false;

        bool isInsideSolidWall = false;
        bool isTouchingSurface = false;
        
        Vector3 testCenter = pos + (Vector3.up * (cellSize * 0.1f));
        float half = (cellSize / 2f);

        Collider[] insideColliders = Physics.OverlapBox(testCenter, new Vector3(half * 0.4f, half * 0.4f, half * 0.4f));
        foreach (Collider col in insideColliders)
        {
            if (!col.isTrigger && col.GetComponentInParent<Fire_propagation>() == null)
            {
                isInsideSolidWall = true;
                break;
            }
        }

        if (isInsideSolidWall) return false; 

        Collider[] adjacentColliders = Physics.OverlapBox(testCenter, new Vector3(half * 1.3f, half * 1.3f, half * 1.3f));
        foreach (Collider col in adjacentColliders)
        {
            if (!col.isTrigger && col.GetComponentInParent<Fire_propagation>() == null)
            {
                isTouchingSurface = true;
                break;
            }
        }

        return isTouchingSurface;
    }

    private bool IsCompletelySurrounded()
    {
        Vector3Int pos = new Vector3Int(
            Mathf.RoundToInt(transform.position.x / cellSize),
            Mathf.RoundToInt(transform.position.y / cellSize),
            Mathf.RoundToInt(transform.position.z / cellSize)
        );

        int burningNeighbors = 0;

        if (burningCells.Contains(pos + new Vector3Int(1, 0, 0))) burningNeighbors++;
        if (burningCells.Contains(pos + new Vector3Int(-1, 0, 0))) burningNeighbors++;
        if (burningCells.Contains(pos + new Vector3Int(0, 1, 0))) burningNeighbors++;
        if (burningCells.Contains(pos + new Vector3Int(0, -1, 0))) burningNeighbors++;
        if (burningCells.Contains(pos + new Vector3Int(0, 0, 1))) burningNeighbors++;
        if (burningCells.Contains(pos + new Vector3Int(0, 0, -1))) burningNeighbors++;

        return burningNeighbors >= 4;
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
