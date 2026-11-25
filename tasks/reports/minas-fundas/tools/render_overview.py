#!/usr/bin/env python3
"""T-533: desenha data/maps/deep_mines.tmj inteiro (render_tmj.py) com os grupos marcados.

Uso (raiz do repositorio):  python tasks/reports/minas-fundas/tools/render_overview.py <saida.png>

Vermelho = Mineiro Possuido, azul = Lobo da Montanha, roxo = Morcego das Cavernas, branco = Golem de Pedra, laranja = Capataz
(o tamanho do ponto cresce com a quantidade); verde = chegada; amarelo = saida e Fornalha; retangulos finos = subareas.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / "mapa-brejo/tools"))
import render_tmj  # noqa: E402

REPO = HERE.parents[3]
TMJ = REPO / "data/maps/deep_mines.tmj"
COLORS = {"monster.possessed_miner": (220, 60, 50), "monster.mountain_wolf": (60, 120, 230), "monster.cave_bat": (170, 80, 220),
          "monster.stone_golem": (255, 255, 255), "monster.forged_overseer": (255, 150, 30)}
SCALE = 0.4


def main(out):
    render_tmj.render(str(TMJ), out, scale=SCALE)
    img = Image.open(out).convert("RGB")
    d = ImageDraw.Draw(img)
    m = json.load(open(TMJ, encoding="utf-8"))
    px = 32 * SCALE
    for layer in m["layers"]:
        for o in layer.get("objects", []):
            x, y = o["x"] / 32 * px, o["y"] / 32 * px
            props = {p["name"]: p["value"] for p in o.get("properties", [])}
            if o["type"] == "monster_spawn":
                r = 3 + 1.3 * props["count"]
                d.ellipse((x - r, y - r, x + r, y + r), fill=COLORS[props["monster"]], outline=(0, 0, 0))
            elif o["type"] == "player_spawn":
                d.ellipse((x - 6, y - 6, x + 6, y + 6), fill=(60, 220, 90), outline=(0, 0, 0))
            elif o["type"] in ("transition", "boss_arena"):
                d.rectangle((x, y, x + o["width"] / 32 * px, y + o["height"] / 32 * px), outline=(255, 220, 0), width=3)
            elif o["type"] == "subarea":
                d.rectangle((x, y, x + o["width"] / 32 * px, y + o["height"] / 32 * px), outline=(200, 200, 200), width=1)
                d.text((x + 4, y + 4), o["name"], fill=(235, 235, 235))
    img.save(out)


if __name__ == "__main__":
    main(sys.argv[1])
