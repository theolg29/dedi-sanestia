using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;

    [Tooltip("Transform MainDuJoueur - l'item ramassé sera reparenté ici")]
    public Transform playerHand;

    [Header("Position de l'item dans la main")]
    public Vector3 holdPosition = Vector3.zero;
    public Vector3 holdRotation = Vector3.zero;

    [Header("Jet")]
    public float throwForce = 8f;

    private GameObject heldObject;
    private bool       heldIsPreExisting;

    void Awake() => instance = this;

    public void PickUp(GameObject item)
    {
        if (playerHand == null) return;

        // If a same-named child already exists in the hand (e.g. Flashlight pre-placed in rig),
        // activate it and destroy the world object instead of reparenting.
        Transform existing = playerHand.Find(item.name);
        if (existing != null)
        {
            heldObject        = existing.gameObject;
            heldIsPreExisting = true;
            existing.gameObject.SetActive(true);
            Destroy(item);
            return;
        }

        heldObject        = item;
        heldIsPreExisting = false;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (Collider col in item.GetComponentsInChildren<Collider>())
            col.enabled = false;

        ItemPickupDialogue dialogue = item.GetComponent<ItemPickupDialogue>();
        if (dialogue != null)
        {
            dialogue.PlayDialogue();
        }

        item.transform.SetParent(playerHand, true);
        item.transform.localPosition = holdPosition;
        item.transform.localRotation = Quaternion.Euler(holdRotation);
    }

    public void Throw(Vector3 spawnPosition, Vector3 direction)
    {
        if (heldObject == null || heldIsPreExisting) return;

        GameObject obj = heldObject;
        heldObject = null;

        obj.transform.SetParent(null);
        obj.transform.position = spawnPosition;

        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = true;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic   = false;
            rb.linearVelocity = direction * throwForce;
        }
    }

    public string GetItem() => heldObject != null ? heldObject.name : "";
    public bool   HasItem() => heldObject != null;
}
