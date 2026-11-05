import os

from PIL import Image

base = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\gifs\test"

for name in ("P.webp", "P55.webp", "P.gif"):
    p = os.path.join(base, name)
    if not os.path.exists(p):
        print(f"{name}: 不存在")
        continue
    im = Image.open(p)
    n = getattr(im, "n_frames", 1)
    print(f"{name:12s} {im.size}  {n} 帧  {os.path.getsize(p)/1024:8.0f} KB  format={im.format}")

# 从 webp 抽一帧存成 png 供查看
im = Image.open(os.path.join(base, "P.webp"))
print("webp 帧数:", getattr(im, "n_frames", 1))
im.seek(min(20, getattr(im, "n_frames", 1) - 1))
im.convert("RGB").save(os.path.join(base, "webp_frame.png"))
print("已导出 webp_frame.png", im.size)
