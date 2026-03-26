using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactDistance = 3f; 
    public Camera playerCamera;

    void Update()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            // --- RAMASSER UN OBJET ---
            if (hit.collider.CompareTag("Item"))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Item picked up: " + hit.collider.gameObject.name);
                    
                    FindFirstObjectByType<InventoryManager>().AddItem(hit.collider.gameObject.name);
                    
                    Destroy(hit.collider.gameObject);
                }
            }

            // --- OUVRIR UNE PORTE ---
            if (hit.collider.CompareTag("Door") && Input.GetKeyDown(KeyCode.E))
            {
                DoorController targetedDoor = hit.collider.GetComponent<DoorController>();
                if (targetedDoor != null)
                {
                    targetedDoor.TryOpen(); 
                }
            }
        }
    }
}