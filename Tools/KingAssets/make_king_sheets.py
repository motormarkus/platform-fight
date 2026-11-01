"""Temppelin kuningas (pomo): kolmesta vihreätaustaisesta videosta (A iskut, B tulipallo, C idle/kävely/juoksu) kuvasarjat
Assets/Sprites/Viholliset/kuningas_<nimi>.png (4 saraketta, ruutu 960 x 768, hahmo ~600 px, jalat 16 px ylös, pivot x 0.4 / y 16/768; hahmo katsoo oikealle) ja
Assets/Resources/Kuningas/tulipallo.png (pallo viimeisestä latauskuvasta).
Ajo: python Tools/KingAssets/make_king_sheets.py <iskut.mp4> <tulipallo.mp4> <liike.mp4> [ulos-kansio-sheeteille] [resources/Kuningas]
Silmukat (idle, kävely, juoksu) haetaan automaattisesti: alku- ja loppukuva mahdollisimman samanlaiset."""
import os, sys
import av, cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame

CW, CH, COLS, FOOT, ANCHOR = 960, 768, 4, 16, 384
CHCUR = CH
# ruudun koko arkin mukaan (muisti: tyhjiä reunoja ei turhaan): nimi -> (leveys, ankkurin x, korkeus); sama taulukko BeatEmUpSetup.cs:ssä (KingCellW / KingPivotX)
DIMS = {"idle": (640, 320, 768), "kavely": (640, 320, 768), "juoksu": (640, 320, 768), "jab": (768, 300, 768), "takasuora": (768, 300, 768), "heitto": (768, 300, 768),
        "suoja": (768, 300, 768), "osuma": (768, 300, 768), "tulipallo": (896, 300, 768), "kaatuminen": (768, 384, 768), "maahan": (768, 384, 768), "nousu": (768, 384, 768), "hyppy": (768, 384, 1024)}
def setdims(name):
    global CW, ANCHOR, CHCUR
    CW, ANCHOR, CHCUR = DIMS[name]
TARGET_H = 600.0
FLOOR = 696                    # lattia videoissa (kiinteä kamera)

def load(path): return [key_frame(f.to_ndarray(format="bgr24")) for f in av.open(path).decode(video=0)]

def bbox(im):
    ys, xs = np.where(im[..., 3] > 128)
    return xs.min(), xs.max() + 1, ys.min(), ys.max() + 1

def anchor_x(im, mode):
    a = im[..., 3] > 128; h = a.shape[0]
    y0, y1, _, _ = bbox(im)[2], bbox(im)[3], 0, 0
    ys, xs = np.where(a); top, bot = ys.min(), ys.max(); hh = bot - top
    if mode == "torso":
        sub = a[top + int(hh * 0.15): top + int(hh * 0.5)]
        _, xs2 = np.where(sub)
        if len(xs2): return xs2.mean()
    if mode == "com": return xs.mean()
    sub = a[top + int(hh * 0.85):]
    _, xs2 = np.where(sub)
    return xs2.mean() if len(xs2) else xs.mean()

def rescale(rgba, f):
    a = rgba.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    r = cv2.resize(a, (round(a.shape[1] * f), round(a.shape[0] * f)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def cell(frame, mode, k, floor=FLOOR, ch=None):
    ch = ch or CHCUR
    ax = anchor_x(frame, mode)
    ys, xs = np.where(frame[..., 3] > 10)
    cx0, cx1, cy0, cy1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    r = rescale(frame[cy0:cy1, cx0:cx1], k)
    c = np.zeros((ch, CW, 4), np.uint8)
    x0 = int(round(ANCHOR + (cx0 - ax) * k)); y0 = int(round(ch - FOOT - (floor - cy0) * k))
    h, w = r.shape[:2]
    sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
    ww, hh = min(w - sx0, CW - dx0), min(h - sy0, ch - dy0)
    if sx0 or sy0 or ww < w - sx0 or hh < h - sy0: print("   HUOM: leikkautuu", x0, y0, w, h)
    c[dy0:dy0 + hh, dx0:dx0 + ww] = r[sy0:sy0 + hh, sx0:sx0 + ww]
    return c

def save(cells, path):
    ch = cells[0].shape[0]
    rows = (len(cells) + COLS - 1) // COLS
    img = np.zeros((rows * ch, COLS * CW, 4), np.uint8)
    for n, c in enumerate(cells): img[(n // COLS) * ch:(n // COLS + 1) * ch, (n % COLS) * CW:(n % COLS + 1) * CW] = c
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA))

def sil(im, mode):
    a = (im[..., 3] > 128).astype(np.float32); ax = int(anchor_x(im, mode))
    out = np.zeros((720, 400), np.float32); x0 = ax - 200
    s0, s1 = max(0, x0), min(a.shape[1], x0 + 400)
    out[:, s0 - x0:s1 - x0] = a[:720, s0:s1]
    return cv2.resize(out, (100, 180), interpolation=cv2.INTER_AREA)

def best_loop(frames, lo, hi, minlen, maxlen, mode):
    """Silmukka [a, a+n): kuva a+n muistuttaa kuvaa a; pienin ero (pitkät silmukat hieman suositumpia)."""
    s = {i: sil(frames[i], mode) for i in range(lo, hi + 1)}
    best = None
    for a in range(lo, hi - minlen):
        for n in range(minlen, maxlen + 1):
            if a + n > hi: break
            d = np.abs(s[a] - s[a + n]).mean() + np.abs(s[a + 1] - s[a + n + 1]).mean() * 0.5 if a + n + 1 <= hi else 9
            if best is None or d < best[0]: best = (d, a, n)
    print("   silmukka", best); return list(range(best[1], best[1] + best[2]))

def main(vA, vB, vC, outs, resdir):
    A, B, C = load(vA), load(vB), load(vC)
    x0, x1, y0, y1 = bbox(C[0]); k = TARGET_H / (y1 - y0)
    print("mittakaava", round(k, 3), "hahmon korkeus", y1 - y0)
    def sheet(name, vid, idx, mode):
        setdims(name)
        cells = [cell(vid[i], mode, k) for i in idx]
        save(cells, f"{outs}/kuningas_{name}.png"); print(name, len(cells), "kuvaa")
    sheet("idle", C, best_loop(C, 0, 36, 14, 34, "low"), "low")
    sheet("kavely", C, best_loop(C, 44, 134, 20, 44, "torso"), "torso")
    sheet("juoksu", C, best_loop(C, 140, 200, 10, 26, "torso"), "torso")
    sheet("jab", A, list(range(9, 21)), "low")
    sheet("takasuora", A, list(range(21, 41)), "low")
    cast = [i for i in range(12, 109, 4) if i != 92]; rec = list(range(168, 217, 6))
    sheet("tulipallo", B, cast + rec, "low")
    print("tulipallo: latausta", len(cast), "kuvaa, laukaisu = indeksi", len(cast))
    # tulipallo: iso pallo viimeisestä latauskuvasta (kirkas alue), rajattu alfalla
    fr = B[108]; bgr = cv2.cvtColor(fr[..., :3], cv2.COLOR_RGB2BGR)
    bright = ((bgr[..., 2] > 215) & (bgr[..., 1] > 205) & (fr[..., 3] > 128)).astype(np.uint8)
    bright[:, :900] = 0
    n, lab, st, cen = cv2.connectedComponentsWithStats(bright)
    j = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); cx, cy = cen[j]; r = int(max(st[j, 2], st[j, 3]) * 0.78)
    crop = fr[max(0, int(cy) - r):int(cy) + r, int(cx) - r:int(cx) + r].copy()
    yy, xx = np.mgrid[:crop.shape[0], :crop.shape[1]]
    d = np.hypot(yy - crop.shape[0] / 2, xx - crop.shape[1] / 2) / (crop.shape[0] / 2)
    R, G, Bc = crop[..., 0].astype(np.float32), crop[..., 1].astype(np.float32), crop[..., 2].astype(np.float32)
    glow = np.clip((np.minimum(R, G) - 175.0) / 45.0, 0, 1)          # vain kirkas keltavalkoinen pallo: harmaa käsi ja vihreä reuna pois
    glow = cv2.GaussianBlur(glow, (0, 0), 2.0)
    crop[..., 3] = (glow * np.clip((0.97 - d) / 0.2, 0, 1) * 255).astype(np.uint8)
    crop[..., 1] = np.minimum(G, np.maximum(R, Bc) + 25).clip(0, 255).astype(np.uint8)   # vihreä sävy pois
    nn, ll, ss, _ = cv2.connectedComponentsWithStats((crop[..., 3] > 30).astype(np.uint8), connectivity=8)
    keep = 1 + int(np.argmax(ss[1:, cv2.CC_STAT_AREA])); crop[..., 3] = np.where(cv2.dilate((ll == keep).astype(np.uint8), np.ones((9, 9), np.uint8)) > 0, crop[..., 3], 0)   # irtonaiset roiskeet pois
    os.makedirs(resdir, exist_ok=True)
    cv2.imwrite(f"{resdir}/tulipallo.png", cv2.cvtColor(crop, cv2.COLOR_RGBA2BGRA)); print("pallo", crop.shape)


def main_extra(vJ, vH, outs):
    """Tärisyttävä hyppy (vJ) ja osuma/kaatuminen/nousu (vH): 30 fps, 1080p; mittakaava seisovan asennon (kuva 0) korkeudesta, kuten idlellä."""
    J, H = load(vJ), load(vH)
    out = {}
    for name, vid in (("J", J), ("H", H)):
        x0, x1, y0, y1 = bbox(vid[0]); out[name] = (TARGET_H / (y1 - y0), y1)
        print(name, "mittakaava", round(out[name][0], 3), "lattia", out[name][1])
    kj, fj = out["J"]; kh, fh = out["H"]
    def sheet(name, vid, idx, mode, k, fl, ch=None):
        setdims(name)
        save([cell(vid[i], mode, k, fl, ch) for i in idx], f"{outs}/kuningas_{name}.png"); print(name, len(idx), "kuvaa", idx)
    wind = list(range(0, 36, 4)); jump = list(range(38, 92, 3)); land = list(range(92, 121, 3))
    print("hyppy: windup", len(wind), "x 0.1333 s, loput", len(jump) + len(land), "x 0.1 s; osumakuva (maahan) indeksi", len(wind) + len(jump))
    sheet("hyppy", J, wind + jump + land, "torso", kj, fj)
    sheet("osuma", H, list(range(8, 24, 2)), "low", kh, fh)
    sheet("kaatuminen", H, list(range(73, 92, 2)) + [110], "com", kh, fh)
    sheet("maahan", H, [93, 96, 100, 104, 108, 112], "com", kh, fh)
    sheet("nousu", H, list(range(137, 180, 3)) + [179], "com", kh, fh)


def main_throw(vT, outs):
    """Heitto (vT, 30 fps, 1080p): ote kuvissa 0-27, kädet yhteen ~28, nosto 43-59, heitto ~62, toipuminen; joka toinen kuva = 1/15 s. Hero ei ole videolla: koodi pitää häntä kuninkaan edessä."""
    T = load(vT); x0, x1, y0, y1 = bbox(T[0]); k = TARGET_H / (y1 - y0)
    idx = list(range(0, 90, 2)); setdims("heitto")
    save([cell(T[i], "low", k, y1) for i in idx], f"{outs}/kuningas_heitto.png"); print("heitto", len(idx), "kuvaa; kuva 28 = indeksi", idx.index(28), "; kuva 62 = indeksi", idx.index(62))


def main_guard(path, outs):
    """Suojaus: kuva, jossa neljä hahmoa vierekkäin (läpinäkyvä tausta). Mittakaava 1. hahmon korkeudesta (taisteluasento = idle, 600 px), jalat samalle lattialle."""
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 3: im = key_frame(im)
    else: im = cv2.cvtColor(im, cv2.COLOR_BGRA2RGBA)
    cols = (im[..., 3] > 20).sum(0) > 3; runs = []; s0 = None
    for x, c in enumerate(cols):
        if c and s0 is None: s0 = x
        if not c and s0 is not None:
            if x - s0 > 80: runs.append((s0, x))
            s0 = None
    if s0 is not None: runs.append((s0, len(cols)))
    print("hahmoja", len(runs))
    figs = []
    for (a, b) in runs[:4]:
        sub = im[:, a:b]; ys, xs = np.where(sub[..., 3] > 128)
        figs.append(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy())
    k = TARGET_H / figs[0].shape[0]; print("mittakaava", round(k, 3)); setdims("suoja")
    cells = []
    for f in figs:
        f2 = f.copy(); f2 = np.pad(f2, ((0, 0), (0, 0), (0, 0)))
        full = np.zeros((f.shape[0] + 40, f.shape[1] + 40, 4), np.uint8); full[20:20 + f.shape[0], 20:20 + f.shape[1]] = f
        cells.append(cell(full, "low", k, f.shape[0] + 20))
    save(cells, f"{outs}/kuningas_suoja.png"); print("suoja", len(cells), "kuvaa")

if __name__ == "__main__":
    a = sys.argv
    if a[1] == "guard": main_guard(a[2], a[3] if len(a) > 3 else "Assets/Sprites/Viholliset"); sys.exit()
    if a[1] == "throw": main_throw(a[2], a[3] if len(a) > 3 else "Assets/Sprites/Viholliset"); sys.exit()
    if a[1] == "extra": main_extra(a[2], a[3], a[4] if len(a) > 4 else "Assets/Sprites/Viholliset"); sys.exit()
    main(a[1], a[2], a[3], a[4] if len(a) > 4 else "Assets/Sprites/Viholliset", a[5] if len(a) > 5 else "Assets/Resources/Kuningas")
