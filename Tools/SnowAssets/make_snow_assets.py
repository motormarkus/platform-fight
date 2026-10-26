"""Lumirinteen kuvat generaattorin kuvista -> Assets/Sprites/Taustat/lumi_*.png.
Ajo: python Tools/SnowAssets/make_snow_assets.py <taivas> <metsa> <puut> <rinne> <pinta> <poly1> <poly2> <etu>
(kahdeksan kuvaa tässä järjestyksessä). Vihreä tausta (#00FF00) -> alfa, mustalle taustalle tehdyt kuvat: kirkkaus -> alfa.
Saumattomuus: reuna-alueet ristiinhäivytetään (kuva lyhenee L px), jolloin vasen reuna jatkuu suoraan oikeaan."""
import os, sys
import numpy as np, cv2

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
L = 120


def wrap_blend(img, horiz=True, vert=False, l=L):
    """Saumaton toisto ristiinhäivyttämällä: uusi reuna = häivytys lopun ja alun välillä, kuva lyhenee l px."""
    img = img.astype(np.float32)
    if horiz:
        W = img.shape[1]
        t = np.linspace(0, 1, l, dtype=np.float32)[None, :, None] if img.ndim == 3 else np.linspace(0, 1, l, dtype=np.float32)[None, :]
        head = img[:, :l] * t + img[:, W - l:] * (1 - t)
        img = np.concatenate([head, img[:, l:W - l]], axis=1)
    if vert:
        H = img.shape[0]
        t = np.linspace(0, 1, l, dtype=np.float32)[:, None, None] if img.ndim == 3 else np.linspace(0, 1, l, dtype=np.float32)[:, None]
        head = img[:l] * t + img[H - l:] * (1 - t)
        img = np.concatenate([head, img[l:H - l]], axis=0)
    return img


def key_green(bgr, t0, t1):
    """Vihreä tausta pois: vihreyden ylitys (g - max(r,b)) -> alfa 0...1; vihreä sävy poistetaan reunoilta (despill)."""
    f = bgr.astype(np.float32)
    b, g, r = f[..., 0], f[..., 1], f[..., 2]
    over = g - np.maximum(r, b)
    a = 1.0 - np.clip((over - t0) / (t1 - t0), 0, 1)
    # reunan puoliläpinäkyvistä pikseleistä vihreä taustasekoitus pois: c = a*fg + (1-a)*vihreä
    k = np.maximum(a, 0.45)[..., None]
    un = (f - (1.0 - a)[..., None] * np.array([0, 255, 0], np.float32)) / k
    un = np.where((a < 0.999)[..., None], un, f).clip(0, 255)
    b, g, r = un[..., 0], un[..., 1], un[..., 2]
    g2 = np.minimum(g, np.maximum(r, b) + 4)
    col = np.dstack([b, g2, r])
    # reunan sävy otetaan sisäpuolen väristä (poistaa vihreän/violetin reunan)
    m = (a > 0.97).astype(np.float32)
    if 0 < m.mean() < 1:
        k = 9
        num = cv2.GaussianBlur(col * m[..., None], (0, 0), k)
        den = cv2.GaussianBlur(m, (0, 0), k)[..., None]
        fill = num / np.maximum(den, 1e-3)
        w = np.clip((den - 0.02) / 0.1, 0, 1) * (a < 0.97)[..., None]
        col = col * (1 - w) + fill * w
    return col, a


def save_rgba(path, col, a):
    a8 = (a * 255).clip(0, 255).astype(np.uint8)
    rgba = np.dstack([col.clip(0, 255).astype(np.uint8), a8])
    rgba[a8 == 0, :3] = 0
    for _ in range(6):
        try:
            cv2.imwrite(path, rgba); return
        except Exception:
            import time; time.sleep(1)
    cv2.imwrite(path, rgba)


def lum_layer(bgr, tint_bgr, gamma, gain):
    """Mustalle tehty kuva -> läpinäkyvä kerros: kirkkaus on alfa, väri vakio (jotta valkoisella lumella näkyy)."""
    lum = bgr.astype(np.float32).max(2) / 255.0
    a = np.clip((lum ** gamma) * gain, 0, 1)
    col = np.zeros_like(bgr, dtype=np.float32)
    col[:] = tint_bgr
    return col, a


def cut_pieces(col, a, prefix, min_area, pad=3, merge=9):
    """Erottaa yksittäiset kappaleet (yhtenäiset alueet) omiksi kuviksi."""
    mask = (a > 0.08).astype(np.uint8)
    grown = cv2.dilate(mask, np.ones((merge, merge), np.uint8))
    n, lab, stats, _ = cv2.connectedComponentsWithStats(grown, connectivity=8)
    pieces = []
    for i in range(1, n):
        x, y, w, h, area = stats[i]
        if area < min_area: continue
        x0, y0, x1, y1 = max(0, x - pad), max(0, y - pad), min(a.shape[1], x + w + pad), min(a.shape[0], y + h + pad)
        pieces.append((x0, y0, x1, y1, (lab == i)))
    pieces.sort(key=lambda p: (p[0] + (p[3] - p[1])))
    return pieces


def main(paths):
    sky_p, forest_p, trees_p, slope_p, surf_p, dust1_p, dust2_p, front_p = paths
    rd = lambda p: cv2.imread(p, cv2.IMREAD_COLOR)

    # 1. taivas + kaukaiset vuoret: vain vaakasuunnassa saumaton
    sky = wrap_blend(rd(sky_p), True, False)
    cv2.imwrite(os.path.join(OUT, "lumi_taivas.png"), sky.clip(0, 255).astype(np.uint8))

    # 2. metsän siluetti: saumaton vaakasuunnassa, vihreä pois
    fo = wrap_blend(rd(forest_p), True, False)
    col, a = key_green(fo, 70, 200)
    save_rgba(os.path.join(OUT, "lumi_metsa.png"), col, a)

    # 3. puut ja kivet erillisiksi kuviksi
    tr = rd(trees_p)
    col, a = key_green(tr, 70, 200)
    pieces = cut_pieces(col, a, "puu", 400)
    nt = nr = 0
    for (x0, y0, x1, y1, pm) in pieces:
        w, h = x1 - x0, y1 - y0
        isrock = (h < 0.72 * w) or (h < 150 and w > 1.1 * h)
        nm = ("lumi_kivi_%d.png" % (nr + 1)) if isrock else ("lumi_puu_%d.png" % (nt + 1))
        nr += isrock; nt += (not isrock)
        save_rgba(os.path.join(OUT, nm), col[y0:y1, x0:x1], a[y0:y1, x0:x1] * pm[y0:y1, x0:x1])
    print("puita", nt, "kiviä", nr)
    # etuparallaksin puunlatvat: puiden yläosa, epätarkka, alareuna häivytetty läpinäkyväksi (leikkauskohta ei näy)
    for old in [f for f in os.listdir(OUT) if f.startswith("lumi_latva_") and f.endswith(".png")]: os.remove(os.path.join(OUT, old))
    nl = 0
    for k in range(1, nt + 1):
        im = cv2.imread(os.path.join(OUT, "lumi_puu_%d.png" % k), cv2.IMREAD_UNCHANGED)
        if im.shape[0] < 200: continue                         # pienet puut jäävät pois
        top = im[: int(im.shape[0] * 0.62)].astype(np.float32)
        a = top[..., 3] / 255.0
        pm = top[..., :3] * a[..., None]                       # esikerrottu epätarkennus (ei tummia reunoja)
        pm = cv2.GaussianBlur(pm, (0, 0), 2.2); a = cv2.GaussianBlur(a, (0, 0), 2.2)
        h = a.shape[0]; fade = np.clip((h - np.arange(h)) / (h * 0.45), 0, 1) ** 1.3   # alin 45 % häipyy
        a = a * fade[:, None]
        col = pm / np.maximum(a / np.maximum(fade[:, None], 1e-3), 1e-3)[..., None] if False else top[..., :3]
        nl += 1
        save_rgba(os.path.join(OUT, "lumi_latva_%d.png" % nl), cv2.GaussianBlur(col, (0, 0), 1.6), a)
    print("latvoja", nl)

    # 4. rinne: yksi kuva, vihreä pois
    sl = rd(slope_p)
    col, a = key_green(sl, 90, 200)
    save_rgba(os.path.join(OUT, "lumi_rinne.png"), col, a)

    # 5. lumipinnan kuvio: saumaton molempiin suuntiin; tummansininen sävy, jotta näkyy valkoisella lumella
    sf = wrap_blend(rd(surf_p), True, True)
    col, a = lum_layer(sf, (205, 150, 100), 0.75, 1.7)   # BGR
    save_rgba(os.path.join(OUT, "lumi_pinta.png"), col, a)

    # 6-7. lumipöly: saumaton molempiin suuntiin, valkoinen (hieman sininen)
    for p, nm, g, gain in ((dust1_p, "lumi_poly_1.png", 0.8, 1.6), (dust2_p, "lumi_poly_2.png", 0.9, 1.0)):
        d = wrap_blend(rd(p), True, True)
        col, a = lum_layer(d, (255, 247, 235), g, gain)
        save_rgba(os.path.join(OUT, nm), col, a)

    # 8. etuparallaksi: erilliset kappaleet (oksat, hiutaleet, kivi)
    fr = rd(front_p)
    col, a = key_green(fr, 10, 120)
    fpieces = cut_pieces(col, a, "etu", 1500, merge=15)
    Hh, Ww = a.shape
    nf = 0
    for i, (x0, y0, x1, y1, pm) in enumerate(fpieces, 1):
        ap = a[y0:y1, x0:x1] * pm[y0:y1, x0:x1]
        # kuvan reunaan osuvat kappaleet häivytetään leikkauskohdasta (ei kovaa suoraa reunaa)
        F = 40
        ramp = np.linspace(0, 1, F, dtype=np.float32)
        if x0 == 0: ap[:, :F] *= ramp[None, :]
        if y0 == 0: ap[:F, :] *= ramp[:, None]
        if x1 == Ww: ap[:, -F:] *= ramp[::-1][None, :]
        if y1 == Hh: ap[-F:, :] *= ramp[::-1][:, None]
        cc = col[y0:y1, x0:x1]
        if 150 < (x1 - x0) < 260 and 150 < (y1 - y0) < 260 and abs((x1 - x0) - (y1 - y0)) < 80:   # hiutale: valkoinen (generaattorin mintunvihreä sävy pois)
            cc = cc * 0.15 + np.array([255, 247, 238], np.float32) * 0.85; ap = ap * 0.85
            nf += 1; nm = "lumi_hiutale_%d.png" % nf
        else:
            nm = "lumi_etu_%d.png" % i   # oksat ja kivi: leikattu kuvan reunasta, kelluvat irrallaan -> ei käytössä
        save_rgba(os.path.join(OUT, nm), cc, ap)
    print("etukappaleita", len(fpieces))
    print("ok")


if __name__ == "__main__":
    main(sys.argv[1:9])
