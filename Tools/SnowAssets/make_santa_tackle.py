"""Joulupukin juoksu (saumaton silmukka, 26 ruudun jakso) ja hyppytaklaus (nopeutettu) -> Assets/Sprites/Joulupukki/pukki_juoksu.png, pukki_taklaus.png
Video 1280x720 @24 fps; yhteinen kiinteä rajaus, mittakaava 0.75 (sama kuin muut pukin sheetit).
Taklaus: valmistelu + juoksu-alku, ponnistus, liito (joka 4. ruutu, nyrkki suorana ~0.8 s), lasku, nousu. Ruutuväli 0.05 s (joka toinen videoruutu = 1.67x nopeampi).
Ajo: python make_santa_tackle.py <ruutukansio f_%03d.png>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]; SC = 0.75
X0, X1, Y0, Y1 = 250, 1040, 40, 716
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f * a[..., None], a])               # esikerrottu
def load(i): return key(cv2.imread(os.path.join(src, "f_%03d.png" % i)))[Y0:Y1, X0:X1]
def fin(f):
    a = f[..., 3:4]; rgb = np.where(a > 1e-3, f[..., :3] / np.maximum(a, 1e-3), 0)
    return cv2.resize(np.dstack([rgb, a * 255]).clip(0, 255).astype(np.uint8), None, fx=SC, fy=SC, interpolation=cv2.INTER_AREA)
def save(name, cells, cols=8):
    ch, cw = cells[0].shape[:2]; rows = (len(cells) + cols - 1) // cols
    s = np.zeros((rows * ch, min(cols, len(cells)) * cw, 4), np.uint8)
    for k, c in enumerate(cells): s[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
    cv2.imwrite(os.path.join(OUT, name), s); print(name, len(cells), "ruutua, solu", cw, "x", ch)
# juoksu: 26 ruudun jakso, viimeiset 6 ristiinhäivytetään jaksoa edeltäviin ruutuihin
START, PER, FADE = 58, 26, 6
seq = []
for k in range(PER):
    f = load(START + k)
    if k >= PER - FADE:
        w = (k - (PER - FADE) + 1) / (FADE + 1); f = f * (1 - w) + load(START - FADE + (k - (PER - FADE))) * w
    seq.append(f)
run = [fin(f) for f in seq[::2]]
save("pukki_juoksu.png", run)
# taklaus
idx = ([33, 37, 41, 45, 49, 53, 57, 61, 65, 69, 73, 77, 81, 85, 89, 93, 95, 97, 99, 101, 103, 105] + list(range(109, 173, 4)) + list(range(173, 207, 2)) + [207, 209, 211, 213, 215, 217])
save("pukki_taklaus.png", [fin(load(i)) for i in idx])
pv = np.hstack([run[0], run[3], run[7], fin(load(101)), fin(load(141)), fin(load(185)), fin(load(205))]); m = pv[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), cv2.resize((np.full(pv.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + pv[..., :3] * m).astype(np.uint8), None, fx=0.5, fy=0.5))
