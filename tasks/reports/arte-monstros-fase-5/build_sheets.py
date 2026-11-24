"""Monta as folhas derivadas dos pacotes do OpenGameArt (T-521).

Uso (as pastas de origem são os downloads, cada um na sua pasta):
    python -I build_sheets.py <golem_dir> <bat_png> <lpc_base_snake_png> <saida_monsters>

- stone_golem.png: [LPC] Golem (Redshrike) com andar, ataque e morte empilhados numa folha só.
- cave_bat.png: [LPC] Bat combined/extended, sem alteração (só renomeado).
- marsh_serpent.png: cobra das LPC Base Assets (Charles Sanchez) com uma linha de morte nova
  (4 quadros, o quadro "de frente" achatado e esmaecendo), porque o pacote só tem o andar.
"""
import os
import shutil
import sys

from PIL import Image

golem_dir, bat_png, snake_png, out = sys.argv[1:5]

# --- Golem: andar 448x256 (4 linhas de 64), ataque 448x384 (4 linhas de 96), morte 448x128 (2 linhas de 64x128)
walk = Image.open(os.path.join(golem_dir, "golem-walk.png")).convert("RGBA")
atk = Image.open(os.path.join(golem_dir, "golem-atk.png")).convert("RGBA")
die = Image.open(os.path.join(golem_dir, "golem-die.png")).convert("RGBA")
sheet = Image.new("RGBA", (448, 256 + 384 + 128), (0, 0, 0, 0))
sheet.paste(walk, (0, 0))
sheet.paste(atk, (0, 256))
sheet.paste(die, (0, 640))
os.makedirs(os.path.join(out, "stone_golem"), exist_ok=True)
sheet.save(os.path.join(out, "stone_golem", "stone_golem.png"))

# --- Morcego: sem alteração
os.makedirs(os.path.join(out, "cave_bat"), exist_ok=True)
shutil.copyfile(bat_png, os.path.join(out, "cave_bat", "cave_bat.png"))

# --- Serpente: 96x128 (3 colunas x 4 linhas de 32; ordem up, left, down, right) + linha de morte
snake = Image.open(snake_png).convert("RGBA")
sheet = Image.new("RGBA", (128, 160), (0, 0, 0, 0))
sheet.paste(snake, (0, 0))
base = snake.crop((0, 64, 32, 96))  # linha "down", primeiro quadro
bbox = base.getbbox()
body = base.crop(bbox)
steps = [(1.0, 0.8, 1.0), (0.75, 1.15, 0.85), (0.5, 1.3, 0.6), (0.3, 1.4, 0.35)]  # (altura, largura, opacidade)
for i, (sy, sx, alpha) in enumerate(steps):
    w, h = max(1, round(body.width * sx)), max(1, round(body.height * sy))
    frame = body.resize((w, h), Image.NEAREST)
    r, g, b, a = frame.split()
    dark = 1.0 - 0.15 * i
    frame = Image.merge("RGBA", (r.point(lambda v: int(v * dark)), g.point(lambda v: int(v * dark)),
                                  b.point(lambda v: int(v * dark)), a.point(lambda v: int(v * alpha))))
    cell = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    # os pés ficam na mesma linha do quadro de origem (base do corpo)
    x = (32 - w) // 2
    y = bbox[3] - h
    cell.alpha_composite(frame, (max(0, x), max(0, y)))
    sheet.paste(cell, (i * 32, 128))
os.makedirs(os.path.join(out, "marsh_serpent"), exist_ok=True)
sheet.save(os.path.join(out, "marsh_serpent", "marsh_serpent.png"))
print("ok")
