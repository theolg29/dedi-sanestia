using UnityEngine;
using System.Collections; // Nécessaire pour les Coroutines

public class LightBlink : MonoBehaviour
{
    [Header("Paramètres de clignotement")]
    [Tooltip("Temps minimum entre deux changements d'état")]
    [SerializeField] private float minWaitTime = 0.1f;

    [Tooltip("Temps maximum entre deux changements d'état")]
    [SerializeField] private float maxWaitTime = 0.5f;

    private Light myLight;

    void Awake()
    {
        // On récupère le composant Light attaché au même objet
        myLight = GetComponent<Light>();
    }

    void Start()
    {
        if (myLight == null)
        {
            Debug.LogError("Pas de composant 'Light' trouvé sur " + gameObject.name);
            return;
        }

        // On lance la boucle de clignotement
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        // Petite astuce : on ajoute un délai initial aléatoire pour que
        // toutes les lumières ne commencent pas EXACTEMENT au même moment
        // au lancement du jeu.
        yield return new WaitForSeconds(Random.Range(0f, 1f));

        while (true)
        {
            // On inverse l'état de la lumière (On devient Off, Off devient On)
            myLight.enabled = !myLight.enabled;

            // On attend un temps aléatoire avant le prochain changement
            float randomDelay = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(randomDelay);
        }
    }
}