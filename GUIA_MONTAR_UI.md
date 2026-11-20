# GUIA — Montar a UI do PowerPulse no Editor

> **Para quem está fazendo:** você. **Para quem escreveu:** o agente.
> São **3 blocos**. Faça na ordem. **Não commite nada** — me avisa no final.

**Legenda das janelas do Unity:**
- **Project** = janela de baixo (onde ficam as pastas e arquivos)
- **Hierarchy** = janela da esquerda (onde ficam os GameObjects da cena)
- **Inspector** = janela da direita (onde ficam os componentes e os campos)

---

## 📦 BLOCO 1 — Criar o arquivo de áudio (3 min)

> **Por que:** os sliders de volume precisam de um "mixer" para funcionar.
> Sem isso o jogo roda, mas o volume não muda.
>
> ⚠️ **Atenção — Unity 6.6:** o seletor no canto superior direito **já é** o "Exposed Parameters". Ele fica vazio até você expor. **Não existe botão "Expose" na janela do mixer** — a exposição é feita pelo **Inspector do grupo**. Siga os passos abaixo.

### Parte A — criar o mixer e os grupos

1. Na janela **Project**, clique na pasta **`Assets/Core/ScriptableObjects`**.
   - Se ela não existir: botão direito em `Assets` → **Create → Folder** → nome `Core`, e repita para `ScriptableObjects`.
2. Com a pasta selecionada, **botão direito na área de arquivos** → **Create → Audio Mixer**.
3. O arquivo novo fica selecionado. No campo de nome (topo da janela Project), digite **`PowerPulseAudio`** e dê **Enter**.
4. **Duplo clique** nesse arquivo. Abre a janela **Mixer** (a aba ao lado de Project/Console/Profiler).
5. No painel esquerdo dessa janela, na seção **Groups**, você vê **Master**.
   - **Botão direito em Master → Create Group** → digite **`Music`** → Enter.
   - **Botão direito em Master → Create Group** → digite **`Sfx`** → Enter.
   - Arraste **Music** e **Sfx** para cima de **Master** até ficarem indentados (sub-itens).

### Parte B — expor o volume de cada grupo (o passo que muda na 6.6)

> **O conceito:** cada grupo tem um **Inspector** próprio (como se fosse um componente).
> É nesse Inspector que você clica com o botão direito no **Volume** e escolhe "Expose to script".

6. Na janela **Mixer**, na seção **Groups**, clique em **`Master`** para selecioná-lo.
   - Uma janela de **Inspector** abre mostrando o AudioGroup do Master (pode ser uma janela nova, ou a aba ao lado do Project).
   - **Se não abrir:** clique em **Window → General → Inspector** (ou `Ctrl+3`) para abrir o Inspector, e mantenha o `Master` selecionado no Mixer.
7. No Inspector do `Master`, procure o campo **`Volume`**.
   - **Botão direito em cima do nome "Volume"** → clique em **`Expose 'Volume (of Master)' to script`**.
8. Volte na janela **Mixer**. Agora o seletor do canto superior direito deve ter mudado de `Exposed Parameters (0)` para **`Exposed Parameters (1)`**.
   - Clique no nome do parâmetro na lista e **renomeie para `MasterVolume`** (botão direito → **Rename**).
9. Repita para os outros dois:
   - Clique em **`Music`** na seção Groups → Inspector abre o AudioGroup do Music → **botão direito no `Volume`** → **Expose to script** → renomeie para **`MusicVolume`**.
   - Clique em **`Sfx`** na seção Groups → **botão direito no `Volume`** → **Expose to script** → renomeie para **`SfxVolume`**.

10. Ao final, o seletor deve mostrar **`Exposed Parameters (3)`** com os três nomes: `MasterVolume`, `MusicVolume`, `SfxVolume`.
11. Clique em qualquer lugar e **Ctrl+S**.

✅ **Bloco 1 pronto.** O seletor no topo direito é a confirmação: **(3)** = os três expostos.

---

## 📦 BLOCO 2 — Criar o prefab da interface (10 min)

> **Por que:** o jogo procura `Assets/Core/Prefabs/Resources/UI/UIManager.prefab`.
> Sem ele a interface não aparece.

### Parte A — esqueleto

1. Na **Hierarchy**, **botão direito → Create Empty**.
2. Com o `GameObject` selecionado, aperte **F2** e renomeie para **`UIManager`**.
3. Com o `UIManager` selecionado, no **Inspector** clique em **Add Component**.
   - Digite **`UIManager`** e clique no resultado (tem ícone de script C#).
   - O Inspector passa a mostrar vários campos agrupados por seção.
4. Na janela **Project**, crie as pastas, uma dentro da outra:
   `Assets` → `Core` → `Prefabs` → `Resources` → `UI`
   - Botão direito em `Assets` → Create → Folder → `Core`
   - Entre em `Core` → Create → Folder → `Prefabs`
   - Entre em `Prefabs` → Create → Folder → `Resources`
   - Entre em `Resources` → Create → Folder → `UI`
5. Selecione o GameObject **`UIManager`** na Hierarchy e **arraste ele para dentro da pasta `UI`**.
   - Se perguntar "Create Prefab?", escolha **Original Prefab** (ou Replace, tanto faz).
   - O ícone dele fica **azul** = virou prefab.

### Parte B — Canvas

6. Na Hierarchy, **botão direito no `UIManager` → UI → Canvas**.
7. Selecione o **`Canvas`** e, no Inspector:
   - Componente **Canvas** → **Render Mode**: `Screen Space - Overlay`.
   - **Sorting Order**: `10`.
   - Clique em **Add Component** → digite **Canvas Scaler**.
     - **UI Scale Mode**: `Scale With Screen Size`.
     - **Reference Resolution**: `1080` x `1920`.
     - **Match**: `0.5`.

### Parte C — EventSystem

8. Na Hierarchy, **botão direito no `UIManager` → UI → EventSystem**.
9. Selecione o **`EventSystem`** e, no Inspector:
   - Se houver um componente **Standalone Input Module**, clique nos **três pontinhos** dele → **Remove Component**.
   - Deve sobrar só o **Input System UI Input Module**.

### Parte D — os 7 painéis

10. Para cada painel da tabela abaixo:
    - **Botão direito no `Canvas` → Create Empty** → renomeie (F2) para o nome da tabela.
    - Com o painel selecionado, **Add Component → Image**.
      - Clique em **Color** e deixe um fundo escuro (alpha `0.9`, cor preta).
    - No **Rect Transform**, segure **Alt** e clique no ancorador **stretch** (canto inferior direito dos quadradinhos) para ele cobrir a tela.

| Nome do GameObject |
|---|
| `HUDPanel` |
| `MainMenuPanel` |
| `PausePanel` |
| `GameOverPanel` |
| `OptionsPanel` |
| `ControlsPanel` |
| `CreditsPanel` |

### Parte E — arrastar os 7 painéis para o script

11. Selecione o **`UIManager`** (raiz do prefab).
12. No Inspector, na seção **Panels**, arraste cada GameObject para o campo correspondente.
    - **Como arrastar:** clique no **círculo** à direita do campo (uma bolinha com um alvo) → na lista, clique no nome do GameObject.
    - **Ou:** arraste o GameObject da Hierarchy direto para o campo.

| Campo no Inspector | Arraste o GameObject |
|---|---|
| `HUD Panel` | `HUDPanel` |
| `Main Menu Panel` | `MainMenuPanel` |
| `Pause Panel` | `PausePanel` |
| `Game Over Panel` | `GameOverPanel` |
| `Options Panel` | `OptionsPanel` |
| `Controls Panel` | `ControlsPanel` |
| `Credits Panel` | `CreditsPanel` |

### Parte F — Config (2 campos)

13. Ainda no Inspector do `UIManager`, seção **Config**:

| Campo | Arraste o arquivo |
|---|---|
| `Input Actions` | `PowerPulseInputActions` (está em `Assets/Player/`) |
| `Palette` | `SO_ColorPalette` (está em `Assets/Core/ScriptableObjects/`) |

### Parte G — o conteúdo dos painéis (o mais demorado)

14. Para cada painel, **crie os itens dentro dele** e **arraste para o campo do script**.

**Como criar cada tipo de item** (botão direito no painel → UI → ...):
- `Button` → **UI → Button**
- `Text - TextMeshPro` → **UI → Text - TextMeshPro** (se pedir "Import TMP Essentials", clique em **Import**)
- `Image` → **UI → Image**
- `Slider` → **UI → Slider**
- `Toggle` → **UI → Toggle**

**Como arrastar:** selecione o item criado e **arraste para o campo correspondente** no Inspector do `UIManager` (mesmo esquema do círculo).

Abaixo, **campo por campo**, na ordem em que aparecem no Inspector.

#### Seção HUD
| Campo | Crie |
|---|---|
| `Score Text` | Text - TextMeshPro |
| `Distance Text` | Text - TextMeshPro |
| `Active Color Image` | Image |
| `Shield Icon` | Image |
| `Boost Icon` | Image |
| `Boost Fill` | Image |
| `Pause Button` | Button |

#### Seção Main Menu
| Campo | Crie |
|---|---|
| `Play Button` | Button |
| `Options Button` | Button |
| `Credits Button` | Button |
| `Quit Button` | Button |

#### Seção Pause
| Campo | Crie |
|---|---|
| `Resume Button` | Button |
| `Pause Options Button` | Button |
| `Pause Menu Button` | Button |

#### Seção Game Over
| Campo | Crie |
|---|---|
| `Final Score Text` | Text - TextMeshPro |
| `Best Score Text` | Text - TextMeshPro |
| `New Record Text` | Text - TextMeshPro |
| `Retry Button` | Button |
| `Game Over Menu Button` | Button |

#### Seção Options
| Campo | Crie |
|---|---|
| `Master Slider` | Slider |
| `Music Slider` | Slider |
| `Sfx Slider` | Slider |
| `Mute Toggle` | Toggle |
| `Controls Button` | Button |
| `Options Back Button` | Button |

> **Detalhe dos sliders:** cada slider deve ir de **0 a 100**.
> Selecione o Slider → no Inspector, componente **Slider**: `Min Value` = **0**, `Max Value` = **100**.

#### Seção Controls
| Campo | Crie |
|---|---|
| `Controls Back Button` | Button |

> Crie também um `Text - TextMeshPro` com o texto dos controles dentro do painel (não precisa arrastar).

#### Seção Credits
| Campo | Crie |
|---|---|
| `Credits Text` | Text - TextMeshPro |
| `Credits Skip Button` | Button |

### Parte H — salvar

15. Selecione o prefab **`UIManager`** na pasta `UI` e aperte **Ctrl+S**.

✅ **Bloco 2 pronto.**

---

## 📦 BLOCO 3 — Limpar a cena Game (1 min)

> **Por que:** a cena `Game` ainda tem um Canvas velho com um texto placeholder.
> A UI agora vem do prefab.

1. Na **Project**, pasta **`Assets/Scenes`** → **duplo clique em `Game`**.
   - Se pedir para salvar a cena atual, clique em **Save**.
2. Na **Hierarchy**, procure por **`Canvas`** (normalmente no fim da lista).
   - Clique nele → **Delete** (ou botão direito → Delete).
3. Confirme que o **`EventSystem`** também sumiu. Se não, delete ele também.
4. **Ctrl+S** para salvar a cena.

✅ **Bloco 3 pronto.**

---

## ✅ FIM

Me avisa. Aí eu:
1. rodo a validação do fluxo (menu → jogar → pausa → game over → reinício → menu);
2. corrijo o TODO da barra de boost;
3. commito tudo e atualizo `CHANGELOG`/`PLAN`.

---

## 🛟 Se travar

Me diz **qual bloco e qual passo**. Exemplos:
- "Bloco 1, passo 6: não achei o botão Expose"
- "Bloco 2, passo 14: o campo não aparece no Inspector"
- "Bloco 3: não tem Canvas na Hierarchy"

Se puder, manda um **print** da janela que eu vejo.

---

## ⚠️ Coisas que podem confundir

- **O `UIManager` não mostra os campos?** Você adicionou o componente `UIManager` no GameObject raiz (passo A.3). Sem isso, o Inspector não mostra nada.
- **O campo está cinza/não aceita?** Campos `Config` e `Panels` aceitam **arrastar o arquivo/GameObject**. Campos de botão aceitam o **GameObject** (não o componente).
- **O `PowerPulseInputActions` não aparece na lista?** Ele está em `Assets/Player/PowerPulseInputActions.inputactions`.
- **O Unity gerou um arquivo `.cs` novo?** É o wrapper do Input System, gerado automaticamente. Não apague.
