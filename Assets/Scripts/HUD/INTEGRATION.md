# INTEGRATION — HUD (MainMenuManager + StaminaManager)

---

## A. MainMenuManager

### Script concerné
`MainMenuManager.cs` — À placer sur un **GameObject vide** dans la scène du menu principal.

### Rôle
Gère la navigation entre les panneaux du menu (Principal / Paramètres / Crédits) et charge la scène de jeu.

### Mise en place

#### 1. Créer la hiérarchie UI dans la scène du menu
```
Canvas
├── PanneauPrincipal     ← boutons Jouer, Paramètres, Crédits, Quitter
├── PanneauParametres    ← contenu paramètres + bouton Retour
└── PanneauCredits       ← contenu crédits + bouton Retour
```

#### 2. Relier les panneaux dans l'Inspector
- `panneauPrincipal` → glisser `PanneauPrincipal`
- `panneauParametres` → glisser `PanneauParametres`
- `panneauCredits` → glisser `PanneauCredits`
- `nomSceneJeu` → entrer le **nom exact** de la scène de jeu (ex. `GameScene`)

#### 3. Relier les boutons aux méthodes

| Bouton | Méthode à appeler |
|---|---|
| Jouer | `MainMenuManager.BoutonJouer()` |
| Paramètres | `MainMenuManager.OpenSettings()` |
| Crédits | `MainMenuManager.OpenCredits()` |
| Retour (depuis paramètres/crédits) | `MainMenuManager.BackToMain()` |
| Quitter | `MainMenuManager.BoutonQuitter()` |

#### 4. Ajouter la scène dans les Build Settings
`File > Build Settings > Add Open Scenes` — ajouter la scène menu ET la scène de jeu.

### Points d'attention
- `BoutonQuitter()` ne fonctionne **qu'en build final** — normal en Editor.
- Le script active/désactive les panneaux au `Start` — s'assurer que `PanneauPrincipal` est bien le seul visible au démarrage.

### Checklist
- [ ] 3 panneaux créés dans le Canvas
- [ ] Champs panneau reliés dans l'Inspector
- [ ] `nomSceneJeu` renseigné
- [ ] Boutons reliés aux bonnes méthodes
- [ ] Scènes ajoutées dans les Build Settings

---

## B. StaminaManager

### Script concerné
`StaminaManager.cs` — À placer sur un **GameObject dédié** dans la scène de jeu (ex. `GameManager` ou le joueur).

### Rôle
Singleton qui gère la stamina du sprint. Le contrôleur du joueur consulte `StaminaManager.instance.canSprint` pour autoriser ou non le sprint.

### Mise en place

#### 1. Ajouter le script
Glisser `StaminaManager.cs` sur un GameObject de la scène. **Un seul** par scène.

#### 2. Créer la barre de stamina dans le HUD
- Dans le Canvas du HUD, créer une `Image` en mode `Filled` (Fill Method = Horizontal).
- Glisser cette `Image` dans le champ `sprintBarUI` de l'Inspector.

#### 3. Régler les paramètres

| Paramètre | Valeur par défaut | Description |
|---|---|---|
| `maxStamina` | `100` | Stamina maximale |
| `drainRate` | `20 /s` | Consommation pendant le sprint |
| `regenRate` | `15 /s` | Régénération au repos |

> La stamina doit remonter à **20%** avant que le sprint soit à nouveau possible (anti-exploit tap-tap).

#### 4. Relier au contrôleur du joueur
Le contrôleur doit lire `StaminaManager.instance.canSprint` avant d'autoriser le sprint :

```csharp
// Dans le script de mouvement du joueur :
bool sprinting = Input.GetKey(KeyCode.LeftShift) 
              && StaminaManager.instance != null 
              && StaminaManager.instance.canSprint;
```

> Le script détecte lui-même le `LeftShift` pour drainer la stamina — s'assurer de ne pas dupliquer cette logique dans le contrôleur.

### Points d'attention
- **Singleton** : `StaminaManager.instance` est assigné dans `Awake`. Ne pas mettre deux instances dans la scène.
- La barre UI est une `Image` (Fill), **pas** un `Slider`.
- Le script détecte les axes `Vertical` et `Horizontal` pour ne drainer que si le joueur se déplace.

### Checklist
- [ ] Un seul `StaminaManager` dans la scène
- [ ] `Image` en mode `Filled` créée dans le HUD Canvas
- [ ] `sprintBarUI` relié à cette Image
- [ ] Contrôleur du joueur vérifie `StaminaManager.instance.canSprint`
