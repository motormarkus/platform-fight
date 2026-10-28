"""Lyöntikombo-gif pelin kuvilla: python Tools/LatkaAssets/make_combo_gif2.py <kombo-sheetit> <takapotku-kansio> <ulos.gif>  (kombo 30 fps, perään roundhouse)"""
import sys, os
import numpy as np
from PIL import Image
CW, CH = 1024, 704
def load(path):
    im = Image.open(path).convert("RGBA"); out = []
    for r in range(im.height // CH):
        for c in range(im.width // CW):
            cell = im.crop((c * CW, r * CH, (c + 1) * CW, (r + 1) * CH))
            if np.asarray(cell)[..., 3].max() > 0: out.append(cell)
    return out
def accel(f, impact, maxrep):
    l = []
    for i in range(impact): l += [f[i]] * max(1, round(maxrep * (1 - i / impact)))
    return l + f[impact:]
combo, kd, outp = sys.argv[1:4]; tl = []
segs = [load(os.path.join(combo, "latkafani_" + n + ".png")) for n in ("jab", "suora", "isku3", "vartalokoukku", "koukku")]
tl.append((segs[0][0], 0.4))
for k, s in enumerate(segs):
    for im in (s if k == 0 else s[1:]): tl.append((im, 1 / 30))
tl.append((tl[-1][0], 0.5))
pass
tl.append((tl[-1][0], 0.5))
step = 0.02; total = sum(d for _, d in tl); frames = []
bg = Image.new("RGBA", (CW, CH), (205, 220, 235, 255))
for k in range(int(total / step)):
    t = k * step; acc = 0
    for im, d in tl:
        if t < acc + d: break
        acc += d
    f = bg.copy(); f.alpha_composite(im); frames.append(f.resize((CW // 2, CH // 2), Image.LANCZOS).convert("P", palette=Image.ADAPTIVE, colors=128))
frames[0].save(outp, save_all=True, append_images=frames[1:], duration=20, loop=0, disposal=1); print(outp, len(frames), round(total, 2), "s")
