# -*- coding: utf-8 -*-
"""
英文版标题卡。字体：HarmonyOS Sans Black（拉丁专用），字距 0.12em。
输出到 Textures/Titles/<key>_en.png，并生成一张总览图。
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
HMOS = os.path.join(HERE, "fonts", "hmos")
OUT_DIR = os.path.join(HERE, "..", "BigWhiteTitle", "Textures", "Titles")

SIZE = 460
MAX_W = 3450          # 超过这个宽度就自动缩字号，保证 1440p 下不被运行时钳制
TRACK = 0.12
SHADOW_MAP = (0, 0, 0)
SHADOW_BOSS = (214, 26, 34)


def find_font(name):
    for dirpath, _, files in os.walk(HMOS):
        if name in files:
            return os.path.join(dirpath, name)
    return ""


FONT = find_font("HarmonyOS_Sans_Black.ttf")

MAPS = [
    ("Mechhive",       "MECHHIVE",      dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
    ("MetalHell",      "METAL HELL",    dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
    ("InsectLair",     "INSECT LAIR",   dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
    ("Glowforest",     "GLOWFOREST",    dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
    ("Scarlands",      "SCARLANDS",     dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
    ("LavaField",      "LAVA FIELD",    dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
    ("GlacialPlain",   "GLACIAL PLAIN", dict(blur=9,  dy=6,  a=0.78, color=SHADOW_MAP)),
]

BOSSES = [
    ("Mech_Warqueen",  "WAR QUEEN",     dict(blur=14, dy=12, a=0.95, color=SHADOW_BOSS)),
    ("Mech_Apocriton", "APOCRITON",     dict(blur=14, dy=12, a=0.95, color=SHADOW_BOSS)),
    ("Mech_Diabolus",  "DIABOLUS",      dict(blur=14, dy=12, a=0.95, color=SHADOW_BOSS)),
]


def layout(font, text, size):
    widths, space = [], int(size * TRACK)
    for ch in text:
        bb = font.getbbox(ch)
        widths.append(bb[2] - bb[0] if bb else size)
    return widths, space, sum(widths) + space * (len(text) - 1)


def render(text, st):
    # 先按基准字号量宽度，超了就等比缩小
    size = SIZE
    base = ImageFont.truetype(FONT, SIZE)
    _, _, w0 = layout(base, text, SIZE)
    if w0 > MAX_W:
        size = max(120, int(SIZE * MAX_W / w0))

    font = ImageFont.truetype(FONT, size)
    widths, space, total = layout(font, text, size)

    ascent, descent = font.getmetrics()
    pad = st["blur"] * 4 + 40
    W = total + pad * 2
    H = ascent + descent + pad * 2

    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    x = pad
    for i, ch in enumerate(text):
        md.text((x, pad), ch, font=font, fill=255)
        x += widths[i] + space
    mask = mask.filter(ImageFilter.GaussianBlur(st["blur"])).point(lambda v: int(v * st["a"]))

    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow.paste(Image.new("RGBA", (W, H), st["color"] + (255,)), (0, st["dy"]), mask)

    body = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    x = pad
    for i, ch in enumerate(text):
        bd.text((x, pad), ch, font=font, fill=(255, 255, 255, 255))
        x += widths[i] + space

    out = Image.alpha_composite(shadow, body)
    bbox = out.getbbox()
    if bbox:
        m = 10
        out = out.crop((max(0, bbox[0] - m), max(0, bbox[1] - m),
                        min(W, bbox[2] + m), min(H, bbox[3] + m)))
    return out


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    if not FONT:
        print("找不到 HarmonyOS_Sans_Black.ttf")
        sys.exit(1)
    print("字体:", FONT)

    made = []
    for key, text, st in MAPS + BOSSES:
        im = render(text, st)
        p = os.path.join(OUT_DIR, key + "_en.png")
        im.save(p)
        made.append((key + "_en.png", im.size, os.path.getsize(p)))
        print(f"  {key+'_en':24s} 「{text}」 {im.width}x{im.height}  {os.path.getsize(p)/1024:5.0f} KB")

    total = sum(m[2] for m in made)
    print(f"\n共 {len(made)} 张，合计 {total/1024/1024:.2f} MB")

    # 总览图
    COLS, CELL_W, CELL_H = 3, 520, 165
    rows = (len(made) + COLS - 1) // COLS
    W, H = COLS * CELL_W, rows * CELL_H + 20
    canvas = Image.new("RGB", (W, H), (44, 42, 38))
    d = ImageDraw.Draw(canvas)
    lab = ImageFont.truetype(FONT, 18)
    for idx, (name, size, _) in enumerate(made):
        c, r = idx % COLS, idx // COLS
        x0, y0 = c * CELL_W, r * CELL_H + 10
        im = Image.open(os.path.join(OUT_DIR, name)).convert("RGBA")
        mw, mh = CELL_W - 20, CELL_H - 50
        k = min(mw / im.width, mh / im.height)
        im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
        canvas.paste(im, (x0 + (CELL_W - im.width) // 2, y0 + (mh - im.height) // 2 + 4), im)
        d.text((x0 + 10, y0 + CELL_H - 26), name[:-4], font=lab, fill=(170, 170, 165))

    sheet = os.path.join(HERE, "titles_sheet_en.png")
    canvas.save(sheet)
    print("总览:", sheet, canvas.size)


if __name__ == "__main__":
    main()
