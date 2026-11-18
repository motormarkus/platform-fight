"""Kyyhkysen lento alkuperäisestä videosta: key_soft pudottaa irronneen kyyhkysen pois (kuvat 86+ ilman lintua), joten tässä avainnetaan key_multillä (irrotetut osat säilyvät),
vähennetään taikuri (key_soft-maski) ja tallennetaan jokainen lentokuva omaksi pieneksi kuvaksi: Assets/Resources/Taikuri/kyyhky_lento_NN.png (NN = kyyhky-sarjan kuvan numero)
+ kyyhky_lento.txt (rivit: numero keskiX keskiY, kuvapikseleitä kyyhky-kuvan koordinaatistossa, y alas). Ajo: python Tools/MagicianAssets/make_dove_flight.py"""
import os, sys, json, inspect
import av, numpy as np, cv2
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import rekey_tony as rt
src = inspect.getsource(rt.key_soft).replace("    if n2 > 2: bgm = bgm | (lab2 != 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])))\n", "").replace("def key_soft", "def key_multi")
ns = {"np": np, "cv2": cv2, "kv": rt.kv}; exec(src, ns); key_multi = ns["key_multi"]
V = "C:/Users/marku/.claude/uploads/1ce608b0-0f6d-41e0-b501-e782c570c2d7/9129749d-gemini_generated_video_367591D2.mp4"
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Taikuri")
C = json.load(open(os.path.join(HERE, "crop_kyyhky.json"))); X0, Y0, SC = C["x0"], C["y0"], C["scale"]
for f in os.listdir(OUT):
    if f.startswith("kyyhky_lento"): os.remove(os.path.join(OUT, f))
frames = [f.to_ndarray(format="bgr24") for f in av.open(V).decode(video=0)]
FIRST = 86; lines = []
for n in range(FIRST, 120):
    bgr = frames[n * 2]
    a_s = rt.key_soft(bgr); a_m = key_multi(bgr)
    body = cv2.dilate((a_s[..., 3] > 10).astype(np.uint8), np.ones((9, 9), np.uint8))
    al = a_m[..., 3].copy(); al[body > 0] = 0
    m = (al > 30).astype(np.uint8)
    k, lab, st, cen = cv2.connectedComponentsWithStats(m, 8)
    if k < 2: print(n, "ei lintua"); continue
    keep = np.zeros_like(m)
    big = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    bx, by, bw, bh = st[big, 0], st[big, 1], st[big, 2], st[big, 3]
    for i in range(1, k):   # pääkomponentti + lähellä olevat osat (siiven irronneet palat)
        x, y, w, h = st[i, :4]
        if i == big or (x < bx + bw + 25 and x + w > bx - 25 and y < by + bh + 25 and y + h > by - 25 and st[i, 4] > 12): keep |= (lab == i).astype(np.uint8)
    ys, xs = np.nonzero(keep); x0, x1, y0, y1 = max(0, xs.min() - 3), xs.max() + 4, max(0, ys.min() - 3), ys.max() + 4
    rgba = a_m.copy(); rgba[..., 3] = np.where(cv2.dilate(keep, np.ones((3, 3), np.uint8)) > 0, al, 0)
    g = rgba[..., 1].astype(np.int16); mx = np.maximum(rgba[..., 0], rgba[..., 2]).astype(np.int16); rgba[..., 1] = np.where(g > mx + 6, mx + 6, g).astype(np.uint8)
    crop = Image.fromarray(rgba[y0:y1, x0:x1], "RGBA"); w, h = int(round(crop.width * SC)), int(round(crop.height * SC))
    crop.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "kyyhky_lento_%02d.png" % n))
    lines.append("%d %.1f %.1f" % (n, ((x0 + x1) / 2 - X0) * SC, ((y0 + y1) / 2 - Y0) * SC))
open(os.path.join(OUT, "kyyhky_lento.txt"), "w").write("\n".join(lines))
print(len(lines), "kuvaa"); print("\n".join(lines[:6]))
