"""Khan osa 3: video G (suojaus: kääriytyy viittaansa) ja H (uudet iskut: jabit, alakoukku, pitkä suora, hyppypotku) -> khan_suojaus, khan_isku2, khan_isku3, khan_alakoukku, khan_suora, khan_hyppy + pivots.json (lisätään).
G: suojaus 24-150 (kääriytyminen 24-48, pito 48-112, avautuminen 112-150). H: isku2 24-50, isku3 51-71, alakoukku 72-90, suora 90-124, hyppy 126-230 (kyykky 126-147, hyppypotku 150-200, alastulo 202-230).
Ajo: python Tools/KhanAssets/make_khan3.py <G.mp4> <H.mp4>"""


import os, sys, json, glob
import av, cv2, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
for p in ("TonyAssets", "KingAssets", "GymAssets"): sys.path.insert(0, os.path.join(HERE, "..", p))
import rekey_tony as rt
OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Khan"); os.makedirs(OUT, exist_ok=True)
SCALE = 0.75; STEP = 2; LIM = 1070


def despill(a):
    g = a[..., 1].astype(np.int16); m = np.maximum(a[..., 0], a[..., 2]).astype(np.int16)
    a[..., 1] = np.where(g > m + 6, m + 6, g).astype(np.uint8); return a

def load(path, smoke=False, white=False):
    out = []
    for f in av.open(path).decode(video=0):
        bgr = f.to_ndarray(format="bgr24")
        if smoke:   # vihreän päällä tumma savu (g > r + 22, ei kirkas) pois ennen avainnusta -> maalataan taustan väriksi
            b, g, r = [bgr[..., i].astype(np.int16) for i in range(3)]
            sm = ((g - r > 22) & (g < 175) & (r < 120)) | (g - np.maximum(r, b) > 38)   # tumma savu + vihreä hohto/kehä
            if white: sm |= (np.minimum(r, b) > 165) & (np.abs(r - b) < 40)   # valkoinen tuuli (läpikuultava kajo Khanin päällä jää: sitä ei voi erottaa)
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

def save_set(name, frames, x_lim=None, anchor=None):
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
    px = float(np.median(xs)) / w; ground = max(bots) + 1; py = (h - ground) / h
    if anchor is not None:   # sama kiintopiste videon koordinaateissa (jalat kuvassa 0) kaikille sarjoille: jatkuvuus kaatumisessa ja nousussa
        px = (anchor[0] - x0) * SCALE / w; py = (y1 - anchor[1]) * SCALE / h
    print(name, len(sel), "kuvaa", w, "x", h)
    return {"n": len(sel), "w": w, "h": h, "px": round(px, 4), "py": round(py, 4), "x0": int(x0), "y1": int(y1)}



def anc(a):
    fx, fb = feet_x(a); return (fx, fb + 1)
G_, H_ = sys.argv[1:3]
pv = json.load(open(os.path.join(OUT, "pivots.json")))
fg = load(G_)
ag = anc(fg[0])
pv["khan_suojaus"] = save_set("suojaus", fg[24:152:STEP], anchor=ag)
del fg
fh = load(H_)
ah = anc(fh[0])
for nm, (s_, e_) in {"isku2": (24, 51), "isku3": (51, 72), "alakoukku": (72, 91), "suora": (90, 125), "hyppy": (126, 232)}.items():
    pv["khan_" + nm] = save_set(nm, fh[s_:e_:STEP], anchor=ah)
json.dump(pv, open(os.path.join(OUT, "pivots.json"), "w"), indent=1)
print("valmis")
