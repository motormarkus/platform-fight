# Pablo (Salsafestin kojun myyja): idle- ja kavelyvideo -> Assets/Sprites/Myyjat/pablo_idle_NN.png / pablo_kavely_NN.png (hahmo 300 px)
import sys, cv2, numpy as np
sys.path.insert(0, "G:/unity pelit/mopopeli/platform fight/Tools/GymAssets")
from key_video import key_frame
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
OUT = "G:/unity pelit/mopopeli/platform fight/Assets/Sprites/Myyjat"
def load(f, step, start=0):
    cap = cv2.VideoCapture(U + f); fr = []; i = 0
    while True:
        ok, x = cap.read()
        if not ok: break
        if i >= start and (i - start) % step == 0: fr.append(key_frame(x, cyan_safe=True))
        i += 1
    return fr
def bbox(fr):
    x0 = y0 = 10**9; x1 = y1 = 0
    for r in fr:
        ys, xs = np.where(r[..., 3] > 40)
        x0 = min(x0, xs.min()); x1 = max(x1, xs.max()); y0 = min(y0, ys.min()); y1 = max(y1, ys.max())
    return x0, x1, y0, y1
idle = load("c163e085-copy_C133D45D-AC40-469C-A1FC-F0DC9CE0CDCE.mov", 5)
walk_all = load("3fff3cfa-copy_4ED5266A-E525-47F2-B9DA-0C5561B8E99A.mov", 1, 45)   # alku on kaantyminen, kavely alkaa n. 1,5 s
# silmukka: etsi jakso, jonka lopun kuva on lahinna alkua
g = [cv2.resize(cv2.cvtColor(r[..., :3], cv2.COLOR_RGB2GRAY), (54, 72)).astype(np.float32) * (r[::20, ::20, 3].mean() * 0 + 1) for r in walk_all]
best = (1e18, 20)
for p in range(14, min(40, len(g) - 1)):
    d = np.abs(g[0] - g[p]).mean()
    if d < best[0]: best = (d, p)
print("jakso", best)
walk = walk_all[:best[1]]
ix0, ix1, iy0, iy1 = bbox(idle)
k = 300.0 / (iy1 - iy0 + 1)
def save(fr, name):
    x0, x1, y0, y1 = bbox(fr)
    for n, r in enumerate(fr):
        c = r[y0:y1 + 1, x0:x1 + 1]
        c = cv2.resize(c, (max(1, round(c.shape[1] * k)), max(1, round(c.shape[0] * k))), interpolation=cv2.INTER_AREA)
        cv2.imwrite(f"{OUT}/{name}_{n:02d}.png", cv2.cvtColor(c, cv2.COLOR_RGBA2BGRA))
    print(name, len(fr), x1 - x0, y1 - y0)
save(idle, "pablo_idle")
save(walk, "pablo_kavely")
