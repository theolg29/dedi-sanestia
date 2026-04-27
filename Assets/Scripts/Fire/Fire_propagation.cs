using UnityEngine;
using System.Collections.Generic;

public class Fire_propagation : MonoBehaviour
{
    [Header("Settings")]
    public float cellSize = 1f; // Taille d'une case (1x1x1)

    [Header("Visual Evolution")]
    public float minScale = 0.1f;
    public float maxScale = 1f;
    public float durationToMax = 3f; // Temps pour atteindre sa taille max

    [Header("Propagation Réaliste")]
    [Tooltip("Délai fixe entre chaque tentative de propagation (plus gérable)")]
    public float propagationInterval = 15f;
    [Tooltip("Probabilité (0-1) que chaque direction propage le feu")]
    public float propagationChance = 0.07f;
    [Tooltip("Nombre max de propagations simultanées par tick")]
    public int maxSimultaneousSpreads = 1;
    [Tooltip("Nombre maximum de flammes dans la scène (0 = illimité)")]
    public int maxFireCount = 60;

    [Header("FX Globaux (Lumière)")]
    public float flamesForMaxEffects = 500f;
    public float maxLightRange = 50f;
    public float maxLightIntensity = 10f;

    [Header("Light Flicker (Scintillement)")]
    [Tooltip("Vitesse du scintillement de la lumière")]
    public float lightFlickerSpeed = 8f;
    [Tooltip("Amplitude du scintillement (0.15 = ±15%)")]
    public float lightFlickerAmount = 0.15f;
    [Tooltip("Couleur de la lumière au début (peu de feu)")]
    public Color lightColorStart = new Color(1f, 0.6f, 0.1f, 1f); // Orange doux
    [Tooltip("Couleur de la lumière au max (beaucoup de feu)")]
    public Color lightColorEnd = new Color(1f, 0.25f, 0f, 1f); // Orange/rouge intense

    [Header("FX_Smoke (Fumée noire / Suie)")]
    public float flamesForMaxSoot = 200f; // S'emballe plus vite
    public float minSootParticleSize = 0.02f;
    public float maxSootParticleSize = 3f;
    public float minSootEmissionArea = 0.2f;
    public float maxSootEmissionArea = 20f;
    public float minSootParticlesRate = 0.5f;
    public float maxSootParticlesRate = 50f;

    [Header("Smoke01 (Fumée atmosphérique lointaine)")]
    public float flamesForMaxSmoke01 = 150f; // Atteint sa taille max TRÈS vite
    [Tooltip("Nombre de flammes pour déclencher la montée massive de fumée")]
    public float flamesForSmoke01Trigger = 5f;
    [Tooltip("Taille de départ (petit wisp visible dans l'air)")]
    public float minSmoke01Size = 1f;
    [Tooltip("Taille finale très large (atmosphérique)")]
    public float maxSmoke01Size = 600f; // 400->600
    [Tooltip("Zone d'émission de départ (concentrée)")]
    public float minSmoke01EmissionArea = 1f;
    [Tooltip("Zone d'émission finale très large (couvre le bâtiment entier)")]
    public float maxSmoke01EmissionArea = 250f; // 150->250
    public float minSmoke01ParticlesRate = 1f;
    public float maxSmoke01ParticlesRate = 200f; // 120->200

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

    // Flag pour que le code FX global ne tourne qu'une seule fois par frame
    private static int lastFXUpdateFrame = -1;

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
                sharedSmoke01.Clear();
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

        // --- PROPAGATION À VITESSE FIXE (Progressive) ---
        timer += Time.deltaTime;

        if (timer >= propagationInterval)
        {
            timer = 0f;
            TryPropagate();

            if (currentScale >= maxScale && IsCompletelySurrounded())
            {
                isEvaporated = true;
                Destroy(gameObject);
            }
        }

        // --- MISE À JOUR GLOBALE (FUMÉE & LUMIÈRE) ---
        // Une seule instance par frame exécute ce bloc pour éviter le travail en O(n)
        if (lastFXUpdateFrame == Time.frameCount) return;
        lastFXUpdateFrame = Time.frameCount;

        // ============================================
        // FX_Smoke = FUMÉE NOIRE / SUIE
        // Courbe CUBIQUE : quasi invisible longtemps, explose quand beaucoup de flammes
        // ============================================
        if (sharedSmoke != null && initialSmokeRate != -1f)
        {
            float sootRatio = Mathf.Clamp01((float)burningCells.Count / flamesForMaxSoot);

            // Courbe cubique (pow 3) = reste minuscule longtemps, puis explose
            float curvedSootRatio = sootRatio * sootRatio * sootRatio;

            var main = sharedSmoke.main;
            main.startSize = Mathf.Lerp(minSootParticleSize, maxSootParticleSize, curvedSootRatio);

            var shape = sharedSmoke.shape;
            float area = Mathf.Lerp(minSootEmissionArea, maxSootEmissionArea, curvedSootRatio);
            shape.scale = new Vector3(area, area, area);

            var emission = sharedSmoke.emission;
            emission.rateOverTime = Mathf.Lerp(minSootParticlesRate, maxSootParticlesRate, curvedSootRatio);
        }

        // ============================================
        // Smoke01 = FUMÉE ATMOSPHÉRIQUE LOINTAINE
        // Deux phases :
        //   1) Toujours un peu de fumée dans l'air (wisps légers)
        //   2) Après ~10 flammes : explosion massive en rayon et intensité
        // ============================================
        if (sharedSmoke01 != null && initialSmoke01Rate != -1f)
        {
            int fireCount = burningCells.Count;
            float ratio01 = Mathf.Clamp01((float)fireCount / flamesForMaxSmoke01);

            // Phase 1 : base légère, toujours visible (petits wisps dans l'air)
            // Phase 2 : après le trigger (~10 flammes), EXPLOSION IMMÉDIATE (pow 0.3)
            float triggerRatio = Mathf.Clamp01((float)fireCount / flamesForSmoke01Trigger);
            float postTrigger = Mathf.Clamp01(((float)fireCount - flamesForSmoke01Trigger) / (flamesForMaxSmoke01 - flamesForSmoke01Trigger));
            
            // Pow(0.3) donne une courbe qui monte EN FLÈCHE dès le début, puis ralentit
            float explosiveGrowth = Mathf.Pow(postTrigger, 0.3f); 

            // Mélange : petit ratio de base (wisps) + grosse croissance après trigger
            float basePortion = triggerRatio * 0.05f; // 5% max avant le trigger (wisps légers)
            float curvedRatio01 = basePortion + explosiveGrowth * 0.95f; // 95% restant après trigger
            curvedRatio01 = Mathf.Clamp01(curvedRatio01);

            var main01 = sharedSmoke01.main;
            main01.startSize = Mathf.Lerp(minSmoke01Size, maxSmoke01Size, curvedRatio01);

            // Zone d'émission : de concentrée à TRÈS large (tout le bâtiment)
            var shape01 = sharedSmoke01.shape;
            float area01 = Mathf.Lerp(minSmoke01EmissionArea, maxSmoke01EmissionArea, curvedRatio01);
            shape01.scale = new Vector3(area01, area01, area01);

            // Débit de particules : wisps au début, beaucoup après trigger
            var emission01 = sharedSmoke01.emission;
            emission01.rateOverTime = Mathf.Lerp(minSmoke01ParticlesRate, maxSmoke01ParticlesRate, curvedRatio01);
        }

        // ============================================
        // LUMIÈRE GLOBALE — Scintillement + Couleur dynamique
        // ============================================
        if (globalLight != null && initialLightRange != 0f)
        {
            float lightRatio = Mathf.Clamp01((float)burningCells.Count / flamesForMaxEffects);

            // Courbe pow(0.7) = monte vite au début → danger immédiat
            float curvedLightRatio = Mathf.Pow(lightRatio, 0.7f);

            // Valeurs de base (sans scintillement)
            float baseRange = Mathf.Lerp(initialLightRange, maxLightRange, curvedLightRatio);
            float baseIntensity = Mathf.Lerp(initialLightIntensity, maxLightIntensity, curvedLightRatio);

            // Scintillement réaliste via Perlin Noise (deux fréquences pour plus d'organicité)
            float flicker1 = Mathf.PerlinNoise(Time.time * lightFlickerSpeed, 0f);
            float flicker2 = Mathf.PerlinNoise(0f, Time.time * lightFlickerSpeed * 1.7f);
            float flickerValue = ((flicker1 + flicker2) / 2f - 0.5f) * 2f; // Normalisé [-1, 1]
            float flickerMultiplier = 1f + flickerValue * lightFlickerAmount;

            globalLight.range = baseRange * flickerMultiplier;
            globalLight.intensity = baseIntensity * flickerMultiplier;

            // Couleur dynamique : orange doux → orange/rouge intense
            globalLight.color = Color.Lerp(lightColorStart, lightColorEnd, curvedLightRatio);
        }
    }

    private void TryPropagate()
    {
        if (maxFireCount > 0 && burningCells.Count >= maxFireCount) return;

        // 14 directions : 6 cardinales + 8 diagonales horizontales (propagation en éventail)
        Vector3[] directions = new Vector3[]
        {
            // Cardinales horizontales
            Vector3.forward, Vector3.back,
            Vector3.left, Vector3.right,
            // Verticales
            Vector3.up, Vector3.down,
            // Diagonales horizontales (le feu se propage en éventail dans un bâtiment)
            new Vector3(1, 0, 1).normalized * 1.41f,
            new Vector3(1, 0, -1).normalized * 1.41f,
            new Vector3(-1, 0, 1).normalized * 1.41f,
            new Vector3(-1, 0, -1).normalized * 1.41f,
            // Diagonales vers le haut (le feu monte en éventail)
            new Vector3(1, 1, 0).normalized * 1.41f,
            new Vector3(-1, 1, 0).normalized * 1.41f,
            new Vector3(0, 1, 1).normalized * 1.41f,
            new Vector3(0, 1, -1).normalized * 1.41f
        };

        // Mélanger aléatoirement les directions
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 temp = directions[i];
            int randomIndex = Random.Range(i, directions.Length);
            directions[i] = directions[randomIndex];
            directions[randomIndex] = temp;
        }

        int spreadCount = 0;

        foreach (Vector3 dir in directions)
        {
            if (spreadCount >= maxSimultaneousSpreads) break;

            // Chaque direction a une chance individuelle de propager
            if (Random.value > propagationChance) continue;

            Vector3 targetPos = transform.position + (dir * cellSize);

            if (IsValidPropagationPosition(targetPos))
            {
                GameObject newFire = Instantiate(gameObject, targetPos, transform.rotation);
                newFire.name = "VFX_FullOpaqueFire_Clone_" + Time.frameCount;
                spreadCount++;
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

        // === VÉRIFICATION 1 : La position n'est PAS à l'intérieur d'un mur ===
        Vector3 testCenter = pos + (Vector3.up * (cellSize * 0.1f));
        float half = (cellSize / 2f);

        Collider[] insideColliders = Physics.OverlapBox(testCenter, new Vector3(half * 0.4f, half * 0.4f, half * 0.4f));
        foreach (Collider col in insideColliders)
        {
            if (!col.isTrigger && col.GetComponentInParent<Fire_propagation>() == null)
            {
                return false; // À l'intérieur d'un mur
            }
        }

        // === VÉRIFICATION 2 : Le feu doit TOUCHER une surface physique adjacente ===
        // Raycasts dans 6 directions pour détecter une surface adjacente.
        // Le feu ne peut PAS se propager dans le vide / l'air.
        float rayLength = cellSize * 0.75f;
        Vector3[] rayDirs = new Vector3[]
        {
            Vector3.down, Vector3.up,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };

        foreach (Vector3 dir in rayDirs)
        {
            if (Physics.Raycast(testCenter, dir, out RaycastHit hit, rayLength))
            {
                if (!hit.collider.isTrigger && hit.collider.GetComponentInParent<Fire_propagation>() == null)
                {
                    return true; // Touche un vrai modèle 3D valide (mur/sol) en intérieur
                }
            }
        }

        return false; // Aucune surface physique à proximité = vide / air
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
        sharedSmoke = null;
        sharedSmoke01 = null;
        globalLight = null;
        initialSmokeRate = -1f;
        initialSmoke01Rate = -1f;
        initialLightRange = 0f;
        lastFXUpdateFrame = -1;
    }
}
