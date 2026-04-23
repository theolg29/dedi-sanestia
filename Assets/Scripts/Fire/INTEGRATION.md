# INTEGRATION — Système de Feu

## Scripts concernés
| Script | Rôle | Où le placer |
|---|---|---|
| `Fire_propagation.cs` | Propagation, visuels, fumée, lumière | Sur le **prefab feu** |
| `FireProximityVision.cs` | Effets visuels au joueur (overlay, flou, vignette) | Sur le **joueur** (ou la caméra) |
| `PlayerHealth.cs` | Vie du joueur, dégâts du feu, mort | Sur le **joueur** |

Ces 3 scripts sont **couplés** — ils doivent tous être présents dans la scène pour fonctionner correctement.

---

## 1. Fire_propagation — Prefab feu

### Structure requise du prefab
Le prefab feu doit avoir ces **child GameObjects** (noms exacts) :

```
[Prefab feu]           ← Fire_propagation.cs ici
├── Hearth             ← Particules du foyer
├── Volume_Out         ← Particules de volume
├── Trail_Out          ← Particules de traînée
└── Particle_Out       ← Particules supplémentaires
```

> Les noms doivent être exactement `Hearth`, `Volume_Out`, `Trail_Out`, `Particle_Out`. Si un child est absent, il est ignoré silencieusement.

### Objets requis dans la scène (noms exacts)
Le script les cherche par nom (`GameObject.Find`) :

| Nom dans la scène | Rôle |
|---|---|
| `FX_Smoke` | Particle System pour la fumée noire/suie |
| `Smoke01` | Particle System pour la fumée atmosphérique lointaine |
| `Point Light` | Light globale qui s'intensifie avec le feu |

> Si ces objets sont absents, les FX globaux sont simplement ignorés (pas d'erreur bloquante).

### Paramètres clés

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `cellSize` | `1` | Taille d'une cellule de grille (doit être identique dans les 3 scripts) |
| `propagationInterval` | `8 s` | Délai entre chaque tentative de propagation |
| `propagationChance` | `0.12` | Probabilité de propagation par direction (12%) |
| `maxSimultaneousSpreads` | `1` | Max de nouvelles flammes par tick |

### Placer le feu de départ
Glisser le prefab directement dans la scène à l'endroit où le feu commence. Il se propagera automatiquement.

---

## 2. FireProximityVision — Effets visuels de chaleur

### Placement
Glisser `FireProximityVision.cs` sur le **GameObject joueur** (ou sur la caméra principale).

### Post Processing (optionnel mais recommandé)
Pour activer le flou (Depth of Field) et la vignette :
1. S'assurer que la caméra principale a **Post Processing activé** (URP Camera Data → `Render Post Processing` = true).
2. Le script crée automatiquement le Volume URP en runtime — pas besoin de le créer à la main.

> Sans Post Processing, seul l'overlay orange UI est visible (toujours fonctionnel).

### Paramètres clés

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `maxEffectDistance` | `15 m` | Distance à laquelle l'effet commence |
| `fullEffectDistance` | `2 m` | Distance à laquelle l'effet est à 100% |
| `fireCellSize` | `1` | Doit correspondre à `cellSize` de `Fire_propagation` |
| `enableBlur` | `true` | Activer/désactiver le flou URP |

### API publique

```csharp
fireProximityVision.LockEffect();      // Fige l'effet (appelé par PlayerHealth à la mort)
fireProximityVision.StartDeathBlur();  // Lance le flou progressif post-mortem
```

---

## 3. PlayerHealth — Vie et mort du joueur

### Placement
Glisser `PlayerHealth.cs` sur le **GameObject joueur**.

### UI requise dans la scène
Le script cherche automatiquement par nom :

| Nom dans la scène | Type | Rôle |
|---|---|---|
| `HealthBar` | `Slider` UI | Barre de vie (vert → rouge) |

> Créer un `Slider` dans le Canvas du HUD et le nommer exactement **`HealthBar`**. Ou le glisser directement dans le champ `healthBarSlider` de l'Inspector.

### Paramètres clés

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `maxHealth` | `3` | Points de vie maximum |
| `fireDamageDistance` | `2 m` | Distance pour subir des dégâts |
| `damagePerSecond` | `1` | Dégâts/seconde dans le feu |
| `fireCellSize` | `1` | Doit correspondre à `cellSize` de `Fire_propagation` |
| `collapseDropHeight` | `0.8 m` | Hauteur de chute de la caméra à la mort |
| `collapseAngle` | `80°` | Angle Z de la caméra à la mort |

> `collapseDropHeight` et `collapseAngle` doivent correspondre à `sleepDropHeight` et `sleepAngle` dans `WakeUpCinematic` pour que les deux animations soient cohérentes.

### API publique

```csharp
playerHealth.TakeDamage(float amount);  // Infliger des dégâts depuis un autre script
playerHealth.Heal(float amount);        // Soigner le joueur
```

---

## Flux de communication entre les 3 scripts

```
Fire_propagation ──(static)──► burningCells (grille des cellules en feu)
                               GetClosestFireDistance(pos)
                               ActiveFireCount
                                     │
                    ┌────────────────┴────────────────┐
                    ▼                                 ▼
          FireProximityVision                   PlayerHealth
          (effets visuels)                      (dégâts + mort)
                    │
                    └──► LockEffect() / StartDeathBlur()
                              (appelé par PlayerHealth.OnPlayerDeath)
```

---

## Checklist d'intégration
- [ ] Prefab feu avec children `Hearth`, `Volume_Out`, `Trail_Out`, `Particle_Out`
- [ ] Objets `FX_Smoke`, `Smoke01`, `Point Light` présents dans la scène
- [ ] `Fire_propagation.cs` sur le prefab feu
- [ ] `FireProximityVision.cs` sur le joueur
- [ ] `PlayerHealth.cs` sur le joueur
- [ ] Slider `HealthBar` dans le HUD Canvas
- [ ] `fireCellSize` = même valeur dans les 3 scripts (défaut : `1`)
- [ ] `collapseDropHeight` / `collapseAngle` synchronisés avec `WakeUpCinematic`
- [ ] Post Processing activé sur la caméra principale (pour le flou)
