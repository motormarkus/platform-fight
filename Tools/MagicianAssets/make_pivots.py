"""Taikurin kuvasarjojen pivotit (jalat) -> Assets/Sprites/Taikuri/pivots.json (Editor/MagicianSetup lukee).
Sarjan pivot x = jalkaterien keskikohta (alimmat 8 % alfa-rivit), mediaani sarjan yli ("first": ensimmäisen kuvan, makuukuville); y = lattia = sarjan alin piste.
savu_rajahdys: koko videoruutu skaalattuna, pivot = taikurin jalat savupommivideossa (heitosarjan kuvasta 0 laskettuna)."""
import os, json, glob
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri")
SETS = ["taikuri_isku", "taikuri_kyyhky", "taikuri_silinteri", "taikuri_savu_heitto", "taikuri_kaatuminen", "taikuri_vaisto", "taikuri_osuma", "taikuri_kavely", "taikuri_juoksu"]
def pad4():   # kuvien koot neljällä jaollisiksi (pakkaus): lisätään läpinäkyvää ylä- ja oikeareunaan (jalat/pohja pysyvät)
    for f in glob.glob(os.path.join(D, "*.png")):
        im = Image.open(f); W, H = im.size; nw, nh = (W + 3) // 4 * 4, (H + 3) // 4 * 4
        if (nw, nh) != (W, H):
            c = Image.new("RGBA", (nw, nh), (0, 0, 0, 0)); c.paste(im.convert("RGBA"), (0, nh - H)); c.save(f)
pad4()
def feet_x(a):
    ys, xs = np.nonzero(a[..., 3] > 90); y1 = ys.max(); y0 = ys.min(); band = a[int(y1 - .08 * (y1 - y0)):y1 + 1, :, 3] > 90
    cols = np.nonzero(band.any(0))[0]; return float(cols.mean()), int(y1)
out = {}
for s in SETS:
    fs = sorted(glob.glob(os.path.join(D, s + "_[0-9]*.png"))); xs = []; bottoms = []
    for f in fs:
        a = np.array(Image.open(f)); x, b = feet_x(a); xs.append(x); bottoms.append(b)
    H = a.shape[0]; W = a.shape[1]; px = (xs[0] if s == "taikuri_kaatuminen" else float(np.median(xs))) / W; ground = max(bottoms) + 1
    out[s] = {"n": len(fs), "w": W, "h": H, "px": round(px, 4), "py": round((H - ground) / H, 4)}
# härkä (make_bull.py kirjoittaa pivotin: jalat videon kuvasta 0)
hk = json.load(open(os.path.join(HERE, "crop_harka.json")))
out["taikuri_harka"] = {"n": hk["a"], "w": hk["w"], "h": hk["h"], "px": hk["px"], "py": hk["py"]}
out["taikuri_harka_juoksu"] = {"n": hk["b"], "w": hk["w"], "h": hk["h"], "px": hk["px"], "py": hk["py"]}
# savupommivideon taikurin jalat (videokoordinaatit) heitosarjan kuvasta 0
hj = json.load(open(os.path.join(HERE, "crop_taikuri_savu_heitto.json"))); a0 = np.array(Image.open(os.path.join(D, "taikuri_savu_heitto_00.png"))); fx, fb = feet_x(a0)
vx = hj["x0"] + fx / hj["scale"]; vy = hj["y0"] + (fb + 1) / hj["scale"]
ej = json.load(open(os.path.join(HERE, "crop_savu_rajahdys.json")))
out["savu_rajahdys"] = {"n": ej["frames"], "w": ej["w"], "h": ej["h"], "px": round(vx / 1280.0, 4), "py": round(1 - vy / 720.0, 4), "src_frames": ej["src_frames"]}
# kaatumisvideon taikurin jalat ja hatun rata
kj = json.load(open(os.path.join(HERE, "crop_kaatuminen.json"))); k0 = np.array(Image.open(os.path.join(D, "taikuri_kaatuminen_00.png"))); kx, kb = feet_x(k0)
out["hattu"] = {"feet_x_video": round(kj["x0"] + kx / kj["scale"], 1), "feet_y_video": round(kj["y0"] + (kb + 1) / kj["scale"], 1), "scale": kj["scale"], "detach": kj["detach"], "reattach": kj["reattach"], "track": kj["hat"]["track"], "rest_angle": kj["hat"]["rest_angle"], "w": kj["hat"]["w"], "h": kj["hat"]["h"]}
json.dump(out, open(os.path.join(D, "pivots.json"), "w"), indent=1)
for k, v in out.items():
    if k != "hattu": print(k, {kk: v[kk] for kk in v if kk != "src_frames"})
print("hattu jalat video", out["hattu"]["feet_x_video"], out["hattu"]["feet_y_video"])
