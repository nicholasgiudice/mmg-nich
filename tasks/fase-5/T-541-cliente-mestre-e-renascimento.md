# T-541 — Cliente: mestre das classes e tela de morte com o destino

- **Ambiente:** Windows
- **Modelo:** Médio
- **Depende de:** T-501 (para o destino na tela de morte); o mestre não depende de nada
- **Libera:** T-550
- **Áreas que pode alterar:** `client/scripts/Main.cs` (`TalkTo`), `client/scripts/Hud/` (tela de habilidades e da segunda classe), `client/scripts/Match/DeathScreen.cs`, `tasks/reports/`

## Contexto

**Mestre das classes.** O design da segunda classe previa escolher falando com um mestre na Cidade. A T-403 deixou a escolha na tela de habilidades, em qualquer lugar fora de combate, "até os mapas serem refeitos". O designer decidiu manter as duas formas (`design/mapas-16-40-v0.1.md` §8, decisão 5): a tela continua, e o mestre é um atalho com cara de jogo. O Core e o servidor não mudam: a escolha continua validada como hoje (nível 20, fora de combate, definitiva).

Hoje, clicar num NPC sem loja só mostra "não tenho nada para vender" (`Main.TalkTo`, T-323). A Cidade já tem um NPC sem função no Salão de Treinamento (`npc.trainer`). O Forte do Passo terá o `npc.class_master` (T-531).

**Tela de morte.** O botão diz "Retornar à Cidade" (T-397). Com a T-501, quem morre na Serra ou nas Minas renasce no Forte do Passo.

## Requisitos

1. Clicar no `npc.trainer` (Cidade) ou no `npc.class_master` (Forte), ao alcance:
   - nível 20 ou mais, sem segunda classe: abre a tela de habilidades já na escolha da segunda classe;
   - abaixo do 20: o mestre diz, sobre o personagem, que volte no nível 20;
   - já com segunda classe: abre a tela de habilidades.
   Longe, "Chegue mais perto", como nas lojas.
2. A lista de NPCs que são mestres fica num lugar só, no cliente (sem `if` espalhado pelo código).
3. Tela de morte: o botão mostra o destino real, "Retornar à Cidade" ou "Retornar ao Forte do Passo", lido do `respawn` da zona em que o personagem morreu (T-501), nos dados que o cliente já tem.
4. O designer aprova no jogo.

## Critérios de aceite

- no jogo (servidor local com `--dev-tools`): personagem no 19 fala com o mestre e recebe o aviso; sobe para o 20 pelo Esc, fala de novo e a tela abre na escolha; escolhe e vê a tela de habilidades;
- morrer na Cidade ou no Brejo mostra "Retornar à Cidade"; na Serra, "Retornar ao Forte do Passo" (depois da T-532; antes, conferir com uma zona de teste);
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
