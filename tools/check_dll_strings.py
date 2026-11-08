import os

BASE = r"C:\Users\13169\Documents\deepseek-harness\default-workspace\rimworld-mod-dev\release\BigWhiteTitle"
KEYS = ["Sounds", "thud.wav", "thud_local.wav", "Titles/", "Textures", "About", "BuildThud", "BwtPaths"]


def find_any(data, key):
    """在 UTF-8 与 UTF-16LE 两种编码下按字节查找。"""
    for enc in ("utf-8", "utf-16-le"):
        if data.find(key.encode(enc)) >= 0:
            return enc
    return None


for ver in ("1.6", "1.7"):
    p = os.path.join(BASE, ver, "Assemblies", "BigWhiteTitle.dll")
    data = open(p, "rb").read()
    print(f"=== {ver}  ({len(data)/1024:.0f} KB) ===")
    for k in KEYS:
        enc = find_any(data, k)
        print(f"  {k:<16} {'有 (' + enc + ')' if enc else '无'}")
