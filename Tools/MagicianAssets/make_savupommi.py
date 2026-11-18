"""Taikurin erikoisliike (savupommi): vihreätaustainen video -> kaksi kuvasarjaa:
  taikuri_savu_heitto_NN  (kuvat 0..HEITTO-1: taikuri heittää pommin, skaala 0,55 = sama kuin muut taikurin kuvat)
  savu_rajahdys_NN        (kuvat ALKU..: räjähdys ja savupilvi, skaala 0,55 (sama kuin taikurin muut kuvat, jotta taikuri ei muuta kokoaan); alku joka kuva, myöhemmin joka toinen)
Savun läpikuultavuus: vihreä tausta B, kuva C = a*F + (1-a)*B -> a = 1 - (g - max(r,b)) / (Bg - max(Br,Bb)) (harmaa/tumma etualue ei sisällä vihreää), F = (C-(1-a)B)/a.
Ajo: python Tools/MagicianAssets/make_savupommi.py <video>"""
import os, sys, json
import av, numpy as np, cv2, inspect
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import rekey_tony as rt
# taikurin heittokuvat avainnetaan samalla key_softilla kuin muut sarjat (reunan pehmeys yhtenäinen); pommi maassa säilyy (ei suurimman alueen sääntöä)
_NL = chr(10)
_src = inspect.getsource(rt.key_soft).replace("    if n2 > 2: bgm = bgm | (lab2 != 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])))" + _NL, "").replace("def key_soft", "def key_multi")
_ns = {"np": np, "cv2": cv2, "kv": rt.kv}; exec(_src, _ns); key_multi = _ns["key_multi"]
def despill(a):
    g = a[..., 1].astype(np.int16); m = np.maximum(a[..., 0], a[..., 2]).astype(np.int16)
    a[..., 1] = np.where(g > m + 6, m + 6, g).astype(np.uint8); return a
OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri"); os.makedirs(OUT, exist_ok=True)
HEITTO, ALKU, DENSE_UNTIL = 29, 28, 40
video = sys.argv[1]
frames = [f.to_ndarray(format="rgb24").astype(np.float32) for f in av.open(video).decode(video=0)]
corner = np.concatenate([frames[0][:12, :12].reshape(-1, 3), frames[0][:12, -12:].reshape(-1, 3), frames[0][-12:, :12].reshape(-1, 3), frames[0][-12:, -12:].reshape(-1, 3)])
B = np.median(corner, axis=0); den = float(B[1] - max(B[0], B[2])); print("tausta", B)
def matte(c):
    spill = c[..., 1] - np.maximum(c[..., 0], c[..., 2])
    a = np.clip(1.0 - spill / den, 0, 1)
    a = np.clip((a - .04) / .92, 0, 1)                 # kohinaraja: puhdas tausta = 0, täysi = 1
    af = np.maximum(a, 1e-3)[..., None]
    F = (c - (1 - a)[..., None] * B) / af
    F = np.clip(F, 0, 255)
    g = np.minimum(F[..., 1], np.maximum(F[..., 0], F[..., 2]) + 4)   # jäännösvihreä pois
    F[..., 1] = g
    return np.dstack([F, a * 255]).clip(0, 255).astype(np.uint8)
def hero(i): return despill(key_multi(np.ascontiguousarray(frames[i][..., ::-1]).astype(np.uint8)))
def save_set(idx, prefix, scale, box=None, keyer=None):
    ms = [(keyer or (lambda i: matte(frames[i])))(i) for i in idx]
    if box is None:
        bs = []
        for m in ms:
            ys, xs = np.nonzero(m[..., 3] > 90)
            if len(xs): bs.append((xs.min(), xs.max(), ys.min(), ys.max()))
        box = (max(0, min(b[0] for b in bs) - 6), max(b[1] for b in bs) + 7, max(0, min(b[2] for b in bs) - 6), max(b[3] for b in bs) + 7)
    x0, x1, y0, y1 = box
    for old in os.listdir(OUT):
        if old.startswith(prefix + "_"): os.remove(os.path.join(OUT, old))
    w, h = int(round((x1 - x0) * scale)), int(round((y1 - y0) * scale))
    for k, m in enumerate(ms):
        Image.fromarray(m[y0:y1, x0:x1], "RGBA").resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "%s_%02d.png" % (prefix, k)))
    info = {"x0": int(x0), "x1": int(x1), "y0": int(y0), "y1": int(y1), "scale": scale, "frames": len(idx), "src_frames": [int(i) for i in idx], "w": w, "h": h}
    json.dump(info, open(os.path.join(HERE, "crop_%s.json" % prefix), "w")); print(prefix, len(idx), "kuvaa", w, "x", h); return info
n = len(frames)
save_set(list(range(0, HEITTO)), "taikuri_savu_heitto", .75, keyer=hero)
idx = list(range(ALKU, DENSE_UNTIL)) + list(range(DENSE_UNTIL, n, 3))
if (n - 1) not in idx: idx.append(n - 1)
save_set(idx, "savu_rajahdys", .75, box=(0, 1280, 0, 720))
