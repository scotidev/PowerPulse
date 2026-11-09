# Changelog

All notable changes to PowerPulse will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows Semantic Versioning when versioned releases are created.

## [Unreleased]

### Added

- Read and consolidated the game's design requirements from `GDD.pdf`.
- Created the implementation plan for the almost-complete WebGL-focused version.
- Defined the project's initial technical, gameplay, UI, audio, persistence, balancing,
  optimization, and delivery phases.
- Renamed the working branch to `scotidev-gdd-game-planning`.
- Added this changelog as the source of truth for completed implementation work.
- Initialized Git LFS for Unity binary assets and configured `.gitattributes` with LFS tracking rules for `.asset`, `.prefab`, `.mat`, `.unity`, `.controller`, `.anim`, and other binary types.
- Provided a full step-by-step guide for WebGL Player Settings configuration (portrait 1080×1920, VSync, URP, color space, Auto Graphics API, etc.).

### Changed

- No gameplay or production logic has been implemented yet.

## Changelog maintenance

Update this file in the same change whenever a project modification is made. Record
only work that was actually completed, under the appropriate `Added`, `Changed`,
`Deprecated`, `Removed`, `Fixed`, or `Security` subsection. Do not list planned or
partially completed work as completed.
