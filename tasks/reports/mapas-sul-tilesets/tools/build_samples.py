#!/usr/bin/env python3
"""T-520: monta as amostras dos tilesets do sul (brejo, serra, mina, forte).

Entrada (pasta --src): <nome>.png + <nome>.json de cada tileset do PixelLab (GET /mcp/tilesets/{id}/image e /metadata),
mais reed1.png, reed2.png e rail_h.png (props de 32 px).
Saida: imagens em client/assets/art/tiles/, .tsx em data/maps/tilesets/, mapas data/maps/sample_<bioma>_<opcao>.tmj.

Mesmo desenho do mapa para as 3 opcoes de cada bioma; so o tileset muda. O terreno e por celula; cada tile do PixelLab
fica centrado no canto entre quatro celulas (camada com offset -16, como nas Ruinas). Conjuntos de 25 tiles (parede com
face) seguem o autotile por pattern_4x4 do guia do PixelLab: o chao logo ao sul de uma parede vira a face dela.
"""
import argparse
import json
from pathlib import Path

from PIL import Image

W, H = 30, 20
REPO = Path(__file__).resolve().parents[4]
TILES = REPO / "client/assets/art/tiles"
TSX = REPO / "data/maps/tilesets"
MAPS = REPO / "data/maps"

# bioma -> opcao -> tilesets (nomes na pasta --src). No brejo sao dois (lama-rasa e rasa-funda).
OPTIONS = {
    "brejo": {"a": ["brejo_a1", "brejo_a2"], "b": ["brejo_b1", "brejo_b2"], "c": ["brejo_c1", "brejo_c2"]},
    "serra": {"a": ["serra_a"], "b": ["serra_b"], "c": ["serra_c"]},
    "mina": {"a": ["mina_a"], "b": ["mina_b"], "c": ["mina_c"]},
    "forte": {"a": ["forte_a"], "b": ["forte_b"], "c": ["forte_c"]},
}
# forte A e a muralha da Cidade, ja no repositorio (pixellab_city_2): so o .tsx existente e reaproveitado.
REUSE = {"forte_a": "pixellab_city_2"}


# ---------------------------------------------------------------- layouts (terreno por celula)
def blank(v):
    return [[v] * W for _ in range(H)]


def rect(g, x0, y0, x1, y1, v):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            g[y][x] = v


def ellipse(g, cx, cy, rx, ry, v):
    for y in range(H):
        for x in range(W):
            if ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1:
                g[y][x] = v


def layout_brejo():
    g = blank(0)  # 0 lama, 1 agua rasa, 2 agua funda
    ellipse(g, 19, 10, 9.6, 6.6, 1)
    ellipse(g, 20, 10, 5.2, 3.4, 2)
    ellipse(g, 6, 4, 3.4, 2.4, 1)
    rect(g, 0, 16, 13, 17, 1)  # riacho raso que vai ate a lagoa
    rect(g, 12, 15, 15, 17, 1)
    return g


def layout_serra():
    g = blank(0)  # 0 chao, 1 plato
    rect(g, 0, 0, 11, 4, 1)
    rect(g, 17, 0, 29, 4, 1)
    rect(g, 5, 10, 11, 13, 1)
    rect(g, 20, 11, 26, 14, 1)
    return g


def layout_mina():
    g = blank(1)  # 0 piso, 1 parede
    rect(g, 3, 4, 26, 15, 0)
    rect(g, 13, 0, 15, 3, 0)
    rect(g, 12, 8, 15, 10, 1)
    return g


def layout_forte():
    g = blank(0)  # 0 terra batida, 1 muralha
    rect(g, 5, 3, 24, 15, 1)
    rect(g, 8, 6, 21, 12, 0)
    rect(g, 13, 13, 15, 15, 0)
    return g


LAYOUTS = {"brejo": layout_brejo, "serra": layout_serra, "mina": layout_mina, "forte": layout_forte}


# ---------------------------------------------------------------- tilesets
class Tileset:
    def __init__(self, src, name):
        self.name = name
        self.meta = json.load(open(src / f"{name}.json", encoding="utf-8"))
        self.image = Image.open(src / f"{name}.png").convert("RGBA")
        self.tiles = self.meta["tileset_data"]["tiles"]
        self.tall = len(self.tiles) > 16
        self.cols = self.image.width // 32
        self.count = (self.image.height // 32) * self.cols
        self.firstgid = 0
        self.install_name = None

    def local_id(self, tile):
        b = tile["bounding_box"]
        return (b["y"] // 32) * self.cols + b["x"] // 32

    def by_corners(self, nw, ne, sw, se):
        for t in self.tiles:
            c = t["corners"]
            if (c["NW"], c["NE"], c["SW"], c["SE"]) == (nw, ne, sw, se):
                return t
        raise KeyError((nw, ne, sw, se))

    def pick_tall(self, v, r, c):
        """Autotile do guia do PixelLab: o pattern_4x4 que mais combina com os vertices em volta da celula."""
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
        return best


def derive_cells(cells):
    """Marca como face (2) o chao logo ao sul de cada celula de parede (1)."""
    out = [row[:] for row in cells]
    for y in range(1, H):
        for x in range(W):
            if cells[y][x] == 0 and cells[y - 1][x] == 1:
                out[y][x] = 2
    return out


def vertices(cells):
    """v[i][j] = celula (i-1, j-1), com a borda repetida: o tile da posicao (r, c) tem os cantos nas 4 celulas em volta."""
    def cl(a, hi):
        return min(max(a, 0), hi)
    return [[cells[cl(i - 1, H - 1)][cl(j - 1, W - 1)] for j in range(W + 1)] for i in range(H + 1)]


def paint_terrain(cells, sets, chain):
    """Devolve a matriz de gids (H x W) da camada de terreno."""
    out = [[0] * W for _ in range(H)]
    if chain:  # brejo: lama(0) - rasa(1) - funda(2), dois conjuntos de 16 tiles
        s1, s2 = sets
        v = vertices(cells)
        for r in range(H):
            for c in range(W):
                n = [v[r][c], v[r][c + 1], v[r + 1][c], v[r + 1][c + 1]]
                if max(n) <= 1:
                    ts, side = s1, ["upper" if t == 1 else "lower" for t in n]
                elif min(n) >= 1:
                    ts, side = s2, ["upper" if t == 2 else "lower" for t in n]
                else:
                    raise ValueError(f"lama encostada em agua funda em ({r},{c})")
                out[r][c] = ts.firstgid + ts.local_id(ts.by_corners(*side))
        return out
    ts = sets[0]
    if not ts.tall:
        v = vertices(cells)
        for r in range(H):
            for c in range(W):
                n = (v[r][c], v[r][c + 1], v[r + 1][c], v[r + 1][c + 1])
                out[r][c] = ts.firstgid + ts.local_id(ts.by_corners(*["upper" if t == 1 else "lower" for t in n]))
        return out
    v = vertices(derive_cells(cells))
    for r in range(H):
        for c in range(W):
            out[r][c] = ts.firstgid + ts.local_id(ts.pick_tall(v, r, c))
    return out


# ---------------------------------------------------------------- saida
def tsx_text(name, image_name, img):
    cols, rows = img.width // 32, img.height // 32
    return (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<tileset version="1.10" tiledversion="1.10.2" name="{name}" tilewidth="32" tileheight="32" '
        f'tilecount="{cols * rows}" columns="{cols}">\n'
        f' <image source="../../../client/assets/art/tiles/{image_name}.png" width="{img.width}" height="{img.height}"/>\n'
        '</tileset>\n'
    )


def tile_layer(lid, name, data, **extra):
    d = {"data": [x for row in data for x in row], "height": H, "id": lid, "name": name, "opacity": 1,
         "type": "tilelayer", "visible": True, "width": W, "x": 0, "y": 0}
    d.update(extra)
    return d


def object_layer(lid, name):
    return {"draworder": "topdown", "id": lid, "name": name, "objects": [], "opacity": 1, "type": "objectgroup",
            "visible": True, "x": 0, "y": 0}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", required=True, type=Path)
    a = ap.parse_args()

    # props: 4 tiles em fila (junco 1, junco 2, trilho horizontal, trilho vertical)
    props = Image.new("RGBA", (128, 32))
    r1, r2, rh = (Image.open(a.src / f"{n}.png").convert("RGBA") for n in ("reed1", "reed2", "rail_h"))
    for k, im in enumerate((r1, r2, rh, rh.rotate(90))):
        props.paste(im, (k * 32, 0))
    props.save(TILES / "pixellab_sul_props.png")
    (TSX / "pixellab_sul_props.tsx").write_text(tsx_text("pixellab_sul_props", "pixellab_sul_props", props), encoding="utf-8")
    prop_gid = {"reed1": 0, "reed2": 1, "rail_h": 2, "rail_v": 3}  # + firstgid

    for biome, opts in OPTIONS.items():
        cells = LAYOUTS[biome]()
        for opt, names in opts.items():
            sets = []
            for n in names:
                ts = Tileset(a.src, n)
                if n in REUSE:
                    ts.install_name = REUSE[n]
                else:
                    ts.install_name = f"pixellab_sul_{n}"
                    ts.image.save(TILES / f"{ts.install_name}.png")
                    (TSX / f"{ts.install_name}.tsx").write_text(
                        tsx_text(ts.install_name, ts.install_name, ts.image), encoding="utf-8")
                sets.append(ts)
            gid = 2  # 1 = collision
            tilesets = [{"firstgid": 1, "source": "tilesets/collision.tsx"}]
            for ts in sets:
                ts.firstgid = gid
                tilesets.append({"firstgid": gid, "source": f"tilesets/{ts.install_name}.tsx"})
                gid += ts.count
            props_first = gid
            tilesets.append({"firstgid": props_first, "source": "tilesets/pixellab_sul_props.tsx"})

            terrain = paint_terrain(cells, sets, chain=len(sets) == 2)

            deco = [[0] * W for _ in range(H)]
            coll = [[0] * W for _ in range(H)]
            if biome == "brejo":
                spots = [(3, 8), (5, 14), (9, 8), (10, 13), (2, 12), (8, 18), (12, 5), (12, 10), (14, 14), (1, 17),
                         (26, 3), (27, 17)]
                for k, (x, y) in enumerate(spots):
                    if cells[y][x] == 0:
                        deco[y][x] = props_first + prop_gid["reed1" if k % 2 else "reed2"]
            if biome == "mina":
                for x in range(4, 26):
                    deco[12][x] = props_first + prop_gid["rail_h"]
                for y in range(0, 4):
                    deco[y][14] = props_first + prop_gid["rail_v"]
            if biome in ("serra", "mina", "forte"):
                for y, row in enumerate(derive_cells(cells)):
                    for x, t in enumerate(row):
                        coll[y][x] = 1 if t in (1, 2) else 0

            layers = [
                tile_layer(1, "terrain_0", terrain, offsetx=-16, offsety=-16),
                tile_layer(2, "decoration", deco),
                tile_layer(3, "collision", coll, opacity=0.5, visible=False),
                object_layer(4, "spawns"),
                object_layer(5, "areas"),
            ]
            tmj = {"compressionlevel": -1, "height": H, "infinite": False, "layers": layers, "nextlayerid": 6,
                   "nextobjectid": 1, "orientation": "orthogonal", "renderorder": "right-down",
                   "tiledversion": "1.11.2", "tileheight": 32, "tilesets": tilesets, "tilewidth": 32,
                   "type": "map", "version": "1.10", "width": W}
            (MAPS / f"sample_{biome}_{opt}.tmj").write_text(json.dumps(tmj, indent=1) + "\n", encoding="utf-8")
            print("ok", biome, opt)


if __name__ == "__main__":
    main()
