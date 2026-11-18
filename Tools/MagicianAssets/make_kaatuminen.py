"""Taikurin kaatuminen ja ylösnousu (hattu lentää korkealle ja palaa itsestään päähän).
Videossa hattu irtoaa kuvassa 11 ja palaa päähän kuvassa 75. Hattu poistetaan vartalokuvista (kuvat 11..74) ja siitä tehdään oma sprite + rata (json):
uusi, paljon korkeampi kaari ylös, pyöriminen, tömähdys maahan ja pomppu, ja paluukaari takaisin päähän. Kaikki pikseliarvot ovat videon koordinaateissa (1280x720),
rajaus ja skaala kuten muillakin taikurin kuvilla (0,55), pivot samaa videon koordinaatistoa.
Ajo: python Tools/MagicianAssets/make_kaatuminen.py <video>"""
import os, sys, json, math, inspect
import av, numpy as np, cv2
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Taikuri")
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import rekey_tony as rt
SCALE = .75; DETACH, REATTACH = 11, 75
# key_soft ilman "vain suurin yhtenäinen alue" -sääntöä, jotta hattu säilyy
src = inspect.getsource(rt.key_soft).replace("    if n2 > 2: bgm = bgm | (lab2 != 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])))\n", "").replace("def key_soft", "def key_multi")
ns = {"np": np, "cv2": cv2, "kv": rt.kv}; exec(src, ns); key_multi = ns["key_multi"]
def despill(a):
    g = a[..., 1].astype(np.int16); m = np.maximum(a[..., 0], a[..., 2]).astype(np.int16)
    a[..., 1] = np.where(g > m + 6, m + 6, g).astype(np.uint8); return a
video = sys.argv[1]
frames = [despill(key_multi(x.to_ndarray(format="bgr24"))) for x in av.open(video).decode(video=0)]; N = len(frames)
def comps(a, thr=100, minarea=150):
    m = (a[..., 3] > thr).astype(np.uint8); n, lab, st, cen = cv2.connectedComponentsWithStats(m, 8)
    cs = sorted([(int(st[k, 4]), k) for k in range(1, n) if st[k, 4] > minarea], reverse=True)
    return lab, st, cen, [k for _, k in cs]
# 1) hattu irti: erota komponentti, tee vartalokuvat ilman hattua
hat_mask = {}; hat_c = {}; bodies = []
for i, a in enumerate(frames):
    a = a.copy()
    if DETACH <= i < REATTACH:
        lab, st, cen, ks = comps(a)
        assert len(ks) >= 2, ("hattu ei erotu kuvassa", i)
        hk = ks[1]; hm = cv2.dilate((lab == hk).astype(np.uint8), np.ones((7, 7), np.uint8)) > 0
        hat_mask[i] = hm & (a[..., 3] > 0); hat_c[i] = (float(cen[hk][0]), float(cen[hk][1]))
        a[hm, 3] = 0
    bodies.append(a)
# 1b) tukka: kuvissa 63..74 päälaki on litistynyt (videon vika) -> kuvan 62 täysi tukka sovitetaan päähän
import hair_fix as hf
for i in range(hf.FIRST, hf.LAST + 1): bodies[i], _cc = hf.fix(bodies[i], bodies[hf.REF])
# 2) hattu-sprite: kuva 70 (pystyssä, päänsä yläpuolella)
REF = 70; ys, xs = np.nonzero(hat_mask[REF]); hx0, hx1, hy0, hy1 = xs.min() - 2, xs.max() + 3, ys.min() - 2, ys.max() + 3
hat_img = frames[REF][hy0:hy1, hx0:hx1].copy(); hat_img[~hat_mask[REF][hy0:hy1, hx0:hx1]] = 0
hat_cx, hat_cy = (xs.min() + xs.max()) / 2.0, (ys.min() + ys.max()) / 2.0          # sprite keskipiste kuvassa 70
tmpl = (hat_img[..., 3] > 60).astype(np.float32); hh, hw = tmpl.shape
def hat_angle(i):
    """Kuinka paljon hattu on kiertynyt kuvassa i (astetta, myötäpäivään) verrattuna kuvaan 70: IoU-haku."""
    ys2, xs2 = np.nonzero(hat_mask[i]); c = ((xs2.min() + xs2.max()) / 2.0, (ys2.min() + ys2.max()) / 2.0)
    target = np.zeros(frames[i].shape[:2], np.float32); target[hat_mask[i]] = 1
    best = (-1, 0)
    for ang in range(-180, 180, 4):
        M = cv2.getRotationMatrix2D((hw / 2.0, hh / 2.0), -ang, 1.0)       # OpenCV: positiivinen = vastapäivään
        M[:, 2] += (c[0] - hw / 2.0, c[1] - hh / 2.0)
        r = cv2.warpAffine(tmpl, M, (target.shape[1], target.shape[0]), flags=cv2.INTER_LINEAR)
        iou = float((r > .5)[target > .5].sum()) / max(1.0, float(((r > .5) | (target > .5)).sum()))
        if iou > best[0]: best = (iou, ang)
    return best[1], best[0]
rest = [hat_angle(i) for i in range(42, 48)]; rest_ang = float(np.median([a for a, _ in rest])); print("lepokulma", rest_ang, [round(q, 2) for _, q in rest])
# 3) uusi rata (videon pikseleinä): kuvat 11..74
start = np.array(hat_c[DETACH]); ground = np.array([hat_c[44][0], hat_c[44][1]]); head = np.array(hat_c[REATTACH - 1])
TL = 39                                   # kuva, jossa hattu osuu maahan
track = []
def ease_out(x): return 1 - (1 - x) ** 2
APEX_RISE = 330.0                         # ylös (video-px; alkuperäinen n. 105 -> reilusti korkeammalle, kuvan yläreunan yli)
TA = 11                                   # kuvaa nousuun
spin0 = 0.0
for i in range(DETACH, REATTACH):
    if i < TL:                            # lento: nousu hidastuen, lasku kiihtyen; vaakasuunnassa tasaisesti ground.x:ään
        u = (i - DETACH) / float(TL - DETACH); x = start[0] + (ground[0] - start[0]) * u
        if i - DETACH <= TA: t = (i - DETACH) / float(TA); y = start[1] - APEX_RISE * (1 - (1 - t) ** 2)
        else: t = (i - DETACH - TA) / float(TL - DETACH - TA); y = (start[1] - APEX_RISE) + ((ground[1]) - (start[1] - APEX_RISE)) * t * t
        ang = rest_ang + 360 * 2 * (1 - u) * -1                          # pari kierrosta, loppu lepokulmaan
        ang = -360.0 * 2.5 * u + rest_ang * u                           # vastapäivään (hattu pyörii ylöspäin lähtiessä), päätyy lepokulmaan
    elif i < TL + 3:                      # tömähdys: pieni pomppu
        bounce = [0, 26, 8][i - TL]; x = ground[0] + (i - TL) * 6; y = ground[1] - bounce; ang = rest_ang + (i - TL) * 5
    elif i < 52:                          # lepo maassa
        x = ground[0] + 12; y = ground[1]; ang = rest_ang + 10
    else:                                 # paluu päähän: kaari, korkea huippu päänsä yläpuolella, pari kierrosta ja pystyyn
        u = (i - 52) / float(REATTACH - 1 - 52); x = (ground[0] + 12) + (head[0] - (ground[0] + 12)) * (3 * u * u - 2 * u * u * u)
        peak = 300.0; y = (ground[1] + (head[1] - ground[1]) * u) - peak * 4 * u * (1 - u)
        ang = (rest_ang + 10) * (1 - u) + 360.0 * 2 * u + 0 * u
    track.append({"f": i, "x": round(float(x), 1), "y": round(float(y), 1), "rot": round(float(ang), 1)})
# 4) kuvat: vartalon yhteinen rajaus (ilman hattua)
bb = []
for a in bodies:
    ys, xs = np.nonzero(a[..., 3] > 90); bb.append((xs.min(), xs.max(), ys.min(), ys.max()))
x0 = max(0, min(b[0] for b in bb) - 6); x1 = max(b[1] for b in bb) + 7; y0 = max(0, min(b[2] for b in bb) - 6); y1 = max(b[3] for b in bb) + 7
for old in os.listdir(OUT):
    if old.startswith("taikuri_kaatuminen_") or old.startswith("taikuri_hattu"): os.remove(os.path.join(OUT, old))
w, h = int(round((x1 - x0) * SCALE)), int(round((y1 - y0) * SCALE))
for i, a in enumerate(bodies): Image.fromarray(a[y0:y1, x0:x1], "RGBA").resize((w, h), Image.LANCZOS).save(os.path.join(OUT, "taikuri_kaatuminen_%02d.png" % i))
Image.fromarray(hat_img, "RGBA").resize((int(round(hw * SCALE)), int(round(hh * SCALE))), Image.LANCZOS).save(os.path.join(OUT, "taikuri_hattu.png"))
json.dump({"x0": int(x0), "x1": int(x1), "y0": int(y0), "y1": int(y1), "scale": SCALE, "frames": N, "fps": 30, "w": w, "h": h, "detach": DETACH, "reattach": REATTACH,
           "hat": {"w": hw, "h": hh, "cx_ref": hat_cx, "cy_ref": hat_cy, "rest_angle": rest_ang, "track": track,
                   "note": "rata = hatun keskipiste videon pikseleinä (x oikealle, y alas), rot = kierto asteina vastapäivään (kuvassa 70 hattu on pystyssä = 0); kuvat 11..74, muulloin hattu on vartalokuvassa"}},
          open(os.path.join(HERE, "crop_kaatuminen.json"), "w"), indent=1)
print("kaatuminen", N, "kuvaa", w, "x", h, "hattu", hw, "x", hh)
# 5) esikatselu (videon koordinaatit, pienennetty): kuvat joka 5.
os.makedirs(os.path.join(HERE, "esikatselu"), exist_ok=True)
tiles = []
for i in list(range(0, N, 5)) + [N - 1]:
    canvas = Image.new("RGBA", (1280, 900), (40, 32, 50, 255)); canvas.alpha_composite(Image.fromarray(bodies[i], "RGBA"), (0, 180))
    if DETACH <= i < REATTACH:
        t = track[i - DETACH]; hs = Image.fromarray(hat_img, "RGBA").rotate(t["rot"], resample=Image.BICUBIC, expand=True)
        canvas.alpha_composite(hs, (int(t["x"] - hs.width / 2), int(t["y"] + 180 - hs.height / 2))) if (t["y"] + 180 - hs.height / 2) >= 0 else None
    tiles.append(canvas.convert("RGB").resize((320, 225)))
cols = 6; rows = (len(tiles) + cols - 1) // cols; sh = Image.new("RGB", (320 * cols, 225 * rows))
for k, t in enumerate(tiles): sh.paste(t, ((k % cols) * 320, (k // cols) * 225))
sh.save(os.path.join(HERE, "esikatselu", "kaatuminen.png")); print(sh.size)
