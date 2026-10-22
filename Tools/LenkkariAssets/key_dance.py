# Ruby+Pablo tanssi/suudelma: tausta vaihtuu vihreasta oranssiin, joten avaus reunoista tulvatayttamalla (ei vihreaa avainta)
import sys, cv2, numpy as np
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
V = U + "96295e2f-copy_9787F5FB-D125-462D-BC15-C378471828F1.mov"
def key(frame, tol=5):
    h, w = frame.shape[:2]
    bl = cv2.GaussianBlur(frame, (0, 0), 1.2)
    mask = np.zeros((h + 2, w + 2), np.uint8)
    for (x, y) in [(2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3), (w // 2, 2), (w // 2, h - 3), (2, h // 2), (w - 3, h // 2)]:
        if mask[y + 1, x + 1] == 0:
            cv2.floodFill(bl.copy(), mask, (x, y), 0, (tol,) * 3, (tol,) * 3, 4 | cv2.FLOODFILL_MASK_ONLY | (255 << 8))
    bg = mask[1:-1, 1:-1] > 0
    # varjo: tausta-sävyä tummempi alue, joka ei ole reunaan yhteydessä -> poista alue, jonka väri on lähellä tausta-arvoa tummennettuna
    med = np.median(frame[bg].reshape(-1, 3), axis=0)
    d = np.abs(bl.astype(np.float32) - med).max(axis=2)
    fg = (~bg).astype(np.uint8) * 255
    # varjon tunnistus: alue jonka sävy (hue) on taustan kaltainen mutta tummempi
    hsv = cv2.cvtColor(bl, cv2.COLOR_BGR2HSV).astype(np.float32); hm = cv2.cvtColor(np.uint8([[med]]), cv2.COLOR_BGR2HSV)[0, 0].astype(np.float32)
    dh = np.minimum(np.abs(hsv[..., 0] - hm[0]), 180 - np.abs(hsv[..., 0] - hm[0]))
    shadow = (dh < 6) & (hsv[..., 1] > hm[1] * 0.8) & (hsv[..., 2] < hm[2]) & (hsv[..., 2] > hm[2] * 0.55) & (fg > 0)
    ys0 = np.where(fg.any(axis=1))[0]
    foot = ys0.max() if len(ys0) else h
    rows = np.zeros_like(shadow); rows[max(0, foot - 130):] = True
    fg[shadow & rows] = 0
    # taustan taskut (esim. jalkojen valissa): tausta-vari hyvin lahella, riittavan iso alue, alaosassa
    pock = (d < 14).astype(np.uint8) * 255
    pock[: int(h * 0.08)] = 0
    pock[bg] = 0
    nn, ll, ss, _ = cv2.connectedComponentsWithStats(pock, connectivity=4)
    for i in range(1, nn):
        if ss[i, 4] > 120: fg[ll == i] = 0
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg, connectivity=8)
    keep = np.zeros_like(fg)
    for i in range(1, n):
        if st[i, 4] > 4000: keep[lab == i] = 255
    keep = cv2.morphologyEx(keep, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    keep = cv2.erode(keep, np.ones((3, 3), np.uint8))
    a = cv2.GaussianBlur(keep, (0, 0), 1.0)
    return np.dstack([cv2.cvtColor(frame, cv2.COLOR_BGR2RGB), a])
if __name__ == "__main__":
    cap = cv2.VideoCapture(V); fr = []
    while True:
        ok, x = cap.read()
        if not ok: break
        fr.append(x)
    idx = [0, 30, 60, 100, 150, 200, 250, 299]
    tiles = []
    for i in idx:
        r = key(fr[i]); a = r[..., 3:] / 255.
        bgc = np.full(r.shape[:2] + (3,), (70, 70, 200), np.uint8)
        o = (r[..., :3][..., ::-1] * a + bgc * (1 - a)).astype(np.uint8)
        tiles.append(cv2.resize(o, None, fx=.3, fy=.3))
    cv2.imwrite("C:/Users/marku/AppData/Local/Temp/claude/G--unity-pelit-mopopeli-platform-fight/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/scratchpad/mama/k.png", np.vstack([np.hstack(tiles[:4]), np.hstack(tiles[4:])]))

def keyed_frames(path, step=4):
    cap = cv2.VideoCapture(path); fr = []; i = 0
    while True:
        ok, x = cap.read()
        if not ok: break
        if i % step == 0 or i == 299: fr.append(x)
        i += 1
    res = []
    for f in fr:
        r = key(f)
        a = r[..., 3]
        band = cv2.dilate((a > 128).astype(np.uint8), np.ones((9, 9), np.uint8)) - cv2.erode((a > 128).astype(np.uint8), np.ones((9, 9), np.uint8))
        g = r[..., 1].astype(np.int32); m = np.maximum(r[..., 0], r[..., 2]).astype(np.int32)
        r[..., 1] = np.where((band > 0) & (g > m), m, g).astype(np.uint8)   # vihrean valuma pois reunoilta
        res.append(r)
    return res

def export():
    OUT = "G:/unity pelit/mopopeli/platform fight/Assets/Sprites/Myyjat"
    sets = {"ruby_pablo_suudelma": keyed_frames(V), "ruby_pablo_tanssi": keyed_frames(U + "c48dff8d-copy_30B53C5D-AF92-4D3C-96FB-6AB7DBE668E1.mov")}
    sets["ruby_pablo_tanssi"][0] = sets["ruby_pablo_tanssi"][1]   # videon eka kuva on eri taustasavyinen, avaus epaonnistuu
    x0 = y0 = 10**9; x1 = y1 = 0
    for res in sets.values():
        for r in res:
            ys, xs = np.where(r[..., 3] > 40)
            x0 = min(x0, xs.min()); x1 = max(x1, xs.max()); y0 = min(y0, ys.min()); y1 = max(y1, ys.max())
    k = 300.0 / (y1 - y0 + 1)   # Pablo seisoo ekassa kuvassa: 300 px kuten idlessa
    for name, res in sets.items():
        for n, r in enumerate(res):
            c = r[y0:y1 + 1, x0:x1 + 1]
            c = cv2.resize(c, (max(1, round(c.shape[1] * k)), max(1, round(c.shape[0] * k))), interpolation=cv2.INTER_AREA)
            cv2.imwrite(f"{OUT}/{name}_{n:02d}.png", cv2.cvtColor(c, cv2.COLOR_RGBA2BGRA))
        print(name, len(res), c.shape)
if len(sys.argv) > 1 and sys.argv[1] == "export": export()
