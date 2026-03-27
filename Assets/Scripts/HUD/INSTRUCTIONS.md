# Documentation : Interface Joueur (HUD)

Ce dossier contient les scripts gérant l'interface affichée à l'écran du joueur (Canvas).

## 1. MainMenuManager.cs

**Rôle :** Gère le menu principal du jeu (Écran titre).
**Fonctionnement :**

- Contrôle les transitions entre les différents écrans (Jouer, Options, Quitter).
- Charge la scène principale du jeu (GameScene) lorsque le joueur clique sur "Play".
- Gère la fermeture de l'application (Quit).

## 2. StaminaManager.cs

**Rôle :** Gère l'endurance du joueur (Sprint) et son affichage à l'écran.
**Fonctionnement :**

- Définit une valeur d'endurance maximale (`Max Stamina`).
- Draine l'endurance selon un `Drain Rate` lorsque le joueur court (touche Shift).
- Régénère l'endurance automatiquement quand le joueur marche ou s'arrête.
- Met à jour visuellement la barre de sprint (UI Image en mode "Filled") en modifiant son `Fill Amount` en temps réel.
