"""Sini: videot -> avainnetut RGBA-ruudut (pickle) tutkimusta ja leikkausta varten."""
import av, glob, os, sys, pickle, numpy as np, cv2
U = "C:/Users/marku/.claude/uploads/738327ec-6328-4b78-9791-c65803258fd9/"
OUT = sys.argv[1]
NAMES = {  # tiedoston alku -> animaatio
    "58f81df8": "idle", "0bcf75e5": "talk", "13ddedd4": "walk",
    "820b9571": "counter", "9311766e": "kiss", "a564550a": "dance"}

def key(rgb):
    """vihreä tausta pois: sävy+kylläisyys, pehmeä reuna, vihreän vuoto pois reunoilta"""
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
    h, s = hsv[..., 0], hsv[..., 1] / 255.0
    green = ((h > 44) & (h < 85)).astype(np.float32) * np.clip((s - 0.30) / 0.25, 0, 1)
    a = 1 - green
    a = cv2.GaussianBlur(a, (0, 0), 0.7)
    a = np.clip((a - 0.15) / 0.7, 0, 1)
    f = rgb.astype(np.float32)
    r, g, b = f[..., 0], f[..., 1], f[..., 2]
    lim = np.maximum(r, b)
    edge = (a < 0.98)
    g2 = np.where(edge, np.minimum(g, lim), g)
    f[..., 1] = g2
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)

for f in glob.glob(U + "*"):
    k = os.path.basename(f)[:8]
    if k not in NAMES: continue
    c = av.open(f)
    fr = []
    for x in c.decode(video=0):
        rgb = x.to_ndarray(format="rgb24")
        h, w = rgb.shape[:2]
        sc = 720.0 / h
        rgb = cv2.resize(rgb, (int(w * sc), 720), interpolation=cv2.INTER_AREA)
        fr.append(key(rgb))
    pickle.dump(fr, open(f"{OUT}/{NAMES[k]}.pkl", "wb"))
    print(NAMES[k], len(fr), fr[0].shape)
