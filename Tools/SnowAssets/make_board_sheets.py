"""Roccon lumilauta: generaattorin videosta (idle 0-76, ponnistus 76-110, voltti 110-168, alastulo 168-196) kuvasarjat
Assets/Sprites/Lumilauta/rocco_lauta_idle|ponnistus|voltti|alastulo.png (4 saraketta, ruutu 768 x 448, jalat/laudan pohja 8 px ylös, hahmo katsoo oikealle).
Voltti tehdään uudelleen sulavaksi: videon kierto askeltaa epätasaisesti (4-8 astetta ruudulta, kopioituja ruutuja). Jokaisen ruudun kierto estimoidaan (ORB + RANSAC),
vartalo käännetään pystyyn, ja uusi voltti on näiden pystyasentojen tasainen ristiinhäivytys + tasainen kierto (kierto loppuu täsmälleen videon alastuloruudun kulmaan).
Ruutujen kiinnitys: laudan pohja = jalkalinja (idle/ponnistus/alastulo); voltissa vartalon painopiste on vakiokorkeudella jalkalinjan yläpuolella (nosto kerrotaan tulosteessa).
Ajo: python Tools/SnowAssets/make_board_sheets.py <video.mp4> <ulos-kansio> [<esikatselu.mp4> <ffmpeg>]"""
import math, os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "PilotAssets"))
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
import make_pilot_sheets as P
from key_video import key_frame

K = P.TARGET_H / 590.0
FLIP_A, FLIP_B = 110, 168          # voltin lähderuudut (nousun alku -> alastuloruutu)
N_FLIP = 28                        # voltin ruutuja


def load(path):
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f, cyan_safe=True))
    return fr


def mask_pts(im):
    ys, xs = np.where(im[..., 3] > 128); return xs.mean(), ys.mean(), ys.max()


def estimate_angles(fr, a, b):
    """Kumulatiivinen kierto (asteina, kuvan koordinaatit: negatiivinen = vastapäivään) ruudusta a ruutuun b."""
    orb = cv2.ORB_create(2000); bf = cv2.BFMatcher(cv2.NORM_HAMMING, crossCheck=True)
    feats = {}
    for i in range(a - 1, b + 1):
        m = (fr[i][..., 3] > 200).astype(np.uint8) * 255
        feats[i] = orb.detectAndCompute(cv2.cvtColor(fr[i][..., [2, 1, 0]], cv2.COLOR_RGB2GRAY), m)
    ang = {a - 1: 0.0}; c = 0.0
    for i in range(a, b + 1):
        k0, d0 = feats[i - 1]; k1, d1 = feats[i]; da = 0.0
        ms = bf.match(d0, d1) if d0 is not None and d1 is not None else []
        if len(ms) > 8:
            A = np.float32([k0[x.queryIdx].pt for x in ms]); B = np.float32([k1[x.trainIdx].pt for x in ms])
            M, _ = cv2.estimateAffinePartial2D(A, B, method=cv2.RANSAC, ransacReprojThreshold=3)
            if M is not None: da = math.degrees(math.atan2(M[1, 0], M[0, 0]))
        c += da; ang[i] = c
    return ang


def derotate(im, angle, size=1400):
    """Vartalo pystyyn: kierto painopisteen ympäri, painopiste kuvan keskelle (size x size)."""
    cx, cy, _ = mask_pts(im)
    M = cv2.getRotationMatrix2D((cx, cy), angle, 1.0)
    M[0, 2] += size / 2 - cx; M[1, 2] += size / 2 - cy
    return cv2.warpAffine(im, M, (size, size), flags=cv2.INTER_LINEAR, borderValue=(0, 0, 0, 0))


def premult_blend(a, b, t):
    fa = a.astype(np.float32); fb = b.astype(np.float32)
    fa[..., :3] *= fa[..., 3:4] / 255.0; fb[..., :3] *= fb[..., 3:4] / 255.0
    o = fa * (1 - t) + fb * t
    al = o[..., 3:4] / 255.0
    o[..., :3] = np.where(al > 1e-3, o[..., :3] / np.maximum(al, 1e-3), 0)
    return o.clip(0, 255).astype(np.uint8)


def trim(rgba):
    ys, xs = np.where(rgba[..., 3] > 8)
    return rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1], xs.min(), ys.min()


def ground_item(im, floor):
    """Maalla olevat ruudut: ankkuri x = laudan/jalkojen keskikohta, jalkalinja = laudan pohja (floor = videon rivi)."""
    a = np.where(im[..., 3] > 128); y0, y1, x0, x1 = a[0].min(), a[0].max() + 1, a[1].min(), a[1].max() + 1
    crop = im[y0:y1, x0:x1]
    r = P.resize_premult(crop, K); ax = P.anchor_x(crop, "low") * K
    return (r, ax, (floor - y0) * K)


def save(items, name, out):
    below = max(r.shape[0] - foot for r, _, foot in items)
    lift = max(0, int(np.ceil(below - P.FOOT_MARGIN + 2)))
    P.save_sheet(items, lift, os.path.join(out, name)); print(name, len(items), "kuvaa nosto", lift)
    return lift


def main(video, out, prev=None, ff="ffmpeg"):
    os.makedirs(out, exist_ok=True)
    fr = load(video)
    floor = np.median([mask_pts(fr[i])[2] for i in (60, 62, 64, 66, 68)])          # laudan pohja idlessä
    idle_idx = list(range(0, 76, 3)); idle_idx = idle_idx + idle_idx[-2:0:-1]        # edestakaisin
    sheets = {}
    sheets["idle"] = [ground_item(fr[i], floor) for i in idle_idx]
    sheets["ponnistus"] = [ground_item(fr[i], floor) for i in list(range(76, 110, 2)) + [110]]
    land = list(range(168, 197, 2))
    sheets["alastulo"] = [ground_item(fr[i], floor) for i in land]

    # ---- voltti: pystyyn käännetyt vartalot, tasainen aikajako ja kierto ----
    ang = estimate_angles(fr, FLIP_A, FLIP_B)
    up = {i: derotate(fr[i], ang[i]) for i in range(FLIP_A, FLIP_B + 1)}
    a0, a1 = -ang[FLIP_A], -ang[FLIP_B]                                              # kierto (vastapäivään, asteina) alussa ja lopussa
    # painopisteen korkeus jalkalinjan yläpuolella nousun alussa (ruutu 110): vakio koko voltin ajan
    cx0, cy0, _ = mask_pts(fr[FLIP_A]); cog_h = (floor - cy0) * K
    flip_items = []
    for n in range(N_FLIP):
        t = n / (N_FLIP - 1)
        s = FLIP_A + t * (FLIP_B - FLIP_A); i0 = int(math.floor(s)); i1 = min(FLIP_B, i0 + 1); f = s - i0
        pose = premult_blend(up[i0], up[i1], f) if i1 != i0 else up[i0]
        ease = 0.75 * t + 0.25 * (t * t * (3 - 2 * t))
        phi = a0 + (a1 - a0) * ease                                                  # cv2: positiivinen = vastapäivään
        M = cv2.getRotationMatrix2D((pose.shape[1] / 2, pose.shape[0] / 2), phi, 1.0)
        rot = cv2.warpAffine(pose, M, (pose.shape[1], pose.shape[0]), flags=cv2.INTER_LINEAR, borderValue=(0, 0, 0, 0))
        crop, ox, oy = trim(rot)
        r = P.resize_premult(crop, K)
        cxp = (pose.shape[1] / 2 - ox) * K; cyp = (pose.shape[0] / 2 - oy) * K        # painopiste pikkukuvassa
        flip_items.append((r, cxp, cyp + cog_h))                                     # "jalkalinja" = painopiste + vakiokorkeus
    sheets["voltti"] = flip_items
    lifts = {}
    for name in ("idle", "ponnistus", "voltti", "alastulo"):
        lifts[name] = save(sheets[name], "rocco_lauta_%s.png" % name, out)
    if prev:
        render_preview(out, prev, ff, lifts)


def cells(path):
    sh = cv2.imread(path, cv2.IMREAD_UNCHANGED); rows = sh.shape[0] // P.CELL_H; cs = []
    for r in range(rows):
        for c in range(P.COLS):
            cell = sh[r * P.CELL_H:(r + 1) * P.CELL_H, c * P.CELL_W:(c + 1) * P.CELL_W]
            if cell[..., 3].max() > 0: cs.append(cell)
    return cs


def render_preview(out, mp4, ff, lifts):
    import subprocess
    seq = []
    idle = cells(os.path.join(out, "rocco_lauta_idle.png"))
    seq += idle + idle
    seq += cells(os.path.join(out, "rocco_lauta_ponnistus.png"))
    seq += cells(os.path.join(out, "rocco_lauta_voltti.png"))
    seq += cells(os.path.join(out, "rocco_lauta_alastulo.png"))
    seq += idle
    # esikatselu: kuvat samalle jalkalinjalle (nostot huomioiden) vihreällä taustalla, 24 kuvaa/s
    names = ["idle"] * len(idle) * 2 + ["ponnistus"] * len(cells(os.path.join(out, "rocco_lauta_ponnistus.png"))) + ["voltti"] * N_FLIP + ["alastulo"] * len(cells(os.path.join(out, "rocco_lauta_alastulo.png"))) + ["idle"] * len(idle)
    tmp = os.path.splitext(mp4)[0] + "_raw.avi"
    vw = cv2.VideoWriter(tmp, cv2.VideoWriter_fourcc(*"MJPG"), 24, (960, 540))
    for cell, nm in zip(seq, names):
        bg = np.zeros((540, 960, 3), np.uint8); bg[:] = (40, 200, 40)
        a = cell[..., 3:4] / 255.0; rgb = cell[..., :3][..., ::-1]
        dy = lifts[nm] - lifts["idle"]                                             # sama jalkalinja kaikissa
        canvas = np.zeros((448, 768, 3), np.float32); canvas[:] = (40, 200, 40)
        canvas = canvas * (1 - a) + rgb * a
        M = np.float32([[1, 0, 0], [0, 1, dy]])
        canvas = cv2.warpAffine(canvas.astype(np.uint8), M, (768, 448), borderValue=(40, 200, 40))
        bg[46:46 + 448, 96:96 + 768] = canvas
        vw.write(bg)
    vw.release()
    subprocess.run([ff, "-y", "-i", tmp, "-c:v", "mpeg4", "-q:v", "3", "-pix_fmt", "yuv420p", mp4], check=True, capture_output=True)
    os.remove(tmp); print("esikatselu", mp4)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None, sys.argv[4] if len(sys.argv) > 4 else "ffmpeg")
