# T-512 — Core e dados: o Capataz Forjado e os monstros das Minas Fundas

- **Ambiente:** VPS
- **Modelo:** **Forte**
- **Depende de:** 511
- **Libera:** T-513, T-533, T-540
- **Áreas que pode alterar:** as mesmas da T-510

## Contexto

As Minas Fundas (`design/mapas-16-40-v0.1.md` §3.4) são a masmorra instanciada do nível 24 ao 27, para 4 pessoas, e a **primeira com papel de grupo de verdade**: o chefe bate forte num só (pede Guardião) e solta dano em todos (pede Sacerdote). As salas usam os monstros da Serra (T-511) nos níveis 24–26.

Mesmo formato da T-351 (o Vigia Sem Rosto): fases, telegráfos e metas de grupo no simulador.

## Requisitos

1. **Design** do chefe (seção nova em `design/combat-encounters-v0.1.md`). Proposta (o designer pode trocar):
   - **Capataz Forjado** (`monster.forged_overseer`, 27, **chefe**, tag `construct`), golem de ferro, três fases:
     1. *Martelo*: golpe em cone curto no alvo da ameaça, muito forte, com telegráfo. Quem não é tanque cai em 2 golpes;
     2. *Fornalha*: pulsos de dano em toda a sala a cada poucos segundos, que pedem cura em grupo; continua o Martelo;
     3. *Chamar a Equipe*: invoca mineiros possuídos (`summon`) e acelera.
2. **Monstros das salas:** os da Serra nos níveis 24–26, mais um Golem de Pedra elite numa rota opcional. Se uma sala pedir um monstro novo, registre.
3. **Core:** só o que faltar, com teste.
4. **Testes de design** (perfis com segunda classe, nível 27):
   - grupo de 4 com Guardião e Sacerdote vence ≥ 75%;
   - grupo de 4 **sem** Guardião e **sem** Sacerdote vence ≤ 40% (o papel importa);
   - duração da luta com o chefe entre 3 e 6 min.
5. **Arte:** só o id. O Capataz não tem corpo no gerador LPC: a T-521 resolve (encomenda ou pacote).

## Critérios de aceite

- seção de design do chefe;
- testes de design passando, com relatório em `tasks/reports/`;
- `design/bestiario.md` atualizado;
- `./scripts/check.sh` completo passa.

## Resultado

_(a preencher)_
