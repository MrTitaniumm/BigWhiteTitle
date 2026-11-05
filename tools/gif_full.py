# -*- coding: utf-8 -*-
"""
全长输出，不做任何裁剪。

同一份源生成两种格式：
  webp/  —— 动画 WebP，体积小、画质高，适合外链或支持 WebP 的宿主
  gif/   —— GIF 兜底，颜色和帧率压低，体积约 1.5-2 MB

WebP 的体积只有 GIF 的 1/7 左右，所以这里反而给了更高的分辨率和帧率。
"""
import os
import subprocess
import sys

sys.stdout.reconfigure(encoding="utf-8")

FF = r"C:\Users\13169\AppData\Local\oopz\ffmpeg.exe"
SRC = r"C:\Users\13169\Videos"
BASE = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\gifs"

CROPS = {"10月29日.mp4": "crop=2304:1440:0:0,"}

WEBP = dict(w=720, fps=15, q=78)
GIF = dict(w=560, fps=12, colors=48)


def run(args):
    subprocess.run([FF, "-y", "-hide_banner", "-loglevel", "error"] + args, check=True)


def main():
    out_w = os.path.join(BASE, "webp")
    out_g = os.path.join(BASE, "gif")
    os.makedirs(out_w, exist_ok=True)
    os.makedirs(out_g, exist_ok=True)

    rows = []
    for name in sorted(os.listdir(SRC)):
        if not name.lower().endswith(".mp4"):
            continue
        path = os.path.join(SRC, name)
        crop = CROPS.get(name, "")
        stem = name[:-4]
        print(f"\n=== {name} ===")

        # WebP
        dst_w = os.path.join(out_w, stem + ".webp")
        vf = f"{crop}fps={WEBP['fps']},scale={WEBP['w']}:-1:flags=lanczos"
        run(["-i", path, "-vf", vf, "-c:v", "libwebp_anim",
             "-lossless", "0", "-q:v", str(WEBP["q"]), "-loop", "0", dst_w])
        kw = os.path.getsize(dst_w) / 1024 if os.path.exists(dst_w) else 0
        print(f"  webp {WEBP['w']}x? {WEBP['fps']}fps q{WEBP['q']}  {kw:8.0f} KB")

        # GIF
        dst_g = os.path.join(out_g, stem + ".gif")
        vf = (f"{crop}fps={GIF['fps']},scale={GIF['w']}:-1:flags=lanczos,"
              f"split[s0][s1];[s0]palettegen=max_colors={GIF['colors']}:stats_mode=diff[p];"
              "[s1][p]paletteuse=dither=none:diff_mode=rectangle")
        run(["-i", path, "-vf", vf, "-loop", "0", dst_g])
        kg = os.path.getsize(dst_g) / 1024 if os.path.exists(dst_g) else 0
        print(f"  gif  {GIF['w']}x? {GIF['fps']}fps {GIF['colors']}色  {kg:8.0f} KB")

        rows.append((name, kw, kg))

    print("\n=== 汇总 ===")
    print(f"{'视频':<22}{'WebP':>10}{'GIF':>10}")
    for n, kw, kg in rows:
        print(f"{n:<22}{kw/1024:>9.2f}M{kg/1024:>9.2f}M")


if __name__ == "__main__":
    main()
