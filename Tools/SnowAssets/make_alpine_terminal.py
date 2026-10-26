"""Alppiterminaalin neljä kuvaa -> Assets/Sprites/Taustat/alppi_terminaali_1..4.png (vasemmalta oikealle annetussa järjestyksessä).
Kuvissa 1 ja 4 ikkunat ja ovilasit ovat läpinäkyviä (alfa 0): niiden reunoilta poistetaan musta/punainen hohto (väri otetaan viereisestä peittävästä kuvasta).
Ajo: python Tools/SnowAssets/make_alpine_terminal.py <kuva1> <kuva2> <kuva3> <kuva4>"""
import os, sys
import numpy as np, cv2

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")


def bleed(bgra, sigma=3.0):
    f = bgra.astype(np.float32); a = f[..., 3] / 255.0
    solid = (a > 0.97).astype(np.float32)
    num = cv2.GaussianBlur(f[..., :3] * solid[..., None], (0, 0), sigma)
    den = cv2.GaussianBlur(solid, (0, 0), sigma)[..., None]
    fill = num / np.maximum(den, 1e-3)
    w = np.clip((den - 0.02) / 0.1, 0, 1) * (a < 0.97)[..., None]
    f[..., :3] = f[..., :3] * (1 - w) + fill * w
    return f.clip(0, 255).astype(np.uint8)


def clean_holes(bgra):
    """Ikkuna-aukon reunan puoliläpinäkyvä punainen/musta kehys pois: alfa < 120 -> 0 ja aukkoa kutistetaan 2 px (kehyksen reuna ei vilku)."""
    a = bgra[..., 3].copy()
    hole = (a < 120).astype(np.uint8)
    hole = cv2.erode(hole, np.ones((5, 5), np.uint8))          # aukko pienenee 2 px: reunan sotku jää kehyksen alle
    # pienet hajapisteet (alfa < 120 muualla) pois
    n, lab, st, _ = cv2.connectedComponentsWithStats(hole, connectivity=8)
    keep = np.zeros_like(hole)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] >= 400: keep[lab == i] = 1
    a2 = np.where(keep > 0, 0, 255).astype(np.uint8)
    out = bgra.copy(); out[..., 3] = a2
    return out


def register(A, B, w, rows=(40, 520), no_rot=False):
    """B:n kuvan koordinaatit -> A:n koordinaatit (skaala + siirto), SIFT + RANSAC; ruudut limittyvät, joten sama esine löytyy molemmista."""
    sift = cv2.SIFT_create(6000)
    ga = cv2.cvtColor(A[..., :3], cv2.COLOR_BGR2GRAY); gb = cv2.cvtColor(B[..., :3], cv2.COLOR_BGR2GRAY)
    ma = np.zeros(ga.shape, np.uint8); ma[rows[0]:rows[1], A.shape[1] - w:] = 255; ma[A[..., 3] < 200] = 0
    mb = np.zeros(gb.shape, np.uint8); mb[rows[0]:rows[1], :w] = 255; mb[B[..., 3] < 200] = 0
    ka, da = sift.detectAndCompute(ga, ma); kb, db = sift.detectAndCompute(gb, mb)
    raw = cv2.BFMatcher().knnMatch(db, da, k=2)
    good = [m for m, n in raw if m.distance < 0.75 * n.distance]
    pb = np.float32([kb[m.queryIdx].pt for m in good]); pa = np.float32([ka[m.trainIdx].pt for m in good])
    M, inl = cv2.estimateAffinePartial2D(pb, pa, method=cv2.RANSAC, ransacReprojThreshold=2.5)
    if no_rot:
        # ei kiertoa: kuvat ovat samassa kamerassa -> sovitetaan vain skaala + siirto (pienimmän neliösumman) RANSAC-inliereillä
        m = inl.ravel() > 0
        X = pb[m]; Y = pa[m]
        xm, ym = X.mean(0), Y.mean(0)
        sc = float(((X - xm) * (Y - ym)).sum() / max(1e-6, ((X - xm) ** 2).sum()))
        M = np.float32([[sc, 0, ym[0] - sc * xm[0]], [0, sc, ym[1] - sc * xm[1]]])
    return M, int(inl.sum())


def stitch(imgs, ws, forced=None, no_rot=False):
    """imgs: RGBA vasemmalta oikealle. Jokainen rekisteröidään edelliseen ja kuvat yhdistetään kapealla ristiinhäivytyksellä limityksen keskellä."""
    H = imgs[0].shape[0]
    T = [np.vstack([np.eye(3)[:2], [0, 0, 1]])]            # kuva 0: identiteetti
    info = []
    for i in range(1, len(imgs)):
        if forced and forced.get(i) is not None:
            M = np.float32([[1, 0, forced[i]], [0, 1, 0]]); n = -1
        else:
            M, n = register(imgs[i - 1], imgs[i], ws[i - 1], no_rot=no_rot)
        Mi = np.vstack([M, [0, 0, 1]])
        T.append(T[-1] @ Mi); info.append((i, n, float(np.hypot(M[0, 0], M[1, 0])), float(M[0, 2])))
    # kanvaasin leveys: viimeisen kuvan oikea reuna
    right = max(float((T[i] @ np.array([imgs[i].shape[1], 0, 1]))[0]) for i in range(len(imgs)))
    Wc = int(np.ceil(right)); canvas = None; extent_r = 0.0
    def warp(i):
        im = imgs[i].astype(np.float32); im[..., :3] *= im[..., 3:4] / 255.0           # esikerrottu
        w = cv2.warpAffine(im, T[i][:2], (Wc, H), flags=cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0))
        cover = cv2.warpAffine(np.full(imgs[i].shape[:2], 255, np.uint8), T[i][:2], (Wc, H), flags=cv2.INTER_LINEAR)
        return w, cover
    canvas, cover0 = warp(0); extent_r = float(imgs[0].shape[1])
    for i in range(1, len(imgs)):
        w, cov = warp(i)
        xs = np.where(cov.max(0) > 128)[0]; xb_l = float(xs.min()); xa_r = extent_r
        xc = (xb_l + xa_r) / 2; half = min(50.0, max(10.0, (xa_r - xb_l) / 2 - 2))
        x = np.arange(Wc, dtype=np.float32)
        t = np.clip((x - (xc - half)) / (2 * half), 0, 1)[None, :, None]
        t = np.where((x[None, :, None] >= xb_l), t, 0)
        canvas = canvas * (1 - t) + w * t
        extent_r = float(xs.max()) + 1
    a = canvas[..., 3:4] / 255.0
    rgb = np.where(a > 1e-3, canvas[..., :3] / np.maximum(a, 1e-3), 0)
    out = np.dstack([rgb, canvas[..., 3]]).clip(0, 255).astype(np.uint8)
    return out[:, :int(extent_r)], T, info


def main(paths):
    for i, p in enumerate(paths, 1):
        im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
        if im.shape[2] == 3:
            im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
        else:
            im = clean_holes(bleed(im))
        if im.shape[:2] != (941, 1672): im = cv2.resize(im, (1672, 941), interpolation=cv2.INTER_CUBIC)
        for _ in range(6):
            try:
                cv2.imwrite(os.path.join(OUT, "alppi_terminaali_%d.png" % i), im); break
            except Exception:
                import time; time.sleep(1)
        print(i, im.shape, "aukkoja", round(float((im[..., 3] < 10).mean()), 3))
    # yhtenäinen nauha: kuvat tulevat vasemmalta oikealle järjestyksessä 4, 3, 2, 1 (annetut tiedostot 1..4)
    ims = [cv2.imread(os.path.join(OUT, "alppi_terminaali_%d.png" % k), cv2.IMREAD_UNCHANGED) for k in (4, 3, 2, 1)]
    strip, T, info = stitch(ims, [700, 700, 300], forced=None)
    for i, n, sc, dx in info: print("liitos", i, "inlierit", n, "skaala", round(sc, 4), "siirto", round(dx, 1))
    # ovien paikat nauhalla ennen peilausta: kuva 4 (uloskäynti) x 175, kuva 1 (hangaarin ovi) x 375 omassa kuvassaan
    door_exit = float((T[0] @ np.array([175, 0, 1]))[0]); door_hangar = float((T[3] @ np.array([375, 0, 1]))[0])
    # peilaus: koko peli etenee oikealle (kiitotie -> terminaali -> kylän katu), joten terminaalissa kuljetaan hangaarin ovelta (vasen pää) uloskäynnille (oikea pää)
    flipped = strip[:, ::-1].copy(); Wn = strip.shape[1]
    cv2.imwrite(os.path.join(OUT, "alppi_terminaali_koko.png"), flipped)
    print("nauha", flipped.shape, "hangaari x", round(Wn - door_hangar), "uloskaynti x", round(Wn - door_exit))


if __name__ == "__main__":
    main(sys.argv[1:5])
