# Changelog

All notable changes to PowerPulse will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows Semantic Versioning when versioned releases are created.

Entries are grouped by implementation phase, newest first.

## Fase 8 — Portais de cor

### Added

- `Portal`: translucent arc that crosses the whole track. The player cannot dodge it; crossing it changes the active color and unlocks the next color in the palette order. Tinted through a `MaterialPropertyBlock` and recycled by the shared `ObjectPool`.
- `PortalManager`: spawns a portal every `PortalInterval` meters (500 m by default, configurable), chooses the next color from `SO_ColorPalette.UnlockOrder`, applies the color change on crossing, and recycles the portals behind the camera.
- `Config_GameBalance.PortalObstacleGap` so a portal never spawns on top of an obstacle.

### Changed

- The unlock order (Red → Pink → Blue → Cyan → Purple) is now the progression source. The first two colors are active at the start; each portal unlocks the next one, up to the five.
- Once all five colors are unlocked, portals keep changing the active color randomly among the unlocked ones.
- The `Portal` component does **not** reference the `ScoreManager`: it only detects the crossing and notifies the `PortalManager` (a scene object), which applies the color change. A prefab cannot reference scene objects, so the responsibility moved to the manager.

## Fase 7 — Progressão e dificuldade

### Added

- `Config_GameBalance.DifficultyMilestone`: ordered, Inspector-configurable milestones (distance, speed, moving/faster enable, max obstacles per row).
- `RunManager` now reacts to milestones on events (`OnMilestoneReached`) instead of recalculating the difficulty every frame. Exposes `MovingEnabled`, `FasterEnabled` and `MaxObstaclesPerRow`.
- `SpawnManager` activates `Moving`/`Faster` behaviours according to the current milestone and spawns up to `MaxObstaclesPerRow` obstacles per row.
- `SpawnManager.PickFreeLanes()`: always leaves at least one lane free, so no row can block the whole track (safety rule).
- `ScoreManager.OnRunReset`: a new run resets the spawn manager, the portal manager and the player, so a restart starts from zero.
- `PlayerDeath.ResetPlayer()` and `PlayerController.ResetMovement()` restore the player (components, rigidbody, lane, renderers) on restart.

### Changed

- The obstacle behaviour is no longer read from the prefab: it is decided by the active milestone, so the difficulty curve is data-driven.
- Obstacles are avoidable by lane change or jump — no "jump-only" obstacle exists, matching the player's tools.

## Fase 6 — Obstáculos, coleta e pontuação

### Added

- `ScoreManager`: authority of the run. Owns the score, the local record, the active color, the unlocked colors, the shield flag and the active boost. Decides the collect/die rule (`CanCollect`), absorbs one incompatible collision when a shield is active and ends the run through the `IGameManager` state machine. Publishes `OnScoreChanged`, `OnShieldChanged`, `OnBoostChanged`, `OnColorChanged`, `OnCollected`, `OnPlayerDied` and `OnRecordBeaten`.
- Obstacle behaviours in `Obstacle`: `Static`, `Moving`, `Faster`, `Tall` and `Wide`, with lateral movement clamped to the track edges.
- Score by obstacle area (1x1 = 1, 2x1 = 2, 2x2 = 4) and recycling of the collected obstacle through the shared `ObjectPool`.
- Shield/boost state mirrored on the player: `PlayerColor` reflects the `ScoreManager` state, shows/hides the shield visual and blinks between the boost colors.
- `SpawnManager.ResetRun()` and `SetSpawningEnabled()` so the death handler does not permanently disable the component.

### Changed

- `PlayerCollision` now forwards every obstacle contact to the `ScoreManager` instead of only exposing the trigger event.
- `SpawnManager` reads the colors in play from the `ScoreManager` (the hardcoded Phase 5 list remains only as fallback) and moves the obstacles laterally according to their behaviour.
- `SpawnManager` stops spawning new obstacles when the player dies.
- `PlayerCollision` no longer references the `ScoreManager`: it asks the `PlayerColor` on the same GameObject, which delegates the rule to the `ScoreManager`.
- `PlayerDeath`: the player now has a visible death animation (blink + hide) before the run closes. The run only changes the global state to `GameOver` after the animation finishes, so the death is readable instead of looking like a freeze.

## Fase 5 — Run básica e deslocamento da pista

### Added

- `RunManager`: owns the run progression (speed, distance, difficulty) and reacts to the global game state.
- `TrackManager` and `TrackTile`: recycled track tiles moved toward the camera, keeping a constant number of active tiles.
- `ObjectPool`: generic GameObject pool shared by track tiles and obstacles.
- `SpawnManager`: first spawn pattern (one obstacle per row, random lane, safe spacing, initial safe distance).
- Obstacle prefabs `Obstacle1x1` and `Obstacle1x2` with `Obstacle` component, trigger collider and `ObstacleCatalog` asset.

### Changed

- The track now looks infinite: tiles are recycled behind the camera and refilled at the front, with no unbounded growth in the Hierarchy.

## Fase 4 — Jogador e controles

### Added

- `PowerPulseInputActions` Input Actions asset with `DashLeft`, `DashRight` and `Jump` (keyboard bindings and `Keyboard&Mouse` / `Touch` control schemes).
- `PlayerController`: single owner of the player position, lane dash (exactly 1 lane, clamped from -2 to +2) and jump with ground raycast.
- `PlayerColor`: active color applied to the player material.
- `PlayerCollision`: trigger contact detection.
- Trail renderer enabled during the dash.

### Changed

- The player uses a non-kinematic `Rigidbody` with continuous collision detection.

## Fase 3 — Fundação visual e física

### Added

- Track plane with 6 neon lane strips (`LaneDark` and `NeonWhite` materials).
- Player: emissive sphere material (`Player_Light`).
- Global Volume with Bloom, Chromatic Aberration and Tonemapping.
- Layers `Player`, `Obstacle`, `PowerUp`, `Portal` and `Track`.
- Folders `Environment/Lanes`, `Environment/Obstacles`, `Environment/PowerUps` and `Player`.

### Changed

- `Game` scene assembled with the visual base (plane, strips, player, camera and volume).

## Fase 2 — Dados configuráveis

### Added

- `ColorId`, `ObstacleBehaviour` and `PowerUpType` enums.
- `SO_ColorPalette`, `SO_ObstacleCatalog` and `SO_PowerUpData` ScriptableObjects.
- `Config_GameBalance` and `Config_InputSettings` ScriptableObjects.
- Default data assets in `Assets/Core/ScriptableObjects/`: color palette, obstacle catalog, four power-up types, game balance and input settings.

### Changed

- Reorganized the project structure: C# scripts moved into `Core/Scripts/Bootstrap`, `Core/Scripts/Managers` and `Core/Scripts/Enums`; ScriptableObjects moved into `Core/ScriptableObjects`.

## Fase 1 — Estrutura arquitetural

### Added

- Core architecture under `Assets/Core/Scripts`: `Bootstraper`, `ServiceLocator`, `IGameService`, `GameManager`/`IGameManager`, `GameState`, `SceneLoader`/`ISceneLoader`.
- `ISaveService` and `SaveManager` skeleton, with `ScoreData` and `SettingsData`.
- `IAudioService` and `AudioManager` skeleton.
- Registration of all persistent services in the `Bootstraper`.

### Fixed

- Added a `GraphicRaycaster` to the fade overlay canvas so `blocksRaycasts` can intercept input during scene transitions.

### Changed

- Added the `Menu` and `Game` scenes to the build settings.

## Fase 0 — Preparação e critérios do projeto

### Added

- Unity 6000.6.4f1 project configured with URP, Input System, TextMeshPro and WebGL build profiles.
- `GDD.pdf` (game design document) and `PLAN.md` (implementation plan).
- Git LFS configuration for Unity binary assets.
- `CHANGELOG.md` as the source of truth for completed work.
- `Menu` scene with a camera and a directional light.

### Changed

- Configured the WebGL Player Settings (1080×1920, VSync, URP, color space, Auto Graphics API).

## Changelog maintenance

Update this file whenever a phase is completed. Create one entry per phase,
newest first. Record only work that was actually completed, under the appropriate
`Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, or `Security` subsection.
Do not list planned or partially completed work as completed.
