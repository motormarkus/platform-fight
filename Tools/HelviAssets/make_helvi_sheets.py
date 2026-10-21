"""Helvin (panttilainaamon myyjä) kuvasarjat videoista. Ruutu 320 x 400 px, 10 saraketta, hahmon seisova korkeus 300 px, jalat rivillä 384 (kuten Candi).
Käyttö: python make_helvi_sheets.py <ulostulo-kansio> [preview]
Lähteet (käyttäjän chat-liitteet, polut alla):
  V3 (2cbb3504): idle (0-105), käännös (105-140), kävely sivusta oikealle (140-299)
  V2 (5fd2bffa): hiusten sipaisu (135-239; ennen sitä vihreä logo taustalla)
V1 (0b7f63b5) ei käytössä: taustalla outoja siluetteja."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from key_video import key_frame

UP = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
V3 = UP + "2cbb3504-copy_F8E5E0C2-6060-4A96-851C-98DF06D2EAF0.mov"
V2 = UP + "5fd2bffa-copy_5845CFD6-6F0A-45C1-87C0-83C2A76B8D18.mov"
CELL_W, CELL_H, COLS, TARGET_H, FEET_ROW = 320, 400, 10, 300.0, 384


def read_all(path):
    cap = cv2.VideoCapture(path); out = []
    while True:
        ok, f = cap.read()
        if not ok: break
        out.append(f)
    return out


def key(bgr, dark=False):
    k = key_frame(bgr, t_in=14.0 if dark else 26.0, t_out=50.0 if dark else 60.0)
    # tumma lattia / jäännösvihreä pois (ihon ja vaatteiden sävyissä g ei ole r:ää suurempi)
    b, g, r, a = [k[..., i].astype(int) for i in range(4)]
    green = (g - r > 14) & (g - b > 6)
    k[green, 3] = 0
    # ohuet jäännösviivat (lattian reuna) pois: avaus katkaisee ohuet yhteydet, jäljelle jää vain hahmo
    solid = (k[..., 3] > 128).astype(np.uint8)
    opened = cv2.morphologyEx(solid, cv2.MORPH_OPEN, np.ones((7, 7), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(opened, connectivity=8)
    if n > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
        opened = (lab == keep).astype(np.uint8)
    allow = cv2.dilate(opened, np.ones((9, 9), np.uint8))   # hahmon ympärillä pieni reunavara (pehmeä reuna)
    k[allow == 0, 3] = 0
    return k


def resize_premult(rgba, w, h):
    f = rgba.astype(np.float32); a = f[..., 3:4] / 255.0; f[..., :3] *= a
    f = cv2.resize(f, (w, h), interpolation=cv2.INTER_AREA)
    a2 = f[..., 3:4] / 255.0
    f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
    return f.clip(0, 255).astype(np.uint8)


def body_center_x(k):
    """Vartalon vaakakeskikohta: hahmon korkeuden 35-55 % välisten rivien peitteisten pikselien keskiarvo (käsien heilunta ei heiluta)."""
    ys, xs = np.where(k[..., 3] > 128)
    y0, y1 = ys.min(), ys.max(); h = y1 - y0
    m = (ys > y0 + 0.35 * h) & (ys < y0 + 0.55 * h)
    return float(xs[m].mean())


def sheet(frames, ref_h, out_path, follow=True):
    """frames: avatut RGBA-ruudut. Skaalaus ref_h:n mukaan (sama kaikille sarjoille)."""
    s0 = TARGET_H / ref_h
    cxs = np.array([body_center_x(k) for k in frames]); bots = np.array([float(np.where(k[..., 3] > 128)[0].max()) for k in frames])
    def rmed(a, w):
        h = w // 2; return np.array([np.median(a[max(0, i - h):i + h + 1]) for i in range(len(a))])
    base = rmed(bots, 9) if follow else np.full(len(frames), np.median(bots))
    cx_all = cxs if follow else np.full(len(frames), cxs[0])
    rows = (len(frames) + COLS - 1) // COLS
    out = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
    pad = 700
    for i, k in enumerate(frames):
        sw, sh = int(round(CELL_W / s0)), int(round(CELL_H / s0))
        x0 = int(round(cx_all[i] - sw / 2)); y1 = int(round(base[i] + (CELL_H - FEET_ROW) / s0)); y0 = y1 - sh
        big = cv2.copyMakeBorder(k, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
        crop = big[y0 + pad:y1 + pad, x0 + pad:x0 + sw + pad]
        cell = resize_premult(crop, CELL_W, CELL_H)
        out[(i // COLS) * CELL_H:(i // COLS + 1) * CELL_H, (i % COLS) * CELL_W:(i % COLS + 1) * CELL_W] = cell
    cv2.imwrite(out_path, cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
    print(os.path.basename(out_path), len(frames), "ruutua")


def best_loop_end(keyed, start, lo, hi):
    """Silmukan loppu: ruutu lähellä alkua (pienin ero)."""
    a = keyed[start][..., 3].astype(np.float32)
    best, bi = 1e18, hi
    for j in range(lo, hi):
        d = float(np.abs(keyed[j][..., 3].astype(np.float32) - a).sum()) + float(np.abs(keyed[j][..., :3].astype(np.float32) - keyed[start][..., :3].astype(np.float32)).mean()) * 1000
        if d < best: best, bi = d, j
    return bi


def main(out):
    os.makedirs(out, exist_ok=True)
    v3 = read_all(V3); v2 = read_all(V2)
    print('v3', len(v3), 'v2', len(v2))
    # idle (v3 0-110): etsi silmukan loppu
    idle_k = [key(v3[i]) for i in range(0, 112)]
    ref_h = float(np.ptp(np.where(idle_k[0][..., 3] > 128)[0]))
    end = best_loop_end(idle_k, 0, 70, 111)
    idle = [idle_k[i] for i in range(0, end, 3)]
    sheet(idle, ref_h, os.path.join(out, 'helvi_idle.png'), follow=False)
    # hiusten sipaisu (v2 135-239), tausta musta
    hair = [key(v2[i], dark=True) for i in range(135, 240, 3)]
    sheet(hair, ref_h, os.path.join(out, 'helvi_hiukset.png'), follow=False)
    # käännös (v3 105-142) joka toinen ruutu
    turn = [key(v3[i]) for i in range(105, 143, 2)]
    sheet(turn, ref_h, os.path.join(out, 'helvi_kaanto.png'), follow=False)
    # kävely (v3 143-299): etsi jakso
    walk_k = [key(v3[i]) for i in range(143, 300)]
    # jakson pituus: ruutu, joka muistuttaa eniten ensimmäistä (jalkojen asento, vartalon keskikohta kohdistettuna)
    def aligned(k):
        cx = int(body_center_x(k)); ys = np.where(k[..., 3] > 128)[0]; yb = int(ys.max())
        win = k[yb - 300:yb + 5, cx - 150:cx + 150, 3].astype(np.float32)
        return win if win.shape == (305, 300) else np.zeros((305, 300), np.float32)
    A = [aligned(k) for k in walk_k]
    diffs = [(float(np.abs(A[j] - A[0]).sum()), j) for j in range(20, min(70, len(A)))]
    period = min(diffs)[1]
    print('kävelyjakso', period, 'ruutua')
    sheet(walk_k[:period:2], ref_h, os.path.join(out, 'helvi_kavely.png'), follow=True)


if __name__ == "__main__":
    main(sys.argv[1])
