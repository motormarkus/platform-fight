"""Lisää/paksuntaa hahmon ääriviivan kuvasarjoissa ja tasoittaa repaleiset reunat. Käyttö: python thicken_outline.py <leveys-ruudun> <paksuus-px> tiedosto.png [...]
Ruutu cellW x 400. Reuna tasoitetaan (pehmennys + kynnys), sitten ympärille piirretään tumma ääriviiva (etäisyysmuunnos, pehmeä reuna), myös aukkoihin (käsivarren ja vartalon väliin)."""
import sys, os, shutil
import cv2, numpy as np

OUTLINE = np.array([22, 14, 18], np.float32)   # RGB, lähes musta violetin sävyllä

def process_cell(c, thick, erode=0):
    a = c[..., 3].astype(np.float32) / 255.0
    if erode > 0:   # taustan värireunus pois: kutista hahmoa ennen ääriviivan piirtoa
        a = cv2.erode(a, np.ones((2 * erode + 1, 2 * erode + 1), np.uint8))
    # tasoita repaleiset reunat
    b = cv2.GaussianBlur(a, (0, 0), 1.1)
    a2 = np.clip((b - 0.5) * 3.0 + 0.5, 0, 1)
    solid = (a2 > 0.5).astype(np.uint8)
    d = cv2.distanceTransform(1 - solid, cv2.DIST_L2, 5)
    ring = np.clip(thick - d + 0.5, 0, 1) * (1 - a2)            # ääriviivan alpha (vain hahmon ulkopuolella)
    out_a = np.clip(a2 + ring, 0, 1)
    rgb = c[..., :3].astype(np.float32)
    # väri: hahmon värit sisällä, tumma ääriviiva ulkona (sekoitus alphan mukaan)
    w = np.where(out_a > 1e-3, (a2 / np.maximum(out_a, 1e-3)), 1.0)[..., None]
    col = rgb * w + OUTLINE[None, None, :] * (1 - w)
    res = np.dstack([col.clip(0, 255), (out_a * 255).clip(0, 255)]).astype(np.uint8)
    return res

def main(cw, thick, files, erode=0):
    for f in files:
        im = cv2.imread(f, cv2.IMREAD_UNCHANGED)
        if im is None: print("ei löydy", f); continue
        rgba = cv2.cvtColor(im, cv2.COLOR_BGRA2RGBA)
        H, W = rgba.shape[:2]
        out = np.zeros_like(rgba)
        for y in range(0, H, 400):
            for x in range(0, W, cw):
                out[y:y + 400, x:x + cw] = process_cell(rgba[y:y + 400, x:x + cw], thick, erode)
        bak = os.environ.get("BACKUP_DIR")
        if bak:
            os.makedirs(bak, exist_ok=True); shutil.copy(f, os.path.join(bak, os.path.basename(f)))
        cv2.imwrite(f, cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
        print("ok", f)

if __name__ == "__main__": main(int(sys.argv[1]), float(sys.argv[2]), sys.argv[3:], int(os.environ.get("ERODE", "0")))
