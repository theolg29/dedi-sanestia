# INTEGRATION — Cinématique de réveil (WakeUpCinematic)

## Script concerné
`WakeUpCinematic.cs` — À placer sur le joueur (même GameObject ou parent que la caméra).

## Rôle
Lance automatiquement au `Start` la séquence d'introduction : le joueur est "allongé" (caméra inclinée), les yeux s'ouvrent, puis il se redresse. C'est le miroir exact de la séquence de mort de `PlayerHealth`.

## Prérequis
- Le joueur doit avoir une **Camera** enfant (détectée automatiquement).
- **Tous les autres scripts du joueur sont désactivés** pendant la cinématique, puis réactivés à la fin — c'est automatique.

## Mise en place

### 1. Ajouter le script
Glisser `WakeUpCinematic.cs` sur le **root du joueur** (ou sur le même objet que `PlayerHealth`).

### 2. Synchroniser les valeurs avec PlayerHealth

> **CRITIQUE** : Pour que la pose de départ du réveil coïncide avec la pose finale de la mort, ces deux paramètres doivent être **identiques** entre les deux scripts :

| Paramètre WakeUpCinematic | Paramètre correspondant PlayerHealth |
|---|---|
| `sleepDropHeight` | `collapseDropHeight` |
| `sleepAngle` | `collapseAngle` |

Valeurs par défaut cohérentes : `sleepDropHeight = 0.8`, `sleepAngle = 80`.

### 3. Régler les timings

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `delayBeforeOpeningEyes` | `1.0 s` | Noir total avant l'ouverture des yeux |
| `eyeOpenDuration` | `2.0 s` | Durée d'ouverture des paupières |
| `getUpDuration` | `1.5 s` | Durée pour se redresser |

## Déroulement de la séquence

```
[Start] → Caméra placée en pose "mort" → Son coupé (AudioListener.volume = 0)
       → Tous les scripts joueur désactivés
       → Pause noir (delayBeforeOpeningEyes)
       → Ouverture des paupières (SmoothStep) + son remonte à 0.5
       → Redressement caméra (EaseOutQuad) + son remonte à 1.0
       → Scripts réactivés → WakeUpCinematic se détruit
```

## Points d'attention
- Le script **se détruit** (`Destroy(this)`) à la fin de la séquence — normal.
- Si la caméra n'est pas trouvée, une erreur est loggée et rien ne se passe.
- `CharacterController` est désactivé séparément des MonoBehaviours (il n'est pas un MonoBehaviour).
- Ne pas utiliser ce script dans une scène où le joueur doit démarrer debout immédiatement.

## Checklist d'intégration
- [ ] Script placé sur le root du joueur
- [ ] `sleepDropHeight` = valeur de `collapseDropHeight` dans `PlayerHealth`
- [ ] `sleepAngle` = valeur de `collapseAngle` dans `PlayerHealth`
- [ ] La scène de jeu contient bien un `AudioListener` (généralement sur la caméra principale)
