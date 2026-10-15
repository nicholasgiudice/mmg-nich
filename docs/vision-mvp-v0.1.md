# Visão do MVP — v0.1

## Conceito

Um MMO RPG 2D medieval em que o jogador começa em uma cidade-fortaleza antiga, explora regiões perigosas, enfrenta monstros e bosses, obtém equipamentos e desenvolve uma entre quatro classes base.

A fantasia é sair de uma cidade segura, avançar gradualmente para além dos muros e retornar mais forte após cada expedição.

## Pilares

1. Exploração com risco crescente;
2. PvE como atividade principal;
3. farm com propósito;
4. poucas classes com identidades mecânicas claras;
5. bosses com mecânicas, não apenas mais vida;
6. cooperação sem tornar o solo impossível;
7. progressão prática antes de sistemas sociais complexos.

## Escopo do MVP

```text
Cidade-Fortaleza
├── Campos da Vigília
├── Floresta Baixa
└── Ruínas da Estrada Antiga
      └── Cripta do Vigia
            └── O Vigia Sem Rosto
```

O MVP terá quatro classes: Guerreiro, Arqueiro, Mago e Clérigo.

## Combate

- tempo real;
- visão 2D de cima;
- movimentação livre;
- ataques básicos e habilidades com cooldown;
- recursos próprios por classe;
- grupo ideal de quatro jogadores;
- PvE solo possível, com vantagens reais para grupos;
- morte inicial sem perda de equipamento;
- retorno à cidade como custo principal.

## PvP e guerras — adiados

A visão futura prevê PvP baseado em equipes, mapas de guerra, pontos de controle e controle territorial. As guerras ocorreriam em janelas fixas, por exemplo duas horas semanais. A guilda com mais pontos controlados venceria o mapa e obteria acesso a uma dungeon específica, com uma peça rara de uma fórmula para um item lendário.

Esse sistema não faz parte do MVP. Não implementar agora.

## Critérios de sucesso

- o combate básico é divertido;
- cada classe joga de modo diferente;
- o jogador tem motivo para explorar além da cidade;
- monstros exigem decisões;
- a dungeon é uma progressão natural;
- o boss exige movimento, prioridade e coordenação;
- o mapa funciona sem PvP.

## Princípio de escopo

Não construir infraestrutura de MMO antes de provar o vertical slice. Servidor persistente, login, guildas, economia, PvP e conteúdo em escala entram somente depois da validação do núcleo.
