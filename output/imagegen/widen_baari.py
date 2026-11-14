"""Levittää vegas_katu_baari.png:n yhden ikkunan verran (277 px). Vain olemassa olevia pikseleitä monistetaan, ei uutta kuvaa.
Kaistat (y): transomi, kornissi+neon+otsapalkki, ikkuna, jalusta+alapaneeli; kaistojen väli ja saumat häivytetään.
Aukko (läpinäkyvä) kapenee D:llä, jotta kuvan leveys ja oikea reuna (kasinon liitos) pysyvät ennallaan."""
import numpy as np, sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
D = 277
im = Image.open(src).convert('RGBA'); A = np.array(im).astype(np.float32)
H, W, _ = A.shape
# kaistan lähde x0, lisäyskohta ix, saumojen crossfade
specs = [(375, 375, 90), (350, 495, 110), (495, 495, 40), (350, 495, 110)]
bounds = [150, 370, 1020]; HW = 25
GAP = 2650

def widen(sx, ix, cf):
    seg = A[:, sx:sx + D]
    new = np.concatenate([A[:, :ix], seg, A[:, ix:]], axis=1)
    def blend(j, ext_start):     # vasen puoli j:n edellä -> jatkuu kuten ext_start:sta alkava sisältö
        for k in range(cf):
            t = (k + 1) / (cf + 1)
            new[:, j - cf + k] = (1 - t) * new[:, j - cf + k] + t * A[:, ext_start - cf + k]
    if sx != ix: blend(ix, sx)
    if sx + D != ix: blend(ix + D, ix)
    return new[:, :W]
R = [widen(*s) for s in specs]
# aukon kavennus: GAP+D asti rakennettu, sen jälkeen alkuperäinen; sauma jalkakäytävällä häivytetty
res = np.zeros_like(A)
ys = np.arange(H, dtype=np.float32)
w = np.zeros((4, H), np.float32)
edges = [-1e9] + bounds + [1e9]
for i in range(4):
    lo, hi = edges[i], edges[i + 1]
    up = np.clip((ys - (lo - HW)) / (2 * HW), 0, 1) if lo > -1e8 else np.ones(H)
    dn = np.clip(((hi + HW) - ys) / (2 * HW), 0, 1) if hi < 1e8 else np.ones(H)
    w[i] = np.minimum(up, dn)
w /= w.sum(axis=0, keepdims=True)
for i in range(4): res += R[i] * w[i][:, None, None]
res[:, GAP + D:] = A[:, GAP + D:]
cf = 160
for k in range(cf):
    t = (k + 1) / (cf + 1); x = GAP + D - cf + k
    res[:, x] = (1 - t) * res[:, x] + t * A[:, x]
Image.fromarray(np.clip(res, 0, 255).astype(np.uint8), 'RGBA').save(out)
