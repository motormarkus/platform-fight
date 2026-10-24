"""Roccon uudet iskut (kyynärpää, pyörähdyslyönti): läpinäkyvät kuvanauhat (Tools/RoccoAssets/new_moves) -> Assets/Resources/Rocco/elbow_NN, spin_NN.
Hahmot erotellaan nauhasta sarakejaksoina (ruudut eivät ole tasavälein), jalat (saappaiden keskikohta) viedään yhteisen kankaan keskelle ja pohja alareunaan,
joten pivot on kaikilla sama (0,5 / ALARAJA). Mittakaava: seisova Rocco = 336 px (sama kuin rocco_etusivukoukku, 100 ppu).
Ajo: python Tools/RoccoAssets/make_rocco_moves.py
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "Tools", "RoccoAssets", "new_moves")
OUT = os.path.join(ROOT, "Assets", "Resources", "Rocco")
TARGET = 336.0
SETS = {"elbow": ["elbow_strip.png", "elbow_final.png"], "spin": ["spin_1.png", "spin_2.png", "spin_3.png"]}
SAME_POSE_AS = {("elbow", 3): 2}   # elbow_final on eri tarkkuudella sama asento kuin nauhan 3. kuva: skaalataan sen korkeuteen


def figures(path):
    im = Image.open(path).convert("RGBA")
    a = np.array(im)[..., 3]
    cols = (a > 40).any(0)
    runs, s = [], None
    for i, c in enumerate(cols):
        if c and s is None: s = i
        if not c and s is not None: runs.append((s, i - 1)); s = None
    if s is not None: runs.append((s, len(cols) - 1))
    runs = [r for r in runs if r[1] - r[0] > 30]
    out = []
    for x0, x1 in runs:
        sub = a[:, x0:x1 + 1] > 40
        ys = np.where(sub.any(1))[0]
        y0, y1 = ys.min(), ys.max()
        boots = sub[max(y0, y1 - 55):y1 + 1]
        bx = np.where(boots.any(0))[0]
        feet = x0 + (bx.min() + bx.max()) / 2.0
        out.append((im.crop((x0, y0, x1 + 1, y1 + 1)), feet - x0, y1 - y0))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    frames = {}
    for name, files in SETS.items():
        fl = []
        for f in files:
            fl += figures(os.path.join(SRC, f))
        frames[name] = fl
    ref = frames["elbow"][1][0].height            # seisova (vartalo ja jalat) = elbow-nauhan 2. kuva
    k = TARGET / ref
    kf = {}
    for (nm, i), j in SAME_POSE_AS.items():
        kf[(nm, i)] = frames[nm][j][0].height * k / frames[nm][i][0].height
    allf = [f for fl in frames.values() for f in fl]
    half = max(max(fx, f.width - fx) * kf.get((nm, i), k) / k for nm, fl in frames.items() for i, (f, fx, _) in enumerate(fl))
    Hc = max(f.height * kf.get((nm, i), k) / k for nm, fl in frames.items() for i, (f, fx, _) in enumerate(fl))
    W, H = int(round(2 * half * k)) + 2, int(round(Hc * k)) + 2
    for name, fl in frames.items():
        for old in os.listdir(OUT):
            if old.startswith(name + "_") and old.endswith(".png"): os.remove(os.path.join(OUT, old))
        for i, (img, fx, _) in enumerate(fl):
            kk = kf.get((name, i), k)
            sm = img.resize((max(1, round(img.width * kk)), max(1, round(img.height * kk))), Image.LANCZOS)
            canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
            canvas.alpha_composite(sm, (int(round(W / 2 - fx * kk)), H - sm.height))
            canvas.save(os.path.join(OUT, "%s_%02d.png" % (name, i)))
        print(name, len(fl), "kuvaa")
    print("kangas", W, "x", H, "kerroin", round(k, 4), "(pivot 0,5 / 0)")


if __name__ == "__main__":
    main()
