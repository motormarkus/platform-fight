"""Sini: valitut ruudut -> Assets/Resources/Sini/<anim>_NN.png (bottom-center pivot, 24 px lattiavara).
Aja ensin extract.py (kirjoittaa .pkl-ruudut annettuun kansioon). Käyttö: make_sprites.py <pkl-kansio> <ulos>"""
import pickle, sys, os, numpy as np, cv2
from counter_clean import clean
SRC, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
CH = 360.0     # hahmon korkeus pikseleinä valmiissa kuvassa
PAD = 24       # tyhjää lattian alla (Unityssä pivotY = 24 / korkeus)

def body_h(a):
    ys, xs = np.where(a[..., 3] > 128)
    return ys.max() - ys.min()

def fix_counter(a):
    o, box = clean(a)
    if box is None: return o
    top, bot = box[0], box[1]
    al = (o[..., 3] > 40).astype(np.uint8)
    # ohuet vaakaviivat (kansilevyn ja alareunan jäänteet) pois: pystyavaus + pieni laajennus reunoja varten
    op = cv2.morphologyEx(al, cv2.MORPH_OPEN, np.ones((13, 1), np.uint8))
    keep = cv2.dilate(op, np.ones((3, 3), np.uint8))
    o[..., 3][keep == 0] = 0
    # pienet reiät sisälle (poistetun kohdan jäljet) umpeen
    a2 = (o[..., 3] > 128).astype(np.uint8)
    holes = cv2.morphologyEx(a2, cv2.MORPH_CLOSE, np.ones((7, 7), np.uint8)) & (1 - a2)
    if holes.any():
        rgb = cv2.inpaint(o[..., :3], holes * 255, 3, cv2.INPAINT_TELEA)
        o[..., :3] = rgb; o[..., 3][holes == 1] = 255
    # pisin komponentti vain (irtopikselit pois)
    n, lab, st, _ = cv2.connectedComponentsWithStats((o[..., 3] > 128).astype(np.uint8), connectivity=8)
    if n > 1:
        keep = 1 + np.argmax(st[1:, cv2.CC_STAT_AREA])
        o[..., 3][lab != keep] = 0
    return o

# animaatio: (pkl, ruudut, taso-ruutu skaalan mittaukseen, puhdistus)
ANIMS = {
    "idle":    ("idle",    list(range(0, 121, 12)), 0, None),
    "talk":    ("talk",    list(range(36, 163, 4)), 0, None),
    "walk":    ("walk",    [round(20 + k * 41 / 12) for k in range(12)], None, None),
    "toCounter": ("counter", [114, 117, 123, 126, 129, 132, 135, 138, 141, 144], None, fix_counter),
    "counter": ("counter", list(range(174, 236, 8)), None, fix_counter),
    "kiss":    ("kiss",    list(range(0, 150, 4)), 0, None),
    "dance":   ("dance",   list(range(10, 181, 5)), 0, None),
}
cache = {}
for name, (pk, idx, ref, fn) in ANIMS.items():
    fr = cache.setdefault(pk, pickle.load(open(f"{SRC}/{pk}.pkl", "rb")))
    if ref is None:
        ref_h = {"walk": 675, "counter": 676}[pk]
    else:
        ref_h = body_h(fr[ref])
    s = CH / ref_h
    imgs = [fn(fr[i]) if fn else fr[i] for i in idx]
    bbs = []
    for a in imgs:
        ys, xs = np.where(a[..., 3] > 40)
        bbs.append((xs.min(), xs.max(), ys.min(), ys.max()))
    # pivot: pysyvä vartalon keskikohta (mediaani jalkojen/ylävartalon keskiarvosta), lattia = 90. persentiili
    cx = float(np.median([(b[0] + b[1]) / 2 for b in bbs])) if name not in ("kiss", "dance", "talk") else float(np.median([np.where(a[..., 3] > 128)[1].mean() for a in imgs]))
    base = float(np.percentile([b[3] for b in bbs], 90))
    L = max(cx - b[0] for b in bbs) * s + 6; R = max(b[1] - cx for b in bbs) * s + 6
    half = int(np.ceil(max(L, R) / 2.0)) * 2          # leveys jaollinen neljällä (pakkaus)
    T = int(np.ceil((base - min(b[2] for b in bbs)) * s)) + 6
    T = int(np.ceil((T + PAD) / 4.0)) * 4 - PAD
    W, H = half * 2, T + PAD
    print(name, "frames", len(imgs), "scale %.3f" % s, "canvas", W, H)
    for k, a in enumerate(imgs):
        m = cv2.resize(a, (int(a.shape[1] * s + .5), int(a.shape[0] * s + .5)), interpolation=cv2.INTER_AREA)
        # premultiply ennen alaskalaus on jo tehty; sijoitus kankaalle
        ox = int(round(half - cx * s)); oy = int(round(T - base * s))
        can = np.zeros((H, W, 4), np.uint8)
        x0, y0 = max(0, ox), max(0, oy); x1, y1 = min(W, ox + m.shape[1]), min(H, oy + m.shape[0])
        can[y0:y1, x0:x1] = m[y0 - oy:y1 - oy, x0 - ox:x1 - ox]
        cv2.imwrite(f"{OUT}/{name}_{k:02d}.png", cv2.cvtColor(can, cv2.COLOR_RGBA2BGRA))
