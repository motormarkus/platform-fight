"""Tulitikun sytytys (hero kyykistyy, raapaisee tikun saappaaseen ja nousee liekki kädessä) videosta.
Kuvat: Assets/Resources/Jerrycan/sytyta_NN.png, 900x768, samassa mittakaavassa kuin heron idle-kuva (hahmo 715 px, jalat 9 px alareunasta,
hahmon keskikohta 75 px solun keskeltä oikealle (leveä solu, ettei etujalka leikkaannu)), joten ne piirretään heron kuvan paikalle samalla skaalalla.
Ajo: python Tools/JerrycanAssets/make_lighting.py <video.mov>"""
import sys, cv2, numpy as np
OUT = "G:/unity pelit/mopopeli/platform fight/Assets/Resources/Jerrycan/"
T1, T2 = 25, 100
START, END, STEP = 6, 60, 2
CW, CH, FOOT, FIG, CX = 900, 768, 9, 715.0, 525

def key(f, bg):
    b, g, r = [f[..., k].astype(np.float32) for k in range(3)]
    gd = g - np.maximum(r, b)
    a = np.clip((T2 - gd) / (T2 - T1), 0, 1); a = a * a * (3 - 2 * a)
    af = np.maximum(a, 0.04)[..., None]
    col = np.clip((f.astype(np.float32) - (1 - a)[..., None] * bg) / af, 0, 255)
    col[..., 1] = np.minimum(col[..., 1], np.maximum(col[..., 0], col[..., 2]) + 2)
    return col, a

cap = cv2.VideoCapture(sys.argv[1]); frames = []
while True:
    ok, f = cap.read()
    if not ok: break
    frames.append(f)
bg = np.median(frames[0][:20, :20].reshape(-1, 3), axis=0).astype(np.float32)
col0, a0 = key(frames[0], bg)
ys, xs = np.where(a0 > 0.5)
cx0, bot0, h0 = (xs.min() + xs.max()) / 2, ys.max(), ys.max() - ys.min()
k = FIG / h0
print("alkufrämin hahmo", h0, "px, kerroin", round(k, 3))
n = 0
for i in range(START, min(END, len(frames) - 1) + 1, STEP):
    col, a = key(frames[i], bg)
    rgba = np.dstack([col * a[..., None], a * 255]).astype(np.float32)
    big = cv2.resize(rgba, (int(rgba.shape[1] * k), int(rgba.shape[0] * k)), interpolation=cv2.INTER_AREA)
    cell = np.zeros((CH, CW, 4), np.float32)
    ox, oy = int(CX - cx0 * k), int(CH - FOOT - 1 - bot0 * k)   # kuvan origo solussa
    sx0, sy0 = max(0, -ox), max(0, -oy)
    dx0, dy0 = max(0, ox), max(0, oy)
    w = min(big.shape[1] - sx0, CW - dx0); hh = min(big.shape[0] - sy0, CH - dy0)
    cell[dy0:dy0 + hh, dx0:dx0 + w] = big[sy0:sy0 + hh, sx0:sx0 + w]
    al = cell[..., 3:4] / 255.0
    rgb = np.where(al > 0.01, cell[..., :3] / np.maximum(al, 0.01), 0)
    out = np.dstack([np.clip(rgb, 0, 255), cell[..., 3]]).astype(np.uint8)
    cv2.imwrite(OUT + "sytyta_%02d.png" % n, out); n += 1
print(n, "kuvaa; liekki kädessä n. kuva", (48 - START) // STEP, "(lähdeframe 48), 30 fps / askel", STEP, "=", 30 // STEP, "kuvaa/s")
