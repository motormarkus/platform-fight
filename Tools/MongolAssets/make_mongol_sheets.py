"""Mongolisoturi (vihollinen): vihreätaustaisista videoista + suojakuvasta kuvasarjat Assets/Sprites/Viholliset/mongoli_<nimi>.png.
Video A (idle + kivenheitto), B (hyppypotku), C (potkut: 1 korkea etupotku, 2 vaakapotku, 3 pyöräytyspotku), suoja.png 4 kuvaa vierekkäin.
Ruudut kuten mainarilla (Tools/MinerAssets): 4 saraketta, hahmo ~640 px, jalat 16 px ylös.
Video D (kaksi suoraa lyöntiä + etupotku, potkua ei käytetä), video E (idle, kävely, juoksu; kypärän punainen tupsu poistetaan kuvista, koska muissa videoissa sitä ei ole).
Ajo: python Tools/MongolAssets/make_mongol_sheets.py <idle_heitto.mov> <hyppypotku.mov> <potkut.mov> <suoja.png> <suorat.mov> <liike.mp4> <osuma_kaatuminen.mp4> [ulos-sheetit]
Video F: osuma, kaatuminen, maahan, nousu."""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 640.0
m.DIMS.update({"g_heitto": (896, 320, 768), "g_hyppypotku": (1152, 384, 1024), "g_potku1": (1024, 320, 768),
               "g_potku2": (1152, 320, 768), "g_potku3": (1152, 448, 896), "g_suoja": (640, 320, 768),
               "g_osuma": (896, 420, 768), "g_kaatuminen": (1152, 576, 1024), "g_maahan": (1152, 576, 768), "g_nousu": (1152, 576, 768),
               "g_suora1": (896, 320, 768), "g_suora2": (896, 320, 768), "g_kavely": (640, 320, 768), "g_juoksu": (768, 320, 768), "g_idle": (640, 320, 768)})


def strip_plume(im):
    """Poistaa kypärän punaisen tupsun: punaiset pikselit kultaisen vanteen yläreunan yläpuolelta (vartalon ja niskasuojan punainen jää)."""
    a = im[..., 3] > 128; ys, xs = np.where(a); y0 = ys.min(); h = ys.max() - y0
    rgb = im[..., :3].astype(np.int32); R, G, B = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    gold = a & (R > 140) & (G > 85) & (G < 175) & (B < 115) & (R > G * 1.15)
    zone = slice(y0, y0 + int(h * 0.30))
    rows = gold[zone].sum(1); band = np.where(rows >= 14)[0]
    if len(band) == 0: return im
    top = y0 + band[0]
    red = a & (R > 40) & (R > G * 1.45) & (R > B * 1.3)
    mx = np.maximum(np.maximum(R, G), B); mn = np.minimum(np.minimum(R, G), B)
    core = a & ~red & ((mx - mn) < 32) & (mx > 70)
    k7 = np.ones((7, 7), np.uint8)
    keepz = cv2.dilate(core.astype(np.uint8), k7).astype(bool) | cv2.dilate(gold.astype(np.uint8), k7).astype(bool)
    kill = np.zeros_like(a); kill[:top] = (red | (a & ~keepz))[:top]
    kill = cv2.dilate(kill.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & a & ~core
    kill[top:] = False
    out = im.copy(); out[kill, 3] = 0
    # kypärän kupu ehjäksi (16.11.2026): tupsu peitti kuvun vasemman reunan -> kupu piirretään puoliellipsinä vanteen päälle
    # (leveys ~0,9 x vanne, korkeus kuvun ehjän oikean puolen mukaan), väri kuvun harmaista pikseleistä peilaten, tumma ääriviiva, kultainen kärki keskellä
    brow = np.where(gold[top])[0]
    if len(brow) >= 10:
        A = out[..., 3] > 128
        gcols = np.where(gold[top:top + 10].any(0))[0]   # vanteen leveys sen ylimmiltä riveiltä
        cx = 0.5 * (gcols.min() + gcols.max()); hw = 0.5 * (gcols.max() - gcols.min()) * 1.0   # kupu vanteen levyinen (käyttäjä: 0,9 oli liian pieni)
        grey = A & ((mx - mn) < 45) & (mx > 55) & (mx < 215)
        g = np.zeros_like(A); g[:top] = grey[:top]
        g = cv2.morphologyEx(g.astype(np.uint8), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8)).astype(bool)
        gy, gx = np.nonzero(g & (np.arange(A.shape[1])[None, :] >= cx))
        if len(gy) > 20:
            hh = max(8.0, top - np.percentile(gy, 2), 0.9 * hw)   # videon kupu: korkeus ~0,9 x puolileveys
            yy, xx = np.mgrid[:top + 1, :A.shape[1]]
            e = ((xx - cx) / hw) ** 2 + ((yy - top) / hh) ** 2          # < 1 kuvun sisällä
            inside = (e < 1.0) & (yy <= top)
            src = out[..., :3].copy()
            # väri: peilikuva oikealta, muuten lähin harmaa kupupikseli
            _, lb = cv2.distanceTransformWithLabels((~g).astype(np.uint8), cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
            dy, dx = np.nonzero(g); lut = np.zeros((lb.max() + 1, 3), np.uint8); lut[lb[dy, dx]] = src[dy, dx]
            iy, ix = np.nonzero(inside)
            sx = np.clip(np.round(2 * cx - ix).astype(int), 0, A.shape[1] - 1)
            use_m = (ix < cx) & g[iy, sx]
            col = lut[lb[iy, ix]].copy(); col[use_m] = src[iy[use_m], sx[use_m]]
            keep_own = g[iy, ix]; col[keep_own] = src[iy[keep_own], ix[keep_own]]
            out[iy, ix, :3] = col
            # tumma ääriviiva ellipsin reunalle (~2 px)
            rim = inside & (e > (1 - 2.6 / min(hw, hh)) ** 2)
            out[:top + 1, :, :3][rim] = (30, 26, 28)
            # alfa: kupu (pehmeä reuna) + kärki; kaikki muu vanteen yläpuolelta pois
            sm = np.clip((1.0 - e) * min(hw, hh) / 1.5 + 0.5, 0, 1); sm[yy > top] = 0
            al = sm
            # kultainen nuppi kuvun huipulle (vanteen väri)
            gy2, gx2 = np.nonzero(gold[top:top + 10]); gc = out[top + gy2, gx2, :3].reshape(-1, 3).mean(0) if len(gy2) else np.array([190, 140, 70])
            r = max(3.0, hh * 0.16); ky = top - hh - r * 0.4
            dk = np.hypot(xx - cx, (yy - ky) / 1.15)
            knob = dk < r
            out[:top + 1, :, :3][knob] = gc.astype(np.uint8)
            out[:top + 1, :, :3][knob & (dk > r - 1.8)] = (30, 26, 28)
            al = np.maximum(al, np.clip(r - dk + 0.5, 0, 1))
            out[:top, :, 3] = (al[:top] * 255).astype(np.uint8)
    return out


def _band(im):
    """Kypärän kultainen vanne: (yläreunan rivi, vasen x, oikea x, korkeus) tai None."""
    a = im[..., 3] > 128; ys, xs = np.where(a); y0 = ys.min(); h = ys.max() - y0
    rgb = im[..., :3].astype(np.int32); R, G, B = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    gold = a & (R > 140) & (G > 85) & (G < 175) & (B < 115) & (R > G * 1.15)
    rows = gold[y0:y0 + int(h * 0.30)].sum(1); band = np.where(rows >= 14)[0]
    if len(band) == 0: return None
    top = y0 + band[0]; bh = 1
    while top + bh < im.shape[0] and gold[top + bh].sum() >= 10: bh += 1
    cols = np.where(gold[top:top + max(4, bh)].any(0))[0]
    return top, cols.min(), cols.max(), bh

def helmet_patch(im):
    """Suippo kypärän kupu + piikki (vanteen yläpuolinen osa) kuvasta, jossa ei ole tupsua (video A)."""
    b = _band(im)
    if b is None: return None
    top, x0, x1, bh = b
    a = im[:top + 2, :, 3]; ys, xs = np.where(a > 20)
    sel = (xs >= x0 - 6) & (xs <= x1 + 6); ys, xs = ys[sel], xs[sel]
    py0 = ys.min(); patch = im[py0:top + 2, x0 - 6:x1 + 7].copy()
    return patch, (x1 - x0)

def replace_helmet(im, hp):
    """Idle/kävely/juoksu-videon pyöreä kypärä + tupsu -> sama suippo kypärä kuin muissa liikkeissä (16.11.2026):
    vanteen yläpuoli tyhjennetään, tupsun jäänteet vanteen vasemmalta puolelta pois, kypäräpala skaalataan vanteen levyiseksi ja asetetaan vanteen päälle."""
    if hp is None: return im
    b = _band(im)
    if b is None: return im
    top, x0, x1, bh = b
    patch, pw = hp; k = (x1 - x0) / max(1, pw)
    ph, pwid = patch.shape[:2]
    P = cv2.resize(patch, (max(1, int(round(pwid * k))), max(1, int(round(ph * k)))), interpolation=cv2.INTER_AREA)
    out = im.copy()
    out[:top + 1, :, 3] = 0
    # tupsun jäänteet niskan puolella vanteen korkeudella: tilalle niskasuojan viisto yläreuna (vanteen kulmasta alas vasemmalle), ei suoraa leikkausta
    yb = min(out.shape[0] - 1, top + bh + 3); xl = max(0, x0 - 1)
    reg = out[top:yb, :xl].copy(); below = out[yb, :xl]
    yy, xx = np.mgrid[top:yb, :xl]
    keep = (below[None, :, 3] > 128) & ((yy - top) >= (xl - xx) * 0.55)
    reg[..., :3] = np.where(keep[..., None], below[None, :, :3], reg[..., :3])
    reg[..., 3] = np.where(keep, 255, 0)
    out[top:yb, :xl] = reg
    # ääriviiva uudelle reunalle ja pehmeä alfa
    km = np.zeros(out.shape[:2], np.uint8); km[top:yb, :xl] = keep
    edge = (km > 0) & (cv2.erode(km, np.ones((5, 5), np.uint8)) == 0)
    edge[yb - 1:] = False
    out[edge, :3] = (32, 22, 24)
    sl = (slice(max(0, top - 3), yb + 2), slice(0, xl + 2))
    a_ = out[sl][..., 3].astype(np.float32) / 255
    out[sl][..., 3] = (np.clip((cv2.GaussianBlur(a_, (0, 0), 0.9) - 0.5) * 2.2 + 0.5, 0, 1) * 255).astype(np.uint8)
    # tupsun ohuet tummat suikaleet niskan takana (juoksussa tupsu liehuu alempana): ohuet tummat ulokkeet pois vanteen vasemmalta puolelta
    ry = slice(max(0, top - 2), min(out.shape[0], top + bh * 5)); rx = slice(0, max(0, x0 + 4))
    am = (out[ry, rx, 3] > 60).astype(np.uint8)
    op = cv2.morphologyEx(am, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))
    lum = out[ry, rx, :3].astype(np.int32).mean(2)
    thin = (am > 0) & (op == 0) & (lum < 90)
    sub = out[ry, rx]; sub[thin, 3] = 0; out[ry, rx] = sub
    # irtonaiset palat pois (pienet erilliset alueet)
    m = (out[..., 3] > 20).astype(np.uint8); n, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
    if n > 2:
        big = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); out[(lab != big) & (lab > 0), 3] = 0
    Ph, Pw = P.shape[:2]; ox = int(round(x0 - 6 * k)); oy = top + 2 - Ph
    sy0, sx0 = max(0, -oy), max(0, -ox); dy0, dx0 = max(0, oy), max(0, ox)
    hh = min(Ph - sy0, out.shape[0] - dy0); ww = min(Pw - sx0, out.shape[1] - dx0)
    if hh > 0 and ww > 0:
        src = P[sy0:sy0 + hh, sx0:sx0 + ww].astype(np.float32); dst = out[dy0:dy0 + hh, dx0:dx0 + ww].astype(np.float32)
        sa = src[..., 3:4] / 255.0; da = dst[..., 3:4] / 255.0
        oa = sa + da * (1 - sa)
        rgb = (src[..., :3] * sa + dst[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-4)
        out[dy0:dy0 + hh, dx0:dx0 + ww] = np.dstack([rgb, oa * 255]).clip(0, 255).astype(np.uint8)
    return out


def main(vA, vB, vC, guard, vD, vE, vF, outs):
    A, B, C, D = m.load(vA), m.load(vB), m.load(vC), m.load(vD)
    E0 = m.load(vE); HP = helmet_patch(A[0]); E = [replace_helmet(f, HP) for f in E0]   # 16.11.2026: pyöreä kupu+tupsu -> sama suippo kypärä kuin muissa liikkeissä (video A)   # mittakaava lasketaan tupsun/piikin poistoa edeltävästä kuvasta
    F = m.load(vF)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C); kD, fD = kf(D); kE, fE = kf(E0); kF, fF = kf(F)
    print("mittakaavat", round(kA, 3), round(kB, 3), round(kC, 3), round(kD, 3), round(kE, 3), round(kF, 3))

    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("g_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/mongoli_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    sheet("heitto", A, list(range(51, 120, 3)), "low", kA, fA); print("heitto: nosto indeksi 9 (kuva 78), irtipäästö noin 9–10")
    sheet("hyppypotku", B, [4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32], lambda i: "low" if i < 12 or i > 28 else "com", kB, fB); print("hyppypotku: osuma indekseissä 6–10 (kuvat 16–24)")
    sheet("potku1", C, [12, 14, 16, 18, 20, 22, 24, 26], "low", kC, fC); print("potku1: osuma indeksi 3–4 (kuvat 18–20)")
    sheet("potku2", C, [39, 41, 43, 45, 47, 49, 51, 53, 55, 57], "low", kC, fC); print("potku2: osuma indeksit 3–5 (kuvat 45–49)")
    sheet("potku3", C, [58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 81], "low", kC, fC); print("potku3: pyöräytys, osuma indeksit 6–8 (kuvat 70–74)")
    sheet("suora1", D, [0, 2, 4, 6, 8, 10, 12, 14, 16], "low", kD, fD); print("suora1: osuma indeksit 3–6 (kuvat 6–12)")
    sheet("suora2", D, [16, 18, 20, 22, 24, 26, 28, 30], "low", kD, fD); print("suora2: osuma indeksit 2–4 (kuvat 20–24)")
    sheet("idle", E, m.best_loop(E, 0, 36, 14, 36, "low"), "low", kE, fE)
    sheet("kavely", E, m.best_loop(E, 39, 112, 16, 50, "torso"), "torso", kE, fE)
    sheet("juoksu", E, m.best_loop(E, 114, 210, 10, 30, "torso"), "torso", kE, fE)
    # osuma/kaatuminen/maahan/nousu: video F (iskun välähdys kuvissa 10–14 jätetään pois)
    sheet("osuma", F, [9, 15, 17, 19, 21, 23], "low", kF, fF)
    sheet("kaatuminen", F, list(range(24, 84, 3)), "com", kF, fF)
    sheet("maahan", F, [84, 90, 96, 102, 108, 114], "com", kF, fF)
    sheet("nousu", F, list(range(117, 170, 3)), "com", kF, fF)
    G = cv2.imread(guard, cv2.IMREAD_UNCHANGED); gh, gw = G.shape[:2]; qw = gw // 4
    quads = [cv2.cvtColor(G[:, i * qw:(i + 1) * qw], cv2.COLOR_BGRA2RGBA) for i in range(4)]
    bb = [m.bbox(q) for q in quads]; kg = m.TARGET_H / (bb[0][3] - bb[0][2])
    m.setdims("g_suoja")
    cells = [m.cell(q, "low", kg, b[3]) for q, b in zip(quads, bb)]
    m.save(cells, f"{outs}/mongoli_suoja.png"); print("suoja 4 kuvaa")


if __name__ == "__main__":
    main(*sys.argv[1:8], sys.argv[8] if len(sys.argv) > 8 else "Assets/Sprites/Viholliset")
