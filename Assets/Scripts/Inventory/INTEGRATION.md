# INTEGRATION — Inventaire & Interaction (InventoryManager + PlayerInteract)

## Scripts concernés
| Script | Rôle | Où le placer |
|---|---|---|
| `InventoryManager.cs` | Hotbar 9 slots, équipement main, drop/throw | Sur le **joueur** |
| `PlayerInteract.cs` | Ramasser des items (E), ouvrir des portes (E) | Sur le **joueur** |

Ces deux scripts fonctionnent ensemble — `PlayerInteract` appelle `InventoryManager.AddItem()` et `DoorController.TryOpen()`.

---

## 1. InventoryManager

### Mise en place

#### A. Créer la hiérarchie UI (Hotbar)

```
Canvas (HUD)
└── Hotbar
    ├── Slot_1
    │   ├── Background (Image)   ← case de fond
    │   └── Texte_Objet (TextMeshProUGUI) ← nom de l'objet
    ├── Slot_2
    │   ├── Background (Image)
    │   └── Texte_Objet
    ... (jusqu'à 9 slots)
└── InventoryMenu (panneau I)
    └── ItemListText (TextMeshProUGUI)
```

#### B. Créer le Player Hand

Sous la caméra du joueur, créer un Transform `MainDuJoueur` (nom exact pour `FlashlightController`).

```
[Joueur]
└── [Caméra]
    └── MainDuJoueur       ← Transform "main" du joueur
        ├── Badge          ← prefab de l'item Badge (désactivé par défaut)
        ├── Key            ← prefab de la clé (désactivé par défaut)
        └── Flashlight     ← prefab de la lampe (désactivé par défaut)
```

> Chaque child de `MainDuJoueur` représente un item équipable. Son **nom doit être identique** au nom de l'objet posé dans la scène (tag `Item`).

#### C. Relier dans l'Inspector

| Champ | Quoi glisser |
|---|---|
| `slotBackgrounds` | Les 9 Images "Background" de chaque slot |
| `slotTexts` | Les 9 TextMeshProUGUI "Texte_Objet" de chaque slot |
| `inventoryMenu` | Le panneau InventoryMenu (désactivé au départ) |
| `itemListText` | Le TMP dans InventoryMenu |
| `playerHand` | Le Transform `MainDuJoueur` |

#### D. Régler les paramètres

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `selectedColor` | Blanc | Couleur du slot actif |
| `normalColor` | Gris 50% | Couleur des slots inactifs |
| `throwForce` | `10` | Force du lancer (clic droit) |

### Contrôles
| Touche | Action |
|---|---|
| `I` | Ouvrir/fermer l'inventaire (menu) |
| Molette souris | Changer d'item actif dans la hotbar |
| `G` | Déposer l'item actif |
| Clic droit (relâché) | Lancer l'item actif |

### API publique

```csharp
inventoryManager.AddItem("Badge");     // Ajouter un item (appelé par PlayerInteract)
inventoryManager.GetActiveItem();      // Retourne le nom de l'item en main (appelé par DoorController)
```

---

## 2. PlayerInteract

### Mise en place

#### A. Ajouter le script sur le joueur
Glisser `PlayerInteract.cs` sur le **même GameObject** que `InventoryManager` (ou un parent/enfant).

#### B. Relier dans l'Inspector

| Champ | Quoi glisser |
|---|---|
| `playerCamera` | La caméra principale du joueur |
| `interactDistance` | `3` m (distance du raycast) |
| `interactPromptText` | (optionnel) TMP existant — créé automatiquement si vide |
| `highlightColor` | Couleur du surlignage (défaut : jaune) |

#### C. Affordance (surlignage + prompt)

Le script gère automatiquement deux types d'affordance quand le joueur vise un objet interactable :

**Surlignage** — Active l'émission URP sur tous les `Renderer` de l'objet visé avec `highlightColor * 0.4`. L'émission originale est restaurée dès que le joueur détourne le regard.

> Pour que le surlignage soit visible, le matériau de l'objet doit utiliser un shader URP standard (`Lit` ou `Simple Lit`). Les shaders custom sans propriété `_EmissionColor` ne seront pas surlignés, mais ça ne causera pas d'erreur.

**Prompt UI** — Un panneau noir semi-transparent apparaît sous le centre de l'écran avec le texte contextuel :
- `E — Ramasser` pour les items
- `E — Ouvrir` pour les portes

Le Canvas du prompt est créé automatiquement au `Start` si `interactPromptText` n'est pas relié dans l'Inspector. Si tu veux un style personnalisé, crée ton propre TMP dans le HUD et glisse-le dans le champ `interactPromptText`.

#### C. Préparer les items dans la scène
Chaque objet ramassable dans la scène doit :
- Avoir le tag **`Item`**
- Avoir un **Collider** (non-trigger)
- Avoir un **nom identique** au child correspondant dans `MainDuJoueur`

#### D. Préparer les portes dans la scène
Chaque porte doit :
- Avoir un **Collider** (non-trigger)
- Avoir le composant `DoorController`

> **Pas de tag nécessaire.** `PlayerInteract` détecte les portes directement via `GetComponent<DoorController>()` — il suffit que le composant soit présent.

---

## Flux de fonctionnement

```
[Joueur appuie E]
       │
       ▼
PlayerInteract.Update()
  → Raycast depuis caméra (3 m)
       │
       ├── Tag "Item" → InventoryManager.AddItem(nom) → Destroy(objet)
       │
       └── Tag "Door" → DoorController.TryOpen()
                              → InventoryManager.GetActiveItem()
```

---

## Checklist d'intégration

**InventoryManager**
- [ ] Arrays `slotBackgrounds` et `slotTexts` remplis (même nombre de slots)
- [ ] `inventoryMenu` relié au panneau (désactivé au départ dans la scène)
- [ ] `itemListText` relié au TMP dans le panneau
- [ ] `playerHand` = Transform `MainDuJoueur`
- [ ] Chaque item dans `MainDuJoueur` est **désactivé** au départ

**PlayerInteract**
- [ ] `playerCamera` relié à la caméra principale
- [ ] Chaque item dans la scène a le tag `Item` + Collider + nom = child de `MainDuJoueur`
- [ ] Chaque porte a un Collider + composant `DoorController` (pas de tag requis)
