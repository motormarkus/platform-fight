"""Donin arkkien reunat siistiksi: rosoinen tumma reunus (videoavainnuksen varjo) pois ja tilalle tasainen ohut ääriviiva.
Lähde Tools/DonAssets/orig_edges/don_*.png (alkuperäiset) -> Assets/Sprites/Viholliset/don_*.png.
1) reunalta kuoritaan tummia pikseleitä (luma < DARK) enintään PEEL kierrosta, 2) maski siistitään (avaus/sulku, pienet roskat pois),
3) tilalle LINE px tasainen tumma ääriviiva, 4) ulkoreuna pehmennetään 1 px."""
import glob, os, sys
import numpy as np, cv2
from PIL import Image
DARK, PEEL, LINE = 70, 6, 2
SRC = "Tools/DonAssets/orig_edges"
DST = "Assets/Sprites/Viholliset"
only = sys.argv[1:]
K3 = np.ones((3, 3), np.uint8)
for f in sorted(glob.glob(SRC + "/don_*.png")):
    n = os.path.basename(f)
    if only and n not in only: continue
    im = np.array(Image.open(f).convert("RGBA"))
    rgb = im[..., :3]
    luma = rgb.astype(np.float32) @ np.array([0.299, 0.587, 0.114], np.float32)
    m = (im[..., 3] >= 128).astype(np.uint8)
    dark = (luma < DARK).astype(np.uint8)
    for _ in range(PEEL):
        edge = m - cv2.erode(m, K3)
        rem = (edge > 0) & (dark > 0)
        if not rem.any(): break
        m[rem] = 0
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, K3)
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, K3)
    k, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
    for i in range(1, k):
        if st[i, cv2.CC_STAT_AREA] < 400: m[lab == i] = 0
    # sisäiset pienet reiät (avainnuksen jättämät kolot, esim. hiha) umpeen; väri inpaintilla
    inv = (1 - m).astype(np.uint8)
    k2, lab2, st2, _ = cv2.connectedComponentsWithStats(inv, 4)
    hole = np.zeros_like(m)
    for i in range(1, k2):
        x, y, w, h, area = st2[i]
        if area < 2500 and x > 0 and y > 0 and x + w < m.shape[1] and y + h < m.shape[0]: hole[lab2 == i] = 1
    if hole.any():
        m[hole > 0] = 1
        rgb = cv2.inpaint(np.ascontiguousarray(rgb), cv2.dilate(hole, K3), 5, cv2.INPAINT_TELEA)
    # sileä ulkoreuna: maski pehmeäksi ja takaisin kynnykseksi (pyöristää porrastuksen)
    sm = cv2.GaussianBlur(m.astype(np.float32), (5, 5), 1.2)
    m2 = (sm >= 0.5).astype(np.uint8)
    full = cv2.dilate(m2, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * LINE + 1, 2 * LINE + 1)))
    ring = (full > 0) & (m2 == 0)
    out = np.zeros_like(im)
    out[..., :3] = rgb
    out[ring, 0], out[ring, 1], out[ring, 2] = 22, 16, 18
    # kuoritut sisäpikselit (m2 mutta ei alkup. maskissa) saavat lähimmän värin
    H, W = full.shape   # sileä reuna: 3x suurennos, pehmennys, kynnys, takaisin (ei porrastusta)
    big = cv2.GaussianBlur(cv2.resize(full.astype(np.float32), (W * 3, H * 3), interpolation=cv2.INTER_CUBIC), (0, 0), 2.4)
    alpha = cv2.resize(np.clip((big - 0.5) * 6 + 0.5, 0, 1), (W, H), interpolation=cv2.INTER_AREA) * 255
    newpx = (alpha > 5) & (full == 0)
    out[newpx, 0], out[newpx, 1], out[newpx, 2] = 22, 16, 18
    out[..., 3] = np.clip(alpha, 0, 255).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(os.path.join(DST, n))
    print(n, "ok")
