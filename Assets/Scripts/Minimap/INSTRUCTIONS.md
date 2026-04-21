# 🗺️ Documentation : Interface et Navigation (HUD)

Ce dossier contient les scripts et configurations qui gèrent l'affichage de la carte et les repères visuels pour se repérer dans la ville.

## 1. MinimapFollow.cs

**Rôle :** Gère le déplacement de la caméra orthographique pour assurer le suivi fluide du joueur en vue de dessus.
**Fonctionnement :**

- Demande un point de référence à suivre en permanence (ex: `Target = "PlayerTransform"`).
- Maintient une altitude constante au-dessus de la cible grâce à la variable `Height`, ignorant les variations d'élévation (sauts, chutes) du joueur.
- S'exécute exclusivement via la fonction `LateUpdate()` pour s'assurer que le joueur a fini de bouger avant de déplacer la caméra, évitant ainsi les effets de saccade (jitter).
- Projette son rendu visuel directement dans une `Render Texture` qui est ensuite lue par l'élément UI `Raw Image` de la minimap.
- Bloque sa rotation pour avoir une carte fixe (Nord en haut), ou s'aligne sur la rotation Y du joueur pour une carte dynamique.

## 2. Repère Visuel (Configuration Sphère)

**Rôle :** Permet d'identifier clairement la position de Théo au milieu des bâtiments low-poly sombres.
**Fonctionnement :**

- Utilise un matériau spécifique ignorant les calculs de lumière de la scène (ex: `Shader = "Unlit/Color"` configuré en blanc pur).
- Est assignée à un calque d'affichage exclusif (`Layer = "Minimap"`).
- Ce calque est spécifiquement isolé dans le `Culling Mask` de la caméra de la minimap, garantissant que seule la sphère blanche est capturée, remplaçant le modèle 3D complet du personnage sur la carte.
