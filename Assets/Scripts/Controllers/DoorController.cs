using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public string requiredItem = "Badge"; // Le nom exact de l'objet nécessaire
    public float openAngle = 90f;         // Angle de la porte ouverte
    public float openSpeed = 2f;          // Vitesse de l'animation

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

        if (inventory != null)
        {
            // On vérifie ce que le joueur tient en main dans sa Hotbar
            string heldItem = inventory.GetActiveItem();

            if (heldItem == requiredItem)
            {
                Debug.Log("Access Granted!");
                isOpen = true; 
                
                // Note : On garde l'objet en main. Si tu veux que le badge 
                // soit détruit après utilisation, on rajoutera une petite ligne plus tard !
            }
            else
            {
                Debug.Log("Access Denied. Required: " + requiredItem + ". You are holding: " + heldItem);
            }
        }
    }
}