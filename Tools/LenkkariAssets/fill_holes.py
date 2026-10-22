# Täyttää tanssijoiden kuvista siluetin sisään jääneet pienet reiät (avaus poisti joitain pikkarin ym. pikseleitä, jolloin lavan tanko näkyi läpi).
import sys, glob, cv2, numpy as np
MAXA = 250
def fix(path):
    im = cv2.imread(path, -1)
    a = im[..., 3]
    solid = (a > 128).astype(np.uint8)
    inv = 1 - solid
    n, lab, st, _ = cv2.connectedComponentsWithStats(inv, connectivity=4)
    h, w = a.shape
    filled = 0
    for i in range(1, n):
        x, y, ww, hh, area = st[i]
        if x == 0 or y == 0 or x + ww >= w or y + hh >= h: continue   # ulkoreunaan yhteydessä = oikeaa taustaa
        if area <= MAXA:
            m = lab == i
            b, g, r = im[..., 0].astype(int), im[..., 1].astype(int), im[..., 2].astype(int)
            green = (g > r + 35) & (g > b + 35)           # videon vihreä tausta jää läpinäkyväksi
            m = m & ~green
            im[..., 3][m] = 255; filled += int(m.sum())
    if filled: cv2.imwrite(path, im)
    return filled
tot = 0; files = 0
for pat in sys.argv[1:]:
    for p in glob.glob(pat):
        f = fix(p); tot += f; files += 1 if f else 0
print("korjattu kuvia", files, "täytettyjä pikseleitä", tot)
