# -*- coding: utf-8 -*-
"""
标题卡 PNG 生成器（v2）。

规格：4K 等效字号 460px、字距 0.08em、**无描边**、紧实投影（blur 9 / 偏移 6）。
地图卡 = 黑色投影；首领卡 = 红色投影。

用法： python render_titles.py boss | map | all
"""
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
HMOS = os.path.join(HERE, "fonts", "hmos")
OUT_DIR = os.path.join(HERE, "..", "BigWhiteTitle", "Textures", "Titles")

SIZE = 460
TRACK = 0.08

STYLE_MAP = dict(stroke_w=0, blur=9, dy=6, alpha=0.78, color=(0, 0, 0))
STYLE_BOSS = dict(stroke_w=0, blur=14, dy=12, alpha=0.95, color=(214, 26, 34))

MAPS = [
    ("Mechhive",   "机械主巢"),
    ("MetalHell",  "金属地狱"),
    ("InsectLair", "虫穴"),
]

BOSSES = [
    ("Mech_Warqueen",  "战争女皇"),
    ("Mech_Apocriton", "憎恶毒蜂"),
    ("Mech_Diabolus",  "炼狱魔王"),
]

BIOMES = [
    ("Glowforest",   "萤光森林"),
    ("Scarlands",    "焦痕废土"),
    ("LavaField",    "熔岩荒原"),
    ("GlacialPlain", "冰蚀平原"),
]


def find_font(name):
    for dirpath, _, files in os.walk(HMOS):
        if name in files:
            return os.path.join(dirpath, name)
    return ""


FONT = find_font("HarmonyOS_Sans_SC_Black.ttf")


def render(text, st):
    font = ImageFont.truetype(FONT, SIZE)

    widths, space = [], int(SIZE * TRACK)
    for ch in text:
        bb = font.getbbox(ch)
        widths.append(bb[2] - bb[0] if bb else SIZE)
    total = sum(widths) + space * (len(text) - 1)

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
    mask = mask.filter(ImageFilter.GaussianBlur(st["blur"])).point(
        lambda v: int(v * st["alpha"]))

    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shadow.paste(Image.new("RGBA", (W, H), st["color"] + (255,)), (0, st["dy"]), mask)

    body = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    bd = ImageDraw.Draw(body)
    x = pad
    for i, ch in enumerate(text):
        bd.text((x, pad), ch, font=font, fill=(255, 255, 255, 255),
                stroke_width=st["stroke_w"], stroke_fill=(0, 0, 0, 0))
        x += widths[i] + space

    out = Image.alpha_composite(shadow, body)
    bbox = out.getbbox()
    if bbox:
        m = 10
        out = out.crop((max(0, bbox[0] - m), max(0, bbox[1] - m),
                        min(W, bbox[2] + m), min(H, bbox[3] + m)))
    return out


def emit(items, st, tag):
    total = 0
    for defName, text in items:
        im = render(text, st)
        p = os.path.join(OUT_DIR, defName + ".png")
        im.save(p)
        sz = os.path.getsize(p)
        total += sz
        print(f"  [{tag}] {defName:22s} 「{text}」 {im.width}x{im.height}  {sz/1024:6.0f} KB")
    return total


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    if not os.path.exists(FONT):
        print("字体缺失:", FONT)
        sys.exit(1)

    mode = sys.argv[1] if len(sys.argv) > 1 else "all"
    total = 0
    if mode in ("boss", "all"):
        total += emit(BOSSES, STYLE_BOSS, "首领")
    if mode in ("map", "all"):
        total += emit(MAPS, STYLE_MAP, "地图")
    if mode in ("biome", "all"):
        total += emit(BIOMES, STYLE_MAP, "群系")
    print(f"\n合计 {total/1024/1024:.2f} MB -> {OUT_DIR}")


if __name__ == "__main__":
    main()
