"""Taikuri (kasinon lava): vihreätaustainen video -> kuvasarja Assets/Sprites/Taikuri/taikuri_<nimi>_NN.png.
Avainnus rekey_tony.key_soft (pehmeä reuna, sormet/kyyhky säilyvät), joka toinen kuva (24 -> 12 kuvaa/s), yhteinen rajaus kaikille kuville
(koko liike mukana, kyyhkynen lentää pois), skaala SCALE. Sama rajaus tallennetaan crop_<nimi>.json (jalat = pivot).
Ajo: python Tools/MagicianAssets/make_magician.py <video> <nimi> [skaala=0.75] [askel=2] [alku] [määrä]"""
import os, sys, json
import av, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import rekey_tony as rt
video, name = sys.argv[1], sys.argv[2]; scale = float(sys.argv[3]) if len(sys.argv) > 3 else .75; step = int(sys.argv[4]) if len(sys.argv) > 4 else 2
OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri"); os.makedirs(OUT, exist_ok=True)
def despill(a):   # taikurissa ei ole vihreää: jäljelle jäävät vihreät pisteet (aukkojen reunat) vedetään harmaaksi
    g = a[..., 1].astype(np.int16); m = np.maximum(a[..., 0], a[..., 2]).astype(np.int16)
    a[..., 1] = np.where(g > m + 6, m + 6, g).astype(np.uint8); return a
START = int(sys.argv[5]) if len(sys.argv) > 5 else 0; COUNT = int(sys.argv[6]) if len(sys.argv) > 6 else 10 ** 6   # valinnainen: vain kuvat START..START+COUNT-1 (silmukat)
frames = [despill(rt.key_soft(f.to_ndarray(format="bgr24"))) for f in av.open(video).decode(video=0)][START:START + COUNT:step]
boxes = []
for a in frames:
    ys, xs = np.nonzero(a[..., 3] > 40)
    if len(xs): boxes.append((xs.min(), xs.max(), ys.min(), ys.max()))
x0 = max(0, min(b[0] for b in boxes) - 6); x1 = max(b[1] for b in boxes) + 7; y0 = max(0, min(b[2] for b in boxes) - 6); y1 = max(b[3] for b in boxes) + 7
for old in os.listdir(OUT):
    if old.startswith("taikuri_%s_" % name): os.remove(os.path.join(OUT, old))
w, h = int(round((x1 - x0) * scale)), int(round((y1 - y0) * scale))
for i, a in enumerate(frames):
    # kuvaa alfalla painotettu pienennys (ei vihreää reunaan)
    im = Image.fromarray(a[y0:y1, x0:x1, [2, 1, 0, 3]] if False else np.dstack([a[y0:y1, x0:x1, 0], a[y0:y1, x0:x1, 1], a[y0:y1, x0:x1, 2], a[y0:y1, x0:x1, 3]]), "RGBA")
    im.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "taikuri_%s_%02d.png" % (name, i)))
json.dump({"x0": int(x0), "x1": int(x1), "y0": int(y0), "y1": int(y1), "scale": scale, "frames": len(frames), "fps": 24 / step, "w": w, "h": h}, open(os.path.join(HERE, "crop_%s.json" % name), "w"))
print(name, len(frames), "kuvaa", w, "x", h)
