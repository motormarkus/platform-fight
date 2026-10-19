"""Marcus (nyrkkeilijä): videoruutujen taustan poisto. Vihreä tausta: Tools/GymAssets/key_video.key_frame.
Muut taustat (harmaa, sininen, tumma liukuväri): täyttö reunoista naapuripikseliin verraten (liukuväri sallitaan,
hahmon ääriviiva pysäyttää täytön). Tulos: lista (rgba_crop, x0, y0) kuvaa kohti, rajattuna hahmon ympärille."""
import os, pickle, sys
import cv2
import numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame

UP = r"C:/Users/marku/.claude/uploads/3a0bc6ca-2c02-49e6-9370-0e4a65bef55c"
VIDEOS = {  # nimi: (tiedostoalku, tausta)
    "kaatuminen1": ("adc88683", "green"), "kaatuminen2": ("ca3df783", "flood"), "kavely": ("45ec888e", "flood"),
    "lyonnit1": ("12199257", "green"), "lyonnit2": ("8cc32e4f", "flood"), "vaisto": ("217d5cb6", "green"),
}


def is_green(bgr):
    b = np.concatenate([bgr[:8].reshape(-1, 3), bgr[-8:].reshape(-1, 3)])
    m = np.median(b, axis=0)
    return m[1] > 120 and m[1] - max(m[0], m[2]) > 60


def _design(xs, ys, deg=3):
    cols = [xs ** i * ys ** j for i in range(deg + 1) for j in range(deg + 1 - i)]
    return np.stack(cols, axis=-1)


def _fit_region(med, region, deg=3, iters=6, thr=9.0):
    """Sovittaa sileän (polynomi) taustan alueelle; hahmon pikselit (poikkeamat) jätetään iteroiden pois."""
    h, w = med.shape[:2]
    yy, xx = np.mgrid[0:h, 0:w]
    X = (xx / w * 2 - 1).astype(np.float32); Y = (yy / h * 2 - 1).astype(np.float32)
    ok = region.copy()
    sub = np.zeros(region.shape, bool); sub[::6, ::6] = True
    out = np.zeros(med.shape, np.float32)
    for _ in range(iters):
        sel = ok & sub
        A = _design(X[sel], Y[sel], deg)
        coef = [np.linalg.lstsq(A, med[..., c][sel].astype(np.float32), rcond=None)[0] for c in range(3)]
        Aall = _design(X, Y, deg)
        for c in range(3): out[..., c] = Aall @ coef[c]
        err = np.abs(med.astype(np.float32) - out).max(axis=2)
        ok = region & (err < thr)
    return out


def make_plate(frames_bgr):
    """Hahmo on kuvassa suurimman osan ajasta, joten mediaani ei kelpaa: taustaksi sovitetaan sileä pinta,
    erikseen seinälle ja lattialle (horisontti = jyrkin pystysuuntainen muutos reunoilla)."""
    med = np.median(np.stack(frames_bgr[::3]), axis=0).astype(np.uint8)
    h, w = med.shape[:2]
    edge = np.concatenate([med[:, :120], med[:, -120:]], axis=1).astype(np.float32).mean(axis=1).mean(axis=1)
    g = np.abs(np.diff(cv2.GaussianBlur(edge.reshape(-1, 1), (1, 0), 2.5).ravel()))
    hy = int(np.argmax(g[h // 4:h - 20])) + h // 4 if g[h // 4:h - 20].max() > 0.15 else -1
    out = np.zeros(med.shape, np.float32)
    if hy < 0:
        out = _fit_region(med, np.ones((h, w), bool))
    else:
        yy = np.arange(h).reshape(-1, 1) * np.ones((1, w))
        up = yy < hy - 6; dn = yy > hy + 6
        a = _fit_region(med, up); b = _fit_region(med, dn)
        out = np.where((yy <= hy)[..., None], a, b)
    return out.clip(0, 255).astype(np.uint8)


def key_plate(bgr, plate, lo=16.0, hi=34.0):
    """Hahmo = pikselit, jotka eroavat staattisesta taustalevystä (kameran liike ei ole). Varjo lattialla poistetaan."""
    img = cv2.GaussianBlur(bgr, (0, 0), 1.0).astype(np.float32)
    pl = cv2.GaussianBlur(plate, (0, 0), 1.0).astype(np.float32)
    d = np.sqrt(((img - pl) ** 2).sum(axis=2))
    ratio = img / np.maximum(pl, 1.0)
    rmean = ratio.mean(axis=2)
    ci = img / np.maximum(img.sum(axis=2, keepdims=True), 1.0)
    cp = pl / np.maximum(pl.sum(axis=2, keepdims=True), 1.0)
    chroma = np.abs(ci - cp).sum(axis=2)
    shadow = (rmean > 0.3) & (rmean < 0.99) & (chroma < 0.05)
    shadow = cv2.morphologyEx(shadow.astype(np.uint8), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8)).astype(bool)
    strong = (d > hi) & ~shadow
    weak = (d > lo) & ~shadow
    n, lab = cv2.connectedComponents(weak.astype(np.uint8), connectivity=8)
    keep = np.unique(lab[strong]); keep = keep[keep > 0]
    fg = np.isin(lab, keep).astype(np.uint8)
    fg = cv2.morphologyEx(fg, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11)))
    fg = cv2.morphologyEx(fg, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg, connectivity=8)
    if n > 1:
        k = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); big = st[k, cv2.CC_STAT_AREA]
        near = cv2.dilate((lab == k).astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (61, 61))) > 0
        ok = [i for i in range(1, n) if i == k or (st[i, cv2.CC_STAT_AREA] > big * 0.002 and near[lab == i].mean() > 0.5)]
        fg = np.isin(lab, ok).astype(np.uint8)
    # vain pienet reiät täytetään (esim. hanskan ja käsivarren väli); isot aukot (jalkojen väli) jäävät läpinäkyviksi
    inv = (1 - fg).astype(np.uint8)
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats(inv, connectivity=4)
    edge = set(np.unique(np.concatenate([lab2[0], lab2[-1], lab2[:, 0], lab2[:, -1]])).tolist())
    for i in range(1, n2):
        if i not in edge and st2[i, cv2.CC_STAT_AREA] < 2500: fg[lab2 == i] = 1
    a = cv2.GaussianBlur(cv2.erode(fg, np.ones((3, 3), np.uint8)).astype(np.float32), (0, 0), 0.8)
    return np.dstack([bgr[..., 2], bgr[..., 1], bgr[..., 0], a * 255]).clip(0, 255).astype(np.uint8)


def key_any(bgr, plate=None):
    if is_green(bgr): return key_frame(bgr)
    return key_plate(bgr, plate) if plate is not None else None


def crop(rgba):
    ys, xs = np.where(rgba[..., 3] > 20)
    if len(ys) == 0: return None
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    return rgba[y0:y1, x0:x1].copy(), int(x0), int(y0)


def run(name, out_dir):
    pre, _ = VIDEOS[name]
    f = next(os.path.join(UP, x) for x in os.listdir(UP) if x.startswith(pre))
    cap = cv2.VideoCapture(f)
    allf = []
    while True:
        ok, fr = cap.read()
        if not ok: break
        allf.append(fr)
    plate = make_plate([x for x in allf if not is_green(x)]) if VIDEOS[name][1] == "flood" else None
    res = []
    for fr in allf:
        k = key_any(fr, plate)
        res.append(crop(k) if k is not None else None)
    with open(os.path.join(out_dir, name + ".pkl"), "wb") as fh:
        pickle.dump(res, fh)
    print(name, len(res))


if __name__ == "__main__":
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    for n in (sys.argv[2:] or VIDEOS):
        run(n, out)
