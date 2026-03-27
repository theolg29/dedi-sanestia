using UnityEngine;
using TMPro; // Obligatoire pour utiliser TextMeshPro
using System.Collections.Generic;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Hotbar UI (Minecraft Style)")]
    public Image[] slotBackgrounds;     // Tes cases (Slot_1, Slot_2...)
    public TextMeshProUGUI[] slotTexts; // NOUVEAU : Tes Textes (Texte_Objet...)
    
    public Color selectedColor = Color.white; 
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

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
        UpdateHotbarUI();
    }

    void Update()
    {
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

        if (inventoryItems.Count > 0)
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll > 0f) ChangeItem(1);
            else if (scroll < 0f) ChangeItem(-1);

            if (Input.GetKeyDown(KeyCode.G)) DropItem(false);
            else if (Input.GetMouseButtonUp(1)) DropItem(true);
        }
    }

    public void AddItem(string itemName) 
    {
        if (!inventoryItems.Contains(itemName) && inventoryItems.Count < slotTexts.Length)
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
    }

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

        UpdateHotbarUI(); 
    }

    public string GetActiveItem() 
    {
        if (activeItemIndex >= 0 && activeItemIndex < inventoryItems.Count) return inventoryItems[activeItemIndex];
        return ""; 
    }

    void UpdateHotbarUI()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            // 1. Affiche le NOM de l'objet au lieu d'une image
            if (i < inventoryItems.Count)
            {
                slotTexts[i].text = inventoryItems[i]; // Écrit "Badge"
            }
            else
            {
                slotTexts[i].text = ""; // Case vide
            }

            // 2. Gérer la surbrillance du fond
            if (i < slotBackgrounds.Length)
            {
                if (i == activeItemIndex) slotBackgrounds[i].color = selectedColor;
                else slotBackgrounds[i].color = normalColor;
            }
        }
    }
}