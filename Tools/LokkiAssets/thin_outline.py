"""Lokin suoja-, väistö- ja polvikuvissa (tekoälyn piirtämät valmiit kuvat) on paksu musta ääriviiva; ohennetaan idlen/videokuvien tasolle (~2 px).
Reunan ulkoa 2 px säilyy, sen sisäpuolella oleva tumma kaista (<= BAND px) korvataan viereisillä väreillä (inpaint)."""
import cv2, numpy as np, os, shutil, json
HERE = os.path.dirname(os.path.abspath(__file__)); SP = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Viholliset")
KEEP, BAND = 2.0, 9.0
d = json.load(open(os.path.join(HERE, "dims.json")))
for name in ("suoja", "vaisto", "polvi"):
    f = os.path.join(SP, f"lokki_{name}.png"); bk = os.path.join(HERE, "backup_outline", f"lokki_{name}.png")
    if not os.path.exists(bk): shutil.copy(f, bk)
    im = cv2.imread(bk, cv2.IMREAD_UNCHANGED); H, W = im.shape[:2]
    cw, anc, ch = d[name]; out = im.copy(); cols = W // cw; rows = H // ch
    for r in range(rows):
        for c in range(cols):
            cell = im[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw].copy(); a = cell[..., 3]
            if (a > 128).sum() < 500: continue
            solid = (a > 128).astype(np.uint8)
            dist = cv2.distanceTransform(solid, cv2.DIST_L2, 5)
            lum = cell[..., :3].astype(np.float32).max(2)
            dark = (lum < 75) & (dist > KEEP) & (dist <= BAND)
            # vain reunaan yhteydessä oleva tumma kaista (ei silmiä, nappeja, hanskoja sisällä)
            band = ((dist <= BAND) & (solid > 0)).astype(np.uint8)
            n, lab = cv2.connectedComponents(dark.astype(np.uint8), connectivity=8)
            edge = (dist <= KEEP + 1.5) & (lum < 75)
            touch = np.unique(lab[cv2.dilate(edge.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0]); touch = touch[touch > 0]
            m = np.isin(lab, touch) & dark
            # hanskat/kengät ovat oikeasti mustia: älä poista isoja tummia alueita (paksuus > BAND tai alue ulottuu syvälle)
            deep = (lum < 75) & (dist > BAND)
            deep_near = cv2.dilate(deep.astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
            m &= ~deep_near
            mk = cv2.dilate(m.astype(np.uint8) * 255, np.ones((3, 3), np.uint8))
            res = cv2.inpaint(cell[..., :3], mk, 4, cv2.INPAINT_TELEA)
            sel = (mk > 0) & (solid > 0) & (dist > KEEP)
            cell[..., :3][sel] = res[sel]
            out[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw] = cell
    cv2.imwrite(f, out); print(name, "valmis")
