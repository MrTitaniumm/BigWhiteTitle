# -*- coding: utf-8 -*-
"""
生成工坊预览图：在游戏截图上方叠加中英文模组名。

样式与正式标题卡一致：纯白、无描边、紧实投影（blur 9 / dy 6 / alpha 0.78）。
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
HMOS = os.path.join(HERE, "fonts", "hmos")
MOD = os.path.join(HERE, "..", "BigWhiteTitle")

SRC = r"C:\Users\13169\Pictures\Screenshots\294100_36.jpg"

ZH = "雷霆白字"
EN = "BIG WHITE TITLE"

FONT_ZH = None
FONT_EN = None


def find_font(root, name):
    for dirpath, _, files in os.walk(root):
        if name in files:
            return os.path.join(dirpath, name)
    return ""


def draw_line(canvas, text, font, track, center_y, shadow=(0, 0, 0),
              blur=9, dy=6, alpha=0.78):
    """居中绘制一行带投影的白字。"""
    W, H = canvas.size

    widths, space = [], int(font.size * track)
    for ch in text:
        bb = font.getbbox(ch)
        widths.append(bb[2] - bb[0] if bb else font.size)
    total = sum(widths) + space * max(0, len(text) - 1)

    ascent, descent = font.getmetrics()
    box_h = ascent + descent
    x = (W - total) // 2
    y = int(center_y - box_h / 2)

    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    cx = x
    for i, ch in enumerate(text):
        md.text((cx, y), ch, font=font, fill=255)
        cx += widths[i] + space

    blurred = mask.filter(ImageFilter.GaussianBlur(blur)).point(lambda v: int(v * alpha))
    sh = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sh.paste(Image.new("RGBA", (W, H), shadow + (255,)), (0, dy), blurred)
    canvas.alpha_composite(sh)

    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    cx = x
    for i, ch in enumerate(text):
        ld.text((cx, y), ch, font=font, fill=(255, 255, 255, 255))
        cx += widths[i] + space
    canvas.alpha_composite(layer)

    return box_h


def measure(text, font, track):
    widths, space = [], int(font.size * track)
    for ch in text:
        bb = font.getbbox(ch)
        widths.append(bb[2] - bb[0] if bb else font.size)
    return widths, space, sum(widths) + space * max(0, len(text) - 1)


def fit_size(text, font_path, target_w, track):
    """求一个字号，使这一行渲染后恰好接近 target_w 宽。"""
    lo, hi = 10, 4000
    while lo < hi:
        mid = (lo + hi + 1) // 2
        f = ImageFont.truetype(font_path, mid)
        if measure(text, f, track)[2] <= target_w:
            lo = mid
        else:
            hi = mid - 1
    return lo


def main():
    global FONT_ZH, FONT_EN
    FONT_ZH = find_font(HMOS, "HarmonyOS_Sans_SC_Black.ttf")
    FONT_EN = find_font(HMOS, "HarmonyOS_Sans_Black.ttf")
    if not FONT_ZH or not FONT_EN:
        print("缺字体")
        sys.exit(1)

    base = Image.open(SRC).convert("RGBA")
    W, H = base.size
    print(f"源图 {W}x{H}")

    canvas = base.copy()

    # 两行同字号：以较宽的英文为准，让它撑到画面 92% 宽
    TRACK_EN = 0.12
    TRACK_ZH = 0.10
    target_w = int(W * 0.92)
    size = fit_size(EN, FONT_EN, target_w, TRACK_EN)
    print(f"字号 {size}px（英文宽度目标 {target_w}px）")

    f_zh = ImageFont.truetype(FONT_ZH, size)
    f_en = ImageFont.truetype(FONT_EN, size)

    w_zh = measure(ZH, f_zh, TRACK_ZH)[2]
    w_en = measure(EN, f_en, TRACK_EN)[2]
    print(f"中文行宽 {w_zh}px ({w_zh/W:.0%})   英文行宽 {w_en}px ({w_en/W:.0%})")

    ascent, descent = f_zh.getmetrics()
    line_h = ascent + descent
    gap = int(line_h * 0.18)
    block_h = line_h * 2 + gap
    top = (H - block_h) * 0.5

    draw_line(canvas, ZH, f_zh, TRACK_ZH, top + line_h / 2)
    draw_line(canvas, EN, f_en, TRACK_EN, top + line_h + gap + line_h / 2)

    # 原尺寸存档，供工坊页面的图集使用
    p_big = os.path.join(HERE, "preview_big.png")
    canvas.convert("RGB").save(p_big, optimize=True)
    print(f"大图 {p_big}  {canvas.size}  {os.path.getsize(p_big)/1024:.0f} KB")

    # Steam 预览图上限 1 MB：缩到 1280x720 并降到 256 色
    out_png = os.path.join(MOD, "About", "Preview.png")
    os.makedirs(os.path.dirname(out_png), exist_ok=True)
    small = canvas.convert("RGB").resize((1280, 720), Image.LANCZOS)
    small.quantize(colors=256, method=Image.MEDIANCUT).save(out_png, optimize=True)
    kb = os.path.getsize(out_png) / 1024
    print(f"输出 {out_png}  1280x720  {kb:.0f} KB  {'OK' if kb < 1024 else '!! 超过 1MB'}")

    # 缩略图预览：模拟模组列表里的大小
    tiny = canvas.convert("RGB").resize((512, int(512 * H / W)), Image.LANCZOS)
    p_small = os.path.join(HERE, "preview_small.png")
    tiny.save(p_small)
    print(f"缩略 {p_small}  {tiny.size}")


if __name__ == "__main__":
    main()
