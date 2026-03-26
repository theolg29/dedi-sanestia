using UnityEngine;
using UnityEngine.SceneManagement; // Obligatoire pour charger une scène

public class MainMenuManager : MonoBehaviour
{
    [Header("Les Panneaux (Panels)")]
    public GameObject panneauPrincipal;   // Celui avec les boutons de base
    public GameObject panneauParametres; // Le fond pour les paramètres
    public GameObject panneauCredits;    // Le fond pour les crédits

    [Header("Le Nom de votre Scène de Jeu")]
    public string nomSceneJeu = "Nom_De_Votre_Scene"; // Remplissez ça !

    void Start()
    {
        // Au démarrage, on s'assure que seul le panneau principal est visible
        ActiverPanneau(panneauPrincipal);
    }

    // --- FONCTIONS DES BOUTONS ---

    public void BoutonJouer()
    {
        // On charge la scène de jeu
        // N'oubliez pas de rajouter votre scène dans les Build Settings (File > Build Settings) !
        SceneManager.LoadScene(nomSceneJeu);
    }

    public void OpenSettings()
    {
        ActiverPanneau(panneauParametres);
    }

    public void OpenCredits()
    {
        ActiverPanneau(panneauCredits);
    }

    public void BackToMain()
    {
        ActiverPanneau(panneauPrincipal);
    }

    public void BoutonQuitter()
    {
        Debug.Log("Le joueur a quitté le jeu (ne fonctionne qu'en Build final)");
        Application.Quit(); // Quitte l'application
    }

    // --- PETITE FONCTION OUTIL ---

    void ActiverPanneau(GameObject panneauAActiver)
    {
        // On cache tous les panneaux
        panneauPrincipal.SetActive(false);
        panneauParametres.SetActive(false);
        panneauCredits.SetActive(false);

        // On active le bon
        if (panneauAActiver != null)
        {
            panneauAActiver.SetActive(true);
        }
    }
}