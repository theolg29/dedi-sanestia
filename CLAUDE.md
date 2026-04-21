# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Sanestia / "Escape The Office"** — a first-person survival escape game in Unity 6.3 LTS (6000.3.11f1). The player is trapped in a burning office and must find key items to unlock doors and escape before fire blocks all exits.

**Team split:**
- Théo → inventory, interaction, player mechanics
- Ronan → fire propagation, player health/damage
- Hassan → level design, visuals

## Unity & Build

This is a Unity project — there are no CLI build commands that run outside the Unity Editor. Open the project in **Unity 6.3 LTS** (6000.3.11f1). The render pipeline is **URP** (`com.unity.render-pipelines.universal` 17.3.0).

Scenes live in `Assets/Scenes/`. The production scenes are:
- `MainMenu.unity` — main menu
- `GameScene.unity` / `GameSceneInterior.unity` — playable levels

The `GameSceneTheo.unity`, `GameSceneRonan.unity`, and `GameSceneHassa.unity` scenes are individual dev sandboxes — do not treat them as canonical.

## Script Architecture

All C# scripts are under `Assets/Scripts/`, organized by system:

```
Scripts/
├── Inventory/     — InventoryManager.cs, PlayerInteract.cs
├── Controllers/   — DoorController.cs
├── Fire/          — Fire_propagation.cs, PlayerHealth.cs, FireProximityVision.cs
├── HUD/           — StaminaManager.cs, MainMenuManager.cs
├── Lights/        — FlashlightController.cs, FlickeringNeon.cs
└── Minimap/       — MinimapFollow.cs
```

### Key systems

**Inventory (`InventoryManager.cs`)** — Minecraft-style 9-slot hotbar. Items are physical GameObjects; the active item appears in the player's hand. `AddItem()`, `DropItem()`, `GetActiveItem()` are the public API. `PlayerInteract.cs` drives pickup (E key, 3 m raycast) and calls `DoorController.TryOpen()`.

**Fire propagation (`Fire_propagation.cs`)** — Grid-based probabilistic spread (12% chance per direction, 14 directions, every 8 s). Uses `OverlapBox` + raycasts to avoid spreading through walls. Exposes `Fire_propagation.GetClosestFireDistance(Vector3)` and `Fire_propagation.ActiveFireCount` as static helpers so other systems can query fire state without direct references.

**Player health (`PlayerHealth.cs`)** — 3 HP max, 1 HP/s damage when within 2 m of fire. Drives camera shake, death animation (camera collapse + eyelid bars), and a blur overlay. Public API: `TakeDamage()`, `Heal()`.

**Fire proximity vision (`FireProximityVision.cs`)** — Multi-layer visual feedback: UI overlay tint + URP Volume post-processing (DoF blur + vignette). Effect range 15 m → full at 2 m. Coordinates with PlayerHealth for the death blur sequence.

**Stamina (`StaminaManager.cs`)** — Singleton (`StaminaManager.instance`). 100 points, drains at 20/s while sprinting, regens at 15/s. Must recover to 20 % before sprint is available again.

**Door (`DoorController.cs`)** — Requires a specific item (badge, key) checked against `GetActiveItem()`. Smooth rotation coroutine. Item is currently **not consumed** on use — marked as a TODO.

### Cross-system conventions

- Systems that need global fire state call `Fire_propagation.GetClosestFireDistance()` / `Fire_propagation.ActiveFireCount` — do not add direct component references.
- UI elements are located by name at runtime (`GameObject.Find`) inside `Awake`/`Start` — this is intentional for scene flexibility.
- Complex sequences (death, blur escalation) are implemented as coroutines, not `Update` state machines.

## Input

Control scheme uses **AZERTY** layout (ZQSD movement). Key bindings: E = interact, I = inventory, G = drop, F = flashlight, Shift = sprint, Ctrl = crouch, mouse wheel = hotbar scroll, right-click release = throw.

## Packages of note

- `com.unity.probuilder` 6.0.9 — level greyboxing
- `com.unity.inputsystem` 1.19.0 — new Input System
- `com.unity.render-pipelines.universal` 17.3.0 — URP
- `com.unity.ai.navigation` 2.0.11 — NavMesh (present, usage TBD)
- `com.unity.timeline` 1.8.11 — animation sequences
