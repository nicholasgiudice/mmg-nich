# Direção Tecnológica — v0.1

## Decisão atual

Usar **Godot 4** para o protótipo e como candidata principal para o cliente final.

## Protótipo

O primeiro protótipo será local e single-player:

```text
Godot 4
├── arena simples;
├── quatro classes;
├── três tipos de inimigo;
├── vida e recurso;
├── habilidades;
└── boss simplificado.
```

Não implementar ainda login, banco remoto, chat, guildas, PvP, matchmaking ou servidor persistente.

## Cliente futuro

Godot continua responsável por:

- renderização 2D;
- mapas e colisões;
- personagens e animações;
- interface;
- efeitos e áudio;
- input.

## Servidor futuro

O jogo online deverá evoluir para:

```text
Cliente Godot
      ↓
Servidor autoritativo
      ↓
Banco de dados
```

O servidor final ainda não está decidido. Pode ser Godot headless, Go, Rust ou outra solução adequada após o vertical slice.

## Princípios de arquitetura

- o cliente não decide dano, loot ou inventário;
- cooldowns e recursos devem ser validados por um sistema de combate separado;
- UI não aplica efeitos diretamente;
- entidades, habilidades, dano e estado devem ter responsabilidades separadas;
- dados de classes, monstros e habilidades devem ser configuráveis;
- o protótipo deve permitir migrar a autoridade do combate para servidor depois.

## Fora da decisão atual

Não usar rAthena para este projeto. rAthena é adequado a um servidor inspirado em Ragnarok, mas imporia um modelo que não corresponde ao design próprio deste MMO.
