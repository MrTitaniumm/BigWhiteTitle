# -*- coding: utf-8 -*-
"""
给一张新卡渲染中英文 PNG。

用法：
    python add_card.py <key> <中文> <英文> [boss]

示例：
    python add_card.py BreadMo_AAT_BasePawnKind 无名暮光 "NAMELESS TWILIGHT" boss

输出到 BigWhiteTitle/Textures/Titles/<key>.png 与 <key>_en.png
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
HMOS = os.path.join(HERE, "fonts", "hmos")
OUT_DIR = os.path.join(HERE, "..", "BigWhiteTitle", "Textures", "Titles")

SIZE = 460
MAX_W = 3450

STYLE_MAP = dict(blur=9, dy=6, alpha=0.78, color=(0, 0, 0), track=0.08)
STYLE_BOSS = dict(blur=14, dy=12, alpha=0.95, color=(214, 26, 34), track=0.08)

EN_TRACK = 0.12


def find_font(root, name):
    for dirpath, _, files in os.walk(root):
        if name in files:
            return os.path.join(dirpath, name)
    return ""


FONT_ZH = find_font(HMOS, "HarmonyOS_Sans_SC_Black.ttf")
FONT_EN = find_font(HMOS, "HarmonyOS_Sans_Black.ttf")


def layout(font, text, size, track):
    widths, space = [], int(size * track)
    for ch in text:
        bb = font.getbbox(ch)
        widths.append(bb[2] - bb[0] if bb else size)
    return widths, space, sum(widths) + space * (len(text) - 1)


def render(text, font_path, st, track):
    size = SIZE
    base = ImageFont.truetype(font_path, SIZE)
    _, _, w0 = layout(base, text, SIZE, track)
    if w0 > MAX_W:
        size = max(120, int(SIZE * MAX_W / w0))

    font = ImageFont.truetype(font_path, size)
    widths, space, total = layout(font, text, size, track)

    ascent, descent = font.getmetrics()
    pad = st["blur"] * 4 + 40
    W, H = total + pad * 2, ascent + descent + pad * 2

    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    x = pad
    for i, ch in enumerate(text):
        md.text((x, pad), ch, font=font, fill=255)
        x += widths[i] + space
    mask = mask.filter(ImageFilter.GaussianBlur(st["blur"])).point(lambda v: int(v * st["alpha"]))

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
        m = 30          # 留白要够：弯引号、字母上缘都可能顶到边
        out = out.crop((max(0, bbox[0] - m), max(0, bbox[1] - m),
                        min(W, bbox[2] + m), min(H, bbox[3] + m)))
    return out


def main():
    if len(sys.argv) < 4:
        print(__doc__)
        sys.exit(1)

    key, zh, en = sys.argv[1], sys.argv[2], sys.argv[3]
    st = STYLE_BOSS if len(sys.argv) > 4 and sys.argv[4].lower() == "boss" else STYLE_MAP

    os.makedirs(OUT_DIR, exist_ok=True)

    p1 = os.path.join(OUT_DIR, key + ".png")
    os.makedirs(os.path.dirname(p1), exist_ok=True)   # key 可带子目录，如 AlphaMechs/AM_Infernus
    im = render(zh, FONT_ZH, st, st["track"])
    im.save(p1)
    print(f"  {key}.png      「{zh}」 {im.width}x{im.height}  {os.path.getsize(p1)/1024:.0f} KB")

    p2 = os.path.join(OUT_DIR, key + "_en.png")
    im = render(en, FONT_EN, st, EN_TRACK)
    im.save(p2)
    print(f"  {key}_en.png   「{en}」 {im.width}x{im.height}  {os.path.getsize(p2)/1024:.0f} KB")


if __name__ == "__main__":
    main()
