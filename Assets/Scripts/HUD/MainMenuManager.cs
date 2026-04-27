using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panneaux")]
    public GameObject panneauPrincipal;
    public GameObject panneauParametres;
    public GameObject panneauCredits;

    [Header("Scène de jeu")]
    public string nomSceneJeu = "Nom_De_Votre_Scene";

    void Start()
    {
        ActiverPanneau(panneauPrincipal);
    }

    public void BoutonJouer()       => SceneManager.LoadScene(nomSceneJeu);
    public void OpenSettings()      => ActiverPanneau(panneauParametres);
    public void OpenCredits()       => ActiverPanneau(panneauCredits);
    public void BackToMain()        => ActiverPanneau(panneauPrincipal);
    public void BoutonQuitter()     => Application.Quit();

    private void ActiverPanneau(GameObject panneau)
    {
        panneauPrincipal.SetActive(false);
        panneauParametres.SetActive(false);
        panneauCredits.SetActive(false);
        if (panneau != null) panneau.SetActive(true);
    }
}
