"""Taikurin teleporttisavu: pieni savupuuska savupommin räjähdyksen (savu_rajahdys_NN) savusta. Rajataan pilven keskiosa, pienennetään taikurin kokoiseksi,
räjähdyksen oranssi välähdys ja roiskeet pois (vain harmaa savu), kevyt violetti sävy ja loppu häipyy. Ajo: python Tools/MagicianAssets/make_poof.py"""
import os, json
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); D = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri")
_info = json.load(open(os.path.join(HERE, "crop_savu_rajahdys.json"))); _vf = _info["src_frames"]; F = _info["scale"] / .55   # savu_rajahdys-kuvien skaala (alun perin .55)
def _set_idx(video_frame): return int(np.argmin([abs(v - video_frame) for v in _vf]))
SRC = [_set_idx(v) for v in list(range(52, 69, 2)) + [68] * 3]    # pilven keskiosa, ei taikuria (videon kuvat >= 52)
CW, CH, SC = 270, 230, .56 / F             # kuvan koko ja pilven perusskaala (maailmankoko pysyy samana kuin .55-versiossa)
for old in os.listdir(D):
    if old.startswith("taikuri_poof_"): os.remove(os.path.join(D, old))
def ease(x): x = min(1.0, max(0.0, x)); return 1 - (1 - x) ** 3
n = len(SRC); out = []
for k, i in enumerate(SRC):
    a = np.array(Image.open(os.path.join(D, "savu_rajahdys_%02d.png" % i))).astype(np.float32)[int(40 * F):int(396 * F), int(120 * F):int(584 * F)]   # 464x356
    sat = a[..., :3].max(2) - a[..., :3].min(2); a[..., 3] *= np.clip(1.0 - (sat - 25) / 40, 0, 1)             # vain harmaa savu
    a[..., 0] = a[..., 0] * .94 + 10; a[..., 1] = a[..., 1] * .9 + 6; a[..., 2] = a[..., 2] * 1.02 + 22        # kevyt violetti sävy
    h, w = a.shape[:2]; yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h * .52) / (h * .52)) ** 2)                               # pehmeä soikea reuna (ei suorakulmiota)
    a[..., 3] *= np.clip((1.0 - r) / .28, 0, 1)
    s = SC * (.45 + .55 * ease(k / 5.0))                                                                          # puhallus: pilvi kasvaa alussa
    im = Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA").resize((int(w * s), int(h * s)), Image.LANCZOS)
    fade = 1.0 if k < n - 5 else max(0.0, (n - 1 - k) / 5.0)
    arr = np.array(im).astype(np.float32); arr[..., 3] *= fade
    canvas = Image.new("RGBA", (CW, CH), (0, 0, 0, 0)); canvas.alpha_composite(Image.fromarray(arr.astype(np.uint8), "RGBA"), ((CW - im.width) // 2, CH - im.height + 8))   # alareuna ankkurina
    out.append(canvas)
for k, c in enumerate(out): c.save(os.path.join(D, "taikuri_poof_%02d.png" % k))
json.dump({"frames": len(out), "w": CW, "h": CH, "fps": 20, "pivot": "alareuna keskellä"}, open(os.path.join(HERE, "crop_taikuri_poof.json"), "w"))
print("poof", len(out), "kuvaa", CW, "x", CH)

# pieni ja nopea versio (väistön teleportti): sama savu, 0,42 x koko, 8 kuvaa, kasvaa heti ja häipyy nopeasti
small = []
for k in range(8):
    src = Image.open(os.path.join(D, "taikuri_poof_%02d.png" % min(len(out) - 1, [0, 1, 2, 3, 4, 6, 8, 9][k]))).convert("RGBA")
    arr = np.array(src).astype(np.float32); arr[..., 3] *= (1.0 if k < 4 else (7 - k) / 4.0)
    im = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA"); w2, h2 = int(CW * .42), int(CH * .42)
    small.append(im.resize((w2, h2), Image.LANCZOS))
for old in os.listdir(D):
    if old.startswith("taikuri_poofpieni_"): os.remove(os.path.join(D, old))
for k, c in enumerate(small): c.save(os.path.join(D, "taikuri_poofpieni_%02d.png" % k))
json.dump({"frames": len(small), "w": small[0].width, "h": small[0].height, "fps": 24, "pivot": "alareuna keskellä"}, open(os.path.join(HERE, "crop_taikuri_poofpieni.json"), "w"))
print("poofpieni", len(small), "kuvaa", small[0].size)
