"""Vartija pampulla: video C -> vartija_pamppu_idle / pamppu1 / pamppu2 (Assets/Sprites/Viholliset) + pamppu.png (valkoinen tausta pois) -> Assets/Resources/Vartija.
Mittakaava kuin make_guard_fight.py:ssä (1.185), jalat kuvan 0 mukaan.
Ajo: python Tools/GuardAssets/make_guard_baton.py <video.mp4> <pamppu-kuva> [ulos-kansio]"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_pamppu_idle": (640, 320, 768), "v_pamppu1": (1152, 320, 768), "v_pamppu2": (1152, 320, 768)})

def baton(src, dst):
    im = cv2.imread(src, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 3:   # ei alfaa: valkoinen tausta -> läpinäkyvä
        a = np.clip((250 - im.min(2).astype(np.float32)) / 40.0, 0, 1)
        im = np.dstack([im, (a * 255).astype(np.uint8)])
    ys, xs = np.where(im[..., 3] > 8)
    im = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    cv2.imwrite(dst, im); print("pamppu", im.shape)

def main(video, bat, outs):
    V = m.load(video)
    x0, x1, y0, y1 = m.bbox(V[0]); k = 1.185; floor = y1
    def sheet(name, idx, mode="low"):
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, floor) for i in idx], f"{outs}/vartija_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("pamppu_idle", m.best_loop(V, 192, 239, 14, 40, "low")[::2])
    sheet("pamppu1", [28, 30, 32, 34, 36, 38, 40, 44, 48, 52]); print("pamppu1: osuma indeksit 5-8 (kuvat 38-48)")
    sheet("pamppu2", [76, 78, 80, 82, 84, 86, 88, 92, 96]); print("pamppu2: osuma indeksit 5-8 (kuvat 86-96)")
    baton(bat, "Assets/Resources/Vartija/pamppu.png")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
