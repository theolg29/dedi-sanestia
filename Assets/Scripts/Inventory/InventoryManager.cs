using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI; // Obligatoire pour modifier des images (Icons)

// Petite structure pour lier un Nom d'objet à son Image
[System.Serializable]
public struct ItemData
{
    public string itemName;
    public Sprite itemIcon;
}

public class InventoryManager : MonoBehaviour
{
    [Header("Hotbar UI (Minecraft Style)")]
    public Image[] slotBackgrounds; // Glisser Slot_1, Slot_2...
    public Image[] slotIcons;       // Glisser leurs enfants Icone_Objet...
    public Color selectedColor = Color.white; // Couleur de la case sélectionnée
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Couleur de la case non sélectionnée
    
    [Space(10)] // Fait un petit espace visuel dans l'Inspector
    public List<ItemData> itemDatabase; // La base de données de tes icônes

    [Header("Old UI (Menu I)")]
    public GameObject inventoryMenu;
    public TextMeshProUGUI itemListText;

    [Header("Player Hand")]
    public Transform playerHand;

    [Header("Throw Settings")]
    public float throwForce = 10f;

    private List<string> inventoryItems = new List<string>();
    private int activeItemIndex = -1;
    private bool isInventoryOpen = false;

    void Start()
    {
        inventoryMenu.SetActive(false);
        foreach (Transform child in playerHand)
        {
            child.gameObject.SetActive(false);
        }
        UpdateHotbarUI(); // Met à jour l'interface au lancement
    }

    void Update()
    {
        // 1. MENU
        if (Input.GetKeyDown(KeyCode.I))
        {
            isInventoryOpen = !isInventoryOpen;
            inventoryMenu.SetActive(isInventoryOpen);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && isInventoryOpen)
        {
            isInventoryOpen = false;
            inventoryMenu.SetActive(false);
        }

        // 2 & 3. MOLETTE & LANCER
        if (inventoryItems.Count > 0)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0f) ChangeItem(1);
            else if (scroll < 0f) ChangeItem(-1);

            if (Input.GetKeyDown(KeyCode.G)) DropItem(false);
            else if (Input.GetMouseButtonUp(1)) DropItem(true);
        }
    }

    // ANCIENNEMENT AjouterObjet
    public void AddItem(string itemName) 
    {
        // On vérifie qu'on a encore de la place dans la Hotbar !
        if (!inventoryItems.Contains(itemName) && inventoryItems.Count < slotIcons.Length)
        {
            inventoryItems.Add(itemName);
            UpdateText();
            
            if (inventoryItems.Count == 1)
            {
                activeItemIndex = 0;
                EquipActiveItem();
            }
            else
            {
                UpdateHotbarUI();
            }
        }
        else if (inventoryItems.Count >= slotIcons.Length)
        {
            Debug.Log("L'inventaire est plein !");
        }
    }

    // ANCIENNEMENT JeterObjet
    void DropItem(bool isThrow) 
    {
        string itemToEquip = inventoryItems[activeItemIndex];

        foreach (Transform child in playerHand)
        {
            if (child.name == itemToEquip)
            {
                GameObject droppedItem = Instantiate(child.gameObject, playerHand.position + playerHand.forward * 1.5f, playerHand.rotation);
                
                droppedItem.name = itemToEquip; 
                droppedItem.SetActive(true);    
                droppedItem.tag = "Item";       
                droppedItem.transform.localScale = Vector3.one; 
                
                if (droppedItem.GetComponent<Collider>() == null) droppedItem.AddComponent<BoxCollider>();
                
                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                if (rb == null) rb = droppedItem.AddComponent<Rigidbody>();

                if (isThrow) rb.AddForce(playerHand.forward * throwForce, ForceMode.Impulse);

                child.gameObject.SetActive(false);
            }
        }

        inventoryItems.RemoveAt(activeItemIndex);
        UpdateText();

        if (inventoryItems.Count == 0) activeItemIndex = -1;
        else
        {
            if (activeItemIndex >= inventoryItems.Count) activeItemIndex = inventoryItems.Count - 1;
            EquipActiveItem();
        }

        UpdateHotbarUI();
    }

    void UpdateText()
    {
        itemListText.text = "INVENTORY :\n\n";
        foreach (string item in inventoryItems) itemListText.text += "- " + item + "\n";
    }

    void ChangeItem(int direction)
    {
        activeItemIndex += direction;
        if (activeItemIndex >= inventoryItems.Count) activeItemIndex = 0;
        else if (activeItemIndex < 0) activeItemIndex = inventoryItems.Count - 1;
        EquipActiveItem();
    }

    void EquipActiveItem()
    {
        if (activeItemIndex < 0 || activeItemIndex >= inventoryItems.Count) return;

        string itemToEquip = inventoryItems[activeItemIndex];

        foreach (Transform child in playerHand)
        {
            child.gameObject.SetActive(child.name == itemToEquip);
        }

        UpdateHotbarUI(); // Met à jour la surbrillance de la case
    }

    // ANCIENNEMENT ObtenirObjetActif
    public string GetActiveItem() 
    {
        if (activeItemIndex >= 0 && activeItemIndex < inventoryItems.Count) return inventoryItems[activeItemIndex];
        return ""; 
    }

    // --- LA MAGIE DE LA HOTBAR ---
    void UpdateHotbarUI()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            // 1. Gérer l'affichage de l'icône
            if (i < inventoryItems.Count)
            {
                slotIcons[i].sprite = GetIconFor(inventoryItems[i]);
                slotIcons[i].enabled = (slotIcons[i].sprite != null); // Cache l'icône s'il n'y a pas d'image
            }
            else
            {
                slotIcons[i].sprite = null;
                slotIcons[i].enabled = false;
            }

            // 2. Gérer la surbrillance du fond
            if (i < slotBackgrounds.Length)
            {
                if (i == activeItemIndex) slotBackgrounds[i].color = selectedColor;
                else slotBackgrounds[i].color = normalColor;
            }
        }
    }

    Sprite GetIconFor(string itemName)
    {
        foreach (ItemData data in itemDatabase)
        {
            if (data.itemName == itemName) return data.itemIcon;
        }
        return null;
    }
}