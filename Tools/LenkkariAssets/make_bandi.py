# Klubin bandi: video -> Assets/Sprites/Bandi/bandi_klubi_NN.png (hahmot n. 300 px, tukipiste alareunassa)
import sys, cv2, numpy as np
sys.path.insert(0, "G:/unity pelit/mopopeli/platform fight/Tools/GymAssets")
from key_video import key_frame
V = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/e653bb15-copy_69079825-9127-46E4-BA8B-DEFBAA5BAE93.mov"
OUT = "G:/unity pelit/mopopeli/platform fight/Assets/Sprites/Bandi"
cap = cv2.VideoCapture(V); fr = []; i = 0
while True:
    ok, x = cap.read()
    if not ok: break
    if i % 3 == 0: fr.append(key_frame(x, cyan_safe=True))
    i += 1
x0 = y0 = 10**9; x1 = y1 = 0
for r in fr:
    ys, xs = np.where(r[..., 3] > 40)
    x0 = min(x0, xs.min()); x1 = max(x1, xs.max()); y0 = min(y0, ys.min()); y1 = max(y1, ys.max())
k = 0.43
for n, r in enumerate(fr):
    c = r[y0:y1 + 1, x0:x1 + 1]
    c = cv2.resize(c, (round(c.shape[1] * k), round(c.shape[0] * k)), interpolation=cv2.INTER_AREA)
    cv2.imwrite(f"{OUT}/bandi_klubi_{n:02d}.png", cv2.cvtColor(c, cv2.COLOR_RGBA2BGRA))
print(len(fr), c.shape)
c = cv2.imread(f"{OUT}/bandi_klubi_10.png", -1); a = c[..., 3:] / 255.
cv2.imwrite("C:/Users/marku/AppData/Local/Temp/claude/G--unity-pelit-mopopeli-platform-fight/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/scratchpad/mama/b.png", (c[..., :3] * a + np.array([70, 70, 190]) * (1 - a)).astype(np.uint8))
