using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Trigger de dialogue basé sur la position (Collider en mode IsTrigger)
/// Joue automatiquement la séquence via DialogueManager
/// </summary>
public class AreaDialogueTrigger : MonoBehaviour
{
    [Header("Configuration Dialogue")]
    public DialogueLine[] lines;
    public float delayBeforeStart = 0f;
    public float pauseBetweenLines = 0.5f;

    [Header("Parametres de declenchement")]
    [Tooltip("Détruit le GameObject après la lecture pour ne le jouer qu'une fois ?")]
    public bool triggerOnce = true;
    
    [Header("Condition speciale")]
    [Tooltip("Si vrai, le dialogue ne se déclenche QUE si le courant est coupé")]
    public bool requirePowerCut = false;

    private bool _hasTriggered = false;

    // Helper Inspector - Charger les clips de Resources
    [ContextMenu("Auto-Load Clips from Resources")]
    private void AutoLoadClips()
    {
        if (lines == null) return;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].clip == null)
            {
                // Cherche le fichier audio basé sur le texte ou assigné manuellement
                Debug.Log($"[AreaDialogueTrigger] {gameObject.name}: Remplir le clip manuellement via l'Inspector, ou assigner le nom du fichier.");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered || !other.CompareTag("Player")) return;

        if (requirePowerCut && !PowerCutTrigger.PowerIsCut) return;

        _hasTriggered = true;

        // Auto-load missing clips based on known subtitles
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].clip == null && !string.IsNullOrEmpty(lines[i].subtitle))
                {
                    string sub = lines[i].subtitle.ToLower();
                    string clipName = "";

                    if (sub.Contains("bâtiment brûle") || sub.Contains("batiment brule")) clipName = "le_batiments_brule";
                    else if (sub.Contains("tant pis")) clipName = "tant_pis, je crame pas";
                    else if (sub.Contains("courant d'urgence")) clipName = "courant_remis";
                    else if (sub.Contains("super proche")) clipName = "le_feu_est_proche";
                    else if (sub.Contains("ascenseur") || sub.Contains("hélico") || sub.Contains("helico")) clipName = "helicot";
                    else if (sub.Contains("quel bordel")) clipName = "trouve_les_clefs";
                    else if (sub.Contains("partir tout de suite")) clipName = "je_dois_partir";

                    if (!string.IsNullOrEmpty(clipName))
                    {
                        lines[i].clip = Resources.Load<AudioClip>(clipName);
                        if (lines[i].clip == null)
                            Debug.LogWarning($"[AreaDialogueTrigger] Impossible de trouver l'audio '{clipName}' dans Resources !");
                    }
                }
            }
        }

        if (DialogueManager.instance == null)
        {
            // Auto-create DialogueManager if missing
            GameObject dmObj = new GameObject("DialogueManager");
            dmObj.AddComponent<DialogueManager>();
        }

        DialogueManager.instance.PlayDialogue(lines, delayBeforeStart, pauseBetweenLines);

        if (triggerOnce)
        {
            // Désactiver le collider pour éviter de re-trigger
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
