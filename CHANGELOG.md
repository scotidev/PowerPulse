# Changelog

All notable changes to PowerPulse will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows Semantic Versioning when versioned releases are created.

Entries are grouped by implementation phase, newest first.

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
