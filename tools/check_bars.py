import os
import subprocess
import sys

from PIL import Image

FF = r"C:\Users\13169\AppData\Local\oopz\ffmpeg.exe"
SRC = r"C:\Users\13169\Videos"
TMP = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\gifs"

for name in sorted(os.listdir(SRC)):
    if not name.lower().endswith(".mp4"):
        continue
    path = os.path.join(SRC, name)
    frame = os.path.join(TMP, "_probe_" + name + ".png")
    subprocess.run([FF, "-y", "-hide_banner", "-loglevel", "error",
                    "-ss", "2", "-i", path, "-frames:v", "1", frame], check=True)
    im = Image.open(frame).convert("L")
    w, h = im.size
    px = im.load()

    def col_mean(x):
        s = 0
        for y in range(0, h, 8):
            s += px[x, y]
        n = len(range(0, h, 8))
        return s / n

    # 从右往左找第一列“有内容”的
    right_edge = w - 1
    for x in range(w - 1, 0, -4):
        if col_mean(x) > 40:
            right_edge = x
            break

    # 从左往右
    left_edge = 0
    for x in range(0, w, 4):
        if col_mean(x) > 40:
            left_edge = x
            break

    print(f"{name}")
    print(f"  尺寸 {w}x{h}   内容左边界 {left_edge}   右边界 {right_edge}")
    print(f"  左侧暗条 {left_edge}px   右侧暗条 {w - 1 - right_edge}px")
    os.remove(frame)
