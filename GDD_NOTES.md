# GDD — Notas de Implementação (Adendo vivo)

O `GDD.pdf` é o documento de referência e **não deve ser regenerado** (é uma
exportação do Notion e regenerar perderia o layout original). Este arquivo é o
adendo: registra onde a implementação divergeu do GDD e por quê, além das
decisões tomadas durante o desenvolvimento.

Entradas organizadas por fase, mais recentes primeiro.

---

## Fase 8 — Portais de cor

### Decisão de design: o portal atravessa as 5 lanes

**O GDD diz:** "Portal de cor: [...] passagem obrigatória; surge a cada 500 m;
libera progressivamente novas cores até o máximo de 5."

**O problema:** a pista tem 5 lanes. Um portal de 1 lane pode ser desviado, e
aí o jogador fica preso na cor inicial para sempre — a progressão nunca acontece.

**O que foi feito:** o portal é um **arco/cortina translúcida que atravessa a
pista inteira**. É impossível desviar, então a "passagem obrigatória" do GDD é
real. A cor é sempre a próxima do `UnlockOrder`, o que garante a progressão sem
morte instantânea por cor não desbloqueada.

### Decisão: o desbloqueio acontece ao atravessar

O GDD diz que o portal "libera progressivamente novas cores". A implementação
desbloqueia **no momento da passagem**, não por distância. Assim o progresso é
do jogador, não um relógio automático.

---

## Fase 7 — Progressão e dificuldade

### Marcos data-driven

**O GDD diz:** "A dificuldade cresce de forma gradual com a distância" e o
`RunManager` "reage a marcos (não roda por frame)".

**O que foi feito:** os marcos são uma lista configurável no `Config_GameBalance`
(distância, velocidade, Moving/Faster, máximo de obstáculos por fileira). O
`RunManager` aplica cada marco uma vez, por evento.

### Sem obstáculo "só de pulo"

**O GDD diz:** "Obstáculos de pulo: alguns só podem ser evitados pulando."

**O que foi feito:** todos os obstáculos podem ser evitados por **troca de lane
ou pulo**. O pulo é uma ferramenta a mais, não um requisito. Confirmação do
jogador durante o planejamento.

### Segurança: uma lane sempre livre

**O que foi feito:** `SpawnManager.PickFreeLanes` distribui os obstáculos em
lanes sorteadas e sempre deixa uma lane livre, impossibilitando fileiras sem
solução. O máximo por fileira é limitado por `MaxObstaclesPerRow` no marco
(atualmente 1, então sempre sobram 4 lanes livres).

---

## Fase 6 — Obstáculos, coleta e pontuação

### `ColorManager` → `ScoreManager`

**O GDD diz:** "Color System — Manager: autoridade sobre cores: as 5 cores, a
cor ativa, as cores desbloqueadas (2 → 5) e o estado de boost (~6 s)."
E, separado: "Score System — Manager: dono do score atual, da distância
percorrida e do recorde."

**O que foi feito:** um único manager, o `ScoreManager`, acumula os dois papéis
(cores + score). Motivo: as regras de coleta (`CanCollect`) e de pontuação
vivem no mesmo lugar — quem decide se a colisão é coleta é quem soma o ponto.
Dois managers para a mesma decisão criaria uma fronteira falsa.

**Consequência:** o nome `ColorManager` não existe no projeto. O `ScoreManager`
é a autoridade única da run: score, recorde, cor ativa, cores desbloqueadas,
escudo e boost.

### Distância continua no `RunManager`

**O GDD diz:** o Score System é "dono do score atual, da distância percorrida e
do recorde", e o Run Manager "lê a distância do ScoreManager".

**O que foi feito:** a distância continua no `RunManager`. A troca ficará para
a **Fase 7**, quando os marcos de distância (0–500 m, 500 m, 1.000 m) existirem
de fato. Antecipar a troca agora quebraria a Fase 5 sem travar nada novo.

### `PlayerCollision` não referencia o `ScoreManager`

**O GDD diz:** "o PlayerCollision lê o ColorId e pergunta ao ColorManager se a
cor é coletável agora (`CanCollect(ColorId)`)."

**O que foi feito:** o `PlayerCollision` pergunta ao `PlayerColor` (mesmo
GameObject), que delega ao `ScoreManager`. Motivo: o GDD manda referência
direta por `[SerializeField]` para objetos da mesma cena, mas nada obriga o
ScoreManager a ser essa referência. Fazer a colisão falar com o estado do
jogador reduz o acoplamento e deixa o `PlayerCollision` sem saber que o
`ScoreManager` existe.

### Regra de coleta confirmada

**O GDD diz:** "`CanCollect(ColorId)` verdadeiro quando é a cor ativa ou uma
cor coberta por boost."

**O que foi feito:** implementado exatamente assim. Uma cor **desbloqueada** mas
**inativa** continua sendo mortal. O `SpawnManager` pinta os obstáculos com o
pool de cores em jogo (ativa + desbloqueadas + boost), que é a variedade que o
jogador já foi treinado a ler.

### `PlayerDeath` (não estava no GDD)

O GDD descreve a morte como "animação de morte e a tela de Game Over". A tela
é da Fase 12, mas a animação é da Fase 6. Sem ela, o game over parecia um
travamento: a pista parava, mas o jogador continuava visível e responsivo.

**O que foi feito:** `PlayerDeath` toca um piscar + esconder por ~0,8 s, desliga
o `Rigidbody` e o input, e só então muda o estado global para `GameOver`. A
Fase 10 substituirá esse placeholder pelo VFX completo (partículas, shake,
flash).

### Escudo e boost: estado pronto, itens não

O `ScoreManager` já implementa `AddShield`, `ActivateBoost`, `HandleIncompatibleHit`
e o timer de expiração. **Os power-ups em si (prefabs, spawn, coleta) são da
Fase 9.** Por isso o shield/boost ainda não foram testados em jogo.

### `SO_ObstacleData` não existe

**O GDD diz:** "Cubos/retângulos com cor e tamanho definidos em `SO_ObstacleData`."

**O que foi feito:** os dados vivem no componente `Obstacle` do próprio prefab
(tamanho, comportamento, velocidade, cor aplicada no spawn) e o catálogo
continua sendo o `SO_ObstacleCatalog` (lista de prefabs). Criar um
`SO_ObstacleData` separado geraria um segundo lugar para configurar o mesmo
obstáculo, sem ganho real. Se a Fase 7 precisar de mais dados por obstáculo, o
SO pode ser introduzido lá.

---

## Fase 5 — Run básica e deslocamento da pista

### `TrackManager` separado do `SpawnManager`

**O GDD diz:** "o SpawnManager posiciona obstáculos e power-ups" e "o RunManager
controla a velocidade crescente; o SpawnManager reposiciona os tiles à frente
do jogador."

**O que foi feito:** na Fase 5, os tiles ficaram no `TrackManager` e os
obstáculos no `SpawnManager`. A fusão (todo o spawn em um lugar só) é uma
refatoração prevista para a Fase 7, quando os padrões de spawn ficarem
complexos. Fazer a fusão agora seria refatorar código que ainda vai mudar muito.

---

## Fase 4 — Jogador e controles

### `PlayerColor` como espelho

**O GDD diz:** "PlayerColor: cor ativa, cores desbloqueadas, estado de boost
(piscar) e flag de shield."

**O que foi feito:** o `PlayerColor` é o **espelho visual** do `ScoreManager`:
ele assina os eventos (`OnColorChanged`, `OnShieldChanged`, `OnBoostChanged`)
e aplica o estado no material. A autoridade continua no `ScoreManager` — o
`PlayerColor` nunca decide regra. O piscar do boost roda a cada 0,25 s.

---

## Fase 2 — Dados configuráveis

### Catálogo em vez de `SO_ObstacleData`

Conforme a nota da Fase 6: o `SO_ObstacleCatalog` (lista de prefabs) substitui
a ideia de um `SO_ObstacleData` por tipo. O projeto tem **um** lugar para
configurar cada obstáculo: o próprio prefab.

---

## Convenções de código

O GDD define `Event_OnCollected` / `Event_OnPlayerDied` como ScriptableObjects
de evento. O projeto usa **eventos C# (`event Action`)**, que é o padrão que o
próprio GDD descreve na seção "Event System" ("Notificação por eventos C# ...
expostos pelos serviços"). Os nomes de evento seguem `OnCollected` /
`OnPlayerDied` (sem o prefixo `Event_`, que no GDD marca o *tipo* do ativo, não
o nome do membro).
