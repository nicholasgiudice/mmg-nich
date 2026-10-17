# T-026 — Cliente: ataque contínuo segurando o botão

- **Ambiente:** Windows
- **Modelo:** Leve
- **Depende de:** T-021, T-024
- **Libera:** nova rodada de playtest (T-025)
- **Áreas que pode alterar:** `client/`

## Contexto

Playtest (T-025, 2026-10-16): "o combate não está muito fluido". Parte disso é controle: hoje **cada clique é um ataque básico** (recarga de ~1 s), então o jogador clica sem parar durante toda a luta (pendência registrada na T-021). O lado numérico (velocidade dos monstros, dano) é a T-019.

## Requisitos

1. **Segurar o botão de ataque** (`attack`) repete o ataque básico automaticamente no alvo sob o cursor ou no selecionado (mesmas regras de alvo da T-021), assim que a recarga permitir.
2. **Sem spam de intenções**: enquanto o ataque está em recarga, conjurando ou impossível (sem alvo, morto), o cliente não envia uma intenção por frame. Usar a recarga do snapshot (`AbilityState.ReadyAtTick`) para enviar perto da hora de ficar pronto; rejeições repetidas não podem gerar um texto flutuante por frame.
3. **Segurar teclas de habilidade** (1–4) também repete a habilidade quando ficar pronta (opcional, mas com a mesma regra anti-spam). Um toque continua funcionando como hoje.
4. O clique sobre o HUD continua não virando ataque.
5. Registrar no Resultado o que mais foi percebido como "pouco fluido" no controle e não coube nesta tarefa.

## Critérios de aceite

- segurar o botão esquerdo sobre um inimigo ataca continuamente no ritmo da recarga;
- nenhuma intenção repetida por frame enquanto nada pode acontecer (verificável no log de intenções/rejeições);
- `dotnet build client/` e `./scripts/check.sh` passam.

## Resultado

_(preencher ao concluir)_
