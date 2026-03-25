# 📋 Répartition des Tâches (Scripts & Intégration)

## Mécaniques Joueur & Interactions

- [x] Intégrer le First Person Controller.
- [x] Créer le script de ramassage (`PlayerInteract.cs` avec Raycast).
- [x] Créer le système d'inventaire UI (`InventoryManager.cs`).
- [x] Créer le système de "Hotbar" (Objet visible en main + Molette).
- [x] Créer le système de lancer physique (Drop sur `G` + Lancer sur `Clic Droit`).
- [ ] **À FAIRE :** Script d'interaction des portes (`DoorController.cs`). Vérifier si le joueur tient le bon objet en main ("Clé", "Badge") pour déverrouiller et ouvrir la porte.
- [ ] **À FAIRE (Bonus) :** Script pour faire clignoter la lumière rouge de l'alarme (`FlashingLight.cs`).

## Dangers & Scénarisation

- [ ] **À FAIRE :** Créer le système de propagation du feu (`FireManager.cs`).
  - _Note de dev :_ Ne pas faire une vraie propagation dynamique complexe. Utiliser un système basé sur un Timer (ex: à T+2min, activer le Prefab "MurDeFeu_Couloir").
- [ ] **À FAIRE :** Gérer les dégâts ou le Game Over si le joueur touche le feu.
- [ ] **À FAIRE :** Gérer le chronomètre global de la partie (UI + fin du jeu si le temps est écoulé).

## Level Design & Intégration

- [x] Greyboxing de la première salle avec ProBuilder.
- [x] Mise en place de l'ambiance lumineuse (Ciel noir, Directional Light à 0, Global Volume).
- [ ] **À FAIRE :** Construire le niveau complet avec l'asset "Low Poly Office Set" (140 modèles).
  - _Règle d'or :_ Bien utiliser le Snap to Grid (Aimantation à la grille) pour éviter les trous entre les murs.
- [ ] **À FAIRE :** Placer les objets ramassables (Clés, Badges) et s'assurer qu'ils ont le tag "Item" et le nom exact attendu par l'inventaire.
- [ ] **À FAIRE :** Placer les prefabs de feu (désactivés par défaut) pour que Ronan puisse les relier à son script.
