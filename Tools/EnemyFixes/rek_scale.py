"""Rekkamiehen kuvien kokomittaus: lippiksen (punainen + kermanvalkoinen) rajauslaatikon leveys ja korkeus hahmon yläosasta."""
import sys, os, glob
import numpy as np, cv2
S = 'Assets/Sprites/Viholliset/'
CW, CH = 512, 448

def frames(n, d=S):
    a = cv2.imread(d + n + '.png', cv2.IMREAD_UNCHANGED); out = []
    for r in range(a.shape[0] // CH):
        for c in range(a.shape[1] // CW):
            f = a[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
            if f[..., 3].max() > 0: out.append(f)
    return out

def cap(f):
    al = f[..., 3] > 128; ys, xs = np.nonzero(al); t = ys.min()
    b, g, r = [f[..., i].astype(int) for i in range(3)]
    red = (r - g > 95) & (r - b > 95)
    cream = (r > 165) & (g > 145) & (b > 115) & (r - b < 70) & (r - g < 40)
    m = (al & (red | cream)).astype(np.uint8); m[t + 60:] = 0
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((2, 2), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    if n < 2: return None
    # lippis: yläreunaa lähin riittävän iso komponentti + sen vierekkäiset (punainen ja kerma)
    big = [k for k in range(1, n) if st[k, 4] >= 120]
    if not big: return None
    top = min(big, key=lambda k: st[k, 1]); y0 = st[top, 1]
    sel = [k for k in big if st[k, 1] < y0 + 25 and abs((st[k, 0] + st[k, 2] / 2) - (st[top, 0] + st[top, 2] / 2)) < 90]
    x0 = min(st[k, 0] for k in sel); x1 = max(st[k, 0] + st[k, 2] for k in sel); y1 = max(st[k, 1] + st[k, 3] for k in sel)
    return x1 - x0, y1 - y0

if __name__ == "__main__":
    for p in sorted(glob.glob(S + 'rekkamies_*.png')):
        n = os.path.basename(p)[:-4]; fs = frames(n)
        cs = [cap(f) for f in fs]
        print('%-24s' % n, ' '.join('%s' % (('%d' % c[0]) if c else '-') for c in cs))
