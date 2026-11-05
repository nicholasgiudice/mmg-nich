"""Monta a folha de sprites de um personagem do PixelLab no formato que o cliente lê (layout de grade do sprites.json).

Uso:
  python build_sheet.py <download.zip | pasta extraída> <saida.png> <saida-layout.json> anim[=pasta][:fps[:loop]] ...

Exemplo (Guerreiro):
  python build_sheet.py warrior.zip ../characters/warrior.png ../characters/warrior.layout.json \
      idle:6:loop walk:10:loop attack:14 death:10

O ZIP é o de `GET https://api.pixellab.ai/mcp/characters/<id>/download` (sem autenticação; o id é a chave):
`<estado>/animations/<nome>/<direção>/frame_NNN.png` e `<estado>/rotations/<direção>.png`.

Cada animação vira 4 linhas, na ordem up, left, down, right (north, west, south, east do PixelLab), e cada quadro
uma coluna. O PixelLab guarda cada animação num quadro do tamanho que ela precisou (o golpe cresce: 64 → 84 px),
sempre com o personagem centrado, então todos os quadros são centrados numa célula única (a maior). Nada é
redimensionado. O pivô (ponto entre os pés) e o headPx saem das rotações das 4 direções.

O JSON de saída é o layout pronto para colar em `layouts` do sprites.json.
"""
import json
import sys
import zipfile
from pathlib import Path

from PIL import Image

DIRECTIONS = [("up", "north"), ("left", "west"), ("down", "south"), ("right", "east")]


def open_source(src: str) -> tuple[dict, callable]:
    path = Path(src)
    if path.is_dir():
        meta = json.loads((path / "metadata.json").read_text(encoding="utf-8"))
        return meta, lambda name: Image.open(path / name).convert("RGBA")
    archive = zipfile.ZipFile(path)
    meta = json.loads(archive.read("metadata.json"))
    return meta, lambda name: Image.open(archive.open(name)).convert("RGBA")


def parse_spec(spec: str) -> tuple[str, str, float, bool]:
    parts = spec.split(":")
    name, _, folder = parts[0].partition("=")
    fps = float(parts[1]) if len(parts) > 1 and parts[1] else 10.0
    loop = len(parts) > 2 and parts[2] == "loop"
    return name, folder or name, fps, loop


def main() -> None:
    src, out_png, out_json, *specs = sys.argv[1:]
    meta, load = open_source(src)
    state = meta["states"][0]
    frames = state["frames"]
    anims = [parse_spec(s) for s in specs]

    # Quadros por animação e direção.
    table = {}
    for name, folder, _, _ in anims:
        if folder not in frames["animations"]:
            sys.exit(f"animação '{folder}' não existe; há: {sorted(frames['animations'])}")
        for ours, theirs in DIRECTIONS:
            files = frames["animations"][folder].get(theirs)
            if not files:
                sys.exit(f"animação '{folder}' sem a direção {theirs}")
            table[(name, ours)] = [load(f) for f in files]

    cell = max(max(im.width, im.height) for ims in table.values() for im in ims)
    columns = max(len(ims) for ims in table.values())
    rows = len(anims) * len(DIRECTIONS)
    sheet = Image.new("RGBA", (cell * columns, cell * rows), (0, 0, 0, 0))
    layout_anims = {}
    for a, (name, _, fps, loop) in enumerate(anims):
        row0 = a * len(DIRECTIONS)
        counts = set()
        for d, (ours, _) in enumerate(DIRECTIONS):
            ims = table[(name, ours)]
            counts.add(len(ims))
            for c, im in enumerate(ims):
                x = c * cell + (cell - im.width) // 2
                y = (row0 + d) * cell + (cell - im.height) // 2
                sheet.alpha_composite(im, (x, y))
        if len(counts) != 1:
            print(f"aviso: '{name}' tem quadros diferentes por direção {sorted(counts)}; usando o menor", file=sys.stderr)
        anim = {"grid": {"row": row0, "col": 0, "count": min(counts)}, "fps": fps}
        if loop:
            anim["loop"] = True
        layout_anims[name] = anim

    # Pivô: média da linha dos pés nas 4 rotações. Num quadrúpede de frente os pés da frente ficam mais baixos
    # que os de lado; a média põe o círculo de colisão sob o meio do corpo em todas as direções.
    boxes = []
    for _, theirs in DIRECTIONS:
        rotation = load(frames["rotations"][theirs])
        offset = (cell - rotation.height) // 2
        _, top, _, bottom = rotation.getbbox()
        boxes.append((offset + top, offset + bottom - 1))
    feet = round(sum(b for _, b in boxes) / len(boxes))
    head = min(t for t, _ in boxes)
    pivot = [cell // 2, feet]
    layout = {
        "frame": [cell, cell],
        "pivot": pivot,
        "headPx": feet - head,
        "dirOrder": [d for d, _ in DIRECTIONS],
        "animations": layout_anims,
    }
    Path(out_png).parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_png)
    Path(out_json).write_text(json.dumps(layout, indent=2) + "\n", encoding="utf-8")
    print(f"{out_png}: {sheet.width}x{sheet.height}, célula {cell}, pivô {pivot}, altura {feet - head} px")


if __name__ == "__main__":
    main()
