from PIL import Image

p = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\output\gifs\frame.png"
im = Image.open(p).convert("RGB")
w, h = im.size
print("size", w, h)

for x in (0, 50, w // 2, w - 300, w - 200, w - 150, w - 100, w - 50, w - 1):
    print(f"  x={x:5d}  rgb={im.getpixel((x, h // 2))}")

last = 0
for x in range(w - 1, 0, -1):
    if max(im.getpixel((x, h // 2))) > 12:
        last = x
        break
print("最右非黑列:", last, "  右侧黑边:", w - 1 - last, "px")
