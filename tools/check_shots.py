import os

from PIL import Image

d = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\shots"

for name in sorted(os.listdir(d)):
    if not name.endswith(".png"):
        continue
    im = Image.open(os.path.join(d, name)).convert("L")
    w, h = im.size
    px = im.load()

    def col_mean(x):
        vals = [px[x, y] for y in range(0, h, 6)]
        return sum(vals) / len(vals)

    right = w - 1
    for x in range(w - 1, 0, -4):
        if col_mean(x) > 40:
            right = x
            break

    left = 0
    for x in range(0, w, 4):
        if col_mean(x) > 40:
            left = x
            break

    print(f"{name}  {w}x{h}   左暗条 {left}px   右暗条 {w - 1 - right}px")
