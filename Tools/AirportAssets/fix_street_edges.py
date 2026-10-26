"""Thaitownin kadun yläkuvien (thaitown_katu*_yla.png) vasen ja oikea reuna: kuvien reunoissa on alfahäivytys (feather) ja tummuva kaista, joista taivas/parallaksi paistaa liitoksessa.
Rakennusrivit (sisempi sarake F on peittävä): häivytys-alue (F = 210 px) tehdään peittäväksi alkuperäisellä värillä (jos väri on tummunut mustaksi, peilataan sisältöä).
Taivasrivit: tumma häivytyskaista poistetaan. Ajo: python Tools/AirportAssets/fix_street_edges.py (palauta ensin kuvat gitistä, jos ajettu aiemmin)"""
import glob, os, numpy as np
from PIL import Image
d = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
F = 210
for f in sorted(glob.glob(os.path.join(d, "thaitown_katu*_yla.png"))):
    a = np.array(Image.open(f).convert("RGBA")); H, W = a.shape[:2]; ch = 0
    for side in (0, 1):
        v = (a if side == 0 else a[:, ::-1]).copy()
        lum = v[..., :3].astype(int).sum(2) / 3.0
        for y in range(H):
            row = v[y]
            if row[F, 3] >= 250 and (row[F:F + 40, 3] > 200).mean() > 0.9:   # rakennusrivi
                if (row[:F, 3] < 250).any():
                    inner = lum[y, F:F + 20].mean()
                    for x in range(F):
                        if row[x, 3] >= 250: continue
                        if lum[y, x] >= 0.35 * inner and row[x, 3] >= 0: v[y, x, 3] = 255
                        else: v[y, x, :3] = row[min(2 * F - 1 - x, W - 1), :3]; v[y, x, 3] = 255
                    ch += F
            else:   # taivasrivi: tummat puoliläpinäkyvät häivytyspikselit pois
                pass
        sky = [y for y in range(H) if not (v[y, F, 3] >= 250 and (v[y, F:F + 40, 3] > 200).mean() > 0.9)]
        if sky:   # kokonaan mustat sarakkeet taivasriveillä (häivytyskaistan reuna) pois
            for x in range(F):
                col = [y for y in sky if v[y, x, 3] > 0]
                if len(col) > 40 and np.median(lum[col, x]) < 60 and np.median(v[col, x, 3]) < 250 or (len(col) > 40 and np.median(lum[col, x]) < 15): v[col, x, 3] = 0; ch += len(col)
        a = v if side == 0 else v[:, ::-1]
    Image.fromarray(a).save(f); print(os.path.basename(f), "muutettu", ch)
