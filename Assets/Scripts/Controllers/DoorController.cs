using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Paramètres de la Porte")]
    public string objetRequis = "Badge"; // Le nom exact de l'objet nécessaire
    public float angleOuverture = 90f;   // Angle de la porte ouverte
    public float vitesseOuverture = 2f;  // Vitesse de l'animation

    private bool estOuverte = false;
    private Quaternion rotationFermee;
    private Quaternion rotationOuverte;
    
    private InventoryManager inventaire;

    void Start()
    {
        // On mémorise la position de départ de la porte
        rotationFermee = transform.rotation;
        
        // On calcule sa position une fois ouverte (rotation sur l'axe Y)
        rotationOuverte = Quaternion.Euler(transform.eulerAngles + new Vector3(0, angleOuverture, 0));
        
        // La porte cherche toute seule le script InventoryManager dans la scène !
        inventaire = FindFirstObjectByType<InventoryManager>();
    }

    void Update()
    {
        // Si elle est déverrouillée, on l'anime de façon fluide
        if (estOuverte)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, rotationOuverte, Time.deltaTime * vitesseOuverture);
        }
    }

    // Fonction appelée quand le joueur clique (E) sur la porte
    public void EssayerOuvrir()
    {
        if (estOuverte) return; // Si déjà ouverte, on ne fait plus rien

        if (inventaire != null)
        {
            // On vérifie ce que le joueur tient en main
            string objetEnMain = inventaire.ObtenirObjetActif();

            if (objetEnMain == objetRequis)
            {
                Debug.Log("Accès Autorisé !");
                estOuverte = true; // Déclenche l'animation dans l'Update
            }
            else
            {
                Debug.Log("Accès Refusé. Il faut : " + objetRequis + ". Vous tenez : " + objetEnMain);
            }
        }
    }
}