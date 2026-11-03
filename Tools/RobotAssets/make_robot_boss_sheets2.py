"""Kuurobotti-pomo, toinen erä: kävely (ei juoksua) ja erikoisisku (ladattu lyönti) -> robotti_pomo_kavely.png, robotti_pomo_erikoisisku.png.
Erikoisiskun videossa on tehosteita (kipinät, savu): ne poistetaan (robotin ääriviivasta täytetty siluetti, suurin komponentti).
Erikoisisku: lataus 6-39 (3 välein), lyönti 42-78 (2 välein; ojennettu 46-72), palautuminen 80-96 -> 36 kuvaa; osumaikkuna indeksit 15-22.
Ajo: python Tools/RobotAssets/make_robot_boss_sheets2.py <kavely.mov> <erikoisisku.mov> [ulos-kansio]"""
import os, sys
import numpy as np, cv2
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import make_robot_boss_sheets as b   # asentaa vihreän poiston (despill) m.cell-kääreeseen
m = b.m
m.DIMS.update({"r_pomo_kavely": (768, 384, 768), "r_pomo_erikoisisku": (1280, 512, 768)})

def strip_effects(im):
    a = im[..., 3] > 128
    lum = im[..., :3].astype(np.int32).sum(2) / 3
    dark = (lum < 75) & a
    cl = cv2.morphologyEx(dark.astype(np.uint8), cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9)))
    ff = cl.copy(); h, w = ff.shape; mk = np.zeros((h + 2, w + 2), np.uint8)
    cv2.floodFill(ff, mk, (0, 0), 2); filled = (ff != 2) | (cl > 0)
    n, lab, st, _ = cv2.connectedComponentsWithStats(filled.astype(np.uint8), connectivity=8)
    core = (lab == 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))).astype(np.uint8)
    core = cv2.dilate(core, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
    out = im.copy(); out[..., 3] = np.where(core > 0, im[..., 3], 0); return out

def main(vW, vS, outs):
    Wk, Sp = m.load(vW), m.load(vS)
    Sp = [strip_effects(f) for f in Sp]
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kW, fW = kf(Wk); kS, fS = kf(Sp); print("mittakaavat", round(kW, 3), round(kS, 3))
    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("r_pomo_" + name)
        cells = [m.cell(vid[i], mode, k, floor) for i in idx]
        m.save(cells, f"{outs}/robotti_pomo_{name}.png"); print(name, len(cells), "kuvaa", list(idx))
    sheet("kavely", Wk, m.best_loop(Wk, 0, len(Wk) - 1, 24, 40, "torso")[::2], "torso", kW, fW)
    idx = list(range(6, 42, 3)) + list(range(42, 80, 2)) + [80, 84, 88, 92, 96]
    sheet("erikoisisku", Sp, idx, "low", kS, fS)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
