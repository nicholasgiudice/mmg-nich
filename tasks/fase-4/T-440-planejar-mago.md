# T-440 — Detalhar o bloco do Mago da fase 4 em tarefas

- **Ambiente:** Windows (só documentos)
- **Modelo:** **Forte**
- **Depende de:** T-430
- **Libera:** T-441
- **Áreas que pode alterar:** `tasks/`, `design/segunda-classe-v0.1.md` (só as decisões do designer)

## Contexto

Continua a T-420 e a T-430. Última classe da revisão uma por uma: o Mago (Piromante e Arcanista), que não tinha nada aprovado.

## Requisitos

1. Rever com o designer o Piromante e o Arcanista.
2. Registrar as decisões em `design/segunda-classe-v0.1.md`.
3. Criar as tarefas do Mago em `tasks/fase-4/`, no formato da T-390, e atualizar `tasks/README.md`, com a ordem das tarefas de Core das três classes.

## Critérios de aceite

- toda tarefa nova pode ser entregue a um agente sem contexto além de `AGENTS.md`, o próprio arquivo e `design/segunda-classe-v0.1.md`;
- dependências sem ciclos.

## Resultado

Concluída em 2026-11-22, no Windows (só documentos), na branch `task/T-420-planejar-clerigo` (junto com a T-420 e a T-430, na mesma conversa com o designer).

**Decisões do designer (2026-11-22)**
- **Piromante aprovado como está.**
- **Arcanista aprovado**, com uma mudança: o **Sifão Arcano aumenta a regeneração de Mana** do Arcanista por 5 s (+6 a +10 por s, 30 a 50 no total, como antes), em vez de devolver Mana na hora. Assim vale a regra "Mana vem só do tempo", como na Inspiração do Sacerdote.

**Avisos da revisão que o designer aceitou**
- O Chão em Chamas e o Meteoro se parecem com o Campo de Relâmpagos e o Cataclismo Arcano da base.
- A Rajada de Chamas (4 tiles) põe o Mago perto dos monstros.
- O Piromante tende a ficar sem Mana: a T-443 pede para ajustar primeiro os custos e tem uma meta de Mana.
- O Arcanista passa muito tempo parado (Raio Arcano, Sifão, Feixe do Vazio). O playtest da T-445 pergunta isso.

**Decisões desta tarefa**
- O Sifão usa a mesma peça da Inspiração (T-421), então a T-442 depende da T-421.
- A Combustão NH5 espalha a Queimadura com a cópia de estados da Flecha Dispersa (T-432), então a T-441 depende da T-432.
- Sugestão para os Orbes Arcanos: um estado no Arcanista com disparo periódico no alvo atual, sem entidades novas (T-442 decide).
- **Ordem das seis tarefas de Core das três classes**, uma de cada vez porque mexem nos mesmos arquivos: T-421, T-422, T-431, T-432, T-441, T-442 (seção 6i de `tasks/README.md`).

**Tarefas criadas**
- T-441 Core: mecânicas do Piromante;
- T-442 Core: mecânicas do Arcanista;
- T-443 Dados: Piromante e Arcanista, metas e testes de design;
- T-444 Cliente: ícones e efeitos (amostra primeiro);
- T-445 Playtest do Mago (pode ser o playtest da fase inteira).

**Pendências**
- Nenhuma revisão de classe falta. Os números das seis segundas classes novas ainda vão passar pelo simulador (T-423, T-433, T-443).
- Os playtests T-413, T-425, T-435 e T-445 continuam abertos.
