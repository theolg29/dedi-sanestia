using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public GameObject requiredItem;

    [Header("Son")]
    public AudioClip sonOuverture;
    public AudioClip sonVerrouille;

    private const float openAngle    = 90f;
    private const float animDuration = 0.6f;

    private bool isOpen      = false;
    private bool isAnimating = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private AudioSource audioSource;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation   = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        audioSource    = GetComponent<AudioSource>();
    }

    public bool IsOpen => isOpen;
    public bool IsLocked => requiredItem != null && PlayerInventory.instance?.GetItem() != requiredItem.name;

    public bool TryToggle()
    {
        if (isAnimating) return false;

        if (isOpen)
        {
            isOpen = false;
            JouerSon();
            StartCoroutine(AnimateRotation(openRotation, closedRotation));
            return true;
        }

        if (requiredItem == null)
        {
            Ouvrir();
            return true;
        }

        if (PlayerInventory.instance == null) return false;

        if (PlayerInventory.instance.GetItem() == requiredItem.name)
        {
            Ouvrir();
            return true;
        }

        return false;
    }

    private void Ouvrir()
    {
        isOpen = true;
        JouerSon();
        StartCoroutine(AnimateRotation(closedRotation, openRotation));
    }

    private IEnumerator AnimateRotation(Quaternion from, Quaternion to)
    {
        isAnimating = true;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(from, to, elapsed / animDuration);
            yield return null;
        }

        transform.rotation = to;
        isAnimating = false;
    }

    public void JouerSonVerrouille()
    {
        if (sonVerrouille == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(sonVerrouille);
        else
            AudioSource.PlayClipAtPoint(sonVerrouille, transform.position);
    }

    private void JouerSon()
    {
        if (sonOuverture == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(sonOuverture);
        else
            AudioSource.PlayClipAtPoint(sonOuverture, transform.position);
    }
}
