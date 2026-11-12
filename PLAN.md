# Plano de Implementação — PowerPulse

## Escopo definido

O objetivo é criar uma versão quase completa do PowerPulse, priorizando WebGL para desktop e mobile.

Decisões confirmadas:

- Seguir rigorosamente a arquitetura descrita no GDD.
- Usar placeholders de arte e áudio inicialmente.
- Manter apenas recorde local nesta versão.
- Usar o jogador praticamente parado no eixo Z, enquanto a pista e os objetos se movem em direção à câmera.
- Identificar os elementos exclusivamente por cor, sem sistema adicional de acessibilidade nesta fase.
- Fazer validação por testes manuais dentro da Unity.
- Manter o `GDD.pdf` como referência e registrar as divergências em `GDD_NOTES.md` (o PDF é uma exportação do Notion e não deve ser regenerado).

## Estado atual

| Fase | Status |
| --- | --- |
| 0 — Preparação | concluída ✅ |
| 1 — Estrutura arquitetural | concluída ✅ |
| 2 — Dados configuráveis | concluída ✅ |
| 3 — Fundação visual e física | concluída ✅ |
| 4 — Jogador e controles | concluída ✅ |
| 5 — Run básica e deslocamento da pista | concluída ✅ |
| 6 — Obstáculos, coleta e pontuação | concluída ✅ |
| 7 — Progressão e dificuldade | próxima |

**Decisões tomadas durante a Fase 6:**

- `ScoreManager` é a autoridade única da run (score, recorde, cor ativa, cores desbloqueadas, escudo e boost). O `ColorManager` previsto no GDD foi incorporado a ele, com um nome mais semântico.
- `PlayerCollision` pergunta a regra ao `PlayerColor` (mesmo GameObject), que delega ao `ScoreManager`. Nenhuma referência direta ao manager na colisão.
- `PlayerDeath` existe para a morte ser legível antes da tela de Game Over (Fase 12).

## Visão do jogo

PowerPulse é um endless runner vertical neon. O jogador controla uma esfera de luz que percorre uma pista infinita dividida em cinco lanes.

- Obstáculos da mesma cor da esfera são coletados e geram pontos.
- Obstáculos de cor incompatível causam game over, exceto quando um shield ou boost estiver ativo.
- A esfera pode trocar de cor ao atravessar portais.
- Power-ups permitem coletar múltiplas cores ou absorver uma colisão.
- A dificuldade aumenta conforme a distância percorrida.
- O jogo não possui narrativa; o foco é leitura imediata, reflexo, fluidez e sessões curtas.

## Fase 0 — Preparação e critérios do projeto

**Status:** concluída ✅

1. Confirmar a versão da Unity e configurar o projeto para Unity 6000.6.4f1.
2. Configurar o repositório:
   - Git LFS para arquivos binários.
   - `.gitignore` adequado para Unity.
   - Estrutura inicial de pastas feature-based.
3. Configurar o projeto para WebGL:
   - Orientação retrato.
   - Resolução-base de 1080x1920.
   - Canvas responsivo.
   - Qualidade e FPS alvo de 60.
4. Instalar e validar:
   - Input System.
   - URP.
   - TextMeshPro.
   - Ferramentas de testes manuais dentro da Unity.
5. Definir as cenas:
   - `Bootstrap` ou carregamento inicial.
   - `MainMenu`.
   - `Game`.

**Resultado:** projeto Unity configurado e pronto para receber sistemas, sem gameplay ainda.

## Fase 1 — Estrutura arquitetural

**Status:** concluída ✅

1. Criar os namespaces e pastas:
   - `Core`
   - `Player`
   - `Obstacles`
   - `PowerUps`
   - `Camera`
   - `UI`
   - `Audio`
   - `Procedural`
   - `Settings`
2. Implementar a base arquitetural:
   - `IGameService`.
   - `ServiceLocator`.
   - `Bootstraper`.
   - Registro de serviços persistentes.
3. Criar os serviços persistentes:
   - `GameManager`.
   - `SaveManager`.
   - `SceneLoader`.
   - `AudioManager`.
4. Definir os estados globais:
   - `MainMenu`.
   - `Tutorial`.
   - `Playing`.
   - `Paused`.
   - `GameOver`.
5. Criar os eventos necessários entre sistemas.
6. Aplicar desde o início:
   - Código em inglês.
   - XML documentation em classes públicas e métodos complexos.
   - Convenções de nomes do GDD.
   - `Tooltip` em todos os campos serializados.

**Validação:** iniciar o projeto, carregar cenas e confirmar transições sem erros no Console.

## Fase 2 — Dados configuráveis

**Status:** concluída ✅

Criar os ScriptableObjects que permitirão ajustar o jogo pelo Inspector:

1. `SO_ColorPalette`
   - Cinco cores oficiais.
   - Valores de emissão.
   - Ordem de desbloqueio.
2. `SO_ObstacleData`
   - Tamanho.
   - Área e pontuação.
   - Cor.
   - Tipo de comportamento.
   - Indicação se pode ser pulado.
3. `SO_PowerUpData`
   - Tipo.
   - Duração.
   - Cores abrangidas.
   - Efeitos visuais e sonoros.
4. `Config_GameBalance`
   - Velocidade inicial e máxima.
   - Curva de dificuldade.
   - Frequência de portais.
   - Frequência de power-ups.
   - Distância mínima entre spawns.
   - Regras de densidade.
5. `Config_InputSettings`
   - Sensibilidade do swipe.
   - Limites do toque.
   - Configurações de dash e pulo.

**Resultado:** balanceamento separado da lógica, sem valores espalhados pelos scripts.

## Fase 3 — Fundação visual e física

**Status:** concluída ✅

1. Pista:
   - Um plano largo (5 unidades × 10 de comprimento).
   - Seis faixas neon (cubos finos de 0,05 × 10) nas divisas, com material emissivo.
   - Tile = 1 plano + 6 faixas (a reciclagem entra na Fase 5).
2. Escala:
   - Largura da pista: 5 unidades.
   - Raio da esfera do jogador: 0,4 unidades.
   - Altura do pulo: ~2 unidades.
   - Tile de pista: 10 unidades de comprimento.
3. Jogador:
   - `Sphere` + material emissivo (`Player_Light`).
   - `Rigidbody` + `SphereCollider` + `Trail Renderer`.
4. Câmera:
   - Fixa atrás e acima do jogador.
   - FOV 60, near 0.3, far 100.
5. URP:
   - Global Volume com Bloom, Chromatic Aberration e Tonemapping.
6. Layers e matriz de colisão:
   - `Player`, `Obstacle`, `PowerUp`, `Portal`, `Track`.
7. Materiais:
   - `LaneDark` (plano), `NeonWhite` (faixas) e `Player_Light` (jogador).
8. Pastas:
   - `Environment/Lanes`, `Environment/Obstacles`, `Environment/PowerUps` e `Player`.

**Validação:** cena visualmente legível em 1080x1920 e em diferentes proporções de tela.

## Fase 4 — Jogador e controles

**Status:** concluída ✅

1. Input:
   - Criar um novo Input Actions asset (`PowerPulseInputActions`).
   - Ações `DashLeft`, `DashRight` e `Jump`.
   - Teclado: A/D, ←/→ e Espaço.
   - Control schemes: `Keyboard&Mouse` e `Touch`.
2. `PlayerController` (lane + pulo, como o GDD):
   - Dono único da posição; tem o `Rigidbody` e o `TrailRenderer`.
   - Lê o input direto pelo Input System.
   - Lane: dash de exatamente 1 lane, resposta imediata, limites em x = −2 a +2.
   - Pulo: vertical, detecção de chão (raycast), estado no ar, sem pulo duplo.
   - `Rigidbody` não-kinemático com detecção contínua.
3. `PlayerColor` (base):
   - Cor ativa, aplicada no material da esfera via `SO_ColorPalette`.
   - Evento `OnColorChanged`.
4. `PlayerCollision` (base):
   - Detecção de trigger.
   - Expõe os contatos (coleta/morte) para as Fases 6–9.
5. Feedback:
   - Trail no dash, pulso da esfera, animação simples de pulo.
6. Pastas:
   - `Assets/Player/Scripts/`.

**Validação:** jogar manualmente usando teclado e touch simulado, verificando que cada input produz apenas uma ação.

## Fase 5 — Run básica e deslocamento da pista

**Status:** concluída ✅

Usar o modelo decidido: o jogador permanece praticamente fixo no eixo Z e a pista e os objetos se deslocam em direção à câmera.

1. Criar o `RunManager`. ✅
2. Controlar:
   - Estado da run. ✅
   - Velocidade atual. ✅
   - Distância percorrida. ✅
   - Dificuldade atual. ✅
3. Criar tiles de pista recicláveis. ✅
4. Criar um `ObjectPool` genérico. ✅
5. Implementar:
   - Ativação de tiles à frente do jogador. ✅
   - Reciclagem de tiles atrás da câmera. ✅
   - Movimento consistente de todos os elementos. ✅
6. Criar o `SpawnManager`. ✅
7. Definir o primeiro padrão de spawn:
   - Obstáculos estáticos. ✅
   - Uma lane por obstáculo. ✅
   - Espaçamento seguro. ✅
   - Cores iniciais limitadas a duas. ✅

**Validação:** a pista deve parecer infinita, sem buracos, travamentos ou crescimento indefinido de objetos na Hierarchy. ✅

## Fase 6 — Obstáculos, coleta e pontuação

**Status:** concluída ✅

1. Criar os prefabs de obstáculos:
   - Cubo 1x1. ✅
   - Retângulo 2x1. ✅
   - Cubo 2x2. ✅
2. Aplicar cor e emissão conforme `ColorId`. ✅
3. Implementar comportamentos:
   - Estático. ✅
   - Movimento lateral. ✅ (código pronto; ativado na Fase 7)
   - Movimento mais rápido. ✅ (código pronto; ativado na Fase 7)
   - Obstáculo alto ou largo. ✅ (código pronto; ativado na Fase 7)
4. Implementar as regras de colisão:
   - Cor compatível: coleta. ✅
   - Cor incompatível: morte. ✅
   - Shield: absorve uma colisão incompatível. ✅
   - Boost: permite cores adicionais. ✅
5. Criar o `ScoreManager`. ✅
6. Calcular pontuação pela área do obstáculo:
   - 1x1 = 1 ponto. ✅
   - 2x1 = 2 pontos. ✅
   - 2x2 = 4 pontos. ✅
7. Emitir eventos:
   - `OnCollected`. ✅
   - `OnPlayerDied`. ✅
   - `OnScoreChanged`. ✅
8. Criar feedback de coleta:
   - Explosão de partículas. ⏳ (Fase 10)
   - Flash. ⏳ (Fase 10)
   - Shake. ⏳ (Fase 10)
   - Atualização de score. ✅

**Notas de implementação (Fase 6):**

- O `ScoreManager` é a autoridade única da run: score, recorde, cor ativa, cores desbloqueadas, escudo e boost. Ele substitui o `ColorManager` previsto no GDD com um nome mais semântico, sem criar dois managers.
- A regra de coleta segue o GDD: `CanCollect` é verdadeiro apenas para a **cor ativa** ou para uma **cor coberta por boost**. Cor desbloqueada mas inativa continua sendo mortal.
- O `PlayerCollision` não referencia o `ScoreManager`: ele pergunta ao `PlayerColor` no mesmo GameObject, que delega a regra. Isso reduz o acoplamento e mantém a colisão falando apenas com o estado do jogador.
- O `PlayerDeath` toca a animação de morte (piscar + esconder) antes de fechar a run. Sem isso, o game over parecia um travamento, porque a pista parava mas o jogador continuava visível e responsivo.
- A tela de Game Over (botão de recomeçar) é da Fase 12. Até lá, o teste exige parar o Play e iniciar de novo.

**Validação:** testar todas as combinações de cores, tamanhos, shield e boost. ✅ (cores e tamanhos testados; shield/boost aguardam os power-ups da Fase 9)

## Fase 7 — Progressão e dificuldade

1. Criar uma curva de dificuldade configurável.
2. Definir os marcos:
   - 0–500 m: duas cores e obstáculos estáticos.
   - 500 m: primeiro portal.
   - Aproximadamente 1.000 m: obstáculos móveis.
   - Distâncias posteriores: maior velocidade, densidade e tamanho.
3. Fazer o `RunManager` reagir a marcos, sem executar decisões complexas por frame.
4. Aumentar gradualmente:
   - Velocidade da pista.
   - Número de cores ativas.
   - Frequência de obstáculos.
   - Complexidade dos padrões.
   - Movimento dos obstáculos.
5. Garantir que a dificuldade não gere padrões impossíveis.
6. Criar regras de segurança:
   - Sempre existir uma solução jogável.
   - Não bloquear todas as lanes sem alternativa.
   - Não colocar power-up em posição impossível.
   - Não exigir reação além do tempo definido.

**Validação:** executar runs manuais em diferentes distâncias, registrando onde o jogo se torna injusto ou trivial.

## Fase 8 — Portais de cor

1. Criar o prefab do portal translúcido.
2. Implementar a frequência por distância:
   - Um portal a cada 500 m.
3. Definir a cor futura do portal.
4. Ao atravessar:
   - Trocar a cor ativa.
   - Liberar progressivamente uma nova cor.
   - Emitir feedback visual e sonoro.
5. Garantir que:
   - O portal seja atravessável.
   - A cor só mude se o jogador realmente passar por ele.
   - O portal não apareça sobre outro objeto.
   - A troca não ocorra por aproximação lateral.
6. Criar variações visuais de portal conforme a progressão.

**Validação:** testar portais em todas as combinações de cores e em diferentes velocidades.

## Fase 9 — Power-ups

Implementar os itens com pool próprio:

1. `Shield`
   - Absorve uma colisão incompatível.
   - Remove o shield após uso.
   - Mostra anel ao redor da esfera.
2. `BoostTwoColors`
   - Permite duas cores durante aproximadamente seis segundos.
   - Faz a esfera alternar visualmente entre as cores.
3. `BoostThreeColors`
   - Permite três cores.
4. `BoostAllColors`
   - Permite qualquer cor.
5. Regras de spawn:
   - Nunca sobrepor obstáculo.
   - Nunca ficar entre lanes.
   - Não gerar sequência excessiva de power-ups.
6. Criar efeitos:
   - Partículas.
   - Mudança de Bloom.
   - Flash.
   - Punch de FOV.
   - SFX específico.
7. Definir prioridade caso efeitos sejam combinados:
   - Shield independente.
   - Boosts substituem ou atualizam o boost atual.
   - Novo boost não deve deixar estados contraditórios.

**Validação:** testar duração, expiração, combinação e coleta durante o pulo ou dash.

## Fase 10 — Câmera, efeitos e juice

1. Criar `CameraFX`.
2. Implementar:
   - Shake na coleta.
   - Shake na morte.
   - Punch de FOV no boost.
   - Flash de tela.
   - Ajustes temporários de Bloom.
   - Chromatic Aberration em eventos fortes.
3. Criar VFX reutilizáveis:
   - Coleta.
   - Morte.
   - Dash.
   - Portal.
   - Power-up.
4. Poolar partículas importantes para evitar `Instantiate` e `Destroy` frequentes.
5. Ajustar a intensidade para preservar a legibilidade das cores.

**Validação:** conferir se os efeitos dão impacto sem esconder obstáculos ou causar desconforto visual.

## Fase 11 — Tutorial

1. Criar o sistema de primeira execução.
2. Salvar a flag no `SaveManager`.
3. Iniciar a primeira run com `Time.timeScale` configurado no objeto de cena.
4. Exibir overlay com:
   - Dash para esquerda.
   - Dash para direita.
   - Swipe para cima.
5. Criar uma sequência inicial controlada de obstáculos.
6. Encerrar o tutorial após os primeiros inputs relevantes.
7. Restaurar o tempo normal.
8. Não exibir o tutorial novamente após concluído.

**Validação:** limpar o save, executar o tutorial e confirmar que ele não reaparece em uma nova run.

## Fase 12 — HUD, menus e fluxo de jogo

1. Criar o HUD:
   - Score.
   - Distância.
   - Botão de pausa.
   - Indicador visual de cor ativa.
   - Indicador de shield e boost.
2. Criar `MainMenu`:
   - Jogar.
   - Opções.
   - Créditos.
   - Sair.
3. Criar o menu de pausa:
   - Continuar.
   - Opções.
   - Voltar ao menu.
4. Criar a tela de Game Over:
   - Animação de morte antes da tela.
   - Score atual.
   - Recorde local.
   - Jogar novamente.
   - Menu principal.
5. Criar o menu de opções:
   - Volume master.
   - Volume de música.
   - Volume de SFX.
   - Som ligado/desligado.
   - Controles.
6. Criar a tela de créditos:
   - Desenvolvedores.
   - Assets.
   - Música.
   - Botão de skip.
7. Implementar transições via `SceneLoader`.

**Validação:** executar o fluxo completo: menu → tutorial → run → pausa → game over → reinício → menu.

## Fase 13 — Áudio

1. Criar o `AudioManager` persistente.
2. Configurar:
   - Música em loop.
   - Pool de `AudioSource` para SFX.
   - Volumes independentes.
   - Mute global.
3. Adicionar placeholders:
   - Dash.
   - Pulo.
   - Coleta.
   - Power-up.
   - Portal.
   - Morte.
   - Clique de UI.
4. Integrar eventos de gameplay ao áudio.
5. Salvar configurações no `SaveManager`.
6. Substituir placeholders por áudio final posteriormente.

**Validação:** testar sobreposição de efeitos sem cortes, distorção ou criação excessiva de `AudioSource`.

## Fase 14 — Salvamento local

1. Criar `ScoreData`.
2. Criar `SettingsData`.
3. Salvar em `PlayerPrefs` como JSON.
4. Persistir:
   - Recorde.
   - Tutorial concluído.
   - Volumes.
   - Estado de som.
5. Gravar ao final de cada run.
6. Carregar os dados durante a inicialização.
7. Criar comportamento seguro para save inexistente ou corrompido.
8. Adicionar opção de reset apenas se for necessária no menu de desenvolvimento.

**Validação:** fechar e reabrir o jogo, confirmando a persistência do recorde e das configurações.

## Fase 15 — Integração visual final

Depois que todo o jogo estiver funcional:

1. Refinar materiais emissivos.
2. Ajustar cores para contraste máximo.
3. Criar grid e fundo neon definitivos.
4. Refinar partículas e efeitos.
5. Substituir placeholders de áudio.
6. Ajustar tipografia e layout da UI.
7. Revisar o feedback visual de todos os estados:
   - Cor ativa.
   - Boost.
   - Shield.
   - Portal.
   - Coleta.
   - Morte.
8. Garantir que o visual não prejudique a leitura dos obstáculos.

## Fase 16 — Balanceamento manual

Criar uma matriz de testes com runs em diferentes faixas:

| Distância     | Objetivo                            |
| ------------- | ----------------------------------- |
| 0–500 m       | Aprendizado e adaptação             |
| 500–1.000 m   | Primeiro aumento de complexidade    |
| 1.000–2.000 m | Introdução consistente de movimento |
| 2.000–5.000 m | Domínio dos sistemas                |
| 5.000 m+      | Desafio de alta habilidade          |

Avaliar manualmente:

- Tempo de reação.
- Espaçamento entre obstáculos.
- Frequência de portais.
- Frequência de power-ups.
- Crescimento da velocidade.
- Justiça dos padrões.
- Clareza das cores.
- Facilidade de reinício.
- Desempenho em WebGL mobile.

## Fase 17 — Otimização para WebGL

1. Validar 60 FPS em desktop.
2. Testar em navegadores diferentes.
3. Testar viewport vertical e redimensionamento.
4. Reduzir:
   - Overdraw.
   - Partículas simultâneas.
   - Materiais caros.
   - Alocações por frame.
5. Confirmar uso correto de pooling.
6. Monitorar:
   - GC Alloc.
   - Tempo de CPU.
   - Tempo de renderização.
   - Quantidade de objetos ativos.
7. Verificar carregamento inicial e tamanho da build.
8. Testar interação por toque em dispositivos móveis ou emuladores compatíveis.

## Fase 18 — Empacotamento e entrega

1. Configurar as cenas na ordem correta de build.
2. Configurar nome, ícone e metadados.
3. Criar build WebGL de produção.
4. Testar a build fora do Editor.
5. Validar:
   - Menu inicial.
   - Input.
   - Áudio.
   - Save.
   - Tutorial.
   - Game Over.
   - Reinício.
6. Registrar problemas conhecidos.
7. Criar uma versão jogável candidata.
8. Só depois iniciar a preparação para Android.

## Ordem resumida das dependências

```text
Configuração do projeto
        ↓
Arquitetura e serviços
        ↓
Dados configuráveis e cena base
        ↓
Jogador e controles
        ↓
Pista reciclável e Object Pool
        ↓
Obstáculos e score
        ↓
Progressão e dificuldade
        ↓
Portais e power-ups
        ↓
Câmera, VFX e áudio
        ↓
Tutorial e UI
        ↓
Save local
        ↓
Balanceamento
        ↓
Otimização WebGL
        ↓
Build final
```

## Critérios de conclusão da versão quase completa

A versão será considerada pronta para uma primeira entrega quando:

- O jogo iniciar pelo menu principal.
- O jogador conseguir iniciar uma run por teclado e touch.
- A pista for infinita e reciclada por pooling.
- O dash e o pulo funcionarem nas cinco lanes.
- Obstáculos, score, cores, portais e power-ups estiverem integrados.
- A progressão de dificuldade ocorrer por distância.
- Tutorial, pausa, game over e reinício funcionarem.
- Recorde e configurações forem persistidos localmente.
- HUD, áudio, efeitos e menus estiverem integrados.
- A build WebGL funcionar fora do Editor.
- O desempenho for aceitável para a meta de 60 FPS.
