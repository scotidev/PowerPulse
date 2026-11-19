# PowerPulse — Fase 11: HUD, menus e fluxo de jogo

**Data:** 2026-11-18
**Status:** aprovado (decisão do jogador)
**Plano:** `docs/superpowers/plans/2026-11-18-fase-11-ui-flow.md`

---

## 1. Objetivo

Entregar o fluxo completo de jogo do PowerPulse: **MainMenu → run → pausa →
game over → reinício → menu**, com HUD dentro da run, menu de Opções funcional
e tela de Créditos.

A fase também antecipa duas fundações que o PLAN original deixava para as
Fases 12 e 13 (`AudioManager` e `SaveManager` reais), porque a UI depende delas
para funcionar de verdade.

**Fora de escopo:** animações/transições elaboradas de UI, tema visual final,
som de clique de UI, integração de áudio de gameplay (Fase 12), balanceamento
(Fase 15), persistência do recorde no `SaveManager` (o `ScoreManager` já grava
em `PlayerPrefs` como fallback e continua assim).

---

## 1.1 Nomenclatura e padrões do projeto (vinculativos)

- Código em inglês; XML doc em classes públicas e métodos complexos.
- Prefixos: `m_` campos privados, `c_` constantes, `s_` estáticos, `SO_`/`Config_` assets, `I` interfaces, `On` eventos.
- Convenção de nomes do GDD: `UIManager`, `GameFlow`, `IUIManager`.
- `Tooltip` em **todos** os campos serializados.
- Nenhuma referência por string: sem `transform.Find`, sem `GameObject.Find`.
- Arquitetura: `Bootstraper` ([RuntimeInitializeOnLoadMethod]) + `ServiceLocator` com `Current` e `Register<T>`/`Get<T>`; serviços persistentes (Layer 1) são criados em código, sistemas de cena (Layer 2) vivem na cena.
- Uma única fonte de verdade por dado: `ScoreManager` (score/cores/escudo/boost), `RunManager` (distância/velocidade), `IGameManager` (estado), `ISaveService` (persistência), `IAudioService` (áudio), `IUIManager` (painéis).
- Sem refatoração não relacionada: o comentário antigo do `RunManager` sobre mover a distância para o `ScoreManager` **não** é desta fase.

---

## 2. Decisões de arquitetura (confirmadas com o jogador)

| # | Decisão | Motivo |
|---|---|---|
| D1 | **Um único script `UIManager`** controlando todos os painéis. Sem controllers por painel. | Decisão do jogador. Menos arquivos, árvore de referências única. |
| D2 | O `UIManager` vive num **prefab** instanciado pelo `Bootstraper` (`DontDestroyOnLoad`) e registrado como `IUIManager` no `ServiceLocator`. | O `Bootstraper` roda antes das cenas: o prefab é o que permite ter referências arrastadas no Inspector. |
| D3 | A árvore de painéis (Canvas, EventSystem, painéis, textos, botões) é montada **no Editor pelo jogador**; o agente escreve os scripts e liga as referências. | Decisão do jogador. |
| D4 | `GameFlow` é um **MonoBehaviour na cena `Game`**, não um serviço. O `UIManager` resolve com `FindObjectOfType<GameFlow>()` + cache. | Evita um `GameObject` persistente novo e mantém a dependência de managers da cena explícita. |
| D5 | Bloqueio de input do jogador: `PlayerController` só lê dash/pulo quando `IGameManager.CurrentState == Playing`. | Sem isso, clicar num botão de UI dá dash na esfera. |
| D6 | Esc/Enter nos menus: action map `UI` no `PowerPulseInputActions`, lida pelo Input System. Foco de teclado/gamepad vem do `EventSystem` padrão (já usa `InputSystemUIInputModule`). | Decisão do jogador. Não mistura `UnityEngine.Input`. |
| D7 | `AudioMixer` novo, com 3 grupos exposed (`Master`, `Music`, `Sfx`). | Volume por mixer é o padrão Unity e não exige criar `AudioSource` para controlar volume. |
| D8 | Sliders de Opções em **0–100%** + checkbox Mudo, gravando em `SettingsData` + `AudioManager`. | Decisão do jogador. |
| D9 | Botão **Sair** desabilitado no WebGL, com tooltip "indisponível no WebGL". | `Application.Quit()` não funciona em WebGL. |
| D10 | Painel de **Controles** é texto estático (dash/pulo) + botão Voltar; **Créditos** tem texto + botão de skip. | Decisão do jogador. |
| D11 | Remover o `Canvas` da cena `Game` e o `Text (TMP)` placeholder. O `Flash` do `CameraFX` passa a ser atribuído no prefab de UI. | Evita dois Canvases concorrendo. |

---

## 3. Estrutura de arquivos

### Novos

| Arquivo | Responsabilidade |
|---|---|
| `Assets/Core/Scripts/Managers/IUIManager.cs` | Contrato do `UIManager` (registrado no `ServiceLocator`). |
| `Assets/Core/Scripts/Managers/UIManager.cs` | Único dono de todos os painéis e de toda a navegação de UI. |
| `Assets/Core/Scripts/Managers/GameFlow.cs` | Transições de jogo (menu → run → pausa → game over → menu). |
| `Assets/Core/Scripts/Enums/UIPanel.cs` | Enum dos painéis (`HUD`, `MainMenu`, `Pause`, `GameOver`, `Options`, `Controls`, `Credits`). |
| `Assets/Core/Prefabs/UIManager.prefab` | Prefab com Canvas + EventSystem + todos os painéis. **(montado no Editor)** |
| `Assets/Core/ScriptableObjects/Audio/PowerPulseAudio.mixer` | `AudioMixer` com `Master`/`Music`/`Sfx`. **(criado no Editor)** |

### Modificados

| Arquivo | Mudança |
|---|---|
| `Assets/Core/Scripts/Bootstrap/Bootstraper.cs` | Remove a linha temporária `LoadScene("Game")`; instancia o prefab `UIManager`; registra `IUIManager`; chama `SaveManager.Load()` e aplica os volumes. |
| `Assets/Core/Scripts/Managers/AudioManager.cs` + `IAudioService.cs` | Implementação real (mixer, pool de SFX, volumes, mute). |
| `Assets/Core/Scripts/Managers/SaveManager.cs` + `ISaveService.cs` | PlayerPrefs + JSON reais. |
| `Assets/Core/Scripts/Managers/RunManager.cs` | Remove `m_AutoStart` (a run passa a ser iniciada pelo `GameFlow`). |
| `Assets/Player/Scripts/PlayerController.cs` | Gate de input por `GameState`. |
| `Assets/Player/PowerPulseInputActions.inputactions` | Novo action map `UI` (`Pause`, `Confirm`, `Cancel`). |
| `Assets/Scenes/Game.unity` | Remove Canvas, `Text (TMP)` e `Flash` legado; cria o GameObject `GameFlow` (por script). |
| `Assets/Scenes/Menu.unity` | Continua só com câmera + luz (a UI vem do prefab). |

---

## 4. Máquina de estados

```
MainMenu ──StartGame()──▶ Playing ◀──Restart()── GameOver
                            │  ▲                      │
                     Pause()│  │Resume()        GoToMenu()
                            ▼  │                      ▼
                          Paused ◀────────────────────┘
```

- `GameState` atual: `MainMenu`, `Playing`, `Paused`, `GameOver` (`Tutorial` foi removido).
- O `GameManager` já possui `SetState`, `PauseTime` e `ResumeTime`.
- **Um painel visível por vez.** O `UIManager` assina `StateChanged` e aplica:
  - `MainMenu` → `MainMenuPanel`
  - `Playing` → `HUDPanel`
  - `Paused` → `PausePanel`
  - `GameOver` → `GameOverPanel`
- `OptionsPanel`, `ControlsPanel` e `CreditsPanel` são **overlays**: substituem o painel do estado atual e têm botão Voltar que devolve ao painel de origem.

### Regras

- `Pause()`: só funciona em `Playing`; faz `SetState(Paused)` + `PauseTime()`.
- `Resume()`: só funciona em `Paused`; faz `ResumeTime()` + `SetState(Playing)`.
- `StartGame()`: de `MainMenu`; faz `SetState(Playing)` + `SceneLoader.LoadScene("Game")`; o `GameFlow` da cena `Game` chama `RunManager.StartRun()`.
- `Restart()`: de `GameOver`; faz `SetState(Playing)` + `RunManager.StartRun()` (sem trocar de cena).
- `GoToMenu()`: de `Paused` ou `GameOver`; faz `ResumeTime()` + `SetState(MainMenu)` + `SceneLoader.LoadScene("Menu")`.

---

## 5. `UIManager` — API e campos

### Campos serializados (árvore do prefab)

```
[Header("Config")]
  m_GameManager        : IGameManager (via ServiceLocator, não serializado)
  m_InputActions       : InputActionAsset
  m_Palette            : SO_ColorPalette (para tingir o indicador de cor)

[Header("Panels")]
  m_HUDPanel           : GameObject
  m_MainMenuPanel      : GameObject
  m_PausePanel         : GameObject
  m_GameOverPanel      : GameObject
  m_OptionsPanel       : GameObject
  m_ControlsPanel      : GameObject
  m_CreditsPanel       : GameObject

[Header("HUD")]
  m_ScoreText          : TextMeshProUGUI
  m_DistanceText       : TextMeshProUGUI
  m_ActiveColorImage   : Image
  m_ShieldIcon         : GameObject
  m_BoostIcon          : GameObject
  m_BoostFill          : Image
  m_PauseButton        : Button

[Header("Main Menu")]
  m_PlayButton         : Button
  m_OptionsButton      : Button
  m_CreditsButton      : Button
  m_QuitButton         : Button
  m_QuitTooltip        : GameObject   (ou texto de tooltip)

[Header("Pause")]
  m_ResumeButton       : Button
  m_PauseOptionsButton : Button
  m_PauseMenuButton    : Button

[Header("Game Over")]
  m_FinalScoreText     : TextMeshProUGUI
  m_BestScoreText      : TextMeshProUGUI
  m_NewRecordText      : GameObject  (visível só quando bateu recorde)
  m_RetryButton        : Button
  m_GameOverMenuButton : Button

[Header("Options")]
  m_MasterSlider       : Slider   (0–100)
  m_MusicSlider        : Slider   (0–100)
  m_SfxSlider          : Slider   (0–100)
  m_MuteToggle         : Toggle
  m_ControlsButton     : Button
  m_OptionsBackButton : Button

[Header("Controls")]
  m_ControlsBackButton : Button

[Header("Credits")]
  m_CreditsText        : TextMeshProUGUI
  m_CreditsSkipButton  : Button

[Header("Scene / Flow")]
  m_GameFlow           : GameFlow   (resolvido em runtime, campo de depuração opcional)
  m_GameSceneName      : string = "Game"
  m_MenuSceneName      : string = "Menu"
```

### API pública

```csharp
public interface IUIManager : IGameService
{
    void ShowPanel(UIPanel panel);   // enum: HUD, MainMenu, Pause, GameOver, Options, Controls, Credits
    void ShowGameOver(int score, int bestScore, bool isNewRecord);
    void SetHudScore(int score);
    void SetHudDistance(float distance);
    void SetHudActiveColor(ColorId color);
    void SetHudShield(bool active);
    void SetHudBoost(bool active, float normalizedTimeLeft);
    void ApplySettings(SettingsData settings);   // Opções: preenche sliders/toggle
    SettingsData CollectSettings();              // Opções: lê sliders/toggle
}
```

### Comportamento interno

- `Awake` do prefab: assina `IGameManager.StateChanged` e `DontDestroyOnLoad` já é feito pelo `Bootstraper`.
- Escrito por estado: ao entrar em `Playing` mostra HUD e subscreve `ScoreManager`/`RunManager`; ao sair, desinscreve (evita referência morta entre cenas).
- Escrito por evento: `Esc` em `Playing` → `GameFlow.Pause()`; `Esc` em `Paused` → `GameFlow.Resume()`. O `Esc` é lido no `Update` do `UIManager` (não no `PlayerController`).
- `Confirm`/`Cancel` do action map `UI` disparam o botão focado via `EventSystem.current.SetSelectedGameObject` + `Submit`/`Cancel`, permitindo teclado/gamepad sem código extra de navegação. Se não houver botão focado, `Confirm` em `MainMenu` inicia o jogo (atalho padrão).
- Nenhum `Find` por string. Tudo por `[SerializeField]`.
- **O `UIManager` subscreve `ScoreManager`/`RunManager` só quando entra em `Playing`**, e desinscreve ao sair. Como esses managers são destruídos ao trocar de cena, assinar de fora de forma permanente deixaria referência morta.
- **`UIPanel` é enum**, não `int`, para o Inspector e a API ficarem legíveis.

---

## 5.1 GameFlow — API

```csharp
public class GameFlow : MonoBehaviour
{
    void StartGame();   // MainMenu → Playing + carrega a cena Game
    void Pause();       // Playing → Paused + timeScale 0
    void Resume();      // Paused → Playing + timeScale 1
    void Restart();     // GameOver → Playing + RunManager.StartRun()
    void GoToMenu();    // Paused/GameOver → MainMenu + carrega a cena Menu
}
```

- `GameFlow` vive num GameObject da cena `Game`. **O agente cria esse GameObject + componente por script** (não é entregável do Editor): um GameObject vazio `GameFlow` na cena `Game`, com o componente `GameFlow`. As referências de que ele precisa são resolvidas em runtime: `RunManager` por `FindObjectOfType<RunManager>()` (cache) e `ISceneLoader`/`IGameManager` pelo `ServiceLocator`. Assim a árvore de UI do jogador não fica suja com um objeto que ela não precisa montar, e `GameFlow` não depende de campos arrastados.
- A cena `Menu` não tem `GameFlow`: o `UIManager` só chama `StartGame()` se houver um `GameFlow` resolvido, senão vai direto ao `SceneLoader`.
- `Restart()` **não** troca de cena: reaproveita o `Game` carregado e chama `RunManager.StartRun()` (o `ScoreManager.Reset()` já zera spawn/portal/jogador).

---

## 6. HUD

| Elemento | Fonte | Evento |
|---|---|---|
| Score | `ScoreManager` | `OnScoreChanged(int)` |
| Distância | `RunManager` | `OnDistanceChanged(float)` |
| Cor ativa | `ScoreManager` | `OnColorChanged(ColorId)` → `SO_ColorPalette.GetColor()` |
| Escudo | `ScoreManager` | `OnShieldChanged(bool)` → liga/desliga `m_ShieldIcon` |
| Boost | `ScoreManager` | `OnBoostChanged(List<ColorId>)` + `BoostTimeLeft/BoostDuration` → `m_BoostFill` |
| Pausa | botão | `GameFlow.Pause()` |

A distância é formatada em metros inteiros (`{0} m`). O score é inteiro.

---

## 7. Opções, Sair, Créditos

**Opções**
- Ao abrir: `ApplySettings(SaveManager.GetSettings())` — sliders em 0–100, toggle de mudo.
- Ao alterar: `CollectSettings()` → `SaveManager.SetSettings()` + `SaveManager.Save()` → `AudioManager.SetMasterVolume/Music/Sfx/Muted`.
- Conversão: slider 0–100 → 0–1 linear; no mixer, `Mathf.Log10(max(v, 0.0001f)) * 20f` (dB) para volume perceptível.
- Painel `Controls`: texto estático listando `A/D` ou `←/→` para dash e `Espaço` (ou toque) para pular. Botão Voltar.

**Sair**
- `Application.Quit()`.
- Editor: `#if UNITY_EDITOR` → `UnityEditor.EditorApplication.isPlaying = false`.
- WebGL: botão `interactable = false` + tooltip "indisponível no WebGL".

**Créditos**
- Texto placeholder com desenvolvedor/assets/música.
- Botão skip volta ao menu.

---

## 8. Áudio real (antecipado)

`AudioManager` passa a usar `AudioMixer` (Asset `PowerPulseAudio.mixer`):

- Grupos: `Master` → `Music`, `Sfx` (expostos como `MasterVolume`, `MusicVolume`, `SfxVolume`).
- `PlayMusic(clip)`: `AudioSource` em loop no grupo `Music`.
- `PlaySFX(clip)`: pega um `AudioSource` do pool (prefab com N fontes) e toca no grupo `Sfx`.
- `SetMasterVolume/MusicVolume/SfxVolume`: `mixer.SetFloat(nome, dB)`.
- `SetMuted(bool)`: `mixer.SetFloat("MasterVolume", muted ? -80f : últimoValor)`.
- O `Bootstraper` aplica os volumes do `SaveManager` ao subir.

`IAudioService` permanece com a mesma assinatura (nenhum consumidor quebra).

---

## 9. Save real (antecipado)

`SaveManager` implementa de verdade:

- `PlayerPrefs` com chaves `PowerPulse.Score` e `PowerPulse.Settings`, valores JSON (`ScoreData`, `SettingsData`).
- `Load()`: lê, desserializa; se vazio/corrompido, usa defaults e registra um `Debug.LogWarning`.
- `Save()`: serializa e grava.
- `SetBestScore(int)`: só sobe; grava.
- `SetSettings(SettingsData)`: substitui e grava.

`ScoreManager` já chama `ISaveService` com fallback para `PlayerPrefs`; o fallback permanece intacto.

---

## 10. Ajustes em código existente

| Arquivo | Ajuste | Por quê |
|---|---|---|
| `Bootstraper` | remover `LoadScene("Game")` temporário | hoje o menu nunca aparece |
| `Bootstraper` | instanciar `UIManager.prefab` + `DontDestroyOnLoad` + `Register<IUIManager>` | UI persistente |
| `Bootstraper` | `SaveManager.Load()` e aplicar volumes | Opções persistidas ao subir |
| `RunManager` | remover `m_AutoStart` e o bloco `if (m_AutoStart) StartRun()` | a run deve começar pelo `GameFlow`, não sozinha |
| `PlayerController` | gate `CurrentState == Playing` em `ReadInput()` | clique em botão não pode mover a esfera |
| `Game.unity` | remover Canvas, `Text (TMP)` e `Flash` legado; referenciar `Flash` no prefab | um Canvas só |
| `PowerPulseInputActions` | action map `UI`: `Pause` (Esc), `Confirm` (Enter/E), `Cancel` (Esc) | input de UI |

---

## 11. Riscos conhecidos

1. **`AudioMixer` é um Asset** — não pode ser criado em runtime. Precisa ser criado no Editor (entregável do jogador). O `AudioManager` deve tolerar mixer nulo (loga aviso e segue sem áudio).
2. **Prefab de UI é um entregável do Editor** — o agente não monta a árvore. Se o prefab não existir, o `Bootstraper` loga erro e o jogo sobe sem UI (não quebra).
3. **`CameraFX` cria Canvas em runtime** se não houver `Flash` atribuído — o prefab deve carregar o `Flash`, senão haverá dois overlays.
4. **`ScoreManager`/`SpawnManager` com fallback `PlayerPrefs`** — o `SaveManager` real passa a ser a fonte; o fallback só é usado se o serviço não estiver registrado.
5. **`RunManager` tem comentário antigo** sobre mover a distância para o `ScoreManager` (Fase 6) — **fora de escopo**, não mexer.
6. **`Menu.unity` não tem Canvas** — a UI vem do prefab; se o prefab falhar, o menu fica vazio (visível, mas sem botões).

---

## 12. Validação (manual, no Editor)

- [ ] Menu abre ao subir o jogo (sem a linha temporária).
- [ ] Jogar → carrega `Game` → HUD visível com score/distância.
- [ ] Esc pausa (painel de pausa) e Esc de novo continua.
- [ ] Botão de pausa no HUD pausa.
- [ ] Morrer → game over com score, recorde e indicação de novo recorde.
- [ ] Jogar novamente reinicia sem trocar de cena.
- [ ] Menu (no game over e na pausa) volta à cena `Menu`.
- [ ] Sliders mudam o volume na hora; Mudo silencia.
- [ ] Fechar e reabrir o jogo mantém volumes.
- [ ] Painel de Controles lista os controles e volta.
- [ ] Créditos abrem e o skip volta ao menu.
- [ ] No WebGL, o botão Sair fica desabilitado com tooltip.
- [ ] Clicar em botões de UI **não** move a esfera.
- [ ] Sem erros no Console em nenhum fluxo.

---

## 13. Pendências do Editor (entregáveis do jogador)

O agente **não** monta estes itens; eles dependem do Editor:

1. **Criar `Assets/Core/ScriptableObjects/Audio/PowerPulseAudio.mixer`** com grupos `Master`, `Music` e `Sfx`, expondo `MasterVolume`, `MusicVolume` e `SfxVolume` (o grupo `Master` é a raiz; `Music`/`Sfx` são filhos).
2. **Criar o prefab `Assets/Core/Prefabs/UIManager.prefab`** com:
   - `Canvas` (`ScreenSpaceOverlay`, `CanvasScaler` 1080×1920, `matchWidthOrHeight` 0.5) e `sortingOrder` acima do overlay de flash;
   - `EventSystem` com `InputSystemUIInputModule`;
   - o componente `UIManager` com a árvore de painéis descrita na seção 5;
   - o `Flash` (imagem full-screen) referenciado no campo `m_Flash` do `CameraFX` — **ou** o `CameraFX` continua criando o seu próprio overlay em runtime, o que é aceitável, mas gera dois Canvases.
3. **Montar os painéis** (textos, botões, sliders, toggle, `Image` de cor ativa, ícones de escudo/boost) e arrastar cada um para o campo correspondente do `UIManager`.
4. **Adicionar o action map `UI`** no `PowerPulseInputActions` (`Pause`, `Confirm`, `Cancel`) — isto é um Asset editável por script (JSON), então o agente faz.

Se o prefab não existir no momento do `Bootstraper`, o jogo sobe sem UI e loga um erro claro — não quebra.

---
