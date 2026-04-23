using UnityEngine;
using System.Collections;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public GameObject requiredItem;

    [Header("Son")]
    public AudioClip sonOuverture;

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

    public void TryToggle()
    {
        if (isAnimating) return;

        if (isOpen)
        {
            isOpen = false;
            JouerSon();
            StartCoroutine(AnimateRotation(openRotation, closedRotation));
            return;
        }

        if (requiredItem == null)
        {
            Ouvrir();
            return;
        }

        if (PlayerInventory.instance == null) return;

        if (PlayerInventory.instance.GetItem() == requiredItem.name)
            Ouvrir();
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

    private void JouerSon()
    {
        if (sonOuverture == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(sonOuverture);
        else
            AudioSource.PlayClipAtPoint(sonOuverture, transform.position);
    }
}
