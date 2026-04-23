using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public GameObject requiredItem;

    [Header("Son")]
    public AudioClip sonOuverture;

    private const float openAngle = 90f;
    private const float openSpeed = 2f;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private AudioSource audioSource;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        Quaternion target = isOpen ? openRotation : closedRotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * openSpeed);
    }

    public bool IsOpen => isOpen;

    public void TryToggle()
    {
        if (isOpen)
        {
            isOpen = false;
            JouerSon();
            return;
        }

        if (requiredItem == null)
        {
            isOpen = true;
            JouerSon();
            return;
        }

        if (PlayerInventory.instance == null) return;

        if (PlayerInventory.instance.GetItem() == requiredItem.name)
        {
            isOpen = true;
            JouerSon();
        }
        else
        {
            Debug.Log("Access Denied. Required: " + requiredItem.name);
        }
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
