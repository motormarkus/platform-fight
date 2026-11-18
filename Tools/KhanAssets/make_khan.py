"""Khan (Tsingiskaani, Muurin tornin katon pomo): kolme vihreätaustaista videota (1280x720, 24 fps, 240 kuvaa) -> kuvasarjat Assets/Sprites/Khan/khan_<sarja>_NN.png + pivots.json.
A: idle 0-57, kävely (silmukka 92-125), juoksu (silmukka 170-195)
B: isku (jabi+suora) 0-28, korkea potku 29-58, matala potku 59-87, kierrepotku 88-107, syöksy 150-239 (108-149 vartiotilaa, ei omaa liikettä)
C: erikoisliike (lohikäärmeen henkäys) 0-239, rajaus vain Khan + lohikäärmeen pää (x <= LIM) -> tuli tehdään koodilla; lohikäärmeen pää omana kuvana (khan_paa.png) ja liekkipalat (khan_liekki_NN.png).
Kuvat joka toinen (24 -> 12 kuvaa/s), jalat = pivot (mediaani), skaala 0,75, ppu 108 (Editor/KhanSetup). Ajo: python Tools/KhanAssets/make_khan.py <A.mp4> <B.mp4> <C.mp4>"""
import os, sys, json, glob
import av, cv2, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
for p in ("TonyAssets", "KingAssets", "GymAssets"): sys.path.insert(0, os.path.join(HERE, "..", p))
import rekey_tony as rt
OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Khan"); os.makedirs(OUT, exist_ok=True)
SCALE = 0.75; STEP = 2; LIM = 1070
A, B, C = sys.argv[1:4]

def despill(a):
    g = a[..., 1].astype(np.int16); m = np.maximum(a[..., 0], a[..., 2]).astype(np.int16)
    a[..., 1] = np.where(g > m + 6, m + 6, g).astype(np.uint8); return a

def load(path, smoke=False):
    out = []
    for f in av.open(path).decode(video=0):
        bgr = f.to_ndarray(format="bgr24")
        if smoke:   # vihreän päällä tumma savu (g > r + 22, ei kirkas) pois ennen avainnusta -> maalataan taustan väriksi
            b, g, r = [bgr[..., i].astype(np.int16) for i in range(3)]
            sm = ((g - r > 22) & (g < 175) & (r < 120)) | (g - np.maximum(r, b) > 38)   # tumma savu + vihreä hohto/kehä
            bg = bgr[2, 2].copy(); bgr = bgr.copy(); bgr[sm] = bg
        out.append(despill(rt.key_soft(bgr)))
    return out

def crop_set(frames, x_lim=None):
    boxes = []
    for a in frames:
        al = a[..., 3] > 40
        if x_lim: al[:, x_lim:] = False
        ys, xs = np.nonzero(al)
        if len(xs): boxes.append((xs.min(), xs.max(), ys.min(), ys.max()))
    x0 = max(0, min(b[0] for b in boxes) - 6); x1 = max(b[1] for b in boxes) + 7; y0 = max(0, min(b[2] for b in boxes) - 6); y1 = max(b[3] for b in boxes) + 7
    return x0, x1, y0, y1

def feet_x(a):
    ys, xs = np.nonzero(a[..., 3] > 90); y1 = ys.max(); y0 = ys.min(); band = a[int(y1 - .08 * (y1 - y0)):y1 + 1, :, 3] > 90
    cols = np.nonzero(band.any(0))[0]; return float(cols.mean()), int(y1)

def save_set(name, frames, x_lim=None):
    sel = frames
    x0, x1, y0, y1 = crop_set(sel, x_lim)
    if x_lim: x1 = min(x1, x_lim)
    w, h = int(round((x1 - x0) * SCALE)), int(round((y1 - y0) * SCALE)); w, h = (w + 3) // 4 * 4, (h + 3) // 4 * 4
    for old in glob.glob(os.path.join(OUT, "khan_%s_[0-9]*.png" % name)): os.remove(old)
    xs, bots = [], []
    for i, a in enumerate(sel):
        c = a[y0:y1, x0:x1].copy()
        if x_lim:   # oikea reuna: pehmeä häivytys, ettei videon leikkaus näy
            fade = max(1, int(26)); c[:, -fade:, 3] = (c[:, -fade:, 3] * np.linspace(1, 0, fade)[None, :]).astype(np.uint8)
        im = Image.fromarray(c, "RGBA").resize((int(round((x1 - x0) * SCALE)), int(round((y1 - y0) * SCALE))), Image.LANCZOS)
        canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0)); canvas.paste(im, (0, h - im.size[1])); canvas.save(os.path.join(OUT, "khan_%s_%02d.png" % (name, i)))
        arr = np.array(canvas); fx, fb = feet_x(arr); xs.append(fx); bots.append(fb)
    px = float(np.median(xs)) / w; ground = max(bots) + 1
    print(name, len(sel), "kuvaa", w, "x", h)
    return {"n": len(sel), "w": w, "h": h, "px": round(px, 4), "py": round((h - ground) / h, 4), "x0": int(x0), "y1": int(y1)}

pv = {}
fa = load(A)
pv["khan_idle"] = save_set("idle", fa[0:58:STEP])
pv["khan_kavely"] = save_set("kavely", fa[92:126:STEP])
pv["khan_juoksu"] = save_set("juoksu", fa[170:196:STEP])
del fa
fb = load(B)
for nm, (s, e) in {"isku": (0, 29), "potku": (29, 59), "matala": (59, 82), "kierre": (88, 108), "syoksy": (150, 240)}.items():
    pv["khan_" + nm] = save_set(nm, fb[s:e:STEP])
del fb
fc = load(C, smoke=True)
pv["khan_erikois"] = save_set("erikois", fc[0:240:STEP], x_lim=LIM)

def save_png(a, name, fade_l=0, fade_r=0):
    im = a.copy()
    if fade_l: im[:, :fade_l, 3] = (im[:, :fade_l, 3] * np.linspace(0, 1, fade_l)[None, :]).astype(np.uint8)
    if fade_r: im[:, -fade_r:, 3] = (im[:, -fade_r:, 3] * np.linspace(1, 0, fade_r)[None, :]).astype(np.uint8)
    ys, xs = np.nonzero(im[..., 3] > 12); im = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    r = Image.fromarray(im, "RGBA"); r = r.resize((int(round(r.size[0] * SCALE)) // 4 * 4 or 4, int(round(r.size[1] * SCALE)) // 4 * 4 or 4), Image.LANCZOS); r.save(os.path.join(OUT, name)); return r.size
# lohikäärmeen pää (kuva 126: suu auki, tuli suusta): rajaus x 735-1075 (Khanin nyrkki jää pois), kaulan alku häivytetään
hx0, hx1, hy0, hy1 = 735, 1075, 25, 535
ph = save_png(fc[126][hy0:hy1, hx0:hx1], "khan_paa.png", fade_l=22)
pv["khan_paa"] = {"w": ph[0], "h": ph[1], "mouth_vx": 880.0, "mouth_vy": 305.0, "head_x0": hx0, "head_y0": hy0, "scale": SCALE}
# liekkipalat (suusta oikealle): kuvat 104-160, rajaus x 1045-1245 y 230-520, pehmeät reunat
for k, fi in enumerate(range(104, 162, 8)):
    save_png(fc[fi][230:520, 1045:1245], "khan_liekki_%d.png" % k, fade_l=20, fade_r=20)
pv["khan_liekki"] = {"n": len(range(104, 162, 8))}
json.dump(pv, open(os.path.join(OUT, "pivots.json"), "w"), indent=1)
print("valmis")
