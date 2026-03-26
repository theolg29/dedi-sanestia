# 🎒 Guide d'Intégration : Système d'Inventaire & Interactions

Ce document explique comment fonctionne le système d'interaction et d'inventaire, et comment intégrer de nouveaux objets ramassables dans la scène sans créer de bugs.

## ⚙️ Les Scripts (Comment ça marche ?)

Notre système repose sur deux scripts principaux qui communiquent entre eux :

1. **`PlayerInteract.cs` (Sur le Joueur)** :
   - Attaché au `FirstPersonController`.
   - Il lance un laser invisible (Raycast) depuis le centre de la caméra.
   - S'il détecte un objet avec le tag `Item` et que le joueur appuie sur `E`, il envoie le nom de l'objet à l'inventaire, puis détruit l'objet au sol.

2. **`InventoryManager.cs` (Sur le GameManager)** :
   - Gère l'interface UI (le menu sombre activé avec `I`).
   - Gère l'objet physique "dans la main" du joueur devant la caméra.
   - Permet de faire défiler les objets avec la molette, de les lâcher doucement (`G`) ou de les lancer avec la physique (`Clic Droit`).

---

## 🛠️ Comment créer un nouvel objet ramassable

Pour ajouter une nouvelle clé, un badge ou un extincteur dans le jeu, il faut **absolument** suivre ces 2 étapes pour que le système le reconnaisse.

### Étape 1 : L'objet sur le sol (Celui qu'on ramasse)

1. Place ton modèle 3D dans le niveau.
2. Assure-toi qu'il possède un `Collider` (BoxCollider, MeshCollider, etc.).
3. Dans l'Inspector, change son **Tag** en `Item` (avec un 'I' majuscule).
4. **⚠️ RÈGLE D'OR :** Donne-lui un nom clair et unique dans la Hierarchy (ex: `Clef_Bureau`, `Badge_Admin`). Ne laisse pas de `(Clone)` ou d'espaces à la fin !

### Étape 2 : L'objet dans la main (Le visuel équipé)

1. Va dans le `FirstPersonController` > `Main Camera` > `MainDuJoueur`.
2. Ajoute une **copie exacte** de ton modèle 3D en tant qu'enfant de `MainDuJoueur`.
3. Renomme cette copie avec **EXACTEMENT LE MÊME NOM** que l'objet au sol (ex: `Clef_Bureau`). Le code utilise ce nom pour faire le lien !
4. Ajuste sa Position, sa Rotation et son Scale pour qu'il rende bien devant la caméra.
5. **Désactive** cet objet (décoche la case en haut à gauche de l'Inspector). Il ne doit s'activer que lorsqu'on le ramasse.

---

## 🎮 Commandes en jeu (Rappel)

- **E** : Ramasser l'objet visé (si taggé `Item`).
- **I** ou **Échap** : Ouvrir/Fermer le menu de l'inventaire textuel.
- **Molette Souris** : Changer l'objet actuellement tenu en main.
- **G** : Relâcher l'objet (il tombe au sol devant le joueur).
- **Clic Droit (Maintenir puis relâcher)** : Lancer l'objet avec force.

---

## 🚨 Dépannage (Troubleshooting)

- _Je ramasse l'objet mais rien n'apparaît dans ma main !_ ➡️ Vérifie que le nom de l'objet au sol correspond **à la lettre près** au nom de l'objet caché dans `MainDuJoueur`.
- _L'objet dans ma main est gigantesque ou invisible !_
  ➡️ Tu as oublié d'ajuster le Transform (Position/Scale) de l'enfant dans `MainDuJoueur`. Assure-toi que le Z est positif (ex: Z = 1) pour qu'il ne soit pas dans la lentille de la caméra.
- _Je ne peux pas ramasser l'objet par terre !_
  ➡️ L'objet n'a pas le Tag `Item` ou il lui manque un Collider.
