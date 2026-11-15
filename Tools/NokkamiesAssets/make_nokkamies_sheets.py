"""Nokkamies (foreman, tyomaan pomo, kypara + huomioliivi): vihreataustaisista videoista ja suojakuvasta kuvasarjat Assets/Sprites/Viholliset/nokkamies_<nimi>.png.
Video A (hyokkays): suorat 1-4, potkut 1-3, syoksy2 (loppu). Video B: kavely (54-120) ja juoksu (126-238). Video C: osuma, kaatuminen, maahan, nousu. Video D (syoksylyonti): syoksy + idle (idle otetaan sen lopusta, hahmo ei katso kameraan).
Suoja: 3200x768 lapinakyva kuva, 5 hahmoa. Hahmo 600 px, jalat 16 px ylos; ruudun koko lasketaan automaattisesti sheetin laajimmasta kuvasta (4 saraketta) ja tulostetaan (dims.json).
SIISTI REUNA: videon binaarialfa pehmennetaan (Gauss) ja nostetaan loppukokoon kuutiointerpoloinnilla, sen jalkeen smoothstep: ei porrasreunoja eika rosoa. Kyparan irtoaminen kaatumisessa jaa pois (vain suurin yhtenainen hahmo).
Ajo: python Tools/NokkamiesAssets/make_nokkamies_sheets.py <hyokkays> <kavely> <kaatuminen> <syoksy> <suoja.png> [ulos]"""
import os, sys, json
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
from key_video import key_frame

TARGET_H, FOOT, COLS, PAD = 600.0, 16, 4, 12
KEY_HI, KEY_RANGE = 0.85, 0.60   # alfa = 1, kun vihreän osuus <= KEY_HI-KEY_RANGE (Lokki: tiukempi, valkoinen puku ei vaadi neonliivin varaa)
def up(v, q=32): return int(-(-v // q) * q)

def soft_key(bgr):
    """Hahmon alfa suoraan vihreän osuudesta pikselissä (ei key_frame-maskia: se söi vaaleat reunat, esim. nenän kärjen).
    exc = g - max(r,b): tausta ~ suuri, hahmo ~ pieni (neonliivikin alle 25 % taustasta). Suurin yhtenäinen hahmo säilyy (kypärän irtoaminen, vesileima pois).
    Reunapikselin väri puretaan vihreästä (F = (p - (1-a) G) / a): ei vihreää kehää."""
    img = bgr.astype(np.float32); b_, g_, r_ = img[..., 0], img[..., 1], img[..., 2]
    border = np.concatenate([img[:10].reshape(-1, 3), img[-10:].reshape(-1, 3), img[:, :10].reshape(-1, 3), img[:, -10:].reshape(-1, 3)])
    G = np.median(border, axis=0)
    exc = g_ - np.maximum(r_, b_); ebg = float(np.median(border[:, 1] - np.maximum(border[:, 2], border[:, 0])))
    alpha = np.clip((KEY_HI * ebg - exc) / (KEY_RANGE * ebg), 0, 1).astype(np.float32)
    n, lab, st, _ = cv2.connectedComponentsWithStats((alpha > 0.5).astype(np.uint8), connectivity=8)
    keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    near = cv2.dilate((lab == keep).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
    alpha = np.where(near, alpha, 0).astype(np.float32)
    # vihreän valumisen poisto reunalta (despill): reunavyöhykkeellä (~4 px taustasta) vihreä kanava ei saa ylittää punaista/sinistä
    band = cv2.dilate((alpha < 0.98).astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
    gmax = np.maximum(r_, b_) + 4.0
    g2 = np.where(band, np.minimum(g_, gmax), g_)
    # myös hieman tummennus reunavyöhykkeellä (taustan vihreän vaalea sekoitus pois)
    dark = np.where(band, 0.92, 1.0)[..., None]
    out = np.dstack([r_, g2, b_]) * dark
    return np.dstack([out, alpha * 255.0]).clip(0, 255).astype(np.uint8)

OUTLINE = np.array([22, 14, 18], np.float32)
INK_DEPTH, INK_ERODE = 0.0, 0.0   # Lokki: reunan "mustetus": alfaa kutistetaan INK_ERODE px ja reunan INK_DEPTH px syvyydeltä väri sekoitetaan ääriviivan väriin (vihreä/olivi reunakehä pois)
def soften_outline(rgba, thick=1.7, ink=True):
    """Pehmennä reuna ja paksunna ääriviivaa hennosti (thick px) tummalla ääriviivalla; sileä alfa, ei portaita.
    ink (INK_DEPTH > 0): reunan sisäpuoli värjätään ääriviivan väriin (tausta-avaimen jäämät pois), siisti yhtenäinen tumma ääriviiva."""
    import math
    a = rgba[..., 3].astype(np.float32) / 255.0
    pad = 6; a = np.pad(a, pad); rgb = np.pad(rgba[..., :3].astype(np.float32), ((pad, pad), (pad, pad), (0, 0)), mode="edge")
    b = cv2.GaussianBlur(a, (0, 0), 0.7)
    thr = 0.5
    use_ink = ink and INK_DEPTH > 0
    if use_ink and INK_ERODE > 0: thr = 0.5 * (1 + math.erf(INK_ERODE / (0.7 * math.sqrt(2))))
    a2 = np.clip((b - thr) * (3.0 if use_ink else 2.0) + 0.5, 0, 1)
    d = cv2.distanceTransform((a2 < 0.5).astype(np.uint8), cv2.DIST_L2, 5)
    ring = np.clip(thick - d + 0.5, 0, 1) * (1 - a2)
    oa = np.clip(a2 + ring, 0, 1)
    w = np.where(oa > 1e-3, a2 / np.maximum(oa, 1e-3), 1.0)[..., None]
    col = rgb * w + OUTLINE[None, None, :] * (1 - w)
    if use_ink:
        din = cv2.distanceTransform((a2 >= 0.5).astype(np.uint8), cv2.DIST_L2, 5)
        inkw = (np.clip(1.0 - (din - 0.5) / INK_DEPTH, 0, 1) * 0.92)[..., None] * (a2 >= 0.5)[..., None]
        col = col * (1 - inkw) + OUTLINE[None, None, :] * inkw
    out = np.dstack([col.clip(0, 255), (oa * 255).clip(0, 255)]).astype(np.uint8)
    return out[pad:-pad, pad:-pad]

def load(path): return [soft_key(f.to_ndarray(format="bgr24")) for f in av.open(path).decode(video=0)]

def clean_rescale(rgba, k):
    """Siisti skaalaus: väri Lanczos (täytettynä lähimmällä näkyvällä värillä) ja pehmeä alfa kuutiointerpoloinnilla; kevyt kontrasti reunaan."""
    h, w = rgba.shape[:2]; nw, nh = max(1, round(w * k)), max(1, round(h * k))
    a = rgba[..., 3].astype(np.float32) / 255.0
    rgb = rgba[..., :3].astype(np.float32)
    solid = (rgba[..., 3] > 250).astype(np.uint8)
    dist, lab = cv2.distanceTransformWithLabels(1 - solid, cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
    ys, xs = np.nonzero(solid)
    if len(ys):
        idx = np.zeros(lab.max() + 1, np.int64); idx[lab[ys, xs]] = np.arange(len(ys))
        src = idx[lab]; rgb = np.where((a < 0.98)[..., None], rgb[ys[src], xs[src]], rgb)
    interp = cv2.INTER_LANCZOS4 if k >= 1 else cv2.INTER_AREA
    rgb2 = cv2.resize(rgb, (nw, nh), interpolation=interp)
    a2 = cv2.resize(a, (nw, nh), interpolation=cv2.INTER_CUBIC)
    a2 = np.clip((a2 - 0.06) / 0.88, 0, 1); a2 = a2 * a2 * (3 - 2 * a2) * 0.35 + a2 * 0.65
    return np.dstack([rgb2.clip(0, 255), a2 * 255.0]).astype(np.uint8)

class Sheet:
    def __init__(self, name): self.name = name; self.items = []
    def add(self, frame, mode, k, floor, ink=True):
        ax = m.anchor_x(frame, mode)
        ys, xs = np.where(frame[..., 3] > 10)
        cx0, cx1, cy0, cy1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
        r = clean_rescale(frame[cy0:cy1, cx0:cx1], k)
        r = np.pad(r, ((3, 3), (3, 3), (0, 0))); r = soften_outline(r, ink=ink)
        cy0 -= 3 / k; cy1 += 3 / k; cx0 -= 3 / k; cx1 += 3 / k
        ty, by = (cy0 - floor) * k, (cy1 - floor) * k
        if by > 0: ty -= by; by = 0.0   # ei lattian alle (maassa makaava hahmo nostetaan lattialle)
        self.items.append((r, (cx0 - ax) * k, ty, by))
    def save(self, outs, dims):
        left = max(-it[1] for it in self.items) + PAD; right = max(it[1] + it[0].shape[1] for it in self.items) + PAD
        top = max(-it[2] for it in self.items); belowfloor = 0
        ANCH = int(round(left)); CW = up(ANCH + right)
        CH = up(top + FOOT + PAD + belowfloor, 64)
        cells = []
        for r, dx, ty, by in self.items:
            c = np.zeros((CH, CW, 4), np.uint8)
            x0 = int(round(ANCH + dx)); y0 = int(round(CH - FOOT + ty))
            h, w = r.shape[:2]; c[y0:y0 + h, x0:x0 + w] = r; cells.append(c)
        rows = (len(cells) + COLS - 1) // COLS
        img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
        for n, c in enumerate(cells): img[(n // COLS) * CH:(n // COLS + 1) * CH, (n % COLS) * CW:(n % COLS + 1) * CW] = c
        os.makedirs(outs, exist_ok=True)
        cv2.imwrite(f"{outs}/nokkamies_{self.name}.png", cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA))
        dims[self.name] = [CW, ANCH, CH]; print(f"{self.name}: {len(cells)} kuvaa, ruutu {CW}x{CH}, ankkuri x {ANCH}")

def main(vA, vB, vC, vD, guard, outs):
    A, B, C, D = load(vA), load(vB), load(vC), load(vD)
    def kf(v): x0, x1, y0, y1 = m.bbox(v[0]); return TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C); kD, fD = kf(D)
    print("mittakaavat", [round(x, 3) for x in (kA, kB, kC, kD)])
    dims = {}
    def sheet(name, vid, idx, mode, k, floor):
        s = Sheet(name); [s.add(vid[i], mode, k, floor) for i in idx]; s.save(outs, dims); print("   kuvat", list(idx))
    sheet("idle", D, m.best_loop(D, 156, 238, 16, 54, "low")[::2], "low", kD, fD)
    sheet("kavely", B, m.best_loop(B, 54, 120, 16, 50, "torso")[::2], "torso", kB, fB)
    sheet("juoksu", B, m.best_loop(B, 126, 238, 10, 30, "torso")[::2], "torso", kB, fB)
    sheet("suora1", A, list(range(22, 33, 2)), "low", kA, fA); print("   suora1: osuma indeksi 3 (kuva 28)")
    sheet("suora2", A, list(range(32, 41, 2)), "low", kA, fA); print("   suora2: osuma indeksi 2 (kuva 36)")
    sheet("suora3", A, list(range(40, 49, 2)), "low", kA, fA); print("   suora3: osuma indeksi 2 (kuva 44)")
    sheet("suora4", A, list(range(48, 59, 2)), "low", kA, fA); print("   suora4: osuma indeksi 2-3 (kuvat 52-54)")
    sheet("potku1", A, list(range(58, 79, 2)), "low", kA, fA); print("   potku1: osuma indeksit 3-4 (kuvat 64-66)")
    sheet("potku2", A, list(range(78, 107, 2)), "low", kA, fA); print("   potku2 (takapotku): osuma indeksit 4-5 (kuvat 86-88)")
    sheet("potku3", A, list(range(108, 139, 2)), "low", kA, fA); print("   potku3 (suora + korkea potku): suora osuma indeksi 3 (kuva 114), potku indeksit 6-8 (kuvat 120-124)")
    sheet("syoksy2", A, list(range(170, 215, 2)), "low", kA, fA); print("   syoksy2: osuma indeksit 5-8 (kuvat 180-186)")
    sheet("syoksy", D, list(range(30, 80, 4)) + list(range(80, 96, 2)) + list(range(96, 134, 6)) + list(range(138, 151, 3)), "low", kD, fD)
    sheet("osuma", C, list(range(20, 31, 2)), "low", kC, fC)
    sheet("kaatuminen", C, list(range(30, 93, 3)), "com", kC, fC)
    sheet("maahan", C, [96, 108, 120, 132, 138], "com", kC, fC)
    sheet("nousu", C, list(range(138, 223, 4)) + [228], "com", kC, fC)
    # suoja: valmis lapinakyva kuva, 5 hahmoa vierekkain
    G = cv2.cvtColor(cv2.imread(guard, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
    cols = (G[..., 3] > 20).sum(0) > 3; runs = []; s0 = None
    for x, c in enumerate(cols):
        if c and s0 is None: s0 = x
        if not c and s0 is not None:
            if x - s0 > 80: runs.append((s0, x))
            s0 = None
    figs = []
    for a, b in runs:
        sub = G[:, a:b]; ys, xs = np.where(sub[..., 3] > 128); figs.append(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
    print("suoja: hahmoja", len(figs), [f.shape[0] for f in figs])
    kg = TARGET_H / figs[0].shape[0]
    s = Sheet("suoja")
    for f in figs:
        fr = np.zeros((f.shape[0] + 40, f.shape[1] + 40, 4), np.uint8); fr[20:20 + f.shape[0], 20:20 + f.shape[1]] = f
        s.add(fr, "low", kg, f.shape[0] + 20)
    s.save(outs, dims)
    json.dump(dims, open(os.path.join(HERE, "dims.json"), "w"), indent=1)

if __name__ == "__main__":
    a = sys.argv
    main(a[1], a[2], a[3], a[4], a[5], a[6] if len(a) > 6 else "Assets/Sprites/Viholliset")
