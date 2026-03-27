# Documentation : Système de Lumières (Lights)

Ce dossier regroupe les scripts gérant l'éclairage dynamique et les ambiances visuelles du jeu.

## 1. Flashlight.cs

**Rôle :** Gère la lampe torche équipable par le joueur.
**Fonctionnement :**

- Permet d'allumer et d'éteindre une source de lumière (Spotlight) attachée à la caméra ou à la main du joueur.
- Réagit à une touche d'activation (ex: touche 'F' ou clic).
- _[Optionnel : Peut inclure une gestion de batterie à l'avenir]._

## 2. FlickeringNeon.cs

**Rôle :** Crée une ambiance horrifique/abandonnée avec des lumières instables.
**Fonctionnement :**

- Fait clignoter une source de lumière (Light component) de manière aléatoire.
- Alterne entre des valeurs d'intensité hautes et basses.
- Utilise des intervalles de temps irréguliers (Random.Range) pour rendre l'effet de néon cassé réaliste et imprévisible.
