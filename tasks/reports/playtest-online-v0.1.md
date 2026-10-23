# Playtest online v0.1 (T-111)

Servidor na VPS (`31.97.162.124:7777`, Docker, T-110), cliente Godot no Windows do designer, rede doméstica. 2026-10-23.

**Decisão: seguir para a fase 2 (T-120).** O teste com quatro pessoas ficou pendente (sem convidados disponíveis) e entra no próximo playtest.

## O que foi testado

| Parte do roteiro | Feito? | Como |
|---|---|---|
| 1. Sozinho contra a VPS (classes, Matriarca com 3 bots) | sim | o designer jogou; o cliente de testes também conectou da mesma máquina |
| 2. Grupo com 1–3 convidados | **não** | sem convidados; substituído por medições com vários clientes de máquina (abaixo) |
| 3. Anotações (ping, pulos, atrasos, quedas) | sim | `--net-stats` do cliente e logs do servidor |

## Respostas

- **Movimento e ataque básico tão responsivos quanto no modo local?** Sim. O designer achou "bem fluido". A predição (T-108) não precisou corrigir nada: correção de 0 px durante a partida e passo entre frames igual à velocidade do personagem.
- **Telegráfos chegam a tempo de desviar?** Sim, sem queixa no teste. Com ping de ~15–20 ms, o atraso de desenho das outras entidades (100 ms) domina; os telegráfos da Matriarca duram 1,0 s ou mais.
- **Algo diferente entre jogadores (dessincronização)?** Não visto. Com dois clientes na mesma sala (T-107) os dois viram as mesmas posições e o mesmo log. Não foi testado com pessoas diferentes em redes diferentes.
- **O que mais irritou?** Chegar com a luta já em andamento: a sala começava quando o servidor subia e os bots lutavam sozinhos. **Corrigido** antes do fim do teste: sala vazia fica parada, quem entra começa do zero e o fim da luta reinicia sozinho em 10 s (`--restart-after`).

## Medições

| Medida | Valor |
|---|---|
| Ping (cliente Windows → VPS) | 14–22 ms; 34–98 ms no primeiro segundo da conexão |
| Perda de snapshots | 0% |
| Correção da predição | 0 px (nenhum "pulo") |
| Quedas | nenhuma |
| Servidor depois de reiniciar a VPS | voltou sozinho e aceitou o cliente (T-110) |
| Bot de rede rodando na própria VPS | ping 9–11 ms, sem perda |
| Carga medida no PC (T-109) | 40 bots em 10 salas: tick médio 0,85 ms, nenhum tick pulado, 4–6 KB/s por jogador |

## Pendências para o próximo playtest

1. **Quatro pessoas reais**, em redes diferentes, na Matriarca, pelo menos 3 vezes: confirmar que ninguém vê nada diferente e anotar o ping de cada um.
2. Teste com ping alto de verdade (150 ms ou mais, ex.: alguém longe ou em rede móvel). No PC, com `--net-lag=150 --net-loss=5`, o movimento ficou sem pulos (T-108), mas a sensação não foi testada por uma pessoa.
3. Os NH escolhidos no menu não valem online (o servidor não recebe; todas as habilidades ficam no NH1). Resolver na fase 2, junto com personagens persistentes.
