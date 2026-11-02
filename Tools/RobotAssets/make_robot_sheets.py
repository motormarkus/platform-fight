"""Kuurobotti (vihollinen): kolmesta vihreätaustaisesta videosta kuvasarjat Assets/Sprites/Viholliset/robotti_<nimi>.png.
Video A (idle 0-44, kävely 46-118, juoksu 122-239), video B (lyönti 1 24-36, lyönti 2 36-52, potku 1 58-84, potku 2 90-114, hyppypotku 153-213), video C (rakettisyöksy: valmistelu, lento, palautuminen).
Ruudut kuten mongolilla (4 saraketta, hahmo ~640 px, jalat 16 px ylös).
Ajo: python Tools/RobotAssets/make_robot_sheets.py <liike.mp4> <iskut.mp4> <raketti.mov> [ulos-kansio] [osuma_kaatuminen.mov] [idle_vaurio.mov]"""
import os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
import av
from key_video import key_frame
# turkoosi/sininen visiiri (silmät) on lähellä vihreää taustaa: cyan_safe pitää sen mukana
import cv2


def smooth_alpha(im):
    """Rosoiset reunat: mediaani poistaa porrastuksen, sitten pehmeä reuna."""
    a = im[..., 3]
    a = cv2.medianBlur(a, 5)
    a = cv2.GaussianBlur(a, (0, 0), 0.9)
    a = np.clip((a.astype(np.float32) - 40.0) * (255.0 / 175.0), 0, 255).astype(np.uint8)   # terävöitys: puolipikselin reuna
    out = im.copy(); out[..., 3] = np.minimum(a, 255)
    return out


def enhance_visor(im):
    """Visiiri (turkoosi) kirkkaammaksi ja hieman isommaksi: väri vahvistetaan ja maski laajennetaan 1 px."""
    r, g, b = [im[..., i].astype(np.int32) for i in range(3)]
    cy = (im[..., 3] > 128) & (g > 140) & (b > 140) & (r < 150) & (np.abs(g - b) < 90)
    n, lab, st, _ = cv2.connectedComponentsWithStats(cy.astype(np.uint8), connectivity=8)
    keep = [i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] >= 14]   # irralliset reunapikselit (valkoisen ja ääriviivan rajalla) eivät ole visiiriä
    cy = np.isin(lab, keep)
    if cy.sum() < 6: return im
    big = cv2.dilate(cy.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & (im[..., 3] > 128)
    out = im.copy()
    out[big, 0] = 40; out[big, 1] = 235; out[big, 2] = 255
    core = cv2.erode(cy.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
    out[core, 0] = 190; out[core, 1] = 255; out[core, 2] = 255   # kirkas ydin
    return out


m.load = lambda path: [smooth_alpha(enhance_visor(key_frame(f.to_ndarray(format="bgr24"), cyan_safe=True))) for f in av.open(path).decode(video=0)]
# skaalaus: suurennettaessa bikubinen (INTER_AREA tekee lohkoja)
_orig_rescale = m.rescale
def _rescale(rgba, f):
    if f <= 1.0: return _orig_rescale(rgba, f)
    a = rgba.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    r = cv2.resize(a, (round(a.shape[1] * f), round(a.shape[0] * f)), interpolation=cv2.INTER_CUBIC)
    al = np.clip(r[..., 3:4] / 255.0, 0, 1); r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    r[..., 3:4] = al * 255.0
    return r.clip(0, 255).astype(np.uint8)
m.rescale = _rescale

m.TARGET_H = 640.0
m.DIMS.update({"r_idle": (640, 320, 768), "r_kavely": (640, 320, 768), "r_juoksu": (768, 320, 768),
               "r_lyonti1": (896, 320, 768), "r_lyonti2": (896, 320, 768), "r_potku1": (1024, 320, 768), "r_potku2": (1024, 320, 768),
               "r_hyppypotku": (1152, 384, 1024), "r_raketti": (1408, 560, 896),
               "r_idle_vaurio": (640, 320, 768), "r_osuma": (896, 420, 768), "r_kaatuminen": (1152, 576, 1024), "r_maahan": (1152, 576, 768), "r_nousu": (1152, 576, 768)})


def main(vA, vB, vC, outs, vD=None, vE=None):
    A, B, C = m.load(vA), m.load(vB), m.load(vC)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C)
    print("mittakaavat", round(kA, 3), round(kB, 3), round(kC, 3))

    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("r_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/robotti_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    sheet("idle", A, m.best_loop(A, 0, 44, 14, 36, "low")[::2], "low", kA, fA)
    sheet("kavely", A, m.best_loop(A, 46, 118, 16, 50, "torso")[::2], "torso", kA, fA)
    sheet("juoksu", A, m.best_loop(A, 122, 239, 10, 30, "torso")[::2], "torso", kA, fA)
    sheet("lyonti1", B, [24, 26, 28, 30, 32, 34, 36, 38], "low", kB, fB); print("lyonti1: osuma indeksit 3-4 (kuvat 30-32)")
    sheet("lyonti2", B, [36, 38, 40, 42, 44, 46, 48, 50, 52], "low", kB, fB); print("lyonti2: osuma indeksit 2-4 (kuvat 40-44)")
    sheet("potku1", B, list(range(58, 86, 2)), "low", kB, fB); print("potku1: osuma indeksit 5-9 (kuvat 68-76)")
    sheet("potku2", B, list(range(90, 116, 2)), "low", kB, fB); print("potku2: osuma indeksit 4-8 (kuvat 98-106)")
    sheet("hyppypotku", B, list(range(153, 216, 3)), lambda i: "low" if i < 165 or i > 207 else "com", kB, fB); print("hyppypotku: osuma indeksit 7-16 (kuvat 174-201), liike 5-16")
    rk = [5, 9, 13, 17, 21, 25, 29, 35, 41, 47, 53, 59, 65, 71, 77, 83, 87, 91, 95, 99, 103, 107]
    sheet("raketti", C, rk, lambda i: "low" if i <= 26 or i >= 87 else "com", kC, fC); print("raketti: lento indeksit 6-14 (kuvat 29-77), osuma samoin; palautuminen 15-21")

    if vD:   # video D: osuma, kaatuminen, maahan, nousu
        D = m.load(vD); kD, fD = kf(D); print("mittakaava D", round(kD, 3))
        sheet("osuma", D, [10, 12, 14, 16, 18, 20, 22, 24], "low", kD, fD)
        sheet("kaatuminen", D, list(range(30, 70, 3)), "com", kD, fD)
        sheet("maahan", D, [72, 80, 100, 120, 138, 144], "com", kD, fD)
        sheet("nousu", D, list(range(146, 181, 3)), "com", kD, fD)

    if vE:   # video E: vaurioitunut idle (elämät alle 30 %), nykivä silmukka
        E = m.load(vE); kE, fE = kf(E); print("mittakaava E", round(kE, 3))
        sheet("idle_vaurio", E, list(range(0, len(E) - 4, 4)), "low", kE, fE)


if __name__ == "__main__":
    main(*sys.argv[1:4], sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset", sys.argv[5] if len(sys.argv) > 5 else None, sys.argv[6] if len(sys.argv) > 6 else None)
