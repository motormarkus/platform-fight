"""Warden: suojaus (2x2-kuva) ja uppercut-hyppy (zip, 121 ruutua 682x512, vihreä tausta) -> warden_suojaus.png, warden_uppercut.png.
Uppercut: lataus (kyykky) 26-46, ponnistus 51, ilmassa 56-101, lasku 116-121. Jalat ankkuroidaan jokaisessa kuvassa omaan pohjaansa (hypyn korkeuden hoitaa Enemy.jumpHeight);
videon zoom vaihtelee, joten kuvakohtainen mittakaava (ZOOM). Ajo: python Tools/PrisonAssets/make_warden_extra.py <block.png> <ruutukansio> [ulos]"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_suojaus": (768, 320, 768), "v_uppercut": (1024, 512, 1024)})

def clean(im):
    a = (im[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, 8)
    if n <= 2: return im
    big = st[1:, cv2.CC_STAT_AREA].max(); out = im.copy()
    for j in range(1, n):
        if st[j, cv2.CC_STAT_AREA] < 0.04 * big: out[lab == j, 3] = 0
    return out

ZOOM = {26: 1.0, 31: 1.0, 36: 1.0, 41: 1.0, 46: 1.0, 51: 1.05, 56: 1.05, 62: 1.05, 70: 1.05, 80: 1.05, 90: 1.05, 101: 1.05, 116: 1.05, 121: 1.0}

def own_floor(im, extra=0):
    ys, xs = np.where(im[..., 3] > 10); return ys.max() + 1 + extra

def main(block, framedir, outs):
    # suojaus: 2x2 -> 4 kuvaa, mittakaava niin että ensimmäisen kuvan korkeus = 640
    b = cv2.imread(block, cv2.IMREAD_UNCHANGED); H, W = b.shape[:2]
    quads = [b[0:H // 2, 0:W // 2], b[0:H // 2, W // 2:], b[H // 2:, 0:W // 2], b[H // 2:, W // 2:]]
    k0 = None; cells = []
    m.setdims("v_suojaus")
    for q in quads:
        q = cv2.cvtColor(q, cv2.COLOR_BGRA2RGBA); q = np.ascontiguousarray(q)
        x0, x1, y0, y1 = m.bbox(q)
        if k0 is None: k0 = 640.0 / (y1 - y0)
        cells.append(m.cell(q, "low", k0, own_floor(q)))
    m.save(cells, f"{outs}/warden_suojaus.png"); print("suojaus", len(cells), "kuvaa k", round(k0, 3))
    # uppercut
    fr = {i: clean(m.key_frame(cv2.imread(os.path.join(framedir, "frame_%03d.png" % i)))) for i in ZOOM}
    x0, x1, y0, y1 = m.bbox(fr[26]); k = 640.0 / (y1 - y0)
    m.setdims("v_uppercut"); cells = []
    for i, z in ZOOM.items():
        mode = "low" if i <= 46 or i >= 116 else "com"
        cells.append(m.cell(fr[i], mode, k / z, own_floor(fr[i], 14 if i == 116 else 0)))
    m.save(cells, f"{outs}/warden_uppercut.png"); print("uppercut", len(cells), "kuvaa", list(ZOOM), "k", round(k, 3))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
