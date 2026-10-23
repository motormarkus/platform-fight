"""Dina: valitut ruudut -> Assets/Resources/Dina/<anim>_NN.png (bottom-center pivot, 24 px lattiavara, hahmo 360 px).
Ajo: extract_dina (dina_extract.py), dina_counter.py, sitten make_dina.py <pkl-kansio> <ulos>"""
import pickle, sys, os, numpy as np, cv2
SRC, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
CH, PAD = 360.0, 24

def body_h(a):
    ys, xs = np.where(a[..., 3] > 128)
    return ys.max() - ys.min()

# animaatio: (pkl, ruudut)
ANIMS = {
    "idle":      ("dina_walk" if False else "dina_idle", list(range(10, 141, 10))),
    "walk":      ("dina_walk", [round(10 + k * 57 / 16) for k in range(16)]),
    "talk":      ("dina_talk", list(range(10, 293, 6))),
    "dance":     ("dina_dance", list(range(20, 291, 6))),
    "toCounter": ("dina_counter_final", list(range(108, 157, 3))),
    "counter":   ("dina_counter_final", list(range(165, 240, 8))),
}
cache = {}
ref_walk = body_h(pickle.load(open(f"{SRC}/dina_walk.pkl", "rb"))[0])
for name, (pk, idx) in ANIMS.items():
    fr = cache.setdefault(pk, pickle.load(open(f"{SRC}/{pk}.pkl", "rb")))
    ref_h = ref_walk if pk == "dina_counter_final" else body_h(fr[0])
    s = CH / ref_h
    imgs = [fr[i] for i in idx]
    bbs = []
    for a in imgs:
        ys, xs = np.where(a[..., 3] > 40)
        bbs.append((xs.min(), xs.max(), ys.min(), ys.max()))
    cx = float(np.median([np.where(a[..., 3] > 128)[1].mean() for a in imgs]))
    base = float(np.percentile([b[3] for b in bbs], 90))
    L = max(cx - b[0] for b in bbs) * s + 6; R = max(b[1] - cx for b in bbs) * s + 6
    half = int(np.ceil(max(L, R) / 2.0)) * 2
    T = int(np.ceil((base - min(b[2] for b in bbs)) * s)) + 6
    T = int(np.ceil((T + PAD) / 4.0)) * 4 - PAD
    W, H = half * 2, T + PAD
    print(name, "frames", len(imgs), "scale %.3f" % s, "canvas", W, H)
    for k, a in enumerate(imgs):
        m = cv2.resize(a, (int(a.shape[1] * s + .5), int(a.shape[0] * s + .5)), interpolation=cv2.INTER_AREA)
        if m.shape[2] == 3: m = np.dstack([m, np.full(m.shape[:2], 255, np.uint8)])
        ox = int(round(half - cx * s)); oy = int(round(T - base * s))
        can = np.zeros((H, W, 4), np.uint8)
        x0, y0 = max(0, ox), max(0, oy); x1, y1 = min(W, ox + m.shape[1]), min(H, oy + m.shape[0])
        can[y0:y1, x0:x1] = m[y0 - oy:y1 - oy, x0 - ox:x1 - ox]
        cv2.imwrite(f"{OUT}/{name}_{k:02d}.png", cv2.cvtColor(can, cv2.COLOR_RGBA2BGRA))
