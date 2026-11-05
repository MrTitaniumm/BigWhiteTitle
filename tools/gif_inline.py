# -*- coding: utf-8 -*-
"""
为工坊页面描述生成内嵌用 GIF。

思路：视频前后大部分是“镜头移动”，真正有信息的是白字出现的那几秒。
所以先按帧检测白字（画面中央大片近白像素），把区间裁出来，再压小。
"""
import os
import shutil
import subprocess
import sys
import tempfile

from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

FF = r"C:\Users\13169\AppData\Local\oopz\ffmpeg.exe"
SRC = r"C:\Users\13169\Videos"
OUT = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\gifs\inline"

PROBE_FPS = 4          # 采样帧率
WHITE = 238            # 近白阈值
RATIO_ON = 0.004       # 中央区域白色像素占比超过此值 → 认为白字出现
LEAD = 0.45            # 白字前留多久
TAIL = 0.30            # 白字后留多久

CROPS = {"10月29日.mp4": "crop=2304:1440:0:0,"}


def probe_card_interval(path, tmp):
    """返回 (start, end) 秒；检测不到就返回整段。"""
    # 低分辨率抽帧，只为分析
    subprocess.run([FF, "-y", "-hide_banner", "-loglevel", "error",
                    "-i", path, "-vf", f"fps={PROBE_FPS},scale=320:-1",
                    os.path.join(tmp, "p_%04d.png")], check=True)

    frames = sorted(os.listdir(tmp))
    hits = []
    for i, f in enumerate(frames):
        im = Image.open(os.path.join(tmp, f)).convert("L")
        w, h = im.size
        # 只看中央 64% 宽、中间 60% 高
        box = (int(w * 0.18), int(h * 0.18), int(w * 0.82), int(h * 0.82))
        crop = im.crop(box)
        px = list(crop.getdata())
        white = sum(1 for v in px if v >= WHITE)
        ratio = white / len(px)
        t = i / PROBE_FPS
        hits.append((t, ratio))
        shutil.os.remove(os.path.join(tmp, f))

    on = [t for t, r in hits if r >= RATIO_ON]
    if not on:
        return None
    return min(on) - LEAD, max(on) + 1.0 / PROBE_FPS + TAIL


def main():
    os.makedirs(OUT, exist_ok=True)
    # 不要用 tempfile.mkdtemp：系统 Temp 在沙箱里不可写，ffmpeg 会失败
    tmp = os.path.join(OUT, "_probe")
    shutil.rmtree(tmp, ignore_errors=True)
    os.makedirs(tmp, exist_ok=True)

    for name in sorted(os.listdir(SRC)):
        if not name.lower().endswith(".mp4"):
            continue
        path = os.path.join(SRC, name)
        print(f"\n=== {name} ===")

        r = probe_card_interval(path, tmp)

        if r is None:
            print("  未检测到白字，使用整段")
            start, end = 0.0, 99.0
        else:
            start, end = max(0.0, r[0]), r[1]
            print(f"  白字区间约 {start:.2f}s → {end:.2f}s  （长 {end-start:.2f}s）")

        crop = CROPS.get(name, "")
        vf = (f"{crop}fps=12,scale=560:-1:flags=lanczos,"
              "split[s0][s1];[s0]palettegen=max_colors=64:stats_mode=diff[p];"
              "[s1][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle")

        dst = os.path.join(OUT, name.replace(".mp4", ".gif"))
        cmd = [FF, "-y", "-hide_banner", "-loglevel", "error",
               "-ss", f"{start:.2f}", "-t", f"{end-start:.2f}",
               "-i", path, "-vf", vf, "-loop", "0", dst]
        subprocess.run(cmd, check=True)

        if os.path.exists(dst):
            print(f"  输出 {os.path.basename(dst)}  {os.path.getsize(dst)/1024/1024:.2f} MB")
        else:
            print("  转换失败")

    shutil.rmtree(tmp, ignore_errors=True)


if __name__ == "__main__":
    main()
