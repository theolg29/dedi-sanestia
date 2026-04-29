using UnityEngine;

/// <summary>
/// A attacher directement au GameObject de l'item (ex: les clefs de l'helico).
/// Sera declenche par PlayerInventory lors du PickUp().
/// </summary>
public class ItemPickupDialogue : MonoBehaviour
{
    public DialogueLine[] lines;
    public float delayBeforeStart = 0f;
    public float pauseBetweenLines = 0.5f;

    public void PlayDialogue()
    {
        // Auto-load missing clips
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].clip == null && !string.IsNullOrEmpty(lines[i].subtitle))
                {
                    if (lines[i].subtitle.ToLower().Contains("partir tout de suite"))
                    {
                        lines[i].clip = Resources.Load<AudioClip>("je_dois_partir");
                        if (lines[i].clip == null)
                            Debug.LogWarning("[ItemPickupDialogue] Impossible de trouver l'audio 'je_dois_partir' dans Resources !");
                    }
                }
            }
        }

        if (DialogueManager.instance == null)
        {
            GameObject dmObj = new GameObject("DialogueManager");
            dmObj.AddComponent<DialogueManager>();
        }

        DialogueManager.instance.PlayDialogue(lines, delayBeforeStart, pauseBetweenLines);
    }
}
