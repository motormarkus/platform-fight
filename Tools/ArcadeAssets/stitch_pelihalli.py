import cv2, numpy as np
import os
root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
d = os.path.join(root, "output", "imagegen") + os.sep
L = cv2.imread(d + "arcade-wide-left.png").astype(np.float32)
R = cv2.imread(d + "arcade-wide-right-v2.png").astype(np.float32)
off = 1625
W = off + R.shape[1]
H = L.shape[0]
out = np.zeros((H, W, 3), np.float32)
out[:, :off] = L[:, :off]
out[:, L.shape[1]:] = R[:, L.shape[1]-off:]
ov = L.shape[1] - off  # 295
t = np.linspace(0, 1, ov)[None, :, None]
t = t * t * (3 - 2 * t)
out[:, off:L.shape[1]] = L[:, off:] * (1 - t) + R[:, :ov] * t
cv2.imwrite(os.path.join(root, "Assets", "Sprites", "Taustat", "pelihalli_sisa.png"), np.clip(out, 0, 255).astype(np.uint8))
print(out.shape)
