#!/usr/bin/env python3
"""T-533: monta data/maps/deep_mines.tmj (Minas Fundas, instancia de 4) e poe o portao das Minas na Boca das Minas da Serra.

Uso (raiz do repositorio):  python tasks/reports/minas-fundas/tools/build_mines.py

Terreno por celula, como a Serra (T-532): 0 piso de cascalho escuro, 1 topo da parede (terra escorada). O piso logo ao sul de
uma parede vira a face dela, com as escoras de madeira (2). Topo e face bloqueiam ("toda pedra de parede bloqueando", T-520).
Tileset da mina escolhido na T-520 (opcao B, `pixellab_mina_0`, autotile pelo pattern_4x4, cada tile centrado no canto entre
quatro celulas, camada com offset -16). Trilhos de `pixellab_sul_props` (so retos: nao ha curva nem cruzamento) e carrinhos
de minerio de `pixellab_serra_props` (2 x 2, bloqueiam: a cobertura do Poco dos Carrinhos).

O mapa e rocha inteira com as galerias escavadas (retangulos em coordenadas pares, porque o tileset nao tem degrau de 1 linha
na base da parede), depois limpo ate nao sobrar fresta sem saida, bolsao fechado, nem combinacao de cantos que o tileset nao
tem. Galerias de mina ficam com as paredes retas (sem os recortes aleatorios da Serra). Deterministico. O .tmj gerado e a
fonte; o script fica para refazer do zero.

Ao fim, chama a Serra (build_ridge.py) para refazer broken_ridge.tmj com o portao das Minas (constantes MINES_GATE e
FROM_MINES de la).
"""
import json
import math
import subprocess
import sys
from collections import deque
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "mapa-brejo/tools"))
import tiledjson  # noqa: E402

REPO = HERE.parents[3]
MAPS = REPO / "data/maps"
META = HERE.parents[1] / "mapas-sul-tilesets/tilesets"

W, H = 140, 80
FLOOR, WALL, FACE = 0, 1, 2

# ------------------------------------------------------------------ traçado (x0, y0, x1, y1), fim exclusivo
# Galeria leste-oeste: 6 linhas (a primeira e a face da parede, 5 livres); norte-sul: 4 colunas livres.
EXIT_Y = 39  # saida (para a Serra): celulas 0..1, linhas 39..43 (a 38 e a face)
SPAWN = (10.5, 41.5)
CARVE = [
    # Galeria de Entrada, em U: o tunel da saida e o salao da entrada; para leste, desce e volta para leste ate o canto
    # sudoeste do Poco, com dois alargamentos
    (0, 38, 6, 44),
    (4, 30, 16, 52),
    (16, 38, 44, 44),
    (24, 34, 32, 48),
    (40, 38, 44, 70),
    (36, 50, 48, 60),
    (40, 64, 56, 70),
    # Poco dos Carrinhos: o salao grande
    (54, 14, 92, 68),
    # Veio Rico (rota do Golem, mais longa): sobe do Poco, corre para leste ate a camara do veio e desce ate a antessala
    (84, 4, 88, 14),
    (84, 4, 100, 10),
    (98, 4, 128, 24),
    (110, 24, 114, 28),
    # Veio Estreito (rota rapida): um tunel curto do Poco direto para a antessala
    (92, 30, 100, 36),
    # Antessala (as duas rotas se juntam) e a descida para a Fornalha: o Capataz nao ve a antessala
    (100, 28, 116, 44),
    (100, 44, 104, 50),
    # Fornalha: a arena do Capataz, 40 x 28 livres (o salao em que a T-512 balanceou o chefe), sem obstaculos
    (98, 49, 138, 78),
]
ARENA = (98, 50, 138, 78)  # celulas livres da Fornalha (a linha 49 e a face)
PILLARS = []

# ------------------------------------------------------------------ subareas (primeira que contem vale)
SUBAREAS = [
    ("Fornalha", (96, 44, 44, 36)),
    ("Veio Estreito", (92, 28, 8, 10)),
    ("Antessala", (100, 28, 16, 16)),
    ("Poço dos Carrinhos", (54, 14, 38, 54)),
    ("Veio Rico", (84, 0, 56, 28)),
    ("Galeria de Entrada", (0, 0, 56, 80)),
]
FURNACE_SUBAREA = (96, 44, 140, 80)  # x0, y0, x1, y1: o Capataz so ve o que esta aqui dentro

MINER, WOLF, BAT, GOLEM = "monster.possessed_miner", "monster.mountain_wolf", "monster.cave_bat", "monster.stone_golem"
OVERSEER = "monster.forged_overseer"
# Grupos, na ordem do percurso: (nome, x, y, [(monstro, nivel, quantidade, raio)]). Instancia: nao renasce.
GROUPS = [
    # Galeria de Entrada: grupos pequenos nos corredores (Mineiros em fila), nivel 24
    ("gallery_miners_1", 27.5, 41.5, [(MINER, 24, 4, 1.5)]),
    ("gallery_mixed", 41.5, 41.5, [(MINER, 24, 3, 1.5), (BAT, 24, 2, 1.0)]),
    ("gallery_miners_2", 41.5, 55.5, [(MINER, 24, 4, 1.5)]),
    ("gallery_file", 48.5, 66.5, [(MINER, 24, 5, 1.5)]),
    # Poco dos Carrinhos: 13 grupos de 6 a 10 (pedem dano em area), entre os carrinhos; nivel 24 na metade de baixo e 25 na de cima
    ("pit_bats_1", 72.5, 66.5, [(BAT, 24, 8, 2.0)]),
    ("pit_crew_1", 86.5, 66.5, [(MINER, 24, 4, 2.0), (BAT, 24, 4, 1.5)]),
    ("pit_crew_2", 86.5, 54.5, [(MINER, 24, 3, 2.0), (BAT, 24, 5, 1.5)]),
    ("pit_bats_2", 72.5, 54.5, [(BAT, 24, 10, 2.0)]),
    ("pit_crew_3", 58.5, 54.5, [(MINER, 24, 4, 2.0), (BAT, 24, 2, 1.5)]),
    ("pit_crew_4", 58.5, 42.5, [(MINER, 25, 4, 2.0), (BAT, 25, 4, 1.5)]),
    ("pit_crew_5", 72.5, 42.5, [(MINER, 25, 3, 2.0), (BAT, 25, 7, 2.0)]),
    ("pit_bats_3", 86.5, 42.5, [(BAT, 25, 8, 2.0)]),
    ("pit_crew_6", 72.5, 30.5, [(MINER, 25, 5, 2.0), (BAT, 25, 3, 1.5)]),
    ("pit_crew_7", 58.5, 30.5, [(MINER, 25, 4, 2.0), (BAT, 25, 6, 2.0)]),
    ("pit_bats_4", 58.5, 18.5, [(BAT, 25, 10, 2.0)]),
    ("pit_crew_8", 72.5, 18.5, [(MINER, 25, 4, 2.0), (BAT, 25, 4, 1.5)]),
    ("pit_crew_9", 86.5, 18.5, [(MINER, 25, 3, 2.0), (BAT, 25, 7, 2.0)]),
    # Veio Rico (mais saque, mais longe): uma matilha no corredor, o Golem de Pedra (elite) com a matilha na camara do veio
    # e uma equipe na descida
    ("rich_wolves", 94.5, 7.5, [(WOLF, 26, 5, 1.5)]),
    ("rich_golem", 113.5, 13.5, [(GOLEM, 26, 1, 0.0), (WOLF, 26, 4, 1.5)]),
    ("rich_crew", 111.5, 25.5, [(MINER, 26, 4, 1.5), (BAT, 26, 4, 1.0)]),
    # Veio Estreito: uma matilha so
    ("narrow_wolves", 95.5, 32.5, [(WOLF, 26, 5, 1.5)]),
    # Antessala: a guarda da Fornalha, nas duas rotas
    ("furnace_guard", 108.5, 38.5, [(MINER, 26, 4, 1.5), (BAT, 26, 4, 1.5)]),
    # Fornalha: o Capataz Forjado
    ("forged_overseer", 122.5, 63.5, [(OVERSEER, 27, 1, 0.0)]),
]

# carrinhos de minerio (2 x 2, bloqueiam): a cobertura do Poco (cortam a visao e o caminho da Escoria e da Rocha Rolante)
CARTS = [(65, 24), (79, 24), (65, 36), (79, 36), (88, 30), (65, 48), (79, 48), (65, 60), (79, 60), (56, 24), (56, 36),
         (66, 66), (89, 48)]
# trilhos retos (so ha reto horizontal e vertical; sem curva, cada trilho acaba numa junta de galerias, num carrinho ou numa
# parede): (x0, y0, x1, y1), fim exclusivo
RAILS = [(0, 41, 40, 42), (41, 46, 42, 64), (44, 67, 66, 68), (88, 7, 128, 8), (92, 33, 100, 34)]


# ------------------------------------------------------------------ terreno
def build_cells():
    g = [[WALL] * W for _ in range(H)]

    def dig(x0, y0, x1, y1):
        # borda de 2 celulas de rocha em volta do mapa (so a saida a oeste a atravessa)
        for y in range(max(y0, 2), min(y1, H - 2)):
            for x in range(max(x0, 0 if x0 == 0 else 2), min(x1, W - 2)):
                g[y][x] = FLOOR

    for r in CARVE:
        dig(*r)
    for x0, y0, x1, y1 in PILLARS:
        for y in range(y0, y1):
            for x in range(x0, x1):
                g[y][x] = WALL
    return g


def derive(cells):
    out = [row[:] for row in cells]
    for y in range(1, H):
        for x in range(W):
            if cells[y][x] == FLOOR and cells[y - 1][x] == WALL:
                out[y][x] = FACE
    return out


class TallTileset:
    """Autotile por pattern_4x4 (guia do PixelLab), como em build_fort.py e build_ridge.py."""

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
    v = vertices(derive(cells))
    bad = set()
    for r in range(H):
        for c in range(W):
            key = (v[r][c], v[r][c + 1], v[r + 1][c], v[r + 1][c + 1])
            if key not in ts.centers:
                for dy, dx in ((-1, -1), (-1, 0), (0, -1), (0, 0)):
                    y, x = r + dy, c + dx
                    if 0 <= y < H and 0 <= x < W and cells[y][x] == FLOOR:
                        bad.add((x, y))
    return bad


def is_exit(x, y):
    return x < 2 and EXIT_Y <= y < EXIT_Y + 5


def cleanup(cells, ts):
    """Rocha onde sobra fresta sem saida, bolsao fechado ou canto sem tile; repete ate estabilizar."""
    rounds = 0
    while True:
        rounds += 1
        cells3 = derive(cells)

        def free(x, y):
            return 0 <= x < W and 0 <= y < H and cells3[y][x] == FLOOR

        changed = set()
        for y in range(H):
            for x in range(W):
                if free(x, y) and not is_exit(x, y):
                    if sum(free(*n) for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))) < 2:
                        changed.add((x, y))
        start = (int(SPAWN[0]), int(SPAWN[1]))
        assert free(*start) and rounds < 50, ("a limpeza fechou o nascimento", rounds)
        seen = flood(free, start)
        changed |= {(x, y) for y in range(H) for x in range(W) if free(x, y) and (x, y) not in seen}
        changed |= bad_corners(cells, ts)
        for y in range(1, H - 1):
            for x in range(W):
                if cells[y][x] == FLOOR and cells[y - 1][x] == WALL and cells[y + 1][x] == WALL:
                    changed.add((x, y))
        if not changed:
            return rounds
        for x, y in changed:
            cells[y][x] = WALL


def flood(free, start):
    seen = {start}
    q = deque([start])
    while q:
        x, y = q.popleft()
        for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if free(*n) and n not in seen:
                seen.add(n)
                q.append(n)
    return seen


def line_of_sight(blocked, a, b):
    """Amostra o segmento a cada 0,25 tile (como o Core: celula bloqueada no caminho corta a visao)."""
    (ax, ay), (bx, by) = a, b
    n = max(1, int(math.hypot(bx - ax, by - ay) / 0.25))
    for i in range(1, n):
        t = i / n
        x, y = ax + (bx - ax) * t, ay + (by - ay) * t
        if blocked(int(math.floor(x)), int(math.floor(y))):
            return False
    return True


# ------------------------------------------------------------------ montagem
def prop(name, ptype, value):
    return {"name": name, "type": ptype, "value": value}


def main():
    mina = TallTileset(META / "pixellab_mina_0.json")
    cells = build_cells()
    rounds = cleanup(cells, mina)
    cells3 = derive(cells)

    tilesets = [{"firstgid": 1, "source": "tilesets/collision.tsx"}]
    mina.firstgid = 2
    tilesets.append({"firstgid": mina.firstgid, "source": "tilesets/pixellab_mina_0.tsx"})
    sul = mina.firstgid + 32
    tilesets.append({"firstgid": sul, "source": "tilesets/pixellab_sul_props.tsx"})
    props_first = sul + 4
    tilesets.append({"firstgid": props_first, "source": "tilesets/pixellab_serra_props.tsx"})
    next_gid = props_first + 8

    v = vertices(cells3)
    terrain = [[mina.pick(v, r, c) for c in range(W)] for r in range(H)]
    coll = [[1 if cells3[y][x] in (WALL, FACE) else 0 for x in range(W)] for y in range(H)]
    props = [[0] * W for _ in range(H)]

    for x0, y0, x1, y1 in RAILS:
        tile = sul + (2 if y1 - y0 == 1 else 3)  # 2: trilho horizontal; 3: vertical
        for y in range(y0, y1):
            for x in range(x0, x1):
                assert cells3[y][x] == FLOOR, ("trilho fora do piso", (x, y))
                props[y][x] = tile

    def place(at, base):
        x, y = at
        for dy in range(2):
            for dx in range(2):
                assert cells3[y + dy][x + dx] == FLOOR, ("carrinho fora do piso", at)
                assert props[y + dy][x + dx] == 0, ("carrinho sobre outro objeto", at)
                props[y + dy][x + dx] = props_first + base + dy * 4 + dx
                coll[y + dy][x + dx] = 1

    for c in CARTS:
        place(c, 2)

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

    spawns = [obj("spawn", "player_spawn", *SPAWN, point=True), obj("from_ridge", "player_spawn", *SPAWN, point=True)]
    for name, x, y, points in GROUPS:
        for i, (monster, level, count, radius) in enumerate(points):
            p = [prop("monster", "string", monster), prop("level", "int", level), prop("count", "int", count),
                 prop("radius", "float", radius)]
            spawns.append(obj(name if i == 0 else f"{name}_{i + 1}", "monster_spawn", x + (2 if i else 0), y, point=True,
                              properties=p))

    areas = [obj(name, "subarea", x, y, w, h) for name, (x, y, w, h) in SUBAREAS]
    ax0, ay0, ax1, ay1 = ARENA
    areas.append(obj("furnace_arena", "boss_arena", ax0, ay0, ax1 - ax0, ay1 - ay0,
                     properties=[prop("name", "string", "Fornalha do Capataz")]))
    transitions = [obj("exit", "transition", 0, EXIT_Y, 2, 5, properties=[
        prop("arrival", "string", "from_mines"), prop("to", "string", "zone.broken_ridge")])]

    # ---------------- conferencias
    def free(x, y):
        return 0 <= x < W and 0 <= y < H and not coll[y][x]

    seen = flood(free, (int(SPAWN[0]), int(SPAWN[1])))
    loose = [(x, y) for y in range(H) for x in range(W) if free(x, y) and (x, y) not in seen]
    assert not loose, ("bolsao", loose[:10])
    slits = [(x, y) for y in range(H) for x in range(W) if free(x, y) and not is_exit(x, y)
             and sum(free(*n) for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))) < 2]
    assert not slits, ("fresta", slits[:10])
    for o in spawns:
        cell = (int(o["x"] // 32), int(o["y"] // 32))
        assert cell in seen, ("ponto inalcancavel", o["name"], cell)
    # a Fornalha: 40 x 28 livres, sem obstaculo
    assert all(free(x, y) for y in range(ay0, ay1) for x in range(ax0, ax1)), "obstaculo na Fornalha"
    # grupos a 12 tiles ou mais uns dos outros
    pts = [(name, [(x + (2 if i else 0), y) for i in range(len(points))]) for name, x, y, points in GROUPS]
    near = []
    for i, (a, pa) in enumerate(pts):
        for b, pb in pts[i + 1:]:
            d = min(math.hypot(ax - bx, ay - by) for ax, ay in pa for bx, by in pb)
            if d < 12:
                near.append((a, b, round(d, 1)))
    assert not near, ("grupos a menos de 12 tiles", near)
    # o Capataz (aggro 40) nao ve a antessala nem as rotas: so quem esta na Fornalha ou na descida para ela
    boss = next((x, y) for name, x, y, _ in GROUPS if name == "forged_overseer")
    fx0, fy0, fx1, fy1 = FURNACE_SUBAREA
    seen_by_boss = [(x, y) for (x, y) in seen if math.hypot(x + 0.5 - boss[0], y + 0.5 - boss[1]) <= 40
                    and not (fx0 <= x < fx1 and fy0 <= y < fy1) and line_of_sight(lambda cx, cy: not free(cx, cy), boss, (x + 0.5, y + 0.5))]
    assert not seen_by_boss, ("o Capataz ve a antessala", seen_by_boss[:10])
    monsters = sum(p[2] for g in GROUPS for p in g[3])
    print(f"limpeza: {rounds} rodadas; livres {len(seen)}; grupos {len(GROUPS)}; monstros {monsters}")

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
        tl(2, "props", props),
        tl(3, "collision", coll, opacity=0.5, visible=False),
        ol(4, "spawns", spawns),
        ol(5, "areas", areas),
        ol(6, "transitions", transitions),
    ]
    assert next_gid > max(max(r) for r in props)
    tmj = {"compressionlevel": -1, "height": H, "infinite": False, "layers": layers, "nextlayerid": 7,
           "nextobjectid": oid[0], "orientation": "orthogonal", "renderorder": "right-down", "tiledversion": "1.11.2",
           "tileheight": 32, "tilesets": tilesets, "tilewidth": 32, "type": "map", "version": "1.10", "width": W}
    (MAPS / "deep_mines.tmj").write_bytes(tiledjson.dumps(tmj).encode("utf-8"))
    # a Serra, com o portao das Minas na Boca das Minas
    subprocess.run([sys.executable, str(HERE.parents[1] / "serra-partida/tools/build_ridge.py")], check=True)


if __name__ == "__main__":
    main()
