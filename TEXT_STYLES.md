# TEXT_STYLES — Sanestia / Escape The Office

Référence de tous les styles de texte UI du jeu. Chaque style est nommé et utilisé de façon cohérente dans le code.

---

## Palette

| Nom              | Valeur                        | Utilisation                        |
|------------------|-------------------------------|------------------------------------|
| `BLANC`          | `#FFFFFF`                     | Textes généraux                    |
| `OR`             | `#FFD932` (1, 0.85, 0.2)      | Objectifs, découvertes importantes |
| `ROUGE_ALERTE`   | `#CC3333` (0.8, 0.2, 0.2)     | Erreurs, blocages                  |
| `FOND_SOMBRE`    | `rgba(0, 0, 0, 0.55)`         | Fond prompt standard               |
| `FOND_ERREUR`    | `rgba(0.8, 0.2, 0.2, 0.75)`   | Fond notification d'erreur         |
| `FOND_OBJECTIF`  | `rgba(0, 0, 0, 0.65)`         | Fond message objectif              |
| `FOND_DIALOGUE`  | `rgba(0, 0, 0, 0.40)`         | Fond sous-titre dialogue           |

---

## Styles définis

### `INTERACTION_PROMPT`
> Prompt qui apparaît quand le joueur vise un objet interactif (porte, item, TV…)

- **Position** : centre écran, légèrement sous le crosshair (`y = -90`)
- **Taille** : `240 × 40`
- **Police** : `20 pt`, blanc, centré
- **Fond** : `FOND_SOMBRE`
- **Animation** : apparition/disparition instantanée
- **Script** : `PlayerInteract.BuildPromptUI()`
- **Exemple** : `E — Ouvrir`, `E — Ramasser`, `E — Caméra suivante (2/4)`

---

### `ERROR_NOTIF`
> Notification temporaire d'erreur ou de blocage

- **Position** : centre écran, sous le prompt (`y = -155`)
- **Taille** : `300 × 40`
- **Police** : `17 pt`, blanc, centré
- **Fond** : `FOND_ERREUR`
- **Animation** : apparition instantanée, disparaît après `2s`
- **Script** : `PlayerInteract.BuildNotifUI()`
- **Exemple** : `Impossible d'ouvrir cette porte`

---

### `OBJECTIVE`
> Message d'objectif affiché lors d'une découverte clé

- **Position** : haut centre (`y = -60` depuis le haut)
- **Taille** : `480 × 44`
- **Police** : `17 pt`, bold, `OR`, centré, `characterSpacing 1.5`
- **Fond** : `FOND_OBJECTIF` (transparence pilotée par le fade)
- **Animation** : fade in `0.4s` → maintenu `objectiveDuration` → fade out `0.6s`
- **Affiché** : une seule fois par session
- **Scripts** : `SecurityTVController.BuildUI()`, `PowerCutTrigger.BuildUI()`
- **Exemple** : `OBJECTIF — Aller vérifier la caméra défaillante`

---

### `DIALOGUE`
> Sous-titre de monologue interne ou réaction du personnage

- **Position** : bas centre — ancres `(0.2, 0.04)` → `(0.8, 0.10)` en espace écran
- **Police** : `19 pt`, blanc, centré, `characterSpacing 0.5`
- **Fond** : `FOND_DIALOGUE` (transparence pilotée par le fade)
- **Padding** : `6 px` horizontal, `3 px` vertical
- **Animation** : fade in `0.18s` → durée du clip audio → fade out `0.18s`
- **Enchaînement** : pause `pauseBetweenLines` entre chaque ligne (défaut `0.5s`)
- **Scripts** : `WakeUpCinematic.BuildSubtitleUI()`, `SecurityTVController.BuildUI()`, `PowerCutTrigger.BuildUI()`
- **Exemple** : `"Tiens ? La caméra numéro 4 ne fonctionne pas ?"`

---

### `HUD_LABEL` *(à implémenter)*
> Étiquette persistante dans l'interface (nom de zone, timer…)

- **Position** : coin haut gauche (`x = +20, y = -20`)
- **Police** : `14 pt`, blanc 70% opacité, aligné gauche
- **Fond** : aucun
- **Animation** : aucune
- **Exemple** : `Salle de sécurité`, `01:45`

---

## Règles générales

- Jamais plus de **2 textes UI actifs simultanément** (un prompt + une notif max)
- Les objectifs s'affichent **une seule fois** — ne pas répéter à chaque interaction
- Les messages d'erreur durent **2 secondes** fixes — pas de dismiss manuel
- Toujours utiliser **TextMeshPro** (`TMPro`) — jamais `UI.Text`
- Les canvas UI sont créés en `ScreenSpaceOverlay`, `sortingOrder` croissant :
  - Prompt : `200`
  - Notif : `201`
  - Objectif : `202`
  - Dialogue : `203`
