# -*- coding: utf-8 -*-
"""把 Pixabay 的 boom 处理成 mod 里用的 thud.wav。

处理：截断到 4.0s → 尾部 0.5s 余弦淡出 → 归一化到 0.89 → 16bit 单声道 WAV
输出：BigWhiteTitle/Sounds/thud.wav
"""
import os
import sys
import wave

import numpy as np

sys.stdout.reconfigure(encoding="utf-8")

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "sfx", "raw.wav")
DST = os.path.join(HERE, "..", "BigWhiteTitle", "Sounds", "thud.wav")
RATE = 44100

DUR = 4.0
FADE = 0.5
PEAK = 0.89


def load(path):
    with wave.open(path, "rb") as w:
        n, ch, sr = w.getnframes(), w.getnchannels(), w.getframerate()
        raw = w.readframes(n)
    x = np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768.0
    if ch > 1:
        x = x.reshape(-1, ch).mean(axis=1)
    return x, sr


def save(path, x, sr):
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(pcm.tobytes())


def bands(x, sr):
    nfft = 4096
    win = np.hanning(nfft).astype(np.float32)
    frames = (len(x) - nfft) // 256
    mag = np.array([np.abs(np.fft.rfft(x[i * 256:i * 256 + nfft] * win))
                    for i in range(frames)]).T
    f = np.fft.rfftfreq(nfft, 1.0 / sr)
    low = np.sqrt(np.mean(mag[(f >= 20) & (f <= 200)] ** 2, axis=0))
    pk = int(np.argmax(low))
    fr = mag[:, pk]
    tot = np.sum(fr ** 2) + 1e-12
    return {f"{lo}-{hi}": 10 * np.log10(np.sum(fr[(f >= lo) & (f < hi)] ** 2) / tot + 1e-12)
            for lo, hi in [(20, 80), (80, 200), (200, 500), (500, 2000), (2000, 8000)]}


if __name__ == "__main__":
    x, sr = load(SRC)
    print(f"源 {len(x)/sr:.2f}s")

    n = int(DUR * sr)
    x = x[:n].copy() if len(x) > n else np.pad(x, (0, n - len(x)))

    fl = int(FADE * sr)
    x[-fl:] *= 0.5 * (1 + np.cos(np.linspace(0, np.pi, fl)))   # 余弦淡出

    x *= PEAK / (np.max(np.abs(x)) or 1.0)

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    save(DST, x, sr)
    print(f"输出 {DST}")
    print(f"      {len(x)/sr:.2f}s  {os.path.getsize(DST)/1024:.0f} KB  峰值 {np.max(np.abs(x)):.2f}")
    print("频段分布:", {k: round(v, 1) for k, v in bands(x, sr).items()})
