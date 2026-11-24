#!/usr/bin/env python3
"""T-530: monta data/maps/drowned_marsh.tmj (Brejo dos Afogados) e abre o portao sul de data/maps/city.tmj.

Uso (raiz do repositorio):  python tasks/reports/mapa-brejo/tools/build_marsh.py

Terreno por celula: 0 lama (chao seco), 1 agua rasa, 2 agua funda (lenta, area slow_terrain), 3 juncal (bloqueia).
Cada tile do PixelLab fica centrado no canto entre quatro celulas (camada com offset -16, como nas Ruinas e na T-520).
Pares que tem tile: lama-rasa (pixellab_brejo_0), rasa-funda (pixellab_brejo_1), lama-juncal (pixellab_brejo_2). Um
bloco 2x2 de celulas com outro par (lama encostada em agua funda, juncal encostado em agua) e corrigido: a funda vira
rasa e a agua vira lama. As linhas 0-3 sao a muralha sul da Cidade, copiada da muralha norte de city.tmj (vista de dentro
ela tem a mesma face virada para o sul). Casas (palafitas e a cabana da Bruxa) sao copiadas de predios da Cidade.
Deterministico (seed fixa). O .tmj gerado e a fonte; o script fica para refazer do zero.
"""
import json
import math
import random
from collections import deque
from pathlib import Path

import tiledjson

REPO = Path(__file__).resolve().parents[4]
MAPS = REPO / "data/maps"
META = Path(__file__).resolve().parents[1] / "tilesets"

W, H = 200, 160
MUD, SHALLOW, DEEP, REED = 0, 1, 2, 3
WALL_ROWS = 4  # linhas 0..3: muralha da Cidade e a faixa de terra batida
GATE_X = 99  # portao norte: celulas 99..102, linhas 0..1
rng = random.Random(530)

# ---------------------------------------------------------------- ruido suave (bordas organicas)
_grid = {}


def noise(x, y, scale=7.0, salt=0):
    gx, gy = x / scale, y / scale
    x0, y0 = math.floor(gx), math.floor(gy)
    fx, fy = gx - x0, gy - y0

    def v(i, j):
        k = (i, j, salt)
        if k not in _grid:
            _grid[k] = random.Random(hash(k) ^ 530).uniform(-1, 1)
        return _grid[k]

    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a = v(x0, y0) + (v(x0 + 1, y0) - v(x0, y0)) * sx
    b = v(x0, y0 + 1) + (v(x0 + 1, y0 + 1) - v(x0, y0 + 1)) * sx
    return a + (b - a) * sy


G = [[MUD] * W for _ in range(H)]


def blob(cx, cy, rx, ry, v, wobble=0.25, salt=1, only=None):
    for y in range(max(0, int(cy - ry * 1.4)), min(H, int(cy + ry * 1.4) + 1)):
        for x in range(max(0, int(cx - rx * 1.4)), min(W, int(cx + rx * 1.4) + 1)):
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d <= 1 + wobble * noise(x, y, salt=salt) and (only is None or G[y][x] in only):
                G[y][x] = v


def rect(x0, y0, x1, y1, v):
    for y in range(max(0, y0), min(H, y1 + 1)):
        for x in range(max(0, x0), min(W, x1 + 1)):
            G[y][x] = v


def line(points, half, v, cells=None, only=None):
    """Faixa de largura 2*half+1 ao longo da poligonal (so sobre as celulas de 'only', se dado)."""
    out = set()
    for (ax, ay), (bx, by) in zip(points, points[1:]):
        n = int(max(abs(bx - ax), abs(by - ay)) * 2) + 1
        for i in range(n + 1):
            t = i / n
            px, py = ax + (bx - ax) * t, ay + (by - ay) * t
            for y in range(int(py - half - 1), int(py + half + 2)):
                for x in range(int(px - half - 1), int(px + half + 2)):
                    if 0 <= x < W and 0 <= y < H and (x - px) ** 2 + (y - py) ** 2 <= (half + 0.5) ** 2 + 0.6:
                        out.add((x, y))
    for x, y in out:
        if only is not None and G[y][x] not in only:
            continue
        G[y][x] = v
        if cells is not None:
            cells.add((x, y))
    return out


# ---------------------------------------------------------------- traçado
# Estrada dos Juncos: lama seca do portao norte ate a borda sul (passagem para o Forte do Passo, T-531).
ROAD_PTS = [(100.5, 0), (100.5, 20), (95, 38), (97, 58), (106, 78), (104, 98), (96, 118), (99, 140), (100.5, 159)]
SUBAREAS = [  # (nome, x, y, w, h) em celulas; o primeiro que contem o ponto vale
    ("Estrada dos Juncos", 0, 0, 200, 48),
    ("Cabana da Bruxa", 0, 108, 98, 52),
    ("Aldeia das Palafitas", 0, 48, 98, 60),
    ("Charco Fundo", 98, 48, 102, 112),
]


def build_terrain():
    # Estrada: lagoas rasas e moitas de juncal, poucas manchas fundas.
    for cx, cy, rx, ry, s in [(38, 20, 15, 8, 2), (68, 30, 9, 6, 3), (142, 17, 16, 8, 4), (170, 36, 11, 7, 5),
                              (128, 40, 7, 4, 6), (16, 40, 7, 5, 7)]:
        blob(cx, cy, rx, ry, SHALLOW, salt=s)
    blob(142, 17, 6, 3, DEEP, salt=8)
    blob(38, 20, 5, 3, DEEP, salt=9)
    for cx, cy, rx, ry, s in [(20, 10, 9, 4, 10), (60, 12, 7, 4, 11), (84, 26, 4, 7, 12), (118, 10, 6, 3, 13),
                              (163, 8, 9, 3, 14), (186, 22, 5, 6, 15), (150, 41, 5, 3, 16), (45, 41, 6, 3, 17),
                              (112, 30, 3, 3, 18)]:
        blob(cx, cy, rx, ry, REED, salt=s)

    # Aldeia das Palafitas: lagoa rasa com ilhas de lama (as palafitas) e um poco fundo no meio.
    blob(48, 78, 42, 27, SHALLOW, wobble=0.15, salt=20)
    blob(50, 79, 8, 5, DEEP, salt=21)
    blob(22, 96, 5, 3, DEEP, salt=22)
    for cx, cy in ISLANDS:
        blob(cx, cy, 8, 6, MUD, wobble=0.1, salt=23 + int(cx))
    # passarelas de lama entre as ilhas e ate a estrada
    for pts in [[(30, 62), (52, 58), (72, 70), (96, 66)], [(72, 70), (66, 92)], [(30, 62), (24, 82), (38, 90), (66, 92)],
                [(66, 92), (96, 100)]]:
        line(pts, 1, MUD)
    for cx, cy, rx, ry, s in [(8, 60, 4, 6, 30), (88, 84, 4, 5, 31), (12, 100, 4, 4, 32), (82, 52, 6, 3, 33)]:
        blob(cx, cy, rx, ry, REED, salt=s)

    # Charco Fundo: agua rasa larga com pocos fundos e ilhas de lama entre eles (onde se luta seco).
    blob(150, 102, 46, 52, SHALLOW, wobble=0.12, salt=40)
    for cx, cy, rx, ry, s in [(130, 64, 11, 6, 41), (166, 62, 13, 7, 42), (146, 90, 15, 8, 43), (180, 100, 7, 11, 44),
                              (126, 118, 11, 7, 45), (160, 128, 14, 7, 46), (183, 142, 6, 5, 47), (118, 92, 4, 4, 48)]:
        blob(cx, cy, rx, ry, DEEP, salt=s)
    for cx, cy, rx, ry, s in [(116, 58, 5, 4, 50), (178, 56, 5, 4, 51), (162, 80, 5, 3, 52), (130, 104, 5, 4, 53),
                              (186, 78, 4, 4, 54), (112, 132, 5, 4, 55), (144, 142, 6, 4, 56), (176, 118, 5, 4, 57),
                              (188, 150, 4, 3, 58), (145, 116, 4, 3, 59)]:
        blob(cx, cy, rx, ry, MUD, wobble=0.15, salt=s)
    for cx, cy, rx, ry, s in [(196, 66, 4, 6, 60), (140, 76, 3, 3, 61), (192, 120, 4, 6, 62), (128, 150, 5, 3, 63)]:
        blob(cx, cy, rx, ry, REED, salt=s)

    # Cabana da Bruxa: ilha de lama cercada por um anel de agua funda, com um aterro ate a estrada.
    blob(48, 134, 28, 20, SHALLOW, wobble=0.12, salt=70)
    blob(48, 134, 19, 13, DEEP, wobble=0.1, salt=71)
    blob(48, 133, 11, 8, MUD, wobble=0.05, salt=72)
    line([(58, 134), (78, 134), (96, 124)], 1, MUD)  # aterro
    for cx, cy, rx, ry, s in [(12, 120, 5, 6, 73), (84, 150, 6, 4, 74), (18, 150, 5, 4, 75), (80, 112, 4, 3, 76)]:
        blob(cx, cy, rx, ry, REED, salt=s)

    # Borda: juncal fechado, com espessura irregular. O norte e a muralha.
    for y in range(H):
        for x in range(W):
            t = 4 + 2.5 * (1 + noise(x, y, scale=9, salt=90))
            if x < t or x > W - 1 - t or y > H - 1 - t:
                G[y][x] = REED
    # A estrada e um aterro: agua rasa dos dois lados na maior parte do caminho (so onde era lama).
    line(ROAD_PTS[1:-1], 6, SHALLOW, only={MUD})
    # A estrada por cima de tudo (seca de borda a borda), com acostamento de lama.
    road = set()
    line(ROAD_PTS, 2, MUD, road)
    rect(0, 0, W - 1, WALL_ROWS - 1, MUD)
    return road


ISLANDS = [(30, 62), (54, 60), (72, 72), (24, 84), (40, 92), (66, 94)]
# Casas: (x, y) do canto de cima; template 'house' (palafita, telhado marrom) ou 'hut' (cabana da Bruxa, telhado cinza).
HOUSES = [(27, 58, "house"), (51, 56, "house"), (69, 68, "house"), (21, 80, "house"), (37, 88, "house"), (63, 90, "house"),
          (45, 125, "hut")]


def fix_pairs(protect):
    """Corrige blocos 2x2 sem tile: agua funda com lama/juncal vira rasa; agua com juncal vira lama."""
    changed = True
    while changed:
        changed = False
        for y in range(H - 1):
            for x in range(W - 1):
                cells = [(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)]
                vals = {G[cy][cx] for cx, cy in cells}
                if DEEP in vals and (MUD in vals or REED in vals):
                    for cx, cy in cells:
                        if G[cy][cx] == DEEP:
                            G[cy][cx] = SHALLOW
                            changed = True
                elif REED in vals and (SHALLOW in vals or DEEP in vals):
                    for cx, cy in cells:
                        if G[cy][cx] in (SHALLOW, DEEP):
                            G[cy][cx] = MUD
                            changed = True


def smooth_reeds(blocked_extra, protect):
    """Tira juncal fino (menos de 2 vizinhos de juncal) e fecha frestas de lama (3+ vizinhos bloqueados)."""
    for _ in range(4):
        changed = False
        for y in range(WALL_ROWS, H):
            for x in range(W):
                def bl(a, b):
                    return a < 0 or b < 0 or a >= W or b >= H or G[b][a] == REED or (a, b) in blocked_extra or b < 2
                n = sum(bl(a, b) for a, b in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))
                if G[y][x] == REED and n < 2 and (x, y) not in protect:
                    G[y][x] = MUD
                    changed = True
                elif G[y][x] != REED and (x, y) not in blocked_extra and n >= 3 and (x, y) not in protect:
                    G[y][x] = REED
                    changed = True
        if not changed:
            break


# ---------------------------------------------------------------- tilesets
class Wang:
    def __init__(self, name):
        meta = json.load(open(META / f"{name}.json", encoding="utf-8"))
        self.tiles = {}
        for t in meta["tileset_data"]["tiles"]:
            c, b = t["corners"], t["bounding_box"]
            self.tiles[(c["NW"], c["NE"], c["SW"], c["SE"])] = (b["y"] // 32) * 4 + b["x"] // 32
        self.firstgid = 0

    def gid(self, corners):
        return self.firstgid + self.tiles[corners]


def terrain_layer():
    sets = {"0": Wang("pixellab_brejo_0"), "1": Wang("pixellab_brejo_1"), "2": Wang("pixellab_brejo_2")}
    return sets


def cell(x, y):
    return G[min(max(y, 0), H - 1)][min(max(x, 0), W - 1)]


def paint(sets):
    out = [[0] * W for _ in range(H)]
    for r in range(WALL_ROWS, H):
        for c in range(W):
            n = (cell(c - 1, r - 1), cell(c, r - 1), cell(c - 1, r), cell(c, r))
            vals = set(n)
            if vals <= {MUD, SHALLOW}:
                ts, up = sets["0"], SHALLOW
            elif vals <= {SHALLOW, DEEP}:
                ts, up = sets["1"], DEEP
            elif vals <= {MUD, REED}:
                ts, up = sets["2"], REED
            else:
                raise ValueError(f"bloco sem tile em ({c},{r}): {n}")
            out[r][c] = ts.gid(tuple("upper" if t == up else "lower" for t in n))
    return out


# ---------------------------------------------------------------- moldes copiados da Cidade
CITY = json.load(open(MAPS / "city.tmj", encoding="utf-8"))
CW = CITY["width"]
CL = {l["name"]: l["data"] for l in CITY["layers"] if l["type"] == "tilelayer"}
TEMPLATES = {"house": (36, 47, 8, 5), "hut": (56, 47, 8, 5)}  # Armazem (telhado marrom) e Mercado (telhado cinza)
COPY_LAYERS = ["props", "walls_detail", "above"]


def city_at(layer, x, y):
    return CL[layer][y * CW + x]


# ---------------------------------------------------------------- pontos de monstros
# (nome, subarea esperada, [(monstro, nivel, qtd, x, y)]); pontos de um mesmo grupo ficam a ate 4 tiles.
D, L, S, WITCH = "monster.drowned", "monster.marsh_lizardman", "monster.marsh_serpent", "monster.marsh_witch"
GROUPS = [
    # Estrada dos Juncos: poucos, nivel 16 (e 17 no fim da estrada)
    ("road_drowned_1", [(D, 16, 2, 80, 14)]),
    ("road_drowned_2", [(D, 16, 3, 124, 26)]),
    ("road_drowned_3", [(D, 16, 3, 58, 33)]),
    ("road_drowned_4", [(D, 16, 2, 154, 28)]),
    ("road_drowned_5", [(D, 16, 3, 28, 28)]),
    ("road_drowned_6", [(D, 16, 3, 178, 12)]),
    ("road_drowned_7", [(D, 16, 2, 106, 12)]),
    ("road_mixed_1", [(D, 17, 1, 78, 44), (L, 17, 1, 80, 45)]),
    ("road_mixed_2", [(D, 17, 1, 181, 44), (L, 17, 1, 183, 45)]),
    ("road_mixed_3", [(D, 17, 1, 135, 32), (L, 17, 1, 137, 33)]),
    # Aldeia das Palafitas: afogados em bando (3; os de 4 a 6 ficam na Cabana), 17-18
    ("village_drowned_1", [(D, 17, 3, 32, 66)]),
    ("village_mixed_3", [(D, 17, 1, 55, 64), (L, 17, 1, 57, 65)]),
    ("village_mixed_1", [(D, 17, 1, 74, 76), (L, 17, 1, 76, 77)]),
    ("village_drowned_2", [(D, 18, 3, 14, 72)]),
    ("village_lizard_2", [(L, 18, 2, 44, 77)]),
    ("village_drowned_3", [(D, 18, 3, 62, 81)]),
    ("village_mixed_2", [(D, 18, 1, 26, 92), (L, 18, 1, 27, 90)]),
    ("village_lizard_3", [(L, 18, 2, 52, 102)]),
    ("village_drowned_4", [(D, 17, 3, 82, 98)]),
    ("village_serpent_1", [(S, 18, 2, 86, 60)]),
    ("village_drowned_5", [(D, 17, 3, 10, 52)]),
    # Charco Fundo: serpentes e homens-lagarto, 18-19, muita agua funda
    ("bog_serpent_1", [(S, 18, 2, 116, 58)]),
    ("bog_lizard_1", [(L, 18, 2, 140, 62)]),
    ("bog_serpent_2", [(S, 18, 2, 178, 56)]),
    ("bog_lizard_2", [(L, 18, 2, 120, 78)]),
    ("bog_serpent_3", [(S, 19, 2, 162, 79)]),
    ("bog_lizard_3", [(L, 19, 2, 186, 90)]),
    ("bog_serpent_4", [(S, 19, 2, 130, 103)]),
    ("bog_lizard_4", [(L, 19, 2, 152, 98)]),
    ("bog_lizard_5", [(L, 19, 2, 120, 122)]),
    ("bog_serpent_5", [(S, 19, 2, 144, 142)]),
    ("bog_serpent_6", [(S, 19, 2, 176, 117)]),
    ("bog_lizard_6", [(L, 19, 2, 160, 130)]),
    ("bog_serpent_7", [(S, 18, 2, 145, 116)]),
    ("bog_lizard_7", [(L, 19, 2, 186, 146)]),
    ("bog_lizard_8", [(L, 18, 2, 166, 64)]),
    # Cabana da Bruxa: a Bruxa (chefe de campo) e bandos grandes de afogados 19 em volta (grupo recomendado)
    ("witch", [(WITCH, 20, 1, 48.5, 134.5)]),
    ("hut_drowned_1", [(D, 19, 5, 22, 118)]),
    ("hut_drowned_2", [(D, 19, 4, 74, 116)]),
    ("hut_drowned_3", [(D, 19, 5, 20, 146)]),
    ("hut_drowned_4", [(D, 19, 4, 72, 150)]),
    ("hut_drowned_5", [(D, 19, 6, 46, 156 - 8)]),
]
RESPAWN = 45
WITCH_RESPAWN = 600


def subarea_of(x, y):
    for name, ax, ay, aw, ah in SUBAREAS:
        if ax <= x < ax + aw and ay <= y < ay + ah:
            return name
    return None


# ---------------------------------------------------------------- montagem
def rects_of(mask):
    """Cobre as celulas marcadas com retangulos (guloso: linhas maximas, depois desce enquanto a faixa for igual)."""
    left = set(mask)
    out = []
    for y in range(H):
        for x in range(W):
            if (x, y) not in left:
                continue
            x1 = x
            while (x1 + 1, y) in left:
                x1 += 1
            y1 = y
            while all((i, y1 + 1) in left for i in range(x, x1 + 1)):
                y1 += 1
            for yy in range(y, y1 + 1):
                for xx in range(x, x1 + 1):
                    left.discard((xx, yy))
            out.append((x, y, x1 - x + 1, y1 - y + 1))
    return out


def main():
    road = build_terrain()
    # casas: celulas bloqueadas e chao de lama em volta
    house_cells = set()
    for hx, hy, kind in HOUSES:
        rect(hx - 1, hy - 1, hx + 8, hy + 6, MUD)
        for y in range(hy, hy + 5):
            for x in range(hx, hx + 8):
                house_cells.add((x, y))
    points = [(x, y) for _, pts in GROUPS for (_, _, _, x, y) in pts]
    protect = set(road)
    for px, py in points:
        for y in range(int(py) - 1, int(py) + 2):
            for x in range(int(px) - 1, int(px) + 2):
                protect.add((x, y))
                if G[y][x] == REED:
                    G[y][x] = MUD
    fix_pairs(protect)
    smooth_reeds(house_cells, protect)
    fix_pairs(protect)

    # colisao: muralha (linhas 0-1 fora do portao), juncal, casas; bolsoes sem acesso viram juncal
    def blocked(x, y):
        if y < 2:
            return not (GATE_X <= x < GATE_X + 4)
        return G[y][x] == REED or (x, y) in house_cells

    seen = {(101, 6)}
    q = deque(seen)
    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < W and 0 <= ny < H and (nx, ny) not in seen and not blocked(nx, ny):
                seen.add((nx, ny))
                q.append((nx, ny))
    sealed = [(x, y) for y in range(WALL_ROWS, H) for x in range(W) if not blocked(x, y) and (x, y) not in seen]
    for x, y in sealed:
        G[y][x] = REED
    fix_pairs(protect)
    print("bolsoes fechados:", len(sealed))

    # tilesets e gids (mesmos firstgid da Cidade para os moldes copiados)
    tilesets = [
        {"firstgid": 1, "source": "tilesets/collision.tsx"},
        {"firstgid": 2, "source": "tilesets/terrain_summer.tsx"},
        {"firstgid": 4098, "source": "tilesets/buildings.tsx"},
        {"firstgid": 8194, "source": "tilesets/pixellab_city_0.tsx"},
        {"firstgid": 8210, "source": "tilesets/pixellab_city_1.tsx"},
        {"firstgid": 8226, "source": "tilesets/pixellab_city_2.tsx"},
    ]
    sets = terrain_layer()
    gid = 8258
    for k in ("0", "1", "2"):
        sets[k].firstgid = gid
        tilesets.append({"firstgid": gid, "source": f"tilesets/pixellab_brejo_{k}.tsx"})
        gid += 16
    props_first = gid
    tilesets.append({"firstgid": props_first, "source": "tilesets/pixellab_sul_props.tsx"})

    terrain = paint(sets)
    wall_ground = [[0] * W for _ in range(H)]
    wall_top = [[0] * W for _ in range(H)]
    layers_extra = {n: [[0] * W for _ in range(H)] for n in COPY_LAYERS}
    coll = [[0] * W for _ in range(H)]
    # muralha: coluna 10 da Cidade (muro liso) e as colunas 34-45 em volta do portao norte (38-41)
    for x in range(W):
        cx = 34 + (x - (GATE_X - 4)) if GATE_X - 4 <= x < GATE_X + 8 else 10
        for y in range(WALL_ROWS):
            wall_ground[y][x] = city_at("terrain_0", cx, y)
            wall_top[y][x] = city_at("terrain_2", cx, y)
            layers_extra["props"][y][x] = city_at("props", cx, y)
            coll[y][x] = 1 if city_at("collision", cx, y) else 0
    for hx, hy, kind in HOUSES:
        tx, ty, tw, th = TEMPLATES[kind]
        for dy in range(-1, th):
            for dx in range(tw):
                for n in COPY_LAYERS:
                    g = city_at(n, tx + dx, ty + dy)
                    if g:
                        layers_extra[n][hy + dy][hx + dx] = g
    for y in range(WALL_ROWS, H):
        for x in range(W):
            coll[y][x] = 1 if G[y][x] == REED or (x, y) in house_cells else 0

    # juncos soltos (decoracao, nao bloqueiam): na lama e na agua rasa, longe da estrada e dos pontos
    deco = [[0] * W for _ in range(H)]
    for y in range(WALL_ROWS + 2, H):
        for x in range(W):
            if G[y][x] in (MUD, SHALLOW) and (x, y) not in road and (x, y) not in protect and (x, y) not in house_cells:
                near_reed = any(G[b][a] == REED for a, b in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))
                                if 0 <= a < W and 0 <= b < H)
                p = 0.10 if near_reed else 0.018
                if rng.random() < p:
                    deco[y][x] = props_first + (0 if rng.random() < 0.5 else 1)
    # beira da estrada: juncos na margem do aterro
    for x, y in sorted(road):
        edge = any((a, b) not in road for a, b in ((x + 1, y), (x - 1, y)))
        if y > 8 and G[y][x] == MUD and edge and (x, y) not in protect - road and rng.random() < 0.22:
            deco[y][x] = props_first + (0 if rng.random() < 0.5 else 1)

    # objetos
    oid = [1]

    def obj(name, kind, x, y, w=0, h=0, props=None, point=False):
        o = {"id": oid[0], "name": name, "type": kind, "x": x * 32, "y": y * 32, "width": w * 32, "height": h * 32,
             "rotation": 0, "visible": True}
        if point:
            o["point"] = True
        if props:
            o["properties"] = props
        oid[0] += 1
        return o

    spawns = [obj("spawn", "player_spawn", 101.0, 6.5, point=True), obj("from_city", "player_spawn", 101.0, 4.5, point=True)]
    for name, pts in GROUPS:
        for i, (m, lvl, count, x, y) in enumerate(pts):
            px, py = (x, y) if isinstance(x, float) else (x + 0.5, y + 0.5)
            assert coll[int(py)][int(px)] == 0, (name, px, py)
            props = [{"name": "monster", "type": "string", "value": m}, {"name": "level", "type": "int", "value": lvl}]
            if m == WITCH:
                props += [{"name": "respawn", "type": "float", "value": WITCH_RESPAWN},
                          {"name": "announce", "type": "bool", "value": True}]
            else:
                props += [{"name": "count", "type": "int", "value": count},
                          {"name": "radius", "type": "float", "value": 1.0 if count > 1 else 0.0},
                          {"name": "respawn", "type": "float", "value": RESPAWN}]
            spawns.append(obj(name if len(pts) == 1 else f"{name}_{i + 1}", "monster_spawn", px, py, props=props, point=True))
    areas = [obj(name, "subarea", x, y, w, h) for name, x, y, w, h in SUBAREAS]
    deep = {(x, y) for y in range(H) for x in range(W) if G[y][x] == DEEP}
    for i, (x, y, w, h) in enumerate(rects_of(deep)):
        areas.append(obj(f"deep_water_{i + 1}", "slow_terrain", x, y, w, h, props=[
            {"name": "status", "type": "string", "value": "status.deep_water"},
            {"name": "ignoreTags", "type": "string", "value": "amphibious"}]))
    transitions = [obj("north_gate", "transition", GATE_X, 0, 4, 2, props=[
        {"name": "to", "type": "string", "value": "zone.city"}, {"name": "arrival", "type": "string", "value": "from_marsh"}])]

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
        tl(2, "terrain_1", wall_ground, offsetx=-16, offsety=-16),
        tl(3, "terrain_2", wall_top, offsetx=-16, offsety=-16),
        tl(4, "decoration", deco),
        tl(5, "props", layers_extra["props"]),
        tl(6, "walls_detail", layers_extra["walls_detail"]),
        tl(7, "above", layers_extra["above"]),
        tl(8, "collision", coll, opacity=0.5, visible=False),
        ol(9, "spawns", spawns),
        ol(10, "areas", areas),
        ol(11, "transitions", transitions),
    ]
    tmj = {"compressionlevel": -1, "height": H, "infinite": False, "layers": layers, "nextlayerid": 12,
           "nextobjectid": oid[0], "orientation": "orthogonal", "renderorder": "right-down", "tiledversion": "1.11.2",
           "tileheight": 32, "tilesets": tilesets, "tilewidth": 32, "type": "map", "version": "1.10", "width": W}
    (MAPS / "drowned_marsh.tmj").write_bytes(tiledjson.dumps(tmj).encode("utf-8"))

    # relatorio rapido
    counts = {v: sum(row.count(v) for row in G) for v in (MUD, SHALLOW, DEEP, REED)}
    print("celulas: lama", counts[MUD], "rasa", counts[SHALLOW], "funda", counts[DEEP], "juncal", counts[REED],
          "retangulos de agua funda", len(rects_of(deep)))
    for name, pts in GROUPS:
        sa = subarea_of(pts[0][3], pts[0][4])
        for _, _, _, x, y in pts:
            assert subarea_of(x, y) == sa, name
    for i, (a, pa) in enumerate(GROUPS):
        for b, pb in GROUPS[i + 1:]:
            d = min(math.dist((p[3], p[4]), (q[3], q[4])) for p in pa for q in pb)
            if d < 12:
                print(f"  perto: {a} e {b} a {d:.1f}")
    on_deep = [n for n, pts in GROUPS for (_, _, _, x, y) in pts if G[int(y)][int(x)] == DEEP]
    print("pontos na agua funda:", on_deep)
    open_city_south_gate()


CITY_GATE_X = 46  # portao sul da Cidade: celulas 46-49, entre o Armazem (36-43) e o Mercado (56-63), fora do eixo do Armazem


def open_city_south_gate():
    """Abre o portao sul da Cidade (celulas 46-49, linhas 58-59), com pilares como os do portao norte."""
    city = json.load(open(MAPS / "city.tmj", encoding="utf-8"))
    w = city["width"]
    gx = CITY_GATE_X
    lay = {l["name"]: l for l in city["layers"]}
    d = {n: l["data"] for n, l in lay.items() if l["type"] == "tilelayer"}
    for x in range(gx, gx + 4):
        d["collision"][59 * w + x] = 0
        d["props"][58 * w + x] = 0
    for x in range(gx + 1, gx + 4):
        d["terrain_2"][59 * w + x] = 0
    for x in range(gx, gx + 5):
        d["terrain_0"][59 * w + x] = 8200
    # pilares (os mesmos do portao norte, nas duas celulas da ponta)
    d["props"][58 * w + gx], d["props"][59 * w + gx] = 5718, 5782
    d["props"][58 * w + gx + 3], d["props"][59 * w + gx + 3] = 5716, 5780
    sp = lay["spawns"]["objects"]
    tr = lay["transitions"]["objects"]
    nid = city["nextobjectid"]
    if not any(o["name"] == "from_marsh" for o in sp):
        sp.append({"height": 0, "id": nid, "name": "from_marsh", "point": True, "rotation": 0, "type": "player_spawn",
                   "visible": True, "width": 0, "x": (gx + 2.0) * 32, "y": 55.5 * 32})
        nid += 1
    if not any(o["name"] == "south_gate" for o in tr):
        tr.append({"height": 64, "id": nid, "name": "south_gate", "properties": [
            {"name": "arrival", "type": "string", "value": "from_city"},
            {"name": "to", "type": "string", "value": "zone.drowned_marsh"}],
            "rotation": 0, "type": "transition", "visible": True, "width": 128, "x": gx * 32, "y": 58 * 32})
        nid += 1
    city["nextobjectid"] = nid
    (MAPS / "city.tmj").write_bytes(tiledjson.dumps(city).encode("utf-8"))


if __name__ == "__main__":
    main()
