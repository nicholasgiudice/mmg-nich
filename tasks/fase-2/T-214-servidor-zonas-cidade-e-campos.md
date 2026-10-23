# T-214 — Servidor: zonas abertas, Cidade e Campos, troca pelo portão

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** T-212
- **Libera:** T-215, T-223
- **Áreas que pode alterar:** `src/Game.Server/`, `src/Game.Core/` (só o necessário para zona aberta), `src/Game.Protocol/`, `data/arenas.json` ou mapas, testes correspondentes

## Contexto

Objetivo da fase 2: **Cidade + Campos online** (`design/first-map-v0.1.md` §1–§2). Hoje o servidor tem **uma sala com um encontro que termina**. Um mundo aberto precisa de zonas que não acabam: monstros que renascem, jogadores entrando e saindo a qualquer hora, troca de zona pelo portão. Os mapas desenhados no Tiled vêm da trilha de arte (T-051/T-055); se ainda não existirem, usar mapas provisórios nos dados com o mesmo tamanho aproximado.

## Requisitos

1. **Várias zonas num processo** (Cidade, Campos da Vigília), cada uma com seu `World`, tick próprio e interesse por grade (T-106). A sala de encontro atual continua existindo como tipo de zona (instância de teste/Matriarca).
2. **Cidade segura**: sem monstros, sem dano entre jogadores (PvP fora de escopo), regenera vida e Mana mais rápido; é onde o personagem nasce e renasce.
3. **Campos**: pontos de surgimento de monstros com respawn (tempo por ponto), níveis 1–4; a zona nunca "termina".
4. **Portões**: área no mapa que leva à outra zona (portão leste da Cidade ↔ entrada dos Campos); o cliente recebe um `Welcome`/troca de zona com arena e época novas. Posição e zona são gravadas (T-212).
5. **Morte no mundo aberto**: o personagem cai, mostra a opção de renascer e volta na Cidade sem perder equipamento (visão do MVP).
6. Protocolo v+1 se a troca de zona pedir mensagem nova.

## Critérios de aceite

- testes: troca de zona leva a entidade e mantém o estado; respawn de monstros; morte e renascimento na Cidade; Cidade sem dano;
- bots de rede (T-109) andando entre zonas sem quedas;
- `./scripts/check.sh` completo passa.

## Resultado

_(preencher ao concluir)_
