"""Vartija: pillin soitto (hälytys) videosta -> vartija_pilli.png (18 kuvaa: veto 0-5, nosto 6-8, puhallus 9-11, lasku 12-17; savuefekti ja ylimääräiset kuvat jätetty pois)
+ ääni Assets/Resources/Sfx/Vartija/pilli.wav (videon äänestä, 1,1 s, normalisoitu).
Ajo: python Tools/GuardAssets/make_guard_whistle.py <video.mp4> [ulos-kansio]"""
import os, sys, wave
import av, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_pilli": (640, 320, 768)})
FRAMES = [21, 27, 33, 39, 45, 51, 57, 63, 69, 75, 81, 87, 138, 147, 156, 165, 174, 195]

def sound(video, dst, t0=3.65, dur=1.1):
    c = av.open(video); rs = av.AudioResampler(format="s16", layout="mono", rate=44100); out = []
    for f in c.decode(audio=0):
        for g in rs.resample(f): out.append(g.to_ndarray().reshape(-1))
    x = np.concatenate(out).astype(np.float32); s = int(t0 * 44100); y = x[s:s + int(dur * 44100)].copy()
    n = len(y); fo = int(0.25 * 44100); y[-fo:] *= np.linspace(1, 0, fo); y[:300] *= np.linspace(0, 1, 300)
    y = y / max(1.0, np.abs(y).max()) * 0.9 * 32767
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with wave.open(dst, "wb") as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(44100); w.writeframes(y.astype(np.int16).tobytes())
    print("ääni", dst, round(n / 44100, 2), "s")

def main(video, outs):
    V = m.load(video)
    x0, x1, y0, y1 = m.bbox(V[0]); k = 1.185; floor = y1
    m.setdims("v_pilli")
    m.save([m.cell(V[i], "low", k, floor) for i in FRAMES], f"{outs}/vartija_pilli.png"); print("pilli", len(FRAMES), "kuvaa")
    sound(video, "Assets/Resources/Sfx/Vartija/pilli.wav")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
