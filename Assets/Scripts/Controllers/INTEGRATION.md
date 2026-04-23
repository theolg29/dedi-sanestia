# INTEGRATION — Système de Portes (DoorController)

## Script concerné
`DoorController.cs` — À placer sur chaque porte verrouillée de la scène.

## Prérequis
- `InventoryManager` présent quelque part dans la scène (détecté automatiquement au `Start`).
- La porte doit avoir un **Collider** (pour que le Raycast de `PlayerInteract` la détecte).

## Mise en place

### 1. Préparer le GameObject de la porte
- La porte doit être un **objet 3D séparé** dont le pivot est sur le côté (bord de la porte), pas au centre — sinon la rotation sera fausse.
- **Pas de tag nécessaire** — `PlayerInteract` détecte les portes par la présence du composant `DoorController`.
- Ajouter un **Collider** (Box ou Mesh, non-trigger).

### 2. Ajouter le script
Glisser `DoorController.cs` sur le GameObject de la porte.

### 3. Régler les paramètres dans l'Inspector

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `requiredItem` | `"Badge"` | Nom **exact** de l'objet dans l'inventaire |
| `openAngle` | `90` | Angle d'ouverture en degrés (axe Y) |
| `openSpeed` | `2` | Vitesse de rotation (Lerp) |

> Le `requiredItem` doit correspondre **exactement** au nom du child dans `PlayerHand` et au nom de l'objet ramassé dans la scène (tag `Item`).

## Comment ça marche

Le joueur appuie sur **E** face à la porte → `PlayerInteract` appelle `TryOpen()` → le script vérifie l'objet actif dans la hotbar via `InventoryManager.GetActiveItem()` → si ça correspond, la porte s'ouvre par rotation Y.

```
[Joueur appuie E] → PlayerInteract.Update() → DoorController.TryOpen()
                                                     ↓
                                          InventoryManager.GetActiveItem()
                                                     ↓
                                     si match → isOpen = true → rotation Y
```

## Points d'attention
- La porte **ne se referme pas** une fois ouverte (comportement volontaire).
- L'objet **n'est pas consommé** après utilisation (TODO dans le code — à implémenter si nécessaire).
- Si `InventoryManager` est introuvable au `Start`, `TryOpen()` ne fait rien silencieusement.

## Checklist d'intégration
- [ ] Collider présent (non-trigger)
- [ ] Pivot de la porte sur le bon bord
- [ ] `requiredItem` = nom exact de l'item dans `PlayerHand`
- [ ] Un `InventoryManager` existe dans la scène
