# 🏢 Projet de Master : "Escape The Office" (Titre Provisoire)

## 📖 Description du Projet

Prototype de jeu en vue à la première personne (FPS) réalisé dans le cadre de notre Master. Le joueur se retrouve piégé dans un bâtiment de bureaux (Low Poly Office) pendant un déclenchement d'alarme incendie. L'objectif est de s'échapper avant que le feu ne bloque toutes les issues, en trouvant les bons objets (badges, clés) pour déverrouiller les portes.

## 👥 L'Équipe

- **Théo :** Programmation des mécaniques Joueur (Interactions, Inventaire, Physique).
- **Ronan :** Programmation Système (Dangers, Propagation du feu, Chronomètre).
- **Hassan :** Level Design (ProBuilder) & Intégration visuelle (Éclairage, Assets).

## 🛠️ Spécifications Techniques & Prérequis

- **Moteur :** Unity 6.3 LTS (6000.3.11f1)
- **Pipeline de Rendu :** Standard / Built-in (avec Post-Processing)
- **Packages Requis :** \* ProBuilder (pour le Greyboxing)
  - TextMeshPro (pour l'UI)
- **Assets Externes :** "Low Poly Office Set", Modular First Person Controller.

## 🎮 Core Gameplay (Mécaniques Principales)

1. **Survie :** Propagation scénarisée du feu obligeant le joueur à se dépêcher sous la pression d'un chronomètre.
2. **Interaction :** Système de Raycast. Visée au centre de l'écran pour ramasser/utiliser.
3. **Inventaire Dynamique :** L'objet ramassé apparaît physiquement dans la main du joueur.
4. **Physique :** Possibilité de manipuler l'environnement en lançant des objets.

## ⌨️ Contrôles du Joueur

- **Z, Q, S, D** : Se déplacer.
- **Shift (Maintenu)** : Courir.
- **Ctrl (Maintenu)** : S'accroupir (passages étroits).
- **E** : Interagir / Ramasser un objet / Ouvrir une porte.
- **I** : Ouvrir / Fermer le menu complet de l'inventaire.
- **Molette Souris** : Faire défiler les objets tenus en main.
- **G** : Relâcher doucement l'objet tenu au sol.
- **Clic Droit (Relâché)** : Lancer l'objet tenu avec une force physique.
