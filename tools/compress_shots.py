# -*- coding: utf-8 -*-
"""
把两张截图压到 2 MB 以内。

优先动颜色数（PNG 是无损格式，截图里大量同色像素，量化收益最大），
不够再降分辨率。每张都会报告不同档位的结果，最后由脚本挑最小可行方案。
"""
import os

from PIL import Image

SRC_DIR = r"C:\Users\13169\Pictures\Screenshots"
OUT_DIR = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\shots"

LIMIT = 2 * 1024 * 1024

os.makedirs(OUT_DIR, exist_ok=True)

files = [n for n in os.listdir(SRC_DIR) if n.endswith(".png") and ("(149)" in n or "(150)" in n)]

for name in sorted(files):
    src = os.path.join(SRC_DIR, name)
    im = Image.open(src).convert("RGB")
    print(f"\n=== {name}  {im.size[0]}x{im.size[1]}  {os.path.getsize(src)/1024/1024:.2f} MB ===")

    candidates = []

    # 1) 原尺寸，不同颜色数
    for colors in (256, 192, 160, 128):
        q = im.quantize(colors=colors, method=Image.MEDIANCUT)
        p = os.path.join(OUT_DIR, f"{colors}c.png")
        q.save(p, optimize=True)
        kb = os.path.getsize(p) / 1024
        candidates.append((colors, f"{colors}色/原尺寸", kb, p, None))
        print(f"  量化 {colors:3d} 色         {kb:7.0f} KB")

    # 2) 缩到 85% 再量化
    w, h = im.size
    small = im.resize((int(w * 0.85), int(h * 0.85)), Image.LANCZOS)
    for colors in (256, 192):
        q = small.quantize(colors=colors, method=Image.MEDIANCUT)
        p = os.path.join(OUT_DIR, f"s85_{colors}c.png")
        q.save(p, optimize=True)
        kb = os.path.getsize(p) / 1024
        candidates.append((colors, f"{colors}色/85%", kb, p, (small.size)))
        print(f"  85% + {colors:3d} 色       {kb:7.0f} KB")

    ok = [c for c in candidates if c[2] * 1024 < LIMIT]
    if ok:
        # 选体积最大但合规的那档（画质最好）
        best = max(ok, key=lambda c: c[2])
        final = os.path.join(OUT_DIR, name)
        os.replace(best[3], final)
        for c in candidates:
            if c[3] != final and os.path.exists(c[3]):
                os.remove(c[3])
        print(f"  → 采用 {best[1]}  {best[2]:.0f} KB   输出 {final}")
    else:
        print("  !! 所有档位都超过 2 MB")
