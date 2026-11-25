#!/usr/bin/env python3
"""T-531: monta data/maps/pass_fort.tmj (Forte do Passo) e abre o portao sul de data/maps/drowned_marsh.tmj.

Uso (raiz do repositorio):  python tasks/reports/forte-do-passo/tools/build_fort.py

Terreno por celula: 0 chao de terra batida, 1 muro (topo de lajes). O chao logo ao sul de um muro vira a face dele (2),
que tambem bloqueia (a licao das Ruinas). Tileset do forte da T-520 (opcao C, `pixellab_forte_0`, 25 tiles com
pattern_4x4): cada tile fica centrado no canto entre quatro celulas (camada com offset -16) e e escolhido pelo autotile do
guia do PixelLab, como em `tasks/reports/mapas-sul-tilesets/tools/build_samples.py`. Muro de 2 celulas (o topo claro de
um muro grosso parece pavimento). Os predios sao copiados da Cidade (camadas props e walls_detail), como as palafitas do
Brejo. Deterministico. O .tmj gerado e a fonte; o script fica para refazer do zero.
"""
import json
import re
import sys
from collections import deque
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "mapa-brejo/tools"))
import tiledjson  # noqa: E402

REPO = HERE.parents[3]
MAPS = REPO / "data/maps"
FORT_META = HERE.parents[1] / "mapas-sul-tilesets/tilesets/pixellab_forte_0.json"

W, H = 60, 45
FLOOR, WALL, FACE = 0, 1, 2
WALL_T = 2  # espessura do muro norte (com a face embaixo, como a muralha da Cidade)
SIDE_T = 1  # muros oeste, leste e sul: 1 celula (o topo claro de um muro grosso parece pavimento, T-520)
NORTH_GATE_X = 28  # portao norte (para o Brejo): celulas 28..31, linhas 0..1
SPAWN = (30.0, 22.5)  # nascimento da zona (e renascimento de quem morre na Serra e nas Minas, T-501)
FROM_MARSH = (30.0, 4.5)

# Predios: (nome do predio, molde na Cidade (x, y, w, h), canto de cima no Forte, deslocamento x do NPC, NPC)
# O NPC fica na celula logo abaixo do predio, na frente da porta, como na Cidade.
BUILDINGS = [
    ("Capela", (10, 7, 8, 6), (6, 6), 3.5,
     {"npc": "npc.priest", "name": "Sacerdote", "shop": "shop.temple"}),
    ("Salão dos Mestres", (22, 8, 10, 5), (43, 6), 4.5,
     {"npc": "npc.class_master", "name": "Mestre das Classes"}),
    ("Estalagem", (14, 46, 9, 6), (6, 32), 4.5,
     {"npc": "npc.innkeeper", "name": "Estalajadeiro"}),
    ("Intendência", (57, 7, 7, 6), (45, 32), 3.5,
     {"npc": "npc.quartermaster", "name": "Intendente", "shop": "shop.pass_fort"}),
]
# Quarteis (sem NPC): o Armazem e o Mercado da Cidade. (nome, molde (x, y, w, h), canto de cima no Forte)
BARRACKS = [("Quartel Oeste", (36, 47, 8, 5), (4, 20)), ("Quartel Leste", (56, 46, 8, 6), (48, 19))]


# ---------------------------------------------------------------- terreno
def build_cells():
    g = [[FLOOR] * W for _ in range(H)]
    for y in range(H):
        for x in range(W):
            if x < SIDE_T or x >= W - SIDE_T or y < WALL_T or y >= H - SIDE_T:
                g[y][x] = WALL
    # portao norte aberto; o leste (Serra Partida, T-532) e o sul (Planalto, fase 6) ficam fechados: muro inteiro.
    for y in range(WALL_T):
        for x in range(NORTH_GATE_X, NORTH_GATE_X + 4):
            g[y][x] = FLOOR
    return g


def derive(cells):
    out = [row[:] for row in cells]
    for y in range(1, H):
        for x in range(W):
            if cells[y][x] == FLOOR and cells[y - 1][x] == WALL:
                out[y][x] = FACE
    return out


class TallTileset:
    """Autotile por pattern_4x4 (guia do PixelLab), como em build_samples.py."""

    def __init__(self, meta_path, cols=4):
        self.tiles = json.load(open(meta_path, encoding="utf-8"))["tileset_data"]["tiles"]
        self.cols = cols
        self.firstgid = 0

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


def paint(cells3, ts):
    def cl(a, hi):
        return min(max(a, 0), hi)

    v = [[cells3[cl(i - 1, H - 1)][cl(j - 1, W - 1)] for j in range(W + 1)] for i in range(H + 1)]
    return [[ts.pick(v, r, c) for c in range(W)] for r in range(H)]


# ---------------------------------------------------------------- moldes da Cidade
CITY = json.load(open(MAPS / "city.tmj", encoding="utf-8"))
CW = CITY["width"]
CL = {l["name"]: l["data"] for l in CITY["layers"] if l["type"] == "tilelayer"}


def city_at(layer, x, y):
    return CL[layer][y * CW + x]


def tilecount(source):
    return int(re.search(r'tilecount="(\d+)"', (MAPS / source).read_text(encoding="utf-8")).group(1))


# ---------------------------------------------------------------- montagem
def main():
    cells = build_cells()
    cells3 = derive(cells)

    tilesets = [dict(t) for t in CITY["tilesets"]]  # mesmos firstgid da Cidade para os moldes copiados
    last = tilesets[-1]
    fort = TallTileset(FORT_META)
    fort.firstgid = last["firstgid"] + tilecount(last["source"])
    tilesets.append({"firstgid": fort.firstgid, "source": "tilesets/pixellab_forte_0.tsx"})

    terrain = paint(cells3, fort)
    coll = [[1 if cells3[y][x] in (WALL, FACE) else 0 for x in range(W)] for y in range(H)]
    props = [[0] * W for _ in range(H)]
    detail = [[0] * W for _ in range(H)]

    areas = []
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

    for building, (tx, ty, tw, th), (fx, fy), dx, npc in BUILDINGS:
        for yy in range(th):
            for xx in range(tw):
                props[fy + yy][fx + xx] = city_at("props", tx + xx, ty + yy)
                detail[fy + yy][fx + xx] = city_at("walls_detail", tx + xx, ty + yy)
                assert cells3[fy + yy][fx + xx] == FLOOR, (building, fx + xx, fy + yy)
                coll[fy + yy][fx + xx] = 1
        p = [{"name": "building", "type": "string", "value": building}]
        p += [{"name": k, "type": "string", "value": v} for k, v in sorted(npc.items())]
        areas.append((building, fx + dx, fy + th, p))
    for building, (tx, ty, tw, th), (fx, fy) in BARRACKS:
        for yy in range(th):
            for xx in range(tw):
                props[fy + yy][fx + xx] = city_at("props", tx + xx, ty + yy)
                detail[fy + yy][fx + xx] = city_at("walls_detail", tx + xx, ty + yy)
                assert cells3[fy + yy][fx + xx] == FLOOR, (building, fx + xx, fy + yy)
                coll[fy + yy][fx + xx] = 1

    spawns = [obj("spawn", "player_spawn", *SPAWN, point=True), obj("from_marsh", "player_spawn", *FROM_MARSH, point=True)]
    area_objs = [obj(name, "npc", x, y, 1, 1, properties=p) for name, x, y, p in areas]
    transitions = [obj("north_gate", "transition", NORTH_GATE_X, 0, 4, WALL_T, properties=[
        {"name": "arrival", "type": "string", "value": "from_fort"},
        {"name": "to", "type": "string", "value": "zone.drowned_marsh"}])]

    # conferencias: tudo livre alcancavel do nascimento, nenhuma fresta sem saida
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
    assert not loose, loose[:10]
    for _, x, y, _ in areas:
        assert (int(x), int(y)) in seen, (x, y)

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
        tl(3, "walls_detail", detail),
        tl(4, "collision", coll, opacity=0.5, visible=False),
        ol(5, "spawns", spawns),
        ol(6, "areas", area_objs),
        ol(7, "transitions", transitions),
    ]
    tmj = {"compressionlevel": -1, "height": H, "infinite": False, "layers": layers, "nextlayerid": 8,
           "nextobjectid": oid[0], "orientation": "orthogonal", "renderorder": "right-down", "tiledversion": "1.11.2",
           "tileheight": 32, "tilesets": tilesets, "tilewidth": 32, "type": "map", "version": "1.10", "width": W}
    (MAPS / "pass_fort.tmj").write_bytes(tiledjson.dumps(tmj).encode("utf-8"))
    print("celulas livres:", len(seen), "bloqueadas:", sum(map(sum, coll)))
    open_marsh_south_gate()


MARSH_GATE_X = 99  # portao sul do Brejo: celulas 99..102, linhas 158..159 (fim do aterro, como o portao norte)


def open_marsh_south_gate():
    """Poe o portao sul do Brejo (para o Forte) no fim do aterro, na borda sul, com a chegada from_fort."""
    marsh = json.load(open(MAPS / "drowned_marsh.tmj", encoding="utf-8"))
    w, h = marsh["width"], marsh["height"]
    lay = {l["name"]: l for l in marsh["layers"]}
    coll = lay["collision"]["data"]
    for y in (h - 2, h - 1):
        for x in range(MARSH_GATE_X, MARSH_GATE_X + 4):
            assert coll[y * w + x] == 0, ("portao sul do Brejo bloqueado", x, y)
    sp = lay["spawns"]["objects"]
    tr = lay["transitions"]["objects"]
    nid = marsh["nextobjectid"]
    if not any(o["name"] == "from_fort" for o in sp):
        assert coll[154 * w + 101] == 0
        sp.insert(2, {"id": nid, "name": "from_fort", "type": "player_spawn", "x": 101.0 * 32, "y": 154.5 * 32,
                      "width": 0, "height": 0, "rotation": 0, "visible": True, "point": True})
        nid += 1
    if not any(o["name"] == "south_gate" for o in tr):
        tr.append({"id": nid, "name": "south_gate", "type": "transition", "x": MARSH_GATE_X * 32, "y": (h - 2) * 32,
                   "width": 128, "height": 64, "rotation": 0, "visible": True, "properties": [
                       {"name": "to", "type": "string", "value": "zone.pass_fort"},
                       {"name": "arrival", "type": "string", "value": "from_marsh"}]})
        nid += 1
    marsh["nextobjectid"] = nid
    (MAPS / "drowned_marsh.tmj").write_bytes(tiledjson.dumps(marsh).encode("utf-8"))


if __name__ == "__main__":
    main()
