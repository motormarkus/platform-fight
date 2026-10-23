"""Dina tiskillä: tausta on oikea tiski+ikkuna (ei avainväriä). Hahmo erotetaan väreillä (musta/tummat sukkikset,
sininen ja violetti, iho) ja rajataan (1) edellisen ruudun maskin lähialueelle ja (2) vakioverhoon: rivikohtainen
vaakaväli, jonka Dina on muissa (avainnetuissa) videoissa ylittänyt, + 18 px. Alalistan rivit rajataan jalkojen väliin.
Ensimmäinen maski tulee avainnetusta vihreästä ruudusta."""
import pickle, sys, numpy as np, cv2
SC = sys.argv[1]
raw = pickle.load(open(f"{SC}/dina_counter.pkl", "rb"))
keyed = pickle.load(open(f"{SC}/dina_counter_k.pkl", "rb"))
START = 103
STRIP = (636, 674)
K = np.ones
H, W = raw[0].shape[:2]
# vakio: hahmon keskilinja ja rivikohtainen verho kaikista avainnetuista ruuduista
allf = []
for n in ("walk", "idle", "talk", "dance"):
    allf += pickle.load(open(f"{SC}/dina_{n}.pkl", "rb"))[::5]
allf += keyed[:START:5]
cxs = [np.where(f[..., 3] > 128)[1].mean() for f in allf]
cx = float(np.median(cxs))
env = np.zeros(H, np.float32)
for f in allf:
    m = f[..., 3] > 128
    for y in range(H):
        xs = np.where(m[y])[0]
        if len(xs): env[y] = max(env[y], abs(xs.min() - cx), abs(xs.max() - cx))
env = cv2.GaussianBlur(env.reshape(-1, 1), (1, 0), 6).ravel()
xx = np.arange(W)[None, :]
envmask = (np.abs(xx - cx) <= (env[:, None] + 18)).astype(np.uint8)
print("cx", cx)
out = list(keyed[:START])
prev = (keyed[START - 1][..., 3] > 128).astype(np.uint8)
for t in range(START, len(raw)):
    rgb = raw[t]
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    h, s, v = hsv[..., 0].astype(int), hsv[..., 1].astype(int), hsv[..., 2].astype(int)
    yy = np.arange(H)[:, None]
    dark = ((v < 56) & ~((h >= 8) & (h <= 14) & (v >= 30))) | ((v < 86) & ((h <= 7) | (h >= 165)))
    blue = (h > 105) & (h < 150) & (s > 110) & (v > 40)
    skin = (h < 21) & (v > 150) & (s > 55) & (s < 175) & (yy < 255)
    char = (dark | blue | skin).astype(np.uint8)
    char[:58 if t >= 140 else 22] = 0     # lamppu pään yläpuolella
    near = cv2.dilate(prev, K((9, 9), np.uint8)) * envmask
    mask = char & near
    # tiskin reunaviivat lantion kohdalla: vain vartalon leveyden verran (rivit 262-296 +-15 px)
    top = prev[262:296].any(axis=0); xt = np.where(top)[0]
    if len(xt):
        lim2 = np.zeros(W, np.uint8); lim2[max(0, xt.min() - 6):xt.max() + 7] = 1
        mask[300:384] *= lim2[None, :]
    # alalista: vain jalkojen väli (ylempien rivien jalat + 8 px)
    ref = mask[STRIP[0] - 14:STRIP[0] - 4].any(axis=0)
    xs = np.where(ref)[0]
    if len(xs):
        lim = np.zeros(W, np.uint8); lim[max(0, xs.min() - 8):xs.max() + 9] = 1
        mask[STRIP[0]:STRIP[1]] *= lim[None, :]
    # ohuet viivat (tiskin reunat) pois: ydin avauksella, reunat takaisin 2 px:n sisällä ytimestä
    core = cv2.morphologyEx(mask, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7)))
    mask = cv2.dilate(core, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5))) & mask
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, K((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if n > 1: mask = (lab == 1 + np.argmax(st[1:, cv2.CC_STAT_AREA])).astype(np.uint8)
    # reiät ja loveukset umpeen; täytetyt pikselit saavat värinsä ympäristöstä (inpaint) eikä tiskin puun väriä
    before = mask.copy()
    inv = (1 - mask).astype(np.uint8)
    nh, labh, sth, _ = cv2.connectedComponentsWithStats(inv, connectivity=4)
    for i in range(1, nh):
        x0, y0, w0, h0, ar = sth[i]
        if ar < 3500 and x0 > 0 and y0 > 0 and x0 + w0 < W and y0 + h0 < H: mask[labh == i] = 1
    legs = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (25, 25)))
    legs[:330] = 0
    mask = mask | legs
    mask = cv2.medianBlur(mask, 3)
    added = ((mask > 0) & (before == 0)).astype(np.uint8)
    if added.any():
        rgb = cv2.inpaint(rgb, cv2.dilate(added, K((3, 3), np.uint8)) * 255, 5, cv2.INPAINT_TELEA)
    prev = mask
    a = cv2.GaussianBlur(cv2.erode(mask, K((3, 3), np.uint8)).astype(np.float32), (0, 0), 0.8)
    out.append(np.dstack([rgb, (a * 255).astype(np.uint8)]))
pickle.dump(out, open(f"{SC}/dina_counter_final.pkl", "wb"))
print("ok", len(out))
