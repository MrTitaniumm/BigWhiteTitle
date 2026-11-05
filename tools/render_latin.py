# -*- coding: utf-8 -*-
"""英文字体对比：同一串大写英文，四种字体，同样的阴影与字距。"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
HMOS = os.path.join(HERE, "fonts", "hmos")
SHS = os.path.join(HERE, "fonts", "shs")


def find_font(root, name):
    for dirpath, _, files in os.walk(root):
        if name in files:
            return os.path.join(dirpath, name)
    return ""


TEXT = "MECHHIVE"
SIZE = 150
TRACK = 0.12          # 英文用更大的字距
W, H = 1600, 860
BG = (52, 50, 44)

ROWS = [
    (r"C:\Windows\Fonts\msyhbd.ttc", "微软雅黑 Bold"),
    (find_font(SHS, "SourceHanSansSC-Heavy.otf"), "思源黑体 Heavy"),
    (find_font(HMOS, "HarmonyOS_Sans_Black.ttf"), "HarmonyOS Sans Black（拉丁专用）"),
    (find_font(HMOS, "HarmonyOS_Sans_SC_Black.ttf"), "HarmonyOS Sans SC Black（含拉丁）"),
]


def draw_row(canvas, path, label, y):
    if not path or not os.path.exists(path):
        print("缺失:", label)
        return
    font = ImageFont.truetype(path, SIZE)

    widths, space = [], int(SIZE * TRACK)
    for ch in TEXT:
        bb = font.getbbox(ch)
        widths.append(bb[2] - bb[0] if bb else SIZE)
    total = sum(widths) + space * (len(TEXT) - 1)
    x = (W - total) // 2

    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    cx = x
    for i, ch in enumerate(TEXT):
        md.text((cx, y), ch, font=font, fill=255)
        cx += widths[i] + space
    mask = mask.filter(ImageFilter.GaussianBlur(9)).point(lambda v: int(v * 0.78))
    canvas.paste(Image.new("RGB", (W, H), (0, 0, 0)), (0, 0), mask)

    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    cx = x
    for i, ch in enumerate(TEXT):
        ld.text((cx, y), ch, font=font, fill=(255, 255, 255, 255))
        cx += widths[i] + space
    canvas.paste(layer, (0, 0), layer)

    d = ImageDraw.Draw(canvas)
    d.text((24, y + SIZE // 2 - 8), label, font=ImageFont.truetype(path, 19),
           fill=(185, 185, 180))
    print(f"  {label:38s} {font.getname()}")


def main():
    canvas = Image.new("RGB", (W, H), BG)
    y = 50
    for path, label in ROWS:
        draw_row(canvas, path, label, y)
        y += 200

    out = os.path.join(HERE, "font_latin.png")
    canvas.save(out)
    print("输出:", out, canvas.size)


if __name__ == "__main__":
    main()
