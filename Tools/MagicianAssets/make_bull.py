"""Taikurin härkä (muodonmuutos + juoksu): vihreätaustainen video (napsautus, taikasäteet ja violetti savu, härkä juoksee pölyn ja nopeusviivojen keskellä) -> kaksi kuvasarjaa samalla rajauksella ja pivotilla:
  taikuri_harka_NN         kuvat 0..126 joka toinen (12 kuvaa/s): napsautus, muodonmuutos ja härän ilmestyminen
  taikuri_harka_juoksu_NN  kuvat 130..142 joka kuva (24 kuvaa/s): saumaton juoksusilmukka (13 kuvaa, valittu runkomaskin eron mukaan)
Läpikuultavat tehosteet (sähkö, savu, pöly) a-ratkaisulla (kuten make_savupommi.py): a = 1 - (g - max(r,b)) / (Bg - max(Br,Bb)).
Pivot: taikurin jalat videon kuvassa 0 (härkä juoksee samasta kohdasta eteenpäin). Tallentaa crop_harka.json (pivot-arvot make_pivots.py:lle).
Ääni: videon äänet erikseen (Resources/Sfx/Taikuri/harka_*.wav, make_bull_audio.py). Ajo: python Tools/MagicianAssets/make_bull.py <video>"""
import os, sys, json
import av, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri")
video = sys.argv[1]; SC = 0.75
frames = [f.to_ndarray(format="rgb24").astype(np.float32) for f in av.open(video).decode(video=0)]
corner = np.concatenate([frames[0][:12, :12].reshape(-1, 3), frames[0][:12, -12:].reshape(-1, 3), frames[0][-12:, :12].reshape(-1, 3), frames[0][-12:, -12:].reshape(-1, 3)])
B = np.median(corner, axis=0); den = float(B[1] - max(B[0], B[2]))
def matte(c):
    spill = c[..., 1] - np.maximum(c[..., 0], c[..., 2]); a = np.clip(1.0 - spill / den, 0, 1); a = np.clip((a - .04) / .92, 0, 1)
    F = np.clip((c - (1 - a)[..., None] * B) / np.maximum(a, 1e-3)[..., None], 0, 255)
    F[..., 1] = np.minimum(F[..., 1], np.maximum(F[..., 0], F[..., 2]) + 4)
    return np.dstack([F, a * 255]).clip(0, 255).astype(np.uint8)
A_IDX = list(range(0, 128, 2)); B_IDX = list(range(130, 143))
allidx = A_IDX + B_IDX; ms = {i: matte(frames[i]) for i in allidx}
bs = []
for i in allidx:
    ys, xs = np.nonzero(ms[i][..., 3] > 90)
    if len(xs): bs.append((xs.min(), xs.max(), ys.min(), ys.max()))
x0 = max(0, min(b[0] for b in bs) - 6); x1 = min(1280, max(b[1] for b in bs) + 7); y0 = max(0, min(b[2] for b in bs) - 6); y1 = min(720, max(b[3] for b in bs) + 7)
a0 = ms[0]; ys, xs = np.nonzero(a0[..., 3] > 90); gy = ys.max() + 1
band = a0[int(ys.max() - .08 * (ys.max() - ys.min())):ys.max() + 1, :, 3] > 90; fx = float(np.nonzero(band.any(0))[0].mean())
w, h = int(round((x1 - x0) * SC)), int(round((y1 - y0) * SC)); W4, H4 = (w + 3) // 4 * 4, (h + 3) // 4 * 4
for old in os.listdir(OUT):
    if old.startswith("taikuri_harka_") or (old.startswith("taikuri_harka") and old.endswith(".png")): os.remove(os.path.join(OUT, old))
def save(idx, prefix):
    for k, i in enumerate(idx):
        im = Image.fromarray(ms[i][y0:y1, x0:x1], "RGBA").resize((w, h), Image.LANCZOS)
        c = Image.new("RGBA", (W4, H4), (0, 0, 0, 0)); c.paste(im, (0, H4 - h)); c.save(os.path.join(OUT, "%s_%02d.png" % (prefix, k)))
save(A_IDX, "taikuri_harka"); save(B_IDX, "taikuri_harka_juoksu")
px = (fx - x0) * SC / W4; py = (H4 - h + (y1 - gy) * SC) / H4   # jalat: lattia = gy (videon y), kuvan alareunan yläpuolella
py = (y1 - gy) * SC / H4
info = {"w": W4, "h": H4, "px": round(px, 4), "py": round(py, 4), "a": len(A_IDX), "b": len(B_IDX), "x0": int(x0), "y0": int(y0), "scale": SC}
json.dump(info, open(os.path.join(HERE, "crop_harka.json"), "w")); print(info)
