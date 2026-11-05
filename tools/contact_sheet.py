# -*- coding: utf-8 -*-
"""把生成好的标题 PNG 拼成一张总览图，用于核对文字与观感。"""
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
TITLES = os.path.join(HERE, "..", "BigWhiteTitle", "Textures", "Titles")
LABEL_FONT = r"C:\Windows\Fonts\msyh.ttc"

ITEMS = [
    ("Mechhive.png", "地图"),
    ("MetalHell.png", "地图"),
    ("InsectLair.png", "地图"),
    ("Glowforest.png", "群系"),
    ("Scarlands.png", "群系"),
    ("LavaField.png", "群系"),
    ("GlacialPlain.png", "群系"),
    ("Mech_Warqueen.png", "首领"),
    ("Mech_Apocriton.png", "首领"),
    ("Mech_Diabolus.png", "首领"),
]

COLS = 3
CELL_W, CELL_H = 520, 165
PAD = 10
BG = (44, 42, 38)


def main():
    rows = (len(ITEMS) + COLS - 1) // COLS
    W = COLS * CELL_W
    H = rows * CELL_H + 20
    canvas = Image.new("RGB", (W, H), BG)
    d = ImageDraw.Draw(canvas)
    f = ImageFont.truetype(LABEL_FONT, 18)

    for idx, (name, kind) in enumerate(ITEMS):
        c, r = idx % COLS, idx // COLS
        x0, y0 = c * CELL_W, r * CELL_H + 10
        p = os.path.join(TITLES, name)
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        max_w, max_h = CELL_W - PAD * 2, CELL_H - 50
        k = min(max_w / im.width, max_h / im.height)
        im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
        px = x0 + (CELL_W - im.width) // 2
        py = y0 + (max_h - im.height) // 2 + 4
        canvas.paste(im, (px, py), im)
        d.text((x0 + PAD, y0 + CELL_H - 26), f"{name[:-4]}  [{kind}]", font=f, fill=(170, 170, 165))

    out = os.path.join(HERE, "titles_sheet.png")
    canvas.save(out)
    print("输出:", out, canvas.size)


if __name__ == "__main__":
    main()
