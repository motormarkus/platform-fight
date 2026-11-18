"""Khan osa 2: video D (osuma, kaatuminen, maahan, nousu) ja E (puuska, tuuli poistettu: tuuli piirretään koodilla) -> khan_osuma/kaatuminen/maahan/nousu/puuska + pivots.json (lisätään).
D: osuma 0-14, kaatuminen 16-46 (ilmassa), maahan 46-100 (pomppu + makuu), nousu 100-190. E: puuska 0-216 (lataus 0-118, purkaus 118-216).
Ajo: python Tools/KhanAssets/make_khan2.py <D.mp4> <E.mp4>"""

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


def load_wind(path):
    """Khan + tuulikerros: tuuli säilytetään läpikuultavana valkoisena (alfa = valkoisen osuus vihreän päällä). Tuuli-pikselit (vihreänvoittoiset tai lähes valkoiset) erotetaan Khanista,
    Khan avainnetaan ilman niitä ja tuulen alfa lisätään päälle; Khanin reunan heikko valkoinen kehä (alfa < 0,4) poistetaan."""
    out = []
    # videon tuuli häivytetään kuvan reunoilla (ei äkillistä leikkausta): reunaan 160 px (sivut), 110 px (ylä), 70 px (ala)
    def smooth(v): v = np.clip(v, 0, 1); return v * v * (3 - 2 * v)
    xs_ = np.arange(1280, dtype=np.float32); ys_ = np.arange(720, dtype=np.float32)
    edge = (smooth(np.minimum(xs_, 1279 - xs_) / 160.0)[None, :] * smooth(ys_ / 110.0)[:, None] * smooth((719 - ys_) / 70.0)[:, None]).astype(np.float32)
    for f in av.open(path).decode(video=0):
        bgr = f.to_ndarray(format="bgr24")
        b, g, r = [bgr[..., i].astype(np.int16) for i in range(3)]
        sm = ((g - r > 22) & (g < 175) & (r < 120)) | (g - np.maximum(r, b) > 38) | ((np.minimum(r, b) > 165) & (np.abs(r - b) < 40))
        clean = bgr.copy(); clean[sm] = bgr[2, 2]
        kh = despill(rt.key_soft(clean))
        core = (kh[..., 3] > 128).astype(np.uint8)
        near = cv2.dilate(core, np.ones((7, 7), np.uint8)).astype(bool)
        bgmin = float(min(bgr[2, 2][2], bgr[2, 2][0]))
        wa = np.clip((np.minimum(r, b).astype(np.float32) - bgmin - 6) / (255.0 - bgmin - 6), 0, 1)
        wa = np.where(sm, wa, 0.0) * edge
        wa = np.where(near & (wa < 0.4), 0.0, wa); wa = np.where(wa < 0.04, 0.0, wa)
        wa = np.clip(cv2.GaussianBlur(wa, (0, 0), 0.8) * 1.12, 0, 1)
        res = kh.copy()
        wmask = (wa * 255 > kh[..., 3])
        res[..., 0][wmask] = 255; res[..., 1][wmask] = 255; res[..., 2][wmask] = 255
        res[..., 3] = np.maximum(kh[..., 3], (wa * 255).astype(np.uint8))
        out.append(res)
    return out

if len(sys.argv) > 3 and sys.argv[3] == "E":   # vain puuska uudelleen (tuuli mukana)
    pv = json.load(open(os.path.join(OUT, "pivots.json")))
    A_, B_ = sys.argv[1:3]
    fe = load_wind(B_)
    def anc(a):
        fx, fb = feet_x(a); return (fx, fb + 1)
    a0 = rt.key_soft(av.open(B_).decode(video=0).__next__().to_ndarray(format="bgr24"))
    pv["khan_puuska"] = save_set("puuska", fe[0:218:STEP], anchor=anc(despill(a0)))
    json.dump(pv, open(os.path.join(OUT, "pivots.json"), "w"), indent=1); print("puuska (tuulella) valmis"); sys.exit(0)

A_, B_ = sys.argv[1:3]
pv = json.load(open(os.path.join(OUT, "pivots.json")))
fd = load(A_)
def anc(a):
    fx, fb = feet_x(a); return (fx, fb + 1)
ad = anc(fd[0])
pv["khan_osuma"] = save_set("osuma", fd[0:16:STEP], anchor=ad)
pv["khan_kaatuminen"] = save_set("kaatuminen", fd[16:48:STEP], anchor=ad)
pv["khan_maahan"] = save_set("maahan", fd[46:102:STEP], anchor=ad)
pv["khan_nousu"] = save_set("nousu", fd[100:192:STEP], anchor=ad)
del fd
fe = load(B_, smoke=True, white=True)
pv["khan_puuska"] = save_set("puuska", fe[0:218:STEP], anchor=anc(fe[0]))
json.dump(pv, open(os.path.join(OUT, "pivots.json"), "w"), indent=1)
print("valmis")
