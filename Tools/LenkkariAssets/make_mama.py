# Mama (Salsafestin kojun myyja): video -> yksittaiset kuvat (Assets/Sprites/Myyjat/mama_myyja_NN.png), hahmo 300 px korkea
import os, sys, cv2, numpy as np
sys.path.insert(0, "G:/unity pelit/mopopeli/platform fight/Tools/GymAssets")
from key_video import key_frame
V = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/d522027f-copy_993E76CE-D130-4658-B940-C9F33BF2D6CC.mov"
OUT = "G:/unity pelit/mopopeli/platform fight/Assets/Sprites/Myyjat"
STEP = 5
cap = cv2.VideoCapture(V); frames = []; i = 0
while True:
    ok, f = cap.read()
    if not ok: break
    if i % STEP == 0: frames.append(key_frame(f, cyan_safe=True))
    i += 1
x0 = y0 = 10**9; x1 = y1 = 0
for r in frames:
    ys, xs = np.where(r[..., 3] > 40)
    x0 = min(x0, xs.min()); x1 = max(x1, xs.max()); y0 = min(y0, ys.min()); y1 = max(y1, ys.max())
print(len(frames), x0, x1, y0, y1)
k = 300.0 / (y1 - y0 + 1)
for n, r in enumerate(frames):
    c = r[y0:y1 + 1, x0:x1 + 1]
    c = cv2.resize(c, (max(1, round(c.shape[1] * k)), 300), interpolation=cv2.INTER_AREA)
    cv2.imwrite(f"{OUT}/mama_myyja_{n:02d}.png", cv2.cvtColor(c, cv2.COLOR_RGBA2BGRA))
