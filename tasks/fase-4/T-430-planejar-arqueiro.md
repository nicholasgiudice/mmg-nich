# T-430 — Detalhar o bloco do Arqueiro da fase 4 em tarefas

- **Ambiente:** Windows (só documentos)
- **Modelo:** **Forte**
- **Depende de:** T-420
- **Libera:** T-431
- **Áreas que pode alterar:** `tasks/`, `design/segunda-classe-v0.1.md` (só as decisões do designer)

## Contexto

Continua a T-420. O designer revisa as segundas classes uma por uma; depois do Clérigo, vem o Arqueiro (Atirador de Elite e Caçador). Só as passivas (Mira Firme e Flecha Envenenada) estavam decididas.

## Requisitos

1. Rever com o designer o Atirador de Elite e o Caçador.
2. Registrar as decisões em `design/segunda-classe-v0.1.md`.
3. Criar as tarefas do Arqueiro em `tasks/fase-4/`, no formato da T-390, e atualizar `tasks/README.md`.

## Critérios de aceite

- toda tarefa nova pode ser entregue a um agente sem contexto além de `AGENTS.md`, o próprio arquivo e `design/segunda-classe-v0.1.md`;
- dependências sem ciclos.

## Resultado

Concluída em 2026-11-22, no Windows (só documentos), na branch `task/T-420-planejar-clerigo` (junto com a T-420, na mesma conversa com o designer).

**Decisões do designer (2026-11-22)**
- **Atirador de Elite aprovado**, com duas mudanças:
  - **Tiro Certeiro sem conjuração** (antes, 1,5 s parado; 1 s no NH5). No NH5, a conjuração mais curta virou **recarga de 8 s**;
  - **Olho de Falcão dura perto de 20 s**, porque 6 a 8 s era pouco para 70 de Mana. Ficou **15 / 17 / 20 s** (a mesma subida por NH que a assinatura já tinha).
- **Caçador aprovado como está.**

**Decisões desta tarefa**
- O dano do Tiro Certeiro foi feito para um tiro com conjuração. A T-433 pede para ajustar primeiro o dano ou o custo dele, e não a duração do Olho de Falcão, se a rajada passar das metas. Ela também propõe uma meta nova para isso (dano nos 20 s do Olho de Falcão até 2,5× o dano sem ele).
- Mecânicas em duas tarefas, uma depois da outra (T-431 → T-432): as duas mexem nas definições de estado, no validador e no schema.
- **Pontos que a T-432 precisa decidir:** o que o Veneno do Caçador faz sem pontos na Flecha Envenenada (sugestão: usa o NH1).

**Avisos da revisão que o designer aceitou**
- A Ferida Aberta pode ficar quase sempre no bônus, porque os monstros andam atrás do Arqueiro.
- A Praga só pula quando o veneno acaba, então quase não aparece nos alvos em que o Caçador continua atirando.
- A Flecha Dispersa e a Praga espalham o veneno, as duas.
- O nome "Caçador" é o da segunda classe e também está na "Marca do Caçador" da base.

O playtest da T-435 pergunta se a Ferida Aberta e a Praga se notam.

**Tarefas criadas**
- T-431 Core: mecânicas do Atirador de Elite;
- T-432 Core: mecânicas do Caçador;
- T-433 Dados: Atirador e Caçador, metas e testes de design;
- T-434 Cliente: ícones e efeitos (amostra primeiro);
- T-435 Playtest do Arqueiro.

**Pendências**
- Revisar com o designer o Mago (Piromante e Arcanista) e criar o bloco dele.
