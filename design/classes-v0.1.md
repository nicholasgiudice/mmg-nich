# Classes Base — v0.1

As classes são arquétipos iniciais. Evoluções e especializações ficam fora do MVP.

**Recurso de todas as classes: Mana** (decisão de 2026-10-17, T-027), que **só regenera com o tempo** (5 por segundo + Inteligência e equipamento; decisão de 2026-10-19, T-032). Nenhuma classe ganha Mana ao atacar, apanhar, curar ou proteger.

**Habilidades:** cada classe tem um ataque básico e **5 habilidades no MVP** — 4 normais e 1 de assinatura —, uma por tecla (1–5). São aprendidas com pontos de habilidade (1 por nível) e têm níveis de habilidade que aumentam o valor e mudam o comportamento; as regras estão em `design/progression-v0.1.md` (§7) e **todos os níveis de cada habilidade** em `design/skills-v0.1.md`. As habilidades em "Depois do MVP" ficam para especializações.

## Guerreiro

**Função:** combate corpo a corpo, resistência, ameaça e proteção.

**Atributos de referência:** Força (dano) e Vitalidade.

**Forças:** resistência, controle de grupos, proteção.

**Fraquezas:** pouco alcance, mobilidade limitada, depende de aproximação.

**Ataque básico:** Golpe de Espada.

**Habilidades do MVP:**

- Golpe Pesado — dano alto e interrupção (no nível de habilidade 3+, atordoa);
- Provocação — direciona monstros ao Guerreiro;
- Golpe Circular — dano e ameaça em área curta (mais giros nos níveis 3 e 5);
- Guarda Firme — redução de dano com mobilidade reduzida;
- **Muralha Viva** (assinatura) — escudo de emergência para o grupo.

**Depois do MVP:** Investida, Interceptação.

## Arqueiro

**Função:** dano à distância, posicionamento e prioridade de alvo.

**Atributos de referência:** Destreza (dano) e Agilidade.

**Forças:** dano consistente, mobilidade, eliminação de alvos prioritários.

**Fraquezas:** pouca resistência, vulnerável quando cercado e em espaços apertados.

**Ataque básico:** Disparo.

**Habilidades do MVP:**

- Disparo Potente — tiro forte em um alvo (2 flechas no nível de habilidade 3, 3 flechas no 5);
- Marca do Caçador — aumenta o dano que o alvo recebe de todos;
- Recuo Rápido — cria distância;
- Chuva de Flechas — dano em área com preparação;
- **Flecha Devastadora** (assinatura) — disparo canalizado de alto impacto em linha.

**Depois do MVP:** Armadilha de Contenção (saiu em 2026-10-19 para o Disparo Potente caber na barra de 5 teclas), Flecha Perfurante, Tiro Debilitante.

## Mago

**Função:** dano mágico, área, controle e exploração de fraquezas.

**Atributo de referência:** Inteligência.

**Forças:** impacto em grupos, controle de área e adaptação elemental.

**Fraquezas:** baixa resistência, vulnerabilidade sob pressão e dependência de Mana.

**Ataque básico:** Projétil Arcano (sem custo de Mana).

**Habilidades do MVP:**

- Explosão Elemental — dano em área à distância (2 acertos no nível de habilidade 3, 3 no 5);
- Prisão de Gelo — congelamento;
- Barreira Arcana — proteção temporária;
- Campo de Relâmpagos — zona de dano à distância;
- **Cataclismo Arcano** (assinatura) — área grande com alto tempo de recarga.

**Depois do MVP:** Combustão, Dissipar Magia.

## Clérigo

**Função:** cura, proteção, purificação, suporte e dano sagrado.

**Atributos de referência:** Inteligência (cura, escudo e dano sagrado) e Vitalidade.

**Forças:** sustenta grupos, remove condições e é eficiente contra mortos-vivos e corrupção.

**Fraquezas:** menor dano geral, vulnerabilidade isolado e necessidade de escolher entre atacar e apoiar.

**Ataque básico:** Golpe Sagrado.

**Habilidades do MVP:**

- Cura Direta — recuperação de vida;
- Corrente Sagrada — dano em alvos corrompidos e recuperação limitada;
- Escudo Sagrado — absorve dano antecipadamente;
- Purificação — remove efeitos negativos;
- **Milagre** (assinatura) — cura de emergência do grupo.

**Depois do MVP:** Bênção, Oração Restauradora.

## Regras de balanceamento

- toda classe joga sozinha no conteúdo básico;
- nenhuma classe é obrigatória para entrar na dungeon;
- especializações futuras devem mudar estilo de jogo, não só números;
- controle é reduzido contra elites e bosses;
- dano em área exige posicionamento e recurso;
- nenhuma classe pode ser dominante em todas as situações;
- com 15 pontos de habilidade não é possível ter todas as habilidades no nível máximo: escolher onde investir faz parte da build.
