"""Tähtipyssy: leikkaa lähdekuvat (violetti tausta) yksittäisiksi läpinäkyviksi PNG-kuviksi -> Assets/Resources/TahtiPyssy.
Aja: python Tools/SpaceGame/slice_sprites.py   (tarvitsee opencv-python ja numpy)
"""
import os, sys
import cv2
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "src")
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "TahtiPyssy")
os.makedirs(OUT, exist_ok=True)


def bg_color(img):
    q = (img // 16).astype(np.int32)
    code = q[..., 0] * 256 + q[..., 1] * 16 + q[..., 2]
    vals, counts = np.unique(code, return_counts=True)
    m = vals[np.argmax(counts)]
    sel = code == m
    return img[sel].mean(axis=0)


def key(img):
    """BGR -> BGRA, violetti taustaväri pois (pehmeä reuna, väri puhdistettu)."""
    bg = bg_color(img)
    f = img.astype(np.float32)
    dist = np.sqrt(((f - bg) ** 2).sum(axis=2))
    a = np.clip((dist - 38.0) / 42.0, 0.0, 1.0)
    # väri: poista taustan osuus (c = (p - (1-a)*bg) / a)
    a3 = np.maximum(a, 1e-3)[..., None]
    col = (f - (1.0 - a[..., None]) * bg) / a3
    col = np.clip(col, 0, 255)
    out = np.dstack([col, a * 255.0]).astype(np.uint8)
    out[a < 0.01] = 0
    return out


def components(rgba, dil, min_area=400):
    mask = (rgba[..., 3] > 20).astype(np.uint8)
    if dil > 0:
        k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (dil * 2 + 1, dil * 2 + 1))
        m2 = cv2.dilate(mask, k)
    else:
        m2 = mask
    n, lab, stats, cent = cv2.connectedComponentsWithStats(m2, connectivity=8)
    comps = []
    for i in range(1, n):
        x, y, w, h, area = stats[i]
        if area < min_area:
            continue
        comps.append((x, y, w, h, i))
    return lab, comps


def crop(rgba, lab, c, pad=2):
    x, y, w, h, i = c
    sub = rgba[y:y + h, x:x + w].copy()
    m = (lab[y:y + h, x:x + w] == i)
    # poista muiden komponenttien pikselit
    sub[~m] = 0
    return trim(sub, pad)


def trim(sub, pad=2):
    ys, xs = np.where(sub[..., 3] > 8)
    if len(xs) == 0:
        return sub
    x0, x1, y0, y1 = max(xs.min() - pad, 0), min(xs.max() + pad + 1, sub.shape[1]), max(ys.min() - pad, 0), min(ys.max() + pad + 1, sub.shape[0])
    return sub[y0:y1, x0:x1]


def rows_of(comps, bounds):
    """Ryhmittele komponentit riveihin (y-keskikohdan mukaan rajojen välissä), rivin sisällä x-järjestykseen."""
    rows = [[] for _ in range(len(bounds) + 1)]
    for c in comps:
        cy = c[1] + c[3] / 2
        r = sum(1 for b in bounds if cy >= b)
        rows[r].append(c)
    for r in rows:
        r.sort(key=lambda c: c[0])
    return rows



def split_cols(rgba, y0, y1, xb):
    """Rivi pystysuorilla rajoilla kuviin (kun dilaatio yhdistäisi viereiset)."""
    sub = rgba[y0:y1]
    edges = [0] + list(xb) + [sub.shape[1]]
    return [trim(sub[:, a:b].copy(), 2) for a, b in zip(edges[:-1], edges[1:])]


def resize(img, scale):
    if scale == 1.0:
        return img
    h, w = img.shape[:2]
    return cv2.resize(img, (max(1, int(round(w * scale))), max(1, int(round(h * scale)))), interpolation=cv2.INTER_AREA)


def save(name, img, scale=1.0):
    img = resize(img, scale)
    p = os.path.join(OUT, name + ".png")
    cv2.imwrite(p, img)
    return p


def union_canvas(frames, align="center"):
    """Sama kangas kaikille kuville (kohdistus keskelle tai oikea reuna keskellä)."""
    W = max(f.shape[1] for f in frames)
    H = max(f.shape[0] for f in frames)
    outs = []
    for f in frames:
        c = np.zeros((H, W, 4), np.uint8)
        h, w = f.shape[:2]
        y0 = (H - h) // 2
        x0 = (W - w) if align == "right" else (W - w) // 2
        c[y0:y0 + h, x0:x0 + w] = f
        outs.append(c)
    return outs


def load(name):
    img = cv2.imread(os.path.join(SRC, name), cv2.IMREAD_COLOR)
    assert img is not None, name
    return img


def report(name, got, want):
    flag = "" if got == want else "   <<< ODOTETTU " + str(want)
    print("%-22s %d%s" % (name, got, flag))


def main():
    # ---- hero ----
    rgba = key(load("hero_ship.png"))
    lab, comps = components(rgba, 10)
    comps.sort(key=lambda c: c[1])
    report("hero", len(comps), 3)
    fr = union_canvas([crop(rgba, lab, c) for c in comps])
    for i, f in enumerate(fr):
        save("hero_%d" % i, f, 0.6)

    rgba = key(load("hero_flame.png"))
    lab, comps = components(rgba, 8)
    comps.sort(key=lambda c: c[0])
    report("flame", len(comps), 4)
    fr = union_canvas([crop(rgba, lab, c) for c in comps], "right")
    for i, f in enumerate(fr):
        save("flame_%d" % i, f, 0.5)

    # ---- fx_sheet: laukaus 3, erikoispanos 3, räjähdys 8, kilpi 3 (rivit erikseen, ettei dilaatio yhdistä rivejä) ----
    rgba = key(load("fx_sheet.png"))
    bands = [(0, 130, "shot", 3), (130, 326, "bolt", 3), (326, 593, "expl", 4), (593, 806, "expl", 4), (806, 1024, "shield", 3)]
    idx = {}
    for (y0, y1, nm, n) in bands:
        band = rgba[y0:y1].copy()
        if nm == "bolt":
            fr = split_cols(rgba, y0, y1, (515, 995))
        else:
            lab, comps = components(band, 14)
            comps.sort(key=lambda c: c[0])
            report("  fx " + nm, len(comps), n)
            fr = [crop(band, lab, c) for c in comps]
            if nm == "shield":
                fr = union_canvas(fr)
        for f in fr:
            k = idx.get(nm, 0)
            save("%s_%d" % (nm, k), f, 0.55)
            idx[nm] = k + 1

    # ---- viholliset ----
    rgba = key(load("enemies.png"))
    lab, comps = components(rgba, 10)
    comps.sort(key=lambda c: c[1])
    report("enemies", len(comps), 3)
    for i, c in enumerate(comps):
        save("enemy_%d" % i, crop(rgba, lab, c), 0.55)

    # ---- pomo (kolme tilaa, sama kangas) ----
    rgba = key(load("boss.png"))
    lab, comps = components(rgba, 12)
    comps.sort(key=lambda c: c[1])
    report("boss", len(comps), 3)
    # toisessa/kolmannessa kuvassa osia voi irrota: yhdistä rivin mukaan
    bands = [(0, 340), (340, 670), (670, 1024)]
    fr = []
    for (y0, y1) in bands:
        sub = rgba[y0:y1].copy()
        fr.append(trim(sub, 2))
    fr = union_canvas(fr, "right")
    for i, f in enumerate(fr):
        save("boss_%d" % i, f, 0.55)

    # ---- kerättävät ----
    rgba = key(load("pickups.png"))
    lab, comps = components(rgba, 8)
    comps.sort(key=lambda c: c[0])
    report("pickups", len(comps), 3)
    for i, c in enumerate(comps):
        save("pickup_%d" % i, crop(rgba, lab, c), 0.28)

    # ---- sade ----
    rgba = key(load("hero_beam.png"))
    lab, comps = components(rgba, 0)
    comps.sort(key=lambda c: c[1])
    report("beam", len(comps), 3)
    fr = union_canvas([crop(rgba, lab, c, 0) for c in comps])
    for i, f in enumerate(fr):
        save("beam_%d" % i, f, 0.6)

    # ---- pomon pulssikiekot (sininen + punaviolettiversio: R<->B) ----
    rgba = key(load("blue_discs.png"))
    lab, comps = components(rgba, 8)
    rows = rows_of(comps, [512])
    flat = rows[0] + rows[1]
    report("discs", len(flat), 6)
    crops = union_canvas([crop(rgba, lab, c) for c in flat])
    for i, f in enumerate(crops):
        hsv = cv2.cvtColor(f[..., :3], cv2.COLOR_BGR2HSV)
        hsv[..., 0] = (hsv[..., 0].astype(np.int32) + 60) % 180   # syaani -> magenta/violetti (erottuu heron sinisestä ja tulesta)
        mg = np.dstack([cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR), f[..., 3]])
        save("disc_%d" % i, mg, 0.4)

    # ---- vihollisaseet ----
    rgba = key(load("enemy_shots.png"))
    lab, comps = components(rgba, 14)
    rows = rows_of(comps, [200, 420, 750])
    report("enemy shots", len(comps), 11)
    for r, nm in zip(rows, ["eshot_s", "eshot_b", "orb", "ebeam"]):
        report("  " + nm, len(r), 2 if nm == "ebeam" else 3)
        fr = [crop(rgba, lab, c) for c in r]
        if nm == "ebeam":
            fr = split_cols(rgba, 750, 1024, (520, 1000))
        if nm in ("orb",):
            fr = union_canvas(fr)
        if nm == "ebeam":
            fr = union_canvas(fr, "right")
        for i, f in enumerate(fr):
            save("%s_%d" % (nm, i), f, 0.5)

    # ---- varoituskyltti ----
    rgba = key(load("warning_sign.png"))
    save("warn", trim(rgba, 2), 0.5)

    # ---- planeetta ja asteroidit ----
    rgba = key(load("planet_asteroids.png"))
    lab, comps = components(rgba, 12)
    comps.sort(key=lambda c: -c[2] * c[3])
    report("planet+asteroids", len(comps), 7)
    save("planet", crop(rgba, lab, comps[0]), 0.7)
    for i, c in enumerate(comps[1:]):
        save("ast_%d" % i, crop(rgba, lab, c), 0.55)

    # ---- romut (etualan siluetit) ----
    rgba = key(load("debris.png"))
    lab, comps = components(rgba, 60)
    comps.sort(key=lambda c: -c[2] * c[3])
    report("debris", len(comps), 4)
    for i, c in enumerate(comps[:4]):
        save("debris_%d" % i, crop(rgba, lab, c), 0.7)

    # ---- tausta ja otsikko (ei avainnusta) ----
    cv2.imwrite(os.path.join(OUT, "stars.png"), load("stars.png"))
    cv2.imwrite(os.path.join(OUT, "title.png"), load("title.png"))
    print("valmis ->", os.path.abspath(OUT))


if __name__ == "__main__":
    main()
