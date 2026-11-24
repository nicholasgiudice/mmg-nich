"""Prancha de amostra dos monstros do sul (T-521), montada a partir do sprites.json, como o cliente lê.

Uso: python -I board.py <raiz do repositório> <pasta de saída>
Gera amostra-fase5.png (quadros estáticos) e amostra-fase5.gif (todos em movimento: andar nas 4 direções,
golpe e morte) com a grama do jogo ao fundo. Aplica 'scale' e 'tint' como o cliente.
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageFont

root, out = sys.argv[1:3]
art = os.path.join(root, "client", "assets", "art")
cfg = json.load(open(os.path.join(art, "sprites.json"), encoding="utf8"))
DIRS = ["up", "left", "down", "right"]
ZOOM = 2

IDS = [
    ("monster.drowned", "Afogado"),
    ("monster.marsh_lizardman", "Homem-Lagarto"),
    ("monster.marsh_serpent", "Serpente do Brejo"),
    ("monster.marsh_witch", "Bruxa do Brejo (chefe, x2)"),
    ("monster.possessed_miner", "Mineiro Possuído"),
    ("monster.mountain_wolf", "Lobo da Serra"),
    ("monster.cave_bat", "Morcego da Caverna"),
    ("monster.stone_golem", "Golem de Pedra"),
    ("monster.forged_overseer", "Capataz Forjado"),
    ("monster.grey_wolf", "Lobo Cinzento (referência)"),
]


def pair(v, fb):
    return tuple(v) if v else fb


def strips(layout, anim):
    frame = pair(layout["frame"], (64, 64))
    pivot = pair(layout.get("pivot"), (frame[0] // 2, frame[1] // 2))
    order = layout.get("dirOrder", DIRS)
    a = layout["animations"][anim]
    if "frame" in a:
        frame = tuple(a["frame"])
        pivot = tuple(a["pivot"]) if "pivot" in a else (frame[0] // 2, frame[1] // 2)
    res = {}
    if "grid" in a:
        g = a["grid"]
        for i, d in enumerate(order):
            res[d] = (g["col"] * frame[0], (g["row"] + i) * frame[1], frame[0], frame[1], g["count"], pivot)
    elif "all" in a:
        s = a["all"]
        st = (s["x"], s["y"], s.get("w", frame[0]), s.get("h", frame[1]), s.get("count", 1), tuple(s["pivot"]) if "pivot" in s else pivot)
        for d in DIRS:
            res[d] = st
    else:
        for d in DIRS:
            if d in a:
                s = a[d]
                res[d] = (s["x"], s["y"], s.get("w", frame[0]), s.get("h", frame[1]), s.get("count", 1), tuple(s["pivot"]) if "pivot" in s else pivot)
    return res, a.get("fps", 10), a.get("loop", False)


def tint_img(im, t):
    if not t:
        return im
    r, g, b, a = im.split()
    t = list(t) + [1.0] * (4 - len(t))
    f = lambda ch, k: ch.point(lambda v: min(255, int(v * k)))
    return Image.merge("RGBA", (f(r, t[0]), f(g, t[1]), f(b, t[2]), f(a, t[3])))


def frames(sid, role, d):
    node = cfg["sprites"][sid]
    layout = cfg["layouts"][node["layout"]]
    anim = node.get("use", {}).get(role, role)
    if anim not in layout["animations"]:
        anim = role
    st, fps, loop = strips(layout, anim)
    x, y, w, h, n, pv = st[d]
    sheet = Image.open(os.path.join(art, node["texture"])).convert("RGBA")
    sc = node.get("scale", 1)
    res = []
    for i in range(n):
        f = sheet.crop((x + i * w, y, x + (i + 1) * w, y + h))
        f = tint_img(f, node.get("tint"))
        f = f.resize((f.width * sc * ZOOM, f.height * sc * ZOOM), Image.NEAREST)
        res.append((f, (pv[0] * sc * ZOOM, pv[1] * sc * ZOOM)))
    return res, fps


grass = Image.open(os.path.join(art, "tiles", "terrain_summer.png")).convert("RGBA") if False else None
CELL = (224, 290)
FOOT = (112, 262)
BG = (86, 128, 62, 255)


def place(canvas, fr, ox, oy):
    f, pv = fr
    canvas.alpha_composite(f, (int(ox + FOOT[0] - pv[0]), int(oy + FOOT[1] - pv[1])))


def timeline(sid):
    """Sequência de (quadro, pivô, duração ms) de um monstro: andar nas 4 direções, golpe em 2 direções, morte."""
    seq = []
    for d in ["down", "left", "right", "up"]:
        fr, fps = frames(sid, "walk", d)
        for q in fr:
            seq.append((q, 1000 / fps))
        seq.append((fr[0], 100))
    for d in ["down", "left"]:
        fr, fps = frames(sid, "attack", d)
        for _ in range(2):
            for q in fr:
                seq.append((q, 1000 / fps))
            seq.append((frames(sid, "idle", d)[0][0], 250))
    fr, fps = frames(sid, "death", "down")
    for q in fr:
        seq.append((q, 1000 / fps))
    seq.append((fr[-1], 800))
    return seq


def font(size):
    for f in ("C:/Windows/Fonts/arial.ttf", "C:/Windows/Fonts/segoeui.ttf"):
        if os.path.exists(f):
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


COLS = 5
rows = (len(IDS) + COLS - 1) // COLS
W, H = CELL[0] * COLS, CELL[1] * rows
STEP = 80  # ms por quadro do GIF
lines = {sid: timeline(sid) for sid, _ in IDS}


def sample(seq, t):
    total = sum(d for _, d in seq)
    t %= total
    for q, d in seq:
        if t < d:
            return q
        t -= d
    return seq[-1][0]


total = max(sum(d for _, d in s) for s in lines.values())
gif = []
f18 = font(15)
for k in range(int(total / STEP) + 1):
    im = Image.new("RGBA", (W, H), BG)
    dr = ImageDraw.Draw(im)
    for i, (sid, name) in enumerate(IDS):
        ox, oy = (i % COLS) * CELL[0], (i // COLS) * CELL[1]
        place(im, sample(lines[sid], k * STEP), ox, oy)
        dr.text((ox + 8, oy + 6), name, fill=(255, 255, 255, 255), font=f18)
    gif.append(im.convert("RGB").convert("P", palette=Image.ADAPTIVE))
os.makedirs(out, exist_ok=True)
gif[0].save(os.path.join(out, "amostra-fase5.gif"), save_all=True, append_images=gif[1:], duration=STEP, loop=0, disposal=2, optimize=True)

# prancha estática: uma linha por monstro, vários quadros de cada animação
FW = 112
png_rows = len(IDS)
RH = 150
sheet = Image.new("RGBA", (FW * 18 + 180, RH * png_rows + 28), BG)
dr = ImageDraw.Draw(sheet)
heads = {0: "andar (baixo)", 3: "andar (esquerda)", 5: "andar (direita)", 7: "andar (cima)", 9: "golpe (baixo)", 12: "golpe (esquerda)", 14: "morte"}
for c, t in heads.items():
    dr.text((180 + c * FW + 6, 6), t, fill=(255, 255, 160, 255), font=font(13))
for r, (sid, name) in enumerate(IDS):
    oy = r * RH + 28
    dr.text((6, oy + 8), name, fill=(255, 255, 255, 255), font=font(13))
    col = 0

    def put(fr_list, picks):
        global col
        for p in picks:
            f, pv = fr_list[min(p, len(fr_list) - 1)]
            cell = Image.new("RGBA", (FW, RH), (0, 0, 0, 0))
            sc = 0.5  # reduz de ZOOM x2 para x1 só aqui
            f2 = f.resize((max(1, int(f.width * sc)), max(1, int(f.height * sc))), Image.NEAREST)
            cell.alpha_composite(f2, (int(FW / 2 - pv[0] * sc), int(RH - 26 - pv[1] * sc)))
            sheet.alpha_composite(cell, (180 + col * FW, oy))
            col += 1

    for d in ["down", "left", "right", "up"]:
        fr, _ = frames(sid, "walk", d)
        put(fr, [0, max(1, len(fr) // 3), 2 * len(fr) // 3] if d == "down" else [0, len(fr) // 2])
    for d in ["down", "left"]:
        fr, _ = frames(sid, "attack", d)
        put(fr, [1, len(fr) // 2, len(fr) - 1] if d == "down" else [len(fr) // 2, len(fr) - 1])
    fr, _ = frames(sid, "death", "down")
    put(fr, [0, 1, 2, len(fr) - 1])
sheet.convert("RGB").save(os.path.join(out, "amostra-fase5.png"))
print("ok")
