using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    [Header("UI (Menu I)")]
    public GameObject menuInventaire;
    public TextMeshProUGUI texteListeObjets;

    [Header("Objet en main (Molette)")]
    public Transform mainDuJoueur;

    [Header("Paramètres de Lancer")]
    public float forceDeLancer = 10f;

    private List<string> objetsPossedes = new List<string>();
    private int indexObjetActif = -1;
    private bool inventaireOuvert = false;

    void Start()
    {
        menuInventaire.SetActive(false);
        foreach (Transform enfant in mainDuJoueur)
        {
            enfant.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 1. GESTION DU MENU (Touche I ou Échap)
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventaireOuvert = !inventaireOuvert;
            menuInventaire.SetActive(inventaireOuvert);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && inventaireOuvert)
        {
            inventaireOuvert = false;
            menuInventaire.SetActive(false);
        }

        // Si on a au moins 1 objet en main...
        if (objetsPossedes.Count > 0)
        {
            // 2. GESTION DE LA MOLETTE
            float molette = Input.mouseScrollDelta.y;
            if (molette > 0f) ChangerObjet(1);
            else if (molette < 0f) ChangerObjet(-1);

            // 3. JETER OU LANCER L'OBJET
            if (Input.GetKeyDown(KeyCode.G))
            {
                JeterObjet(false); // Lâcher doucement avec G
            }
            // LA MODIFICATION EST ICI : GetMouseButtonUp au lieu de Down
            else if (Input.GetMouseButtonUp(1)) 
            {
                JeterObjet(true);  // Lancer quand on RELÂCHE le clic droit
            }
        }
    }

    public void AjouterObjet(string nomObjet)
    {
        if (!objetsPossedes.Contains(nomObjet))
        {
            objetsPossedes.Add(nomObjet);
            MettreAJourTexte();
            
            if (objetsPossedes.Count == 1)
            {
                indexObjetActif = 0;
                EquiperObjetActif();
            }
        }
    }

    void JeterObjet(bool estUnLancer)
    {
        string nomAEquiper = objetsPossedes[indexObjetActif];

        foreach (Transform enfant in mainDuJoueur)
        {
            if (enfant.name == nomAEquiper)
            {
                // On crée une copie physique de l'objet
                GameObject objetJete = Instantiate(enfant.gameObject, mainDuJoueur.position + mainDuJoueur.forward * 1.5f, mainDuJoueur.rotation);
                
                objetJete.name = nomAEquiper; 
                objetJete.SetActive(true);    
                objetJete.tag = "Item";       
                
                objetJete.transform.localScale = Vector3.one; 
                
                // On prépare la physique
                if (objetJete.GetComponent<Collider>() == null) objetJete.AddComponent<BoxCollider>();
                
                Rigidbody rb = objetJete.GetComponent<Rigidbody>();
                if (rb == null) rb = objetJete.AddComponent<Rigidbody>();

                if (estUnLancer)
                {
                    // Propulse l'objet en avant
                    rb.AddForce(mainDuJoueur.forward * forceDeLancer, ForceMode.Impulse);
                }

                enfant.gameObject.SetActive(false);
            }
        }

        objetsPossedes.RemoveAt(indexObjetActif);
        MettreAJourTexte();

        if (objetsPossedes.Count == 0)
        {
            indexObjetActif = -1;
        }
        else
        {
            if (indexObjetActif >= objetsPossedes.Count) indexObjetActif = objetsPossedes.Count - 1;
            EquiperObjetActif();
        }
    }

    void MettreAJourTexte()
    {
        texteListeObjets.text = "INVENTAIRE :\n\n";
        foreach (string objet in objetsPossedes)
        {
            texteListeObjets.text += "- " + objet + "\n";
        }
    }

    void ChangerObjet(int direction)
    {
        indexObjetActif += direction;
        
        if (indexObjetActif >= objetsPossedes.Count) indexObjetActif = 0;
        else if (indexObjetActif < 0) indexObjetActif = objetsPossedes.Count - 1;

        EquiperObjetActif();
    }

    void EquiperObjetActif()
    {
        if (indexObjetActif < 0 || indexObjetActif >= objetsPossedes.Count) return;

        string nomAEquiper = objetsPossedes[indexObjetActif];

        foreach (Transform enfant in mainDuJoueur)
        {
            if (enfant.name == nomAEquiper)
            {
                enfant.gameObject.SetActive(true);
            }
            else
            {
                enfant.gameObject.SetActive(false);
            }
        }
    }

    // Petite fonction bonus pour que d'autres scripts puissent vérifier ce qu'on a en main
    public string ObtenirObjetActif()
    {
        if (indexObjetActif >= 0 && indexObjetActif < objetsPossedes.Count)
        {
            return objetsPossedes[indexObjetActif];
        }
        return ""; // Rien en main
    }
}