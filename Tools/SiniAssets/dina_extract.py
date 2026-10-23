"""Dina: videot -> RGBA-ruudut (pickle). Tausta vaihtelee (vihreä -> keltainen -> oliivi), joten avain tehdään
ruudun reunojen taustavärin mukaan: reunasta alkava täyttö + pehmeä reuna. Tiskivideo (counter) tallennetaan raakana
(tausta on oikea tiski+ikkuna) ja leikataan erikseen dina_counter.py:llä."""
import av, os, sys, pickle, numpy as np, cv2
U = "C:/Users/marku/.claude/uploads/738327ec-6328-4b78-9791-c65803258fd9/"
OUT = sys.argv[1]
NAMES = {"161290ba": "walk", "b43500e7": "idle", "f8578944": "talk", "cf798c48": "dance", "49daf9c9": "counter"}

def key(rgb):
    """tausta pois: sävy (vihreä..keltainen..oliivi, Dinalla ei ole näitä värejä) + reunasta alkava täyttö taustavärin mukaan"""
    h, w = rgb.shape[:2]
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
    hue, sat, val = hsv[..., 0], hsv[..., 1] / 255.0, hsv[..., 2]
    hm = ((hue > 17) & (hue < 88)).astype(np.float32) * np.clip((sat - 0.28) / 0.22, 0, 1) * np.clip((val - 45) / 40.0, 0, 1)
    border = np.concatenate([rgb[:8].reshape(-1, 3), rgb[-8:].reshape(-1, 3), rgb[:, :8].reshape(-1, 3), rgb[:, -8:].reshape(-1, 3)])
    bg = np.median(border, axis=0)
    d = np.linalg.norm(rgb.astype(np.float32) - bg, axis=2)
    cand = (d < 60).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(cand, connectivity=4)
    edge_labels = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    bgmask = np.isin(lab, list(edge_labels))
    flood = np.where(cv2.dilate(bgmask.astype(np.uint8), np.ones((5, 5), np.uint8)) > 0, 1 - np.clip((d - 30) / 45.0, 0, 1), 0.0)
    bga = np.maximum(hm, flood.astype(np.float32))
    a = 1 - bga
    a = cv2.GaussianBlur(a, (0, 0), 0.6)
    a = np.clip((a - 0.12) / 0.76, 0, 1)
    out = rgb.astype(np.float32)
    m = (a > 0.02) & (a < 0.98)
    aa = np.maximum(a[m], 0.25)[:, None]
    out[m] = np.clip((out[m] - (1 - aa) * bg) / aa, 0, 255)
    # vihreän/keltaisen vuoto reunoilla
    out[..., 1] = np.where(m, np.minimum(out[..., 1], np.maximum(out[..., 0], out[..., 2]) * 1.05), out[..., 1])
    return np.dstack([out, a * 255]).clip(0, 255).astype(np.uint8)

for fn in os.listdir(U):
    k = fn[:8]
    if k not in NAMES: continue
    c = av.open(U + fn)
    fr = []
    for x in c.decode(video=0):
        rgb = x.to_ndarray(format="rgb24"); h, w = rgb.shape[:2]; sc = 720.0 / h
        rgb = cv2.resize(rgb, (int(w * sc), 720), interpolation=cv2.INTER_AREA)
        fr.append(rgb if NAMES[k] == "counter" else key(rgb))
    if NAMES[k] == "counter": pickle.dump([key(r) if i < 103 else r for i, r in enumerate(fr)], open(f"{OUT}/dina_counter_k.pkl", "wb"))
    pickle.dump(fr, open(f"{OUT}/dina_{NAMES[k]}.pkl", "wb"))
    print(NAMES[k], len(fr), fr[0].shape)
