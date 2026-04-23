using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public GameObject requiredItem;  // Glisser ici le prefab de l'objet nécessaire

    private const float openAngle = 90f;
    private const float openSpeed = 2f;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    private InventoryManager inventory;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));
        inventory = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        if (isOpen)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, openRotation, Time.deltaTime * openSpeed);
        }
    }

    public void TryOpen()
    {
        if (isOpen) return;
        if (inventory == null) return;
        if (requiredItem == null)
        {
            // Aucun item requis : la porte s'ouvre librement
            isOpen = true;
            return;
        }

        string heldItem = inventory.GetActiveItem();

        if (heldItem == requiredItem.name)
        {
            Debug.Log("Access Granted!");
            isOpen = true;
        }
        else
        {
            Debug.Log("Access Denied. Required: " + requiredItem.name + ". You are holding: " + heldItem);
        }
    }
}
