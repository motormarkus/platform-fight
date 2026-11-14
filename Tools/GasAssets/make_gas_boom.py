"""Kaasupullon räjähdys vihreätaustaisesta videosta -> Assets/Resources/Kaasupullo/boom_NN.png (640 x 360, RGBA, reunat häivytetty ettei savu leikkaudu jyrkästi).
Käyttö: python Tools/GasAssets/make_gas_boom.py <video.mp4> [ensimmäinen kuva=208] [viimeinen=239]"""
import sys, os, cv2, numpy as np
src = sys.argv[1]; f0 = int(sys.argv[2]) if len(sys.argv) > 2 else 208; f1 = int(sys.argv[3]) if len(sys.argv) > 3 else 239
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Kaasupullo")
os.makedirs(out, exist_ok=True)
cap = cv2.VideoCapture(src); i = 0; n = 0
H, W = 360, 640
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
def ramp(d, width): return np.clip(d / width, 0, 1)
feather = ramp(yy, H * 0.22) * ramp(H - 1 - yy, H * 0.12) * ramp(xx, W * 0.07) * ramp(W - 1 - xx, W * 0.07)
feather = feather * feather * (3 - 2 * feather)
while True:
    ok, f = cap.read()
    if not ok or i > f1: break
    if i >= f0:
        b, g, r = [f[..., k].astype(np.float32) for k in range(3)]
        d = g - np.maximum(r, b)
        a = np.clip(1 - (d - 25) / 45.0, 0, 1)
        g2 = np.minimum(g, np.maximum(r, b) + 4)
        rgba = np.dstack([b, g2, r, a * 255.0]).astype(np.float32)
        rgba = cv2.resize(rgba, (W, H), interpolation=cv2.INTER_AREA)
        rgba[..., 3] *= feather
        rgba = rgba.clip(0, 255).astype(np.uint8)
        rgba[rgba[..., 3] == 0, :3] = 0
        cv2.imwrite(os.path.join(out, f"boom_{n:02d}.png"), rgba); n += 1
    i += 1
print("kuvia", n)
