"""Converte um tileset Wang do PixelLab (PNG + metadata JSON) num tileset do Tiled (.tsx) com wangset de canto.

Uso:
  python tileset_to_tiled.py <sheet.png> <metadata.json> <saida.tsx> <caminho-da-imagem-relativo-ao-tsx> [nome] [nome-lower] [nome-upper]

O PixelLab entrega a folha (4x4 tiles no modo de 16 tiles) e, por tile, os cantos NW/NE/SW/SE
("lower"/"upper") e o retângulo na folha (bounding_box). O Tiled numera os tiles por posição na folha
(id = linha * colunas + coluna) e, num wangset de canto, guarda 8 valores em wangid:
topo, canto sup. dir., direita, canto inf. dir., baixo, canto inf. esq., esquerda, canto sup. esq.
Só os cantos (índices 1, 3, 5, 7) são usados. Cor 1 = terreno de baixo (lower), cor 2 = de cima (upper).
"""
import json
import sys
from xml.sax.saxutils import quoteattr

from PIL import Image


def main() -> None:
    sheet, meta_path, out, image_rel = sys.argv[1:5]
    name = sys.argv[5] if len(sys.argv) > 5 else "pixellab_terrain"
    meta = json.load(open(meta_path, encoding="utf-8"))
    tw, th = meta["tile_size"]["width"], meta["tile_size"]["height"]
    width, height = Image.open(sheet).size
    columns = width // tw
    count = columns * (height // th)
    color = {"lower": 1, "upper": 2}
    lower_name = sys.argv[6] if len(sys.argv) > 6 else "lower"
    upper_name = sys.argv[7] if len(sys.argv) > 7 else "upper"

    tiles = []
    for tile in meta["tileset_data"]["tiles"]:
        box = tile["bounding_box"]
        tile_id = (box["y"] // th) * columns + box["x"] // tw
        c = tile["corners"]
        wangid = [0, color[c["NE"]], 0, color[c["SE"]], 0, color[c["SW"]], 0, color[c["NW"]]]
        tiles.append((tile_id, wangid))
    tiles.sort()

    # Tile que é só "upper" (ou só "lower") serve de ícone de cada cor.
    full = {w[1]: i for i, w in tiles if len(set(w[1::2])) == 1}
    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<tileset version="1.10" tiledversion="1.10.2" name="{name}" tilewidth="{tw}" tileheight="{th}" tilecount="{count}" columns="{columns}">',
        f' <image source={quoteattr(image_rel)} width="{width}" height="{height}"/>',
        " <wangsets>",
        f'  <wangset name="{name}" type="corner" tile="{full.get(2, -1)}">',
        f'   <wangcolor name={quoteattr(lower_name)} color="#8b5a2b" tile="{full.get(1, -1)}" probability="1"/>',
        f'   <wangcolor name={quoteattr(upper_name)} color="#4caf50" tile="{full.get(2, -1)}" probability="1"/>',
    ]
    for tile_id, wangid in tiles:
        lines.append(f'   <wangtile tileid="{tile_id}" wangid="{",".join(map(str, wangid))}"/>')
    lines += ["  </wangset>", " </wangsets>", "</tileset>", ""]
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
    print(f"{out}: {len(tiles)} tiles com cantos, {count} na folha")


if __name__ == "__main__":
    main()
