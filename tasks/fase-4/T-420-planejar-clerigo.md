# T-420 — Detalhar o bloco do Clérigo da fase 4 em tarefas

- **Ambiente:** Windows (só documentos)
- **Modelo:** **Forte**
- **Depende de:** T-390
- **Libera:** T-421
- **Áreas que pode alterar:** `tasks/`, `design/segunda-classe-v0.1.md` (só as decisões do designer)

## Contexto

Pedido do designer em 2026-11-22: seguir para as outras classes da fase 4. O bloco do Guerreiro (T-401 a T-412) está na `main`. O playtest dele (T-413) ainda não foi feito, e o designer decidiu **não esperar por ele**.

## Requisitos

1. Rever com o designer as segundas classes que faltavam aprovar, uma por uma, começando pelo Clérigo.
2. Registrar as decisões em `design/segunda-classe-v0.1.md`.
3. Criar as tarefas do Clérigo em `tasks/fase-4/`, no formato da T-390, e atualizar `tasks/README.md` (estado da fase, índice, ondas, cuidados de integração, grafo e esboço).

## Critérios de aceite

- toda tarefa nova pode ser entregue a um agente sem contexto além de `AGENTS.md`, o próprio arquivo e `design/segunda-classe-v0.1.md`;
- dependências sem ciclos.

## Resultado

Concluída em 2026-11-22, no Windows (só documentos), branch `task/T-420-planejar-clerigo`.

**Decisões do designer (2026-11-22)**
- **Seguir sem o playtest do Guerreiro.** A T-413 fica pendente; o playtest do Clérigo (T-425) pode cobrir as duas classes.
- **Revisar as segundas classes uma por uma**, começando pelo **Clérigo**.
- **Inquisidor aprovado como está** (seção 8 do design).
- **Elo da Vida trocado pela Inspiração** no Sacerdote: o Elo repetia o desvio de dano da Interceptação. A Inspiração aumenta a regeneração de Mana dos aliados no raio, menos o próprio Sacerdote.

**Decisões desta tarefa**
- A Inspiração dá **regeneração de Mana**, e não Mana na hora, para respeitar a regra "Mana vem só do tempo" (`design/progression-v0.1.md`, seção 4). O Sacerdote fica de fora para a habilidade não pagar o próprio custo. Os números (custo 30, recarga 30 s, +(1,5 + 0,04 × INT)/s por 12 s no NH1) seguem a escada da seção 2 do design e ainda vão passar pelo simulador (T-423).
- Os atributos da Bênção e a regeneração da Inspiração usam **a mesma peça nova do Core**: estatísticas vindas de um estado, pela INT de quem lançou (T-421).
- O Santuário NH3 ("cura 20% da vida máxima ao acabar") pede duas peças que ainda não existem: efeito ao acabar o estado e cura pela vida máxima do alvo (T-421).
- Mecânicas em duas tarefas, uma depois da outra (T-421 → T-422), como a T-406 → T-407: as duas mexem nas definições de estado, no validador e no schema.
- Metas de design do Clérigo propostas na T-423 (Inquisidor no máximo 85% do tempo do Clérigo só com a base; Sacerdote dá pelo menos 10% mais dano por segundo ao grupo). A T-423 pode ajustá-las com o simulador.

**Tarefas criadas**
- T-421 Core: mecânicas do Sacerdote;
- T-422 Core: mecânicas do Inquisidor;
- T-423 Dados: Sacerdote e Inquisidor, metas e testes de design;
- T-424 Cliente: ícones e efeitos (amostra primeiro);
- T-425 Playtest do Clérigo.

**Pendências**
- Revisar com o designer o Arqueiro e o Mago e criar os blocos deles: feitos na T-430 e na T-440.
- T-413 (playtest do Guerreiro) continua aberta.
