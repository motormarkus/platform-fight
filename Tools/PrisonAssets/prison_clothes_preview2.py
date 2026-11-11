"""Esikatselu 2: vankioranssi lisää vihuista (Samoalainen, Merirosvo, Metsuri, Puliukko, Romuukko, Rosvokapteeni), pelkkä värjäys.
Ajo: python Tools/PrisonAssets/prison_clothes_preview2.py <ulos.png>"""
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import prison_clothes_preview as P

def cellg(n):
    im = Image.open(P.V + n + "_idle.png").convert("RGBA"); W, H = im.size
    return np.array(im.crop((0, 0, W // 4, min(H, 384))))
def feat(a):
    h, s, v = P.hsv(a); al = a[..., 3] > 128; ys, xs = np.where(al); y0, y1 = ys.min(), ys.max()
    return h, s, v, al, (np.arange(a.shape[0])[:, None] - y0) / (y1 - y0)
R = P.recolor
def samoa():
    a = cellg("samoa"); h, s, v, al, Y = feat(a); skin = (h < 0.1) & (s > 0.35)
    a = R(a, al & ~skin & (Y > 0.5) & (Y < 0.85) & (s > 0.15) & (h > 0.08) & (h < 0.25), lambda v: v * 1.6 + 0.1)
    return R(a, al & ~skin & (Y > 0.2) & (Y < 0.62) & (v < 0.55) & (s < 0.35), lambda v: v * 1.8 + 0.3)
def merirosvo():
    a = cellg("merirosvo"); h, s, v, al, Y = feat(a)
    a = R(a, al & (h > 0.09) & (h < 0.17) & (s > 0.5), lambda v: v)
    a = R(a, al & (h > 0.17) & (h < 0.4) & (s > 0.2), lambda v: v * 1.5 + 0.25)
    return R(a, al & (h > 0.5) & (h < 0.7) & (s > 0.15), lambda v: v * 1.4 + 0.2)
def metsuri():
    a = cellg("metsuri"); h, s, v, al, Y = feat(a); return R(a, al & (h > 0.5) & (h < 0.7) & (s > 0.25), lambda v: v * 1.4 + 0.2)
def puliukko():
    a = cellg("puliukko"); h, s, v, al, Y = feat(a)
    a = R(a, al & (h > 0.72) & (h < 0.88) & (s > 0.3), lambda v: v * 1.6 + 0.2)
    return R(a, al & (h > 0.55) & (h < 0.72) & (s > 0.3), lambda v: v * 2.0 + 0.25)
def romuukko():
    a = cellg("romuukko"); h, s, v, al, Y = feat(a); skin = (h < 0.1) & (s > 0.3)
    return R(a, al & ~skin & (h > 0.5) & (h < 0.85) & (s > 0.08) & (Y > 0.1), lambda v: v * 1.3 + 0.15)
def rosvokapt():
    a = cellg("rosvokapteeni"); h, s, v, al, Y = feat(a)
    a = R(a, al & (h > 0.5) & (h < 0.7) & (s > 0.12) & (Y < 0.6), lambda v: v * 1.3 + 0.25)
    return R(a, al & (h > 0.1) & (h < 0.35) & (s > 0.12) & (Y > 0.45), lambda v: v * 1.3 + 0.2)

def main(out):
    items = [("samoa", samoa), ("merirosvo", merirosvo), ("metsuri", metsuri), ("puliukko", puliukko), ("romuukko", romuukko), ("rosvokapteeni", rosvokapt)]
    tiles = []
    for n, f in items:
        o = Image.fromarray(cellg(n)); r = Image.fromarray(f()); bb = o.getbbox(); tiles.append((o.crop(bb), r.crop(bb)))
    H = max(t[0].height for t in tiles); W = sum(t[0].width for t in tiles)
    im = Image.new("RGBA", (W, H * 2), (70, 70, 70, 255)); x = 0
    for o, r in tiles:
        im.alpha_composite(o, (x, H - o.height)); im.alpha_composite(r, (x, 2 * H - o.height)); x += o.width
    im.convert("RGB").save(out)
if __name__ == "__main__": main(sys.argv[1])
