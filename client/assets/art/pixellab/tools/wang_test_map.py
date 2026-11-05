"""Monta um mapa de teste (.tmj) escolhendo cada tile pelos cantos do wangset, como o pincel de terreno do Tiled.

Uso: python wang_test_map.py <tileset.tsx> <saida.tmj> [largura] [altura] [seed]

Gera um campo de cantos (grama com manchas de terra), escolhe para cada célula o tile cujos 4 cantos batem
e grava o mapa. Renderizar com `tmxrasterizer` mostra se as emendas casam.
"""
import json
import os
import random
import sys
import xml.etree.ElementTree as ET


def main() -> None:
    tsx, out = sys.argv[1:3]
    w = int(sys.argv[3]) if len(sys.argv) > 3 else 20
    h = int(sys.argv[4]) if len(sys.argv) > 4 else 12
    rng = random.Random(int(sys.argv[5]) if len(sys.argv) > 5 else 90)
    root = ET.parse(tsx).getroot()
    by_corners = {}
    for t in root.iter("wangtile"):
        ids = [int(v) for v in t.get("wangid").split(",")]
        # (NW, NE, SW, SE)
        by_corners[(ids[7], ids[1], ids[5], ids[3])] = int(t.get("tileid"))
    missing = [(a, b, c, d) for a in (1, 2) for b in (1, 2) for c in (1, 2) for d in (1, 2) if (a, b, c, d) not in by_corners]
    if missing:
        sys.exit(f"faltam combinações de canto: {missing}")

    # Cantos: grama (2) com manchas e um caminho de terra (1).
    corners = [[2] * (w + 1) for _ in range(h + 1)]
    for _ in range(6):
        cx, cy, r = rng.randrange(w + 1), rng.randrange(h + 1), rng.randint(1, 3)
        for y in range(h + 1):
            for x in range(w + 1):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r:
                    corners[y][x] = 1
    path_y = h // 2
    for x in range(w + 1):
        corners[path_y][x] = corners[path_y + 1][x] = 1
        if rng.random() < 0.3:
            path_y = max(1, min(h - 2, path_y + rng.choice((-1, 1))))
            corners[path_y][x] = corners[path_y + 1][x] = 1

    data = [by_corners[(corners[y][x], corners[y][x + 1], corners[y + 1][x], corners[y + 1][x + 1])] + 1
            for y in range(h) for x in range(w)]
    tile = int(root.get("tilewidth"))
    rel = os.path.relpath(os.path.abspath(tsx), os.path.dirname(os.path.abspath(out))).replace("\\", "/")
    tmj = {
        "type": "map", "version": "1.10", "tiledversion": "1.10.2", "orientation": "orthogonal",
        "renderorder": "right-down", "width": w, "height": h, "tilewidth": tile, "tileheight": tile,
        "infinite": False, "nextlayerid": 2, "nextobjectid": 1,
        "tilesets": [{"firstgid": 1, "source": rel}],
        "layers": [{"id": 1, "name": "ground", "type": "tilelayer", "width": w, "height": h, "x": 0, "y": 0,
                    "opacity": 1, "visible": True, "data": data}],
    }
    json.dump(tmj, open(out, "w", encoding="utf-8"), indent=1)
    print(f"{out}: {w}x{h}")


if __name__ == "__main__":
    main()
