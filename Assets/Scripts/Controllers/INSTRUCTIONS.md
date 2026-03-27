# Documentation : Contrôleurs d'objets (Controllers)

Ce dossier contient les scripts qui dictent le comportement des objets interactifs dans le monde (portes, mécanismes, etc.).

## 1. DoorController.cs

**Rôle :** Gère l'ouverture sécurisée des portes du niveau.
**Fonctionnement :**

- Demande un objet spécifique pour s'ouvrir (ex: `Required Item = "Badge"`).
- Communique avec l'`InventoryManager` via la fonction `TryOpen()` lorsqu'elle est ciblée par le joueur.
- Vérifie l'objet actif dans la main du joueur (`GetActiveItem()`).
- Si l'accès est autorisé, effectue une rotation fluide (Quaternion.Lerp) vers un angle d'ouverture défini (`Open Angle`) à une vitesse donnée (`Open Speed`).
