using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("Lumière")]
    public Light maLumiere;

    [Header("Sons")]
    public AudioSource sourceAudio;
    public AudioClip   sonAllumer;
    public AudioClip   sonEteindre;

    [Header("Contrôles")]
    public KeyCode toucheAllumer = KeyCode.F;

    void Update()
    {
        if (transform.parent == null || transform.parent.name != "MainDuJoueur") return;
        if (!Input.GetKeyDown(toucheAllumer) || maLumiere == null) return;

        maLumiere.enabled = !maLumiere.enabled;

        if (sourceAudio != null)
        {
            AudioClip clip = maLumiere.enabled ? sonAllumer : sonEteindre;
            if (clip != null) sourceAudio.PlayOneShot(clip);
        }
    }
}
