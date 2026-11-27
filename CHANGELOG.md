# Changelog

All notable changes to PowerPulse will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows Semantic Versioning when versioned releases are created.

Entries are grouped by implementation phase, newest first.

## Fase 13 — Visual polish, power-up VFX, spawn fixes & emission control

### Added

- **Track grid color sync** (`TrackGridColor`): neon grid lines on track tiles now mirror the player's active color in real-time. During `CollectAllColors` boost, grid flashes through all 5 palette colors with dynamic blink interval (starts fast ~0.08s, slows to ~0.25s) matching `PlayerColor` blink exactly. Uses single shared Material asset — modifying it affects all active + pooled tiles instantly.

- **Power-up dedicated VFX**: new `ShieldVfx` (white explosive burst) and `CollectAllColorsVfx` (multi-color burst) in `Config_VFX`. `VFXClip` gained `PlayMultiColor(Color[])` using Gradient Random Color mode — one VFX instance emits particles of multiple colors simultaneously. `CameraFX.HandlePowerUpCollected` selects correct VFX by `PowerUpType` and passes all palette colors for `CollectAllColors`.

- **CollectAllColors visual on player** (`PlayerColor.m_CollectAllColorsVisual`): optional child GameObject on Player that activates via `SetActive(true/false)` when boost starts/ends. Auto-disables on run reset.

- **Player emission independence** (`SO_ColorPalette.m_PlayerEmissionMultiplier`): global emission multiplier for player material only (default 2x). Decouples player glow from VFX/grid/obstacle emission intensities. `PlayerColor.ApplyColor` now uses this instead of per-color `EmissionIntensity`.

### Changed

- **SpawnManager obstacle color pool**: `GetRandomSpawnColor` no longer adds `BoostColors` to spawn pool. Obstacles only spawn from active + unlocked colors — `CollectAllColors` boost lets player *collect* all colors but doesn't make locked-color obstacles spawn.

- **Power-up spawn clearance**: `FindFreeLaneAtZ` now uses full `m_PowerUpObstacleClearance` (was half). Obstacle lane calculation fixed to inverse spawn formula (`startLane = x/laneWidth + (lanes-1)*0.5 - (width-1)*0.5`), correctly mapping X=-2 → lane 0 (leftmost).

- **Portal cooldown resets power-up timer**: `OpenPortalCooldown` now zeroes `m_DistanceSinceLastPowerUp` so no instant power-up spawn after portal.

- **Obstacle base colors**: fixed ColorPalette entries — all 5 colors now use saturated Base Colors (not pastel/white) so obstacles show solid colored bodies with emission glow, not "white with colored emission".

### Fixed

- **Power-up spawning on top of obstacles**: `FindSafeSpawnZ` (full clearance) + `FindFreeLaneAtZ` (full clearance + correct lane math) now guarantee power-up spawns in free lane at safe Z distance.

- **Power-up spawning immediately after portal**: `OpenPortalCooldown` resets both obstacle and power-up distance accumulators.

- **Grid material URP/Lit error**: `TrackGridColor.CacheOriginalMaterialProperties` now reads `_EmissionColor` (Color) instead of non-existent `_EmissionIntensity` (float) — compatible with URP/Lit shader.

- **Player emission overriding material**: player material emission now controlled by single `PlayerEmissionMultiplier` in ColorPalette, independent of per-color VFX emission.

- **Debug log spam removed**: verbose diagnostic logs in `FindSafeSpawnZ` and `FindFreeLaneAtZ` stripped; only `NO FREE LANE` warning remains.

- **Hardcoded Resources path in Bootstraper**: replaced `Resources.Load("UI/UIManager")` with `BootstrapConfig` ScriptableObject — single hardcoded path loads config asset, prefab reference set in Inspector. Moving UI folder no longer breaks bootstrap.

### Removed

- Hold-to-move (tap-repeat) experiment from `Config_InputSettings` and `PlayerController` — caused double-dash bug, reverted to single-tap-only dash per press.

### Added

- **Power-up simplification**: reduzido para 2 tipos essenciais — `Shield` (absorve 1 colisão incompatível) e `CollectAllColors` (permite coletar qualquer cor por duração configurável no Inspector). Removidos `BoostTwoColors`, `BoostThreeColors`, `BoostAllColors`.
- **Boost early return (FOV + Speed)**: câmera FOV e velocidade da pista começam a voltar ao normal **antes do boost acabar** (tempo configurável `BoostFovReturnTime`, `BoostSpeedReturnTime`, curva `BoostReturnCurve`, multiplicador `BoostSpeedMultiplier` no `Config_VFX`). Dá aviso visual/claro ao jogador.
- **Obstacle width-aware spawning**: `SpawnManager` lê `Obstacle.Size.x` do prefab (cache no prewarm) e calcula lanes válidas considerando largura real. Obstáculo 1x2 só spawna onde caiba em 2 lanes consecutivas (0+1, 1+2, 2+3, 3+4), mantendo regra de "pelo menos 1 lane livre".
- **CameraFollow manual mode**: novo campo `Manual YZ Rotation` (default true). Quando ligado, câmera **só segue X do player**; Y, Z e Rotation vêm do Transform (posicionamento direto no Scene View). Adicionado `Vertical Offset` para controle fino da posição vertical do player na tela.
- **SpawnDistanceAhead no SpawnManager**: novo campo `Spawn Distance Ahead` (default 50m). Obstáculos, portais e power-ups agora spawnam à frente do player (Z=0) em distância fixa configurável, **desacoplado do TrackManager.FrontZ**. Corrige obstáculo aparecendo aos 150m (causado por tiles pré-criados).
- **InitialSafeDistance movido para Config_GameBalance**: single source of truth. `SpawnManager` lê do asset de balanceamento. Inspector centralizado.
- **Player air dash**: `Instant Dash In Air` (default true) — dash completa instantaneamente no ar para troca de lane responsiva mid-jump. `Air Dash Speed Multiplier` (default 3x) como fallback.

### Changed

- **Removed Moving/Faster obstacle behaviours**: enum `ObstacleBehaviour` mantém só `Static`, `Tall`, `Wide`. Toda lógica de movimento lateral removida (`MoveObstacleLaterally`, `ChooseBehaviour` Moving/Faster, `GetBehaviourSpeed`, `m_MovingSpeed`, `m_FasterSpeed`, `c_FasterChance`, `MovingEnabled`, `FasterEnabled` no RunManager, milestones `EnableMoving`/`EnableFaster`).
- **Obstacle component simplificado**: removido `MoveLateral`, `IsMovable`, `MoveSpeed`, `m_Direction`, `c_TrackHalfWidth`, overload `SetBehaviour(behaviour, speed)`. Agora só `SetBehaviour(behaviour)`.
- **SpawnManager desacoplado do TrackManager**: usa `m_SpawnDistanceAhead` para obstáculos, portais e power-ups. `HasPortalClearance`, `GetSafePortalZ`, `TrySpawnPowerUp` usam distância fixa.
- **Config_GameBalance limpeza**: removidos `DifficultyMilestone.EnableMoving`, `EnableFaster`, `m_MovingSpeed`, `m_FasterSpeed`, properties `MovingSpeed`, `FasterSpeed`.
- **RunManager limpeza**: removidas properties `MovingEnabled`, `FasterEnabled`, log de milestone simplificado.

### Fixed

- **Y da câmera descendo progressivamente (3.9→3.2)**: `CameraFollow` agora guarda `m_BaseY`/`m_BaseZ` no `Awake` e **nunca lê do transform** (evita vazamento do shake da CameraFX).
- **Death camera caindo no infinito**: `CameraFollow` para de seguir quando `GameState == GameOver`.
- **Primeiro obstáculo aos 150m**: causado por tiles pré-criados no TrackManager (`FrontZ` ~150m). Corrigido com `SpawnDistanceAhead` (50m default).
- **SRP violado (InitialSafeDistance em 2 lugares)**: consolidado no `Config_GameBalance` apenas.
- **SpawnSpacing log spam**: removidos logs de debug excessivos.

### Removed

- `ObstacleBehaviour.Moving`, `ObstacleBehaviour.Faster` do enum.
- `m_MovingSpeed`, `m_FasterSpeed`, `MoveObstacleLaterally`, `ChooseBehaviour` (Moving/Faster), `GetBehaviourSpeed`, `c_FasterChance` do SpawnManager.
- `EnableMoving`, `EnableFaster` do `DifficultyMilestone` e `MovingEnabled`, `FasterEnabled` do RunManager.
- `m_MovingSpeed`, `m_FasterSpeed`, `MovingSpeed`, `FasterSpeed` do Config_GameBalance.
- Logs de debug verbosos do `TrySpawnObstacles` e `SpawnObstacle`.

### Added

- `UIManager` (`Assets/Core/Scripts/Managers/UIManager.cs`): dono único de todos os painéis (HUD, MainMenu, Pause, GameOver, Options, Controls, Credits). Reage ao `IGameManager.StateChanged`, assina/desinscreve `ScoreManager`/`RunManager`, lê o action map `UI` (Esc/Enter/E), liga os botões e gerencia os sliders de volume.
- `IUIManager` + `UIPanel`: contrato e enum dos painéis, registrados no `ServiceLocator`.
- `GameFlow` + `SceneFlowBootstrap` (`Assets/Core/Scripts/Managers/`): transições `StartGame`/`Pause`/`Resume`/`Restart`/`GoToMenu`. O `GameFlow` nasce por script nas cenas `Menu` e `Game` (não precisa de GameObject na cena).
- `UIInput`: helper estático para o action map `UI`, escopado ao mapa (evita colisão de nome entre mapas).
- Action map `UI` no `PowerPulseInputActions` (`Pause` = Esc, `Confirm` = Enter/E, `Cancel` = Esc). O mapa `Player` e os control schemes foram preservados.
- `SaveManager` real: PlayerPrefs + JSON para `ScoreData` e `SettingsData`, com defaults seguros e log de aviso para save corrompido.
- `AudioManager` real: `AudioMixer` com `MasterVolume`/`MusicVolume`/`SfxVolume`, pool de `AudioSource` para SFX e conversão de volume para decibéis.
- `UIButtonSound` (`Assets/Core/Scripts/UI/`): placeholder opcional de som de clique nos botões de UI.
- `GameFlow`, `SceneFlowBootstrap`, `UIManager`, `IUIManager`, `UIInput`, `UIPanel`, `UIButtonSound` — scripts e `.meta` novos.

### Changed

- `Bootstraper`: removida a linha temporária `LoadScene("Game")`; instância o prefab `UIManager` de `Resources/UI/UIManager` com `DontDestroyOnLoad` e registra `IUIManager`; chama `saveManager.Load()` e aplica os volumes no `AudioManager`.
- `AudioManager` passa a resolver o mixer em `Resources/Audio/PowerPulseAudio` quando o campo está vazio (o manager nasce por código, sem Inspector para atribuir).
- `PlayerController` só lê dash/pulo quando o estado global é `Playing` — clicar em botão de UI não move mais a esfera.
- `RunManager` perdeu o `m_AutoStart` (a run passa a ser iniciada pelo `GameFlow`); ganhou `HasStarted` e `RestoreRun()`.
- O mixer `PowerPulseAudio.mixer` foi movido para `Assets/Core/ScriptableObjects/Resources/Audio/` para ser carregado em runtime.

### Fixed

- **A run nunca começava.** `GameFlow.StartGame()` mudava o estado para `Playing` antes de carregar a cena `Game`; o `RunManager` se inscrevia depois, o evento já tinha disparado e ninguém chamava `StartRun()`. O `RunManager.Start()` agora aplica o estado atual.
- **A UI nascia invisível.** O `UIManager` só ligava painéis em resposta ao `StateChanged`; no boot o jogo já está em `MainMenu` e o evento não dispara. O `OnEnable` aplica o painel do estado atual.
- **`Resume()` reiniciava a corrida.** `StartRun()` chama `Reset()` (zera distância/velocidade), então pausar e continuar recomeçava o jogo. Criado `RunManager.RestoreRun()` com o flag `HasStarted`, separado de `IsRunning`.
- **`Restart` chamava `StartRun()` duas vezes** (o evento `Playing` + a chamada explícita). O `HandleStateChanged` só inicia a run se `!HasStarted`.
- **`GameFlow` não nascia no Menu.** O `SceneFlowBootstrap` criava o flow só na cena `Game`, então o botão Jogar não funcionava. Agora cria nas duas cenas.
- **O `UIManager` persistente não desinscrevia os scene-systems.** Ao sair do `Game` para o Menu, os managers ficavam apontando para objetos destruídos (o Unity compara destruído como `null`), e o HUD morria na segunda run. `UnsubscribeSceneSystems()` roda em `MainMenu`/`Paused`/`GameOver`, e `ResolveSceneSystems()` roda ao entrar em `Playing`.
- **Overlays não existiam de verdade.** `ShowOptions()` usava `ShowPanel()`, que desligava o Menu, e o Voltar sempre ia para Pausa. Separados `m_StatePanel` e `m_OverlayPanel`, com `ShowOverlay`/`CloseOverlay`; `ShowPanel` fecha overlays abertos.
- **`AudioMixer.FindParameter` não existe no Unity 6000.6** — usava uma API inexistente e não compilava. Trocado por `GetFloat(nome, out _)`.
- **`Confirm`/`Cancel` habilitados e nunca lidos.** Agora `ReadUIInput` chama `HandleConfirmInput`/`HandleCancelInput` (despacho no botão selecionado + atalhos sem seleção) e o fallback não age com overlay aberto.
- **Selo de "novo recorde" sempre visível.** O `ScoreManager` atualiza o recorde antes de avisar a UI, então `Score >= BestScore` era sempre verdadeiro. O `UIManager` passou a assinar `OnRecordBeaten`.
- **`SaveManager.Load()` nunca era chamado** — o recorde salvo não era lido e podia ser sobrescrito por um score menor. Agora é chamado no `Bootstraper`.
- **Save parcialmente válido não era detectado.** O `JsonUtility.FromJson` é leniente; um save corrompido podia carregar `BestScore` negativo ou volumes fora da faixa. Adicionados `Sanitize` (valida o recorde e aplica `Clamp01` nos volumes).
- **`m_AutoStart` órfão** removido do YAML de `Assets/Scenes/Game.unity`.
- **`Object` ambíguo no `UIManager`** (`using System;` + `UnityEngine`): adicionado o alias `using Object = UnityEngine.Object;`.
- **Campos mortos removidos** do `UIManager` (`m_GameSceneName`, `m_MenuSceneName`).
- **O `Bootstraper` não registrava `null`** se o prefab de UI existisse sem o componente `UIManager`.

### Removed

- A linha temporária `LoadScene("Game")` do `Bootstraper` (o jogo agora abre no Menu).
- O campo `m_AutoStart` do `RunManager`.
- O estado `GameState.Tutorial` e toda a seção de tutorial dos docs (ver entrada "Remoção do tutorial", acima).

## Remoção do tutorial (docs + estado de jogo)

### Removed

- O tutorial previsto no GDD foi removido por decisão do jogador: o jogo não terá tutorial e a primeira run já é a experiência completa.
- O estado `GameState.Tutorial` (`Assets/Core/Scripts/Enums/GameState.cs`) e o `case` correspondente em `RunManager.HandleStateChanged`. O `GameManager` transiciona direto entre `MainMenu` e `Playing`.
- A seção "Tutorial System" do plano e toda referência a tutorial nos docs (`PLAN.md`, `GDD_NOTES.md`). Nenhum asset de tutorial existia no `Assets/` — não houve exclusão de cena, overlay ou flag de save.
- As fases do plano foram renumeradas (12 → 11, 13 → 12, 14 → 13, 15 → 14, 16 → 15, 17 → 16, 18 → 17), sem gap.

## Fase 10 — Câmera, efeitos e juice

### Added

- `CameraFX` (`Assets/Core/Scripts/Camera/`): shake de posição na coleta e na morte, punch de FOV no boost, flash de tela, ajustes temporários de Bloom/Chromatic Aberration e spawn/coloração/reciclagem dos VFX. Roda depois do `CameraFollow` e reage apenas aos eventos da run.
- `VFXClip` (`Assets/Core/Scripts/VFX/`): VFX reutilizável com `ParticleSystem` criado em runtime; cada prefab é um data holder configurável.
- `Config_VFX` + `VFXSettings.asset`: intensidades, durações, flash, Bloom/CA, prefabs de VFX, prewarm e escala.
- Prefabs de VFX: `VFX_Collect`, `VFX_Death`, `VFX_Dash`, `VFX_Portal` e `VFX_PowerUp`.
- Eventos `PlayerController.OnDashed`, `SpawnManager.OnPortalCrossed` e `SpawnManager.OnPowerUpCollected`.
- `CameraFollow.FollowPosition`, para o shake ser aplicado sem interferir no suavizamento.

### Changed

- `ScoreManager.OnCollected` agora inclui `ColorId`, para tingir o VFX de coleta.
- `PlayerDeath` deixou de piscar: esconde a esfera e deixa o VFX de morte assumir o feedback.
- `PowerUpCatalog` passou a referenciar `VFX_PowerUp` no campo `VfxPrefab`.
- O `ScoreManager` expõe `Palette` para os sistemas de VFX.
- O flash usa um Canvas overlay (criado em runtime quando a cena não tem um), deixando o caminho pronto para o HUD da Fase 12.

### Fixed

- **Aviso de `ParticleSystem` no prewarm.** O `VFXClip` agora para e limpa o sistema antes de definir a `duration`; o Unity recusava alterar a duração enquanto o `ParticleSystem` tocava no `Awake`.

## Fase 9 — Power-ups

### Added

- `PowerUp` e os quatro prefabs (`ShieldPowerUp`, `BoostTwoPowerUp`, `BoostThreePowerUp`, `BoostAllPowerUp`), com rotação/flutuação e detecção de coleta.
- `SO_PowerUpCatalog` com tipo, duração, cores, SFX e VFX por item.
- Spawn de power-ups no `SpawnManager` com regras de não-sobreposição (obstáculo/portal), intervalo mínimo e disponibilidade por cores desbloqueadas.
- Efeitos aplicados ao `ScoreManager`: shield (absorve uma colisão incompatível) e boosts de 2/3/todas as cores.
- Piscar do boost na `PlayerColor`, com intervalo que acelera conforme o boost expira.

## Ajustes de ritmo e cobertura (pós-Fase 8/9)

### Added

- `SpawnManager.PortalCooldown`: an adaptation window after each portal. No obstacles spawn for the configured distance (25 m by default), giving the player time to read the new active color before the next challenge.
- `SpawnManager.ClearObstaclesNear()`: when a portal spawns, every obstacle too close to it is recycled, so the player never meets a row of obstacles together with the portal.
- `SpawnManager.PortalClearance` / `PortalClearanceAhead`: asymmetric clearance around the portal. The clearance **ahead** is larger (base + extra), because the obstacles that already spawned are still traveling toward it and the player should meet them only after reading the new color.
- `Config_GameBalance.PortalClearance` (40 m), `PortalClearanceAhead` (20 m extra) and `PortalCooldown` (25 m): the same values in the balance asset, so everything is configured in one place.
- `PortalManager` now calls `SpawnManager.OpenPortalCooldown()` when a portal is crossed.

### Changed

- `TrackManager` has **9 tiles** and `InitialTileZ = -20` as the new script defaults; the scene still uses 6 tiles / `z = 0` (kept as-is by decision). The end of the track should stop being visible once the scene is updated.
- `TrackManager.InitialTileZ` and `TileCount` are configurable in the Inspector.
- The portal clearance is now measured as the stretch the player still has to travel (the difference in world Z between the spawn point and the portal), not as an absolute world distance. This matters because the front edge of the track advances while the portals travel toward the camera.

### Fixed

- **Obstacles spawning together with the portal.** The portal is now placed at a safe world Z, pushed forward from any active obstacle that would be too close. Nothing is removed from the track: the obstacles that already exist keep moving, and the portal simply spawns further ahead.
- **Obstacles ahead of the portal.** The clearance now has two values: 40 m behind the portal and 60 m ahead of it. Validated by simulation: the player gets **40 m of breathing room before** the portal and **60 m after** it.
- **Removed `ClearObstaclesNear`.** It used to recycle every obstacle within the clearance radius, which made all obstacles vanish when a portal appeared. The prevention of new spawns is enough; existing obstacles are never removed.

## Correções de estabilidade (pós-Fases 7/8)

### Fixed

- **Player drifting away from the camera.** Nothing owned the Z axis of the player, so residual physics pushed the sphere forward over time. The `PlayerController` now locks the Z axis (rigidbody constraint + `ApplyZAxisLock` in `FixedUpdate`), with `Lock Z Axis` and `Locked Z` configurable in the Inspector.
- **Obstacles disappearing / track "tangling".** Two causes: (1) `ObjectPool.Release` accepted the same instance twice, so it entered the free queue twice and was handed out twice — spawning duplicates that moved twice as fast; (2) `TrackManager` and `SpawnManager` each computed their own movement delta in separate `Update` calls, so tiles and obstacles drifted apart. The pool now tracks `InUse` and rejects a double release; the `TrackManager` owns the movement of the whole scene and pushes the obstacles with the same delta as the tiles.
- **Camera framing.** New `CameraFollow` follows the player on the X axis with a locked offset, so the framing stays readable when the player changes lane. The GDD describes a fixed camera; this implements that as a follow with a locked offset (the standard Unity approach).

### Added

- `CameraFollow` (`Assets/Core/Scripts/Camera/`): follows the target on X, keeps Y/Z offsets constant, configurable smoothing.

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
