using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory instance;

    [Tooltip("Transform MainDuJoueur — les items enfants seront activés/désactivés")]
    public Transform playerHand;

    private string heldItem = "";

    void Awake()
    {
        instance = this;
    }

    public void PickUp(string itemName)
    {
        heldItem = itemName;

        if (playerHand == null) return;

        foreach (Transform child in playerHand)
            child.gameObject.SetActive(child.name == itemName);
    }

    public string GetItem() => heldItem;
}
