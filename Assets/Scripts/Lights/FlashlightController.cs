using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    [Header("La Lumière")]
    public Light maLumiere;

    [Header("Sons (Audio)")]
    public AudioSource sourceAudio; // Le lecteur de son
    public AudioClip sonAllumer;    // Le fichier son "Clic On"
    public AudioClip sonEteindre;   // Le fichier son "Clic Off"

    [Header("Contrôles")]
    public KeyCode toucheAllumer = KeyCode.F;

    void Update()
    {
        // On vérifie que la lampe est bien dans la main du joueur
        if (transform.parent != null && transform.parent.name == "MainDuJoueur")
        {
            if (Input.GetKeyDown(toucheAllumer))
            {
                if (maLumiere != null)
                {
                    // 1. On inverse l'état de la lumière
                    maLumiere.enabled = !maLumiere.enabled;

                    // 2. On joue le bon son en fonction du nouvel état
                    if (sourceAudio != null)
                    {
                        if (maLumiere.enabled && sonAllumer != null)
                        {
                            sourceAudio.PlayOneShot(sonAllumer); // Joue le son ON
                        }
                        else if (!maLumiere.enabled && sonEteindre != null)
                        {
                            sourceAudio.PlayOneShot(sonEteindre); // Joue le son OFF
                        }
                    }
                }
            }
        }
    }
}