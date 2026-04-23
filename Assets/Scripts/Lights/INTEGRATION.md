# INTEGRATION — Lumières (FlashlightController + FlickeringNeon)

---

## A. FlashlightController — Lampe de poche

### Script concerné
`FlashlightController.cs` — À placer sur le **prefab de la lampe de poche**, dans `MainDuJoueur`.

### Rôle
Active/désactive la lumière de la lampe avec la touche **F**, uniquement quand la lampe est dans la main du joueur (parent nommé `MainDuJoueur`).

### Mise en place

#### 1. Structure du prefab lampe

```
Flashlight (prefab)       ← FlashlightController.cs ici
├── SpotLight             ← composant Light (Spot ou Point)
└── AudioSource           ← pour les sons On/Off
```

> Le prefab doit être un **child de `MainDuJoueur`** dans la hiérarchie du joueur pour que le script s'active.

#### 2. Relier dans l'Inspector

| Champ | Quoi glisser |
|---|---|
| `maLumiere` | La `Light` enfant du prefab |
| `sourceAudio` | L'`AudioSource` du prefab |
| `sonAllumer` | AudioClip du clic "ON" |
| `sonEteindre` | AudioClip du clic "OFF" |
| `toucheAllumer` | `F` (par défaut) |

#### 3. Désactiver la lumière par défaut
Dans l'Inspector de la `Light`, décocher **Enable** — la lampe commence éteinte.

### Comportement
- Le script vérifie à chaque frame que `transform.parent.name == "MainDuJoueur"`.
- Si la lampe est droppée/throwée (parent change), la touche F ne fonctionne plus.
- L'état On/Off persiste si le joueur reéquipe la lampe.

### Points d'attention
- Le nom du parent est hardcodé : le Transform doit s'appeler exactement **`MainDuJoueur`**.
- Si aucun son n'est relié, la lumière bascule quand même sans audio.

### Checklist
- [ ] `FlashlightController.cs` sur le prefab lampe
- [ ] Prefab placé comme child de `MainDuJoueur` (désactivé au départ)
- [ ] `maLumiere` relié à la Light du prefab
- [ ] Light désactivée par défaut dans l'Inspector
- [ ] Sons On/Off assignés (optionnel)

---

## B. FlickeringNeon — Néon clignotant

### Script concerné
`FlickeringNeon.cs` — À placer sur chaque **néon clignotant** de la scène.

### Rôle
Fait clignoter le néon de façon aléatoire (durées On/Off entre 0.05 s et 1 s) avec tremblement d'intensité, synchronisé avec un son de bourdonnement électrique.

### Mise en place

#### 1. Structure du GameObject néon

```
Neon_01                ← FlickeringNeon.cs ici
├── NeonLight          ← composant Light (Area ou Point)
├── NeonMesh           ← MeshRenderer du tube néon
└── AudioSource        ← son de bourdonnement en boucle
```

#### 2. Préparer le matériau du néon
Le tube néon doit avoir un matériau avec **Emission activée** :
- Dans le matériau, cocher `Emission`.
- La couleur d'émission sera sauvegardée au `Start` et coupée quand le néon s'éteint.

#### 3. Relier dans l'Inspector

| Champ | Quoi glisser |
|---|---|
| `lumiereNeon` | La `Light` du néon |
| `cylindreNeon` | Le `Renderer` du tube (MeshRenderer) |
| `audioNeon` | L'`AudioSource` avec le son de bourdonnement |

> L'`AudioSource` doit avoir `Loop` = true et `Play On Awake` = true — le script gère le mute/unmute.

### Comportement
- **Quand allumé** : intensité de la lumière tremble entre 10% et 100% de la valeur de base (définie dans l'Inspector) à chaque frame.
- **Quand éteint** : lumière désactivée, émission du matériau noire, audio muté.
- Durées d'état On/Off : aléatoires entre **0.05 s** et **1.0 s**.

### Paramètres ajustables dans le code (non exposés)
Pour changer les durées min/max de clignotement, modifier directement dans le script :
```csharp
private float tempsMin = 0.05f;
private float tempsMax = 1.0f;
```

### Points d'attention
- Ne pas exposer `tempsMin`/`tempsMax` à plusieurs néons différents — les modifier dans le code si besoin de varier les comportements.
- Le matériau est modifié via `cylindreNeon.material` (instance) — les changements ne se propagent **pas** aux autres objets partageant le même matériau d'origine.
- Si `lumiereNeon` est null, le script plante dans la coroutine. Toujours relier la Light.

### Checklist
- [ ] `FlickeringNeon.cs` sur le GameObject néon
- [ ] `lumiereNeon` relié
- [ ] `cylindreNeon` relié (MeshRenderer avec Emission activée sur le matériau)
- [ ] `audioNeon` relié avec son en boucle et Play On Awake
- [ ] Intensité de la Light réglée dans l'Inspector (sera la valeur de référence pour le tremblement)
