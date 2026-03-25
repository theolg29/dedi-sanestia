using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Paramètres d'interaction")]
    public float distanceInteraction = 3f; // La longueur du bras du joueur
    public Camera cameraJoueur;

    void Update()
    {
        // On crée un rayon qui part du centre de la caméra vers l'avant
        Ray rayon = new Ray(cameraJoueur.transform.position, cameraJoueur.transform.forward);
        RaycastHit hit;

        // On lance le rayon invisible
        if (Physics.Raycast(rayon, out hit, distanceInteraction))
        {
            // Si le rayon touche un objet qui possède le tag "Item"
            if (hit.collider.CompareTag("Item"))
            {
                // C'est ici qu'on pourra afficher ton pop-up "Appuyez sur E" sur l'interface graphique plus tard
                
                // Si le joueur appuie sur la touche E
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Objet ramassé : " + hit.collider.gameObject.name);
                    
                    // On trouve le manager d'inventaire dans la scène et on lui ajoute l'objet
                    FindObjectOfType<InventoryManager>().AjouterObjet(hit.collider.gameObject.name);
                    
                    // On détruit l'objet 3D de la scène pour simuler le ramassage
                    Destroy(hit.collider.gameObject);
                }
            }
        }
    }
}