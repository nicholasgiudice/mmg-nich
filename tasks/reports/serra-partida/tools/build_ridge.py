#!/usr/bin/env python3
"""T-532: monta data/maps/broken_ridge.tmj (Serra Partida) e abre o portao leste de data/maps/pass_fort.tmj.

Uso (raiz do repositorio):  python tasks/reports/serra-partida/tools/build_ridge.py

Terreno por celula: 0 encosta (chao de terra ocre), 1 plato de arenito (topo do penhasco). O chao logo ao sul de um plato
vira a face do penhasco (2). Plato e face bloqueiam (a licao das Ruinas). Tileset da serra escolhido na T-520 (opcao B,
`pixellab_serra_0`, autotile pelo pattern_4x4, como o Forte da T-531: cada tile centrado no canto entre quatro celulas,
camada com offset -16). A boca das Minas e a caverna dos morcegos sao recortes no penhasco pintados com o piso escuro da
mina (`pixellab_mina_0`); barracas e carrocas do Acampamento em `pixellab_serra_props` (gerados para esta tarefa).

O mapa e desenhado como plato inteiro com o chao escavado (retangulos), depois limpo ate nao sobrar fresta sem saida,
bolsao fechado, nem combinacao de cantos que o tileset nao tem. Deterministico. O .tmj gerado e a fonte; o script fica
para refazer do zero.
"""
import json
import math
import random
import sys
from collections import deque
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "mapa-brejo/tools"))
sys.path.insert(0, str(HERE.parents[1] / "forte-do-passo/tools"))
import tiledjson  # noqa: E402

REPO = HERE.parents[3]
MAPS = REPO / "data/maps"
META = HERE.parents[1] / "mapas-sul-tilesets/tilesets"

W, H = 200, 160
GROUND, MESA, FACE = 0, 1, 2

# ------------------------------------------------------------------ traçado (x0, y0, x1, y1), fim exclusivo
GATE_Y = 77  # portao oeste (para o Forte): celulas 0..1, linhas 77..80 (a linha 76 e a face do penhasco; segue o desfiladeiro reto)
SPAWN = (9.5, 84.5)
FROM_FORT = (5.5, 78.5)

# Todo retangulo em coordenadas pares (blocos de 2 x 2 celulas): assim todo degrau do penhasco tem 2 linhas ou mais (o
# tileset nao tem degrau de 1 linha) e nenhuma faixa de chao fica com 1 linha so. Corredor norte-sul: 4 livres; leste-oeste:
# 6 linhas, a primeira e a face do penhasco, 5 livres.
CARVE = [
    # Encosta da entrada, junto ao portao oeste
    (0, GATE_Y - 1, 4, GATE_Y + 5),
    (4, 66, 24, 96),
    # Desfiladeiro Reto: 128 tiles em linha (5 livres)
    (24, 76, 152, 82),
    # Desfiladeiro norte: sobe da encosta, corre para leste e desce de volta ao reto (um anel)
    (8, 18, 12, 66),
    (8, 18, 74, 24),
    (68, 24, 72, 76),
    # ligacao norte para o Ninho
    (74, 18, 112, 24),
    # Desfiladeiro sul: desce da encosta e corre para leste ate o Acampamento
    (10, 96, 14, 140),
    (10, 134, 46, 140),
    # ligacoes do reto: ao sul para o Acampamento e ao norte para o Ninho
    (96, 82, 100, 112),
    (116, 50, 120, 76),
    # alargamentos (lugar para lutar) no meio dos desfiladeiros
    (36, 14, 48, 24),
    (36, 128, 46, 140),
    (64, 40, 76, 52),
    (44, 72, 54, 86),
    (80, 74, 88, 86),
    (124, 72, 132, 86),
    (92, 90, 104, 100),
    # Ninho dos Morcegos: bacia ao norte, com pilares de rocha
    (108, 8, 184, 50),
    (140, 2, 146, 8),  # caverna dos morcegos (recorte no penhasco norte)
    # Boca das Minas: bacia a leste, no fim do desfiladeiro reto
    (152, 58, 196, 106),
    (170, 52, 176, 58),  # boca da mina (recorte no penhasco norte da bacia)
    # Acampamento dos Mineiros: bacia ao sul, fora da trilha principal
    (46, 110, 150, 154),
]
# rocha deixada (ou devolvida) dentro das bacias
PILLARS = [
    (122, 18, 130, 24), (152, 16, 160, 22), (132, 34, 140, 40), (164, 30, 172, 38),
    (160, 72, 166, 78), (180, 86, 188, 92), (162, 94, 168, 100),
    (70, 120, 78, 126), (104, 124, 112, 132), (128, 116, 136, 122), (60, 140, 68, 146), (118, 140, 126, 146),
]
# rocha que as reentrancias nao podem comer: atras da caverna e da mina, e entre o Ninho e a Boca das Minas
KEEP_MESA = [(0, 0, 200, 4), (132, 0, 154, 8), (150, 50, 200, 58)]
CAVE = (140, 2, 146, 8)  # piso escuro + face escura: a entrada da caverna
MINE = (170, 52, 176, 58)
MINE_RAILS_TO = 66  # trilho da boca da mina ate o meio da bacia

# ------------------------------------------------------------------ subareas (primeira que contem vale)
SUBAREAS = [
    ("Boca das Minas", (150, 50, 50, 60)),
    ("Ninho dos Morcegos", (104, 0, 96, 52)),
    ("Acampamento dos Mineiros", (44, 106, 156, 54)),
    ("Desfiladeiro", (0, 0, 200, 160)),
]

MINER, WOLF, BAT, GOLEM = "monster.possessed_miner", "monster.mountain_wolf", "monster.cave_bat", "monster.stone_golem"
SMALL, BIG = 45, 60  # renascimento (s): grupos pequenos como no Brejo; grupos grandes um pouco mais
# Grupos: (nome, x, y, [(monstro, nivel, quantidade, raio)], renascimento)
GROUPS = [
    # Desfiladeiro Reto, oeste: pares de Mineiros nv21 em fila
    ("ridge_miners_1", 32.5, 78.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("ridge_miners_2", 49.5, 79.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("ridge_miners_3", 64.5, 78.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("ridge_miners_4", 84.5, 79.5, [(MINER, 21, 2, 1.0)], SMALL),
    # Desfiladeiro norte: Mineiros nv21 e Lobos nv22
    ("north_miners_1", 10.5, 50.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("north_miners_2", 10.5, 32.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("north_miners_3", 41.5, 19.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("north_miners_4", 58.5, 20.5, [(MINER, 21, 2, 1.0)], SMALL),
    ("north_wolves_1", 70.5, 34.5, [(WOLF, 22, 2, 1.0)], SMALL),
    ("north_pack", 70.5, 46.5, [(WOLF, 22, 5, 1.5)], BIG),
    ("north_wolves_3", 70.5, 62.5, [(WOLF, 22, 2, 1.0)], SMALL),
    ("north_miners_5", 88.5, 20.5, [(MINER, 22, 2, 1.0)], SMALL),
    # Desfiladeiro Reto, leste: fila de 5 Mineiros e Lobos nv22
    ("ridge_file_1", 104.5, 78.5, [(MINER, 22, 5, 1.5)], BIG),
    ("ridge_wolves_1", 118.5, 79.5, [(WOLF, 22, 2, 1.0)], SMALL),
    ("ridge_wolves_2", 130.5, 78.5, [(WOLF, 22, 2, 1.0)], SMALL),
    ("ridge_file_2", 144.5, 78.5, [(MINER, 22, 5, 1.5)], BIG),
    # Desfiladeiro sul: Lobos e Mineiros nv22
    ("south_wolves_1", 12.5, 108.5, [(WOLF, 22, 2, 1.0)], SMALL),
    ("south_wolves_2", 12.5, 124.5, [(WOLF, 22, 2, 1.0)], SMALL),
    ("south_miners_1", 26.5, 136.5, [(MINER, 22, 2, 1.0)], SMALL),
    ("south_miners_2", 40.5, 132.5, [(MINER, 22, 2, 1.0)], SMALL),
    ("link_miners_1", 98.5, 94.5, [(MINER, 22, 2, 1.0)], SMALL),
    # Acampamento dos Mineiros: sentinelas (pares nv23) na volta e acampamentos grandes no meio (no maximo 4 Mineiros por
    # acampamento: com 5 ou 6 Mineiros nv23-24 o Guardiao, o Berserker e o Atirador sozinhos venciam 0-80%; o resto e morcego)
    ("camp_guard_1", 52.5, 116.5, [(MINER, 23, 2, 1.0)], SMALL),
    ("camp_guard_2", 98.5, 108.5, [(MINER, 23, 2, 1.0)], SMALL),
    ("camp_guard_3", 52.5, 148.5, [(MINER, 23, 2, 1.0)], SMALL),
    ("camp_guard_4", 90.5, 150.5, [(MINER, 23, 2, 1.0)], SMALL),
    ("camp_guard_5", 142.5, 114.5, [(MINER, 23, 2, 1.0)], SMALL),
    ("camp_guard_6", 146.5, 150.5, [(MINER, 23, 2, 1.0)], SMALL),
    ("camp_1", 66.5, 132.5, [(MINER, 22, 4, 2.0), (BAT, 22, 2, 1.5)], BIG),
    ("camp_2", 82.5, 116.5, [(MINER, 22, 4, 2.0), (BAT, 22, 2, 1.5)], BIG),
    ("camp_3", 86.5, 138.5, [(MINER, 23, 4, 2.0), (BAT, 23, 4, 1.5)], BIG),
    ("camp_4", 102.5, 140.5, [(MINER, 23, 4, 2.0), (BAT, 23, 4, 1.5)], BIG),
    ("camp_5", 116.5, 116.5, [(MINER, 23, 4, 2.0), (BAT, 23, 4, 1.5)], BIG),
    ("camp_6", 120.5, 132.5, [(MINER, 24, 3, 2.0), (BAT, 24, 7, 2.0)], BIG),
    ("camp_7", 136.5, 140.5, [(MINER, 24, 3, 2.0), (BAT, 24, 7, 2.0)], BIG),
    ("camp_8", 136.5, 126.5, [(MINER, 23, 4, 2.0), (BAT, 23, 4, 1.5)], BIG),
    # Ninho dos Morcegos: Lobos nv24 em pares e bandos de morcegos
    ("nest_wolves_1", 112.5, 30.5, [(WOLF, 24, 2, 1.0)], SMALL),
    ("nest_wolves_2", 118.5, 44.5, [(WOLF, 24, 2, 1.0)], SMALL),
    ("nest_wolves_3", 150.5, 46.5, [(WOLF, 24, 2, 1.0)], SMALL),
    ("nest_wolves_4", 176.5, 44.5, [(WOLF, 24, 2, 1.0)], SMALL),
    ("nest_wolves_5", 178.5, 14.5, [(WOLF, 24, 2, 1.0)], SMALL),
    ("nest_wolves_6", 124.5, 12.5, [(WOLF, 24, 2, 1.0)], SMALL),
    ("nest_bats_1", 142.5, 12.5, [(BAT, 24, 8, 2.0)], BIG),
    ("nest_bats_2", 136.5, 26.5, [(BAT, 23, 6, 2.0)], BIG),
    ("nest_bats_3", 164.5, 20.5, [(BAT, 23, 6, 2.0)], BIG),
    ("nest_bats_4", 158.5, 34.5, [(BAT, 24, 8, 2.0)], BIG),
    ("nest_bats_5", 130.5, 40.5, [(BAT, 23, 6, 2.0)], BIG),
    # Boca das Minas: o Golem de Pedra (elite nv25, renasce em 3 min), na linha do desfiladeiro reto
    ("stone_golem", 172.5, 78.5, [(GOLEM, 25, 1, 0.0)], 180),
]

TENTS = [(60, 128), (72, 130), (78, 112), (88, 112), (80, 134), (92, 134), (98, 136), (110, 112), (122, 112), (114, 128),
         (126, 128), (130, 136), (142, 136), (128, 124), (140, 122)]
CARTS = [(62, 136), (86, 120), (106, 144), (120, 120), (116, 136), (132, 144), (92, 142), (176, 62), (168, 62)]


# ------------------------------------------------------------------ terreno
STRAIGHT = (24, 76, 152, 82)  # o desfiladeiro reto fica com as paredes lisas (a linha da Rocha Rolante e das flechas)


def build_cells():
    """Plato inteiro com o chao escavado. As bordas de cada escavacao (menos o desfiladeiro reto, o portao e os recortes
    da caverna e da mina) ganham reentrancias de 0 ou 2 celulas, em trechos de 4 a 8 (pares), para a rocha nao parecer regua."""
    rng = random.Random(532)
    g = [[MESA] * W for _ in range(H)]

    def dig(x0, y0, x1, y1, edge=2):
        # borda de 2 celulas de plato em volta do mapa (so o portao oeste a atravessa)
        for y in range(max(y0, 2), min(y1, H - 2)):
            for x in range(max(x0, edge), min(x1, W - 2)):
                g[y][x] = GROUND

    for r in CARVE:
        x0, y0, x1, y1 = r
        dig(*r, edge=0 if x0 == 0 else 2)
        if r == STRAIGHT or r in (CAVE, MINE) or x0 == 0:
            continue
        for side in ("n", "s", "w", "e"):
            length = (x1 - x0) if side in "ns" else (y1 - y0)
            i = 0
            while i < length:
                # Recuo de 0 ou 2 celulas em trechos pares (o tileset nao tem degrau de 1 linha na base do penhasco); de lado,
                # a reentrancia comeca 2 linhas abaixo do topo da escavacao (abaixo da face).
                basin = min(x1 - x0, y1 - y0) >= 20  # bacia: recortes maiores
                seg = 2 * (rng.randint(4, 8) if basin else rng.randint(2, 4))
                depth = rng.choice((0, 2, 4) if basin else (0, 0, 2))
                a, b = (i + 2, min(i + seg, length - 2)) if side in "ns" else (max(i, 2), min(i + seg, length - 2))
                if depth and b > a:
                    if side == "n":
                        dig(x0 + a, y0 - depth, x0 + b, y0)
                    elif side == "s":
                        dig(x0 + a, y1, x0 + b, y1 + depth)
                    elif side == "w":
                        dig(x0 - depth, y0 + a, x0, y0 + b)
                    else:
                        dig(x1, y0 + a, x1 + depth, y0 + b)
                i += seg
    for x0, y0, x1, y1 in PILLARS + KEEP_MESA:
        for y in range(y0, y1):
            for x in range(x0, x1):
                g[y][x] = MESA
    dig(*CAVE)
    dig(*MINE)
    return g


def derive(cells):
    out = [row[:] for row in cells]
    for y in range(1, H):
        for x in range(W):
            if cells[y][x] == GROUND and cells[y - 1][x] == MESA:
                out[y][x] = FACE
    return out


class TallTileset:
    """Autotile por pattern_4x4 (guia do PixelLab), como em build_fort.py."""

    def __init__(self, meta_path, cols=4):
        self.tiles = json.load(open(meta_path, encoding="utf-8"))["tileset_data"]["tiles"]
        self.cols = cols
        self.firstgid = 0
        self.centers = {self.center(t) for t in self.tiles}

    @staticmethod
    def center(t):
        p = t["pattern_4x4"]
        return (p["row_1"][1], p["row_1"][2], p["row_2"][1], p["row_2"][2])

    def local_id(self, t):
        b = t["bounding_box"]
        return (b["y"] // 32) * self.cols + b["x"] // 32

    def pick(self, v, r, c):
        best, best_score = None, -10**9
        for t in self.tiles:
            p = t["pattern_4x4"]
            pattern = [p["row_0"], p["row_1"], p["row_2"], p["row_3"]]
            score = 0
            for i in range(4):
                for j in range(4):
                    want = pattern[i][j]
                    rr, cc = r - 1 + i, c - 1 + j
                    have = v[rr][cc] if 0 <= rr < len(v) and 0 <= cc < len(v[0]) else -1
                    if want == 255 or have == -1:
                        continue
                    center = (i, j) in ((1, 1), (1, 2), (2, 1), (2, 2))
                    if have == want:
                        score += 10 if center else 1
                    else:
                        score -= 30 if center else 1
            if score > best_score:
                best, best_score = t, score
        return self.firstgid + self.local_id(best)


def vertices(cells3):
    def cl(a, hi):
        return min(max(a, 0), hi)

    return [[cells3[cl(i - 1, H - 1)][cl(j - 1, W - 1)] for j in range(W + 1)] for i in range(H + 1)]


def bad_corners(cells, ts):
    """Celulas de chao que entram numa combinacao de cantos que o tileset nao tem (viram plato na limpeza)."""
    v = vertices(derive(cells))
    bad = set()
    for r in range(H):
        for c in range(W):
            key = (v[r][c], v[r][c + 1], v[r + 1][c], v[r + 1][c + 1])
            if key not in ts.centers:
                for dy, dx in ((-1, -1), (-1, 0), (0, -1), (0, 0)):
                    y, x = r + dy, c + dx
                    if 0 <= y < H and 0 <= x < W and cells[y][x] == GROUND:
                        bad.add((x, y))
    return bad


def is_gate(x, y):
    return x < 2 and GATE_Y <= y < GATE_Y + 4


def cleanup(cells, ts):
    """Plato onde sobra fresta sem saida, bolsao fechado ou canto sem tile; repete ate estabilizar."""
    rounds = 0
    while True:
        rounds += 1
        cells3 = derive(cells)

        def free(x, y):
            return 0 <= x < W and 0 <= y < H and cells3[y][x] == GROUND

        changed = set()
        for y in range(H):
            for x in range(W):
                if free(x, y) and not is_gate(x, y):
                    exits = sum(free(*n) for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
                    if exits < 2:
                        changed.add((x, y))
        start = (int(SPAWN[0]), int(SPAWN[1]))
        assert free(*start) and rounds < 50, ("a limpeza fechou o nascimento", rounds)
        seen = {start}
        q = deque([start])
        while q:
            x, y = q.popleft()
            for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if free(*n) and n not in seen:
                    seen.add(n)
                    q.append(n)
        changed |= {(x, y) for y in range(H) for x in range(W) if free(x, y) and (x, y) not in seen}
        changed |= bad_corners(cells, ts)
        # celula de chao entre plato (em cima) e plato (embaixo): vira face de 1 linha sem lugar; fecha
        for y in range(1, H - 1):
            for x in range(W):
                if cells[y][x] == GROUND and cells[y - 1][x] == MESA and cells[y + 1][x] == MESA:
                    changed.add((x, y))
        if not changed:
            return rounds
        for x, y in changed:
            cells[y][x] = MESA


# ------------------------------------------------------------------ montagem
def prop(name, ptype, value):
    return {"name": name, "type": ptype, "value": value}


def main():
    serra = TallTileset(META / "pixellab_serra_0.json")
    mina = TallTileset(META / "pixellab_mina_0.json")
    cells = build_cells()
    rounds = cleanup(cells, serra)
    cells3 = derive(cells)

    tilesets = [{"firstgid": 1, "source": "tilesets/collision.tsx"}]
    serra.firstgid = 2
    tilesets.append({"firstgid": serra.firstgid, "source": "tilesets/pixellab_serra_0.tsx"})
    mina.firstgid = serra.firstgid + 32
    tilesets.append({"firstgid": mina.firstgid, "source": "tilesets/pixellab_mina_0.tsx"})
    sul = mina.firstgid + 32
    tilesets.append({"firstgid": sul, "source": "tilesets/pixellab_sul_props.tsx"})
    props_first = sul + 4
    tilesets.append({"firstgid": props_first, "source": "tilesets/pixellab_serra_props.tsx"})
    next_gid = props_first + 8

    v = vertices(cells3)
    terrain = [[serra.pick(v, r, c) for c in range(W)] for r in range(H)]
    coll = [[1 if cells3[y][x] in (MESA, FACE) else 0 for x in range(W)] for y in range(H)]
    deco = [[0] * W for _ in range(H)]
    props = [[0] * W for _ in range(H)]

    # caverna e boca da mina: piso escuro da mina por cima do recorte (inclusive a face do fundo, que continua bloqueando)
    dark = mina.firstgid + 10  # tile 10 do pixellab_mina_0: piso inteiro
    for x0, y0, x1, y1 in (CAVE, MINE):
        for y in range(y0, y1):
            for x in range(x0, x1):
                if cells[y][x] == GROUND:
                    deco[y][x] = dark
    rail_v = sul + 3
    for y in range(MINE[1], MINE_RAILS_TO):
        if cells3[y][MINE[0] + 2] != MESA:
            props[y][MINE[0] + 2] = rail_v

    # barracas e carrocas (2 x 2, bloqueiam)
    def place(at, base):
        x, y = at
        for dy in range(2):
            for dx in range(2):
                assert cells3[y + dy][x + dx] == GROUND, ("objeto fora do chao", at)
                assert props[y + dy][x + dx] == 0, ("objeto sobre objeto", at)
                props[y + dy][x + dx] = props_first + base + dy * 4 + dx
                coll[y + dy][x + dx] = 1

    for t in TENTS:
        place(t, 0)
    for t in CARTS:
        place(t, 2)

    # objetos
    oid = [1]

    def obj(name, kind, x, y, w=0.0, h=0.0, properties=None, point=False):
        o = {"id": oid[0], "name": name, "type": kind, "x": x * 32, "y": y * 32, "width": w * 32, "height": h * 32,
             "rotation": 0, "visible": True}
        if point:
            o["point"] = True
        if properties:
            o["properties"] = properties
        oid[0] += 1
        return o

    spawns = [obj("spawn", "player_spawn", *SPAWN, point=True), obj("from_fort", "player_spawn", *FROM_FORT, point=True)]
    for name, x, y, points, respawn in GROUPS:
        for i, (monster, level, count, radius) in enumerate(points):
            p = [prop("monster", "string", monster), prop("level", "int", level), prop("count", "int", count),
                 prop("radius", "float", radius), prop("respawn", "float", respawn)]
            # pontos do mesmo grupo um ao lado do outro (a ate 4 tiles: um grupo so para os testes de design)
            px, py = x + (2 if i else 0), y
            spawns.append(obj(name if i == 0 else f"{name}_{i + 1}", "monster_spawn", px, py, point=True, properties=p))

    areas = [obj(name, "subarea", x, y, w, h) for name, (x, y, w, h) in SUBAREAS]
    mx0, my0, mx1, my1 = MINE
    areas.append(obj("mines_entrance", "mines_entrance", mx0, my0, mx1 - mx0, my1 - my0 + 4))
    transitions = [obj("west_gate", "transition", 0, GATE_Y, 2, 4, properties=[
        prop("arrival", "string", "from_ridge"), prop("to", "string", "zone.pass_fort")])]

    # conferencias
    def free(x, y):
        return 0 <= x < W and 0 <= y < H and not coll[y][x]

    start = (int(SPAWN[0]), int(SPAWN[1]))
    seen = {start}
    q = deque([start])
    while q:
        x, y = q.popleft()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if free(*n) and n not in seen:
                seen.add(n)
                q.append(n)
    loose = [(x, y) for y in range(H) for x in range(W) if free(x, y) and (x, y) not in seen]
    assert not loose, ("bolsao", loose[:10])
    slits = [(x, y) for y in range(H) for x in range(W) if free(x, y) and not is_gate(x, y)
             and sum(free(*n) for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))) < 2]
    assert not slits, ("fresta", slits[:10])
    for o in spawns:
        cell = (int(o["x"] // 32), int(o["y"] // 32))
        assert cell in seen, ("ponto inalcancavel", o["name"], cell)
    pts = [(name, [(x + (2 if i else 0), y) for i in range(len(points))]) for name, x, y, points, _ in GROUPS]
    near = []
    for i, (a, pa) in enumerate(pts):
        for b, pb in pts[i + 1:]:
            d = min(math.hypot(ax - bx, ay - by) for ax, ay in pa for bx, by in pb)
            if d < 12:
                near.append((a, b, round(d, 1)))
    assert not near, ("grupos a menos de 12 tiles", near)
    big = sum(1 for g in GROUPS if sum(p[2] for p in g[3]) >= 5)
    print(f"limpeza: {rounds} rodadas; livres {len(seen)}; grupos {len(GROUPS)}, com 5+ {big} "
          f"({big / len(GROUPS):.0%}); monstros {sum(p[2] for g in GROUPS for p in g[3])}")

    def tl(lid, name, data, **extra):
        d = {"data": [v for row in data for v in row], "height": H, "id": lid, "name": name, "opacity": 1,
             "type": "tilelayer", "visible": True, "width": W, "x": 0, "y": 0}
        d.update(extra)
        return d

    def ol(lid, name, objects):
        return {"draworder": "topdown", "id": lid, "name": name, "objects": objects, "opacity": 1, "type": "objectgroup",
                "visible": True, "x": 0, "y": 0}

    layers = [
        tl(1, "terrain_0", terrain, offsetx=-16, offsety=-16),
        tl(2, "decoration", deco),
        tl(3, "props", props),
        tl(4, "collision", coll, opacity=0.5, visible=False),
        ol(5, "spawns", spawns),
        ol(6, "areas", areas),
        ol(7, "transitions", transitions),
    ]
    assert next_gid > max(max(r) for r in props)
    tmj = {"compressionlevel": -1, "height": H, "infinite": False, "layers": layers, "nextlayerid": 8,
           "nextobjectid": oid[0], "orientation": "orthogonal", "renderorder": "right-down", "tiledversion": "1.11.2",
           "tileheight": 32, "tilesets": tilesets, "tilewidth": 32, "type": "map", "version": "1.10", "width": W}
    (MAPS / "broken_ridge.tmj").write_bytes(tiledjson.dumps(tmj).encode("utf-8"))
    open_fort_east_gate()


FORT_GATE_Y = 22  # portao leste do Forte: coluna 59, linhas 22..25 (a 21 e a face do muro; nascimento do Forte em (30; 22,5))


def open_fort_east_gate():
    """Abre o muro leste do Forte (coluna 59) nas linhas do portao, com a transicao e a chegada from_ridge. Idempotente."""
    fort = json.load(open(MAPS / "pass_fort.tmj", encoding="utf-8"))
    w, h = fort["width"], fort["height"]
    lay = {l["name"]: l for l in fort["layers"]}
    fsets = sorted(fort["tilesets"], key=lambda t: t["firstgid"])
    forte_first = next(t["firstgid"] for t in fsets if t["source"].endswith("pixellab_forte_0.tsx"))
    sys.path.insert(0, str(HERE.parents[1] / "forte-do-passo/tools"))
    ts = TallTileset(META / "pixellab_forte_0.json")
    ts.firstgid = forte_first
    # refaz o terreno do Forte a partir da colisao do muro (muro = coluna/linha de borda bloqueada fora dos predios)
    coll = lay["collision"]["data"]
    for y in range(FORT_GATE_Y - 1, FORT_GATE_Y + 4):
        coll[y * w + (w - 1)] = 0
    # o terreno do muro: reconstruido como em build_fort.py (bordas), com o vao do portao leste
    cells = [[0] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if x < 1 or x >= w - 1 or y < 2 or y >= h - 1:
                cells[y][x] = 1
    for y in range(2):
        for x in range(28, 32):
            cells[y][x] = 0
    for y in range(FORT_GATE_Y - 1, FORT_GATE_Y + 4):
        cells[y][w - 1] = 0
    c3 = [row[:] for row in cells]
    for y in range(1, h):
        for x in range(w):
            if cells[y][x] == 0 and cells[y - 1][x] == 1:
                c3[y][x] = 2

    def cl(a, hi):
        return min(max(a, 0), hi)

    vv = [[c3[cl(i - 1, h - 1)][cl(j - 1, w - 1)] for j in range(w + 1)] for i in range(h + 1)]
    lay["terrain_0"]["data"] = [ts.pick(vv, r, c) for r in range(h) for c in range(w)]
    # muro e face bloqueiam (a face sob a ponta do muro, na linha de cima do vao, tambem)
    for y in range(h):
        for x in range(w):
            if c3[y][x] in (1, 2):
                coll[y * w + x] = 1
    sp = lay["spawns"]["objects"]
    tr = lay["transitions"]["objects"]
    nid = fort["nextobjectid"]
    if not any(o["name"] == "from_ridge" for o in sp):
        sp.append({"id": nid, "name": "from_ridge", "type": "player_spawn", "x": 56.5 * 32, "y": 23.5 * 32,
                   "width": 0, "height": 0, "rotation": 0, "visible": True, "point": True})
        nid += 1
    if not any(o["name"] == "east_gate" for o in tr):
        tr.append({"id": nid, "name": "east_gate", "type": "transition", "x": (w - 1) * 32, "y": FORT_GATE_Y * 32,
                   "width": 32, "height": 128, "rotation": 0, "visible": True, "properties": [
                       prop("arrival", "string", "from_fort"), prop("to", "string", "zone.broken_ridge")]})
        nid += 1
    fort["nextobjectid"] = nid
    (MAPS / "pass_fort.tmj").write_bytes(tiledjson.dumps(fort).encode("utf-8"))


if __name__ == "__main__":
    main()
