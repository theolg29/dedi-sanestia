# INTEGRATION — Minimap (MinimapFollow)

## Script concerné
`MinimapFollow.cs` — À placer sur la **caméra de la minimap**.

## Rôle
La caméra de minimap suit le joueur en X et Z, mais garde sa hauteur (Y) fixe pour avoir une vue de dessus constante.

## Mise en place

### 1. Créer la caméra minimap
- Créer un nouveau GameObject `Camera` nommé `MinimapCamera`.
- La positionner en hauteur (ex. Y = 20) au-dessus de la scène, rotation X = 90 (vue de dessus).
- Régler `Clear Flags` → `Solid Color`, `Background` → noir ou couleur neutre.
- Régler `Culling Mask` pour n'afficher que les layers voulus (ex. `Minimap` layer).
- Régler `Projection` → `Orthographic`, `Size` selon la zone à couvrir.

### 2. Créer le RenderTexture
- `Assets > Create > Render Texture` → nommer `MinimapRender`.
- Sur la `MinimapCamera`, champ `Target Texture` → assigner `MinimapRender`.

### 3. Afficher la minimap en HUD
- Dans le Canvas du HUD, créer une `Raw Image`.
- `Texture` → assigner `MinimapRender`.
- Positionner/redimensionner selon le design.

### 4. Ajouter le script
Glisser `MinimapFollow.cs` sur `MinimapCamera`.

### 5. Relier le joueur
Dans l'Inspector, champ `player` → glisser le Transform du joueur (root du personnage).

## Paramètres — MinimapFollow

| Paramètre | Description |
|---|---|
| `player` | Transform du joueur à suivre |

## Points d'attention
- La caméra minimap suit en `LateUpdate` (après que le joueur ait bougé dans `Update`) — pas de décalage d'une frame.
- Ne **jamais** parenter la `MinimapCamera` au joueur : elle doit rester à hauteur fixe.
- Si `player` est null, la caméra reste immobile sans erreur.

---

## MinimapToggle — Agrandissement avec M

### Script concerné
`MinimapToggle.cs` — À placer sur n'importe quel GameObject de la scène.

### Rôle
Appuyer sur **M** bascule la minimap entre sa taille normale (coin de l'écran) et une version agrandie centrée à l'écran (500×500 par défaut).

### Mise en place
1. Ajouter `MinimapToggle.cs` sur un GameObject (ex. `GameManager` ou le joueur).
2. Glisser le **RectTransform de la Raw Image** de la minimap dans le champ `minimapRect`.
3. La taille et la position d'origine sont **sauvegardées automatiquement au Start** depuis ce qui est réglé dans l'Inspector — pas besoin de les saisir deux fois.

### Paramètres

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `minimapRect` | — | RectTransform de la Raw Image minimap |
| `expandedSize` | `500 × 500` | Taille de la minimap agrandie (en pixels) |

---

## Checklist d'intégration
- [ ] `MinimapCamera` créée avec Projection Orthographic, rotation X = 90
- [ ] `Target Texture` (RenderTexture) assignée à la caméra
- [ ] `Raw Image` dans le HUD Canvas avec la RenderTexture, positionnée dans un coin
- [ ] `MinimapFollow` : champ `player` relié au Transform du joueur
- [ ] `MinimapToggle` : champ `minimapRect` relié au RectTransform de la Raw Image
