# T-034 — Design: todos os níveis de todas as habilidades e trilhas de implementação

- **Ambiente:** qualquer (documentos)
- **Modelo:** Humano + Médio
- **Depende de:** T-032
- **Libera:** T-060 (trilha de habilidades)
- **Áreas que pode alterar:** `design/`, `tasks/`

## Contexto

Pedidos do designer (2026-10-19):

1. Definir **todos os níveis de todas as habilidades**, no formato "Nível 1: 1 flecha e dano base x; Nível 2: 1 flecha e dano base y; Nível 3: 2 flechas e dano base z..." — cada nível melhorando o dano base, a potência ou o tempo.
2. Colocar a implementação em tarefas.
3. Criar a trilha de arte e mapas (pacotes de assets, estilo simples como o Tibia mas não idêntico), sem executar agora.

## Requisitos

1. `design/skills-v0.1.md` com uma tabela por habilidade (16 normais × 5 NH, 4 assinaturas × 3 NH).
2. `design/progression-v0.1.md` §7 e `design/classes-v0.1.md` apontando para o novo documento (sem duas fontes divergentes).
3. Trilha de habilidades (T-060 a T-064) e trilha de arte e mapas (T-050 a T-055) em `tasks/`, com índice em `tasks/README.md`.

## Resultado

Concluída em 2026-10-19.

- **`design/skills-v0.1.md`** (gerado por script a partir das fórmulas, para não errar contas): para cada habilidade, uma linha por NH com acertos (flechas, explosões, giros, ondas, pulsos ou alvos), fórmula do valor (`base + coeficiente × atributo`), outros efeitos e o valor no perfil do nível 1. Regra: valor × 1,0/1,2/1,4/1,6/1,8 por NH (assinatura × 1,0/1,3/1,6); acertos extras valem 85% (NH3, NH5) ou 100% (NH4) do NH1 cada — total 1,0 → 1,2 → 1,7 → 2,0 → 2,55. Ex.: Disparo Potente 40 → 48 → 2 × 34 → 2 × 40 → 3 × 34; Explosão 44 → 53 → 2 × 37 → 2 × 44 → 3 × 37. Habilidades sem dano melhoram duração, raio, alvos, recarga, cargas.
- Valores do NH1 conferidos com os dados atuais; correção: **Guarda Firme** reduz **60%** do dano frontal (o design dizia 40%).
- Campo de Relâmpagos NH5 trocado de "pulsos a cada 0,5 s" para "raio 4,5" — o primeiro ficava 4,4 × o NH1, muito acima das outras (~2,5 ×).
- `design/progression-v0.1.md` §7: tabelas por classe trocadas por um resumo e o link para `skills-v0.1.md`; comparação do nível 15 recalculada (Disparo Potente NH5 = 386).
- **Trilha de habilidades** (`tasks/trilha-habilidades/`): T-060 Core de NH e múltiplos acertos, T-061 efeitos novos, T-062 dados de todos os níveis e habilidades que faltam, T-063 simulador e balanceamento por NH, T-064 cliente (escolha de NH no menu e visual).
- **Trilha de arte e mapas** (`tasks/trilha-arte/`): T-050 direção de arte e pacotes (LPC como candidato), T-051 pipeline Tiled → Core, T-052 mapas no cliente, T-053 sprites e animações, T-054 efeitos visuais, T-055 mapas da Cidade e dos Campos.

**Situação:** os níveis de habilidade existem só no design; o jogo continua com NH1. A implementação é a trilha T-060 a T-064 — não depende da rede e pode rodar em paralelo à fase 1 ou depois dela.
