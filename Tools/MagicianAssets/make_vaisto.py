"""Ruudukkokuvasarja (kuvat 3x2 läpinäkyvällä taustalla) -> taikuri_<nimi>_NN. Taikurin väistö ja osumareaktio.
Ajo: python Tools/MagicianAssets/make_vaisto.py [lähde.png=vaisto/vaisto_lahde.png] [nimi=vaisto]
(vanha kuvaus:) Taikurin väistö: kuusi kuvaa läpinäkyvällä taustalla (3x2 ruudukko, 1536x1024, vaisto_lahde.png) -> taikuri_vaisto_00..05.
Jokainen kuva rajataan alfan mukaan, skaalataan samaan kokoon kuin taikurin videosarjat (seisova taikuri 388 px = 0,75 x videon 517 px)
ja ankkuroidaan jalkoihin (alin kohta + jalkaterien keskikohta) yhteiseen rajaukseen. Ajo: python Tools/MagicianAssets/make_vaisto.py"""
import os, json
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri")
import sys
SRC = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "vaisto", "vaisto_lahde.png"); NAME = sys.argv[2] if len(sys.argv) > 2 else "vaisto"
im = np.array(Image.open(SRC).convert("RGBA")); H, W = im.shape[:2]; cw, ch = W // 3, H // 2
cells = []
for r in range(2):
    for c in range(3):
        a = im[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw].copy(); a[a[..., 3] < 14, 3] = 0
        ys, xs = np.nonzero(a[..., 3] > 40); cells.append((a, xs.min(), xs.max(), ys.min(), ys.max()))
h0 = cells[0][4] - cells[0][3] + 1; S = 388.0 / h0; print("skaala", round(S, 3), "kuvan 0 korkeus", h0)
anch = []
for a, x0, x1, y0, y1 in cells:
    low = a[int(y1 - .10 * (y1 - y0)):y1 + 1, :, 3] > 40; xs = np.nonzero(low.any(0))[0]; anch.append((float(np.mean(xs)), float(y1)))   # jalkaterien x, pohja
L = max(ax - c[1] for (ax, _), c in zip(anch, cells)); R = max(c[2] - ax for (ax, _), c in zip(anch, cells)); T = max(ay - c[3] for (_, ay), c in zip(anch, cells))
CW, CH = int(np.ceil((L + R + 8) * S)), int(np.ceil((T + 8) * S)); print("kuva", CW, "x", CH)
for old in os.listdir(D):
    if old.startswith("taikuri_%s_" % NAME): os.remove(os.path.join(D, old))
for k, ((a, x0, x1, y0, y1), (ax, ay)) in enumerate(zip(cells, anch)):
    crop = Image.fromarray(a, "RGBA").resize((int(round(a.shape[1] * S)), int(round(a.shape[0] * S))), Image.LANCZOS)
    canvas = Image.new("RGBA", (CW, CH), (0, 0, 0, 0)); ox = int(round(L * S - ax * S + 4 * S / 1)); oy = int(round(CH - 4 - ay * S - 0))
    canvas.alpha_composite(crop, (max(0, ox) if ox >= 0 else 0, 0) if False else (ox, oy + 0))
    canvas.save(os.path.join(D, "taikuri_%s_%02d.png" % (NAME, k)))
json.dump({"frames": 6, "w": CW, "h": CH, "scale_vs_video": .75, "pivot": "jalat: alareuna, jalkaterien keskikohta x = %.1f" % (L * S + 4 * S)}, open(os.path.join(HERE, "crop_%s.json" % NAME), "w"))
print("ok")
