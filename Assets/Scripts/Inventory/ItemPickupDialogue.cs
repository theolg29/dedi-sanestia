using UnityEngine;

public class ItemPickupDialogue : MonoBehaviour
{
    public AudioClip clip;
    [TextArea(1, 3)]
    public string subtitle;
    public float delay = 0f;

    public void PlayDialogue()
    {
        if (DialogueManager.instance == null)
        {
            GameObject dmObj = new GameObject("DialogueManager");
            dmObj.AddComponent<DialogueManager>();
        }

        var line = new DialogueLine { clip = clip, subtitle = subtitle };
        DialogueManager.instance.PlayDialogue(new[] { line }, delay, 0f);
    }
}
