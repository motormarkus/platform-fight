"""Lentokentän kuvien saumat: katukuvien pystysiirto (2: -2, 3: +14 ylös, nykyisiin) ja värisävyn tasaus saumoissa (kadun viimeinen kuva -> lentokenttä 1-3, sisäkuvat 1-4)
sekä parallaksina ikkunoiden takana näkyvän sisätilakuvan sumennus. Ajo (projektin juuresta): python Tools/AirportAssets/fix_seams.py [--shift]
--shift: tee myös pystysiirrot (vain kerran!). Ilman: vain värien tasaus ja sumennus."""
import sys, os
import numpy as np, cv2
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from make_airport_images import T, CUT, CURB, holes

RAMP = 260        # px: gain liukuu 1:een näin pitkällä matkalla
EDGE = 24         # px: reunan vertailukaista

def gain_rows(A, B, rows_smooth=61):
    """Rivikohtainen kanavakerroin B:n vasemmalle reunalle, jotta se täsmää A:n oikeaan reunaan."""
    a = A[:, -EDGE:, :3].astype(np.float32); b = B[:, :EDGE, :3].astype(np.float32)
    aa = A[:, -EDGE:, 3].mean(1) > 230; ba = B[:, :EDGE, 3].mean(1) > 230
    ma = a.mean(1); mb = b.mean(1)
    g = np.ones((A.shape[0], 3), np.float32); ok = aa & ba
    g[ok] = (ma[ok] + 4) / (mb[ok] + 4)
    # täytä aukot lähimmällä, tasoita
    idx = np.where(ok)[0]
    if len(idx) == 0: return g
    for c in range(3):
        g[:, c] = np.interp(np.arange(len(g)), idx, g[idx, c])
        k = np.ones(rows_smooth) / rows_smooth
        g[:, c] = np.convolve(np.pad(g[:, c], rows_smooth // 2, mode="edge"), k, mode="valid")
    return np.clip(g, 0.8, 1.25)

def apply_gain(B, g):
    B = B.copy().astype(np.float32)
    ramp = np.clip(1 - np.arange(B.shape[1]) / RAMP, 0, 1)[None, :, None]      # 1 reunalla, 0 kauempana
    mult = 1 + (g[:, None, :] - 1) * ramp
    B[..., :3] = np.clip(B[..., :3] * mult, 0, 255)
    return B.astype(np.uint8)

def load_street(k):
    return np.vstack([np.array(Image.open(os.path.join(T, "thaitown_lentokentta_katu_%d_%s.png" % (k, s))).convert("RGBA")) for s in ("yla", "kav", "ala")])

def save_street(k, im):
    base = os.path.join(T, "thaitown_lentokentta_katu_%d" % k); h, w = im.shape[:2]
    Image.fromarray(im).crop((0, 0, w, CUT)).save(base + "_yla.png")
    Image.fromarray(im).crop((0, CUT, w, CURB)).save(base + "_kav.png")
    Image.fromarray(im).crop((0, CURB, w, h)).save(base + "_ala.png")
    m = holes(im[:CUT, :, 3]); mask = np.zeros((CUT, w, 4), np.uint8); mask[m > 0] = (255, 255, 255, 255)
    Image.fromarray(mask, "RGBA").save(base + "_ikkunat.png")

def vshift(im, s):
    h = im.shape[0]; out = np.zeros_like(im)
    if s > 0: out[:h - s] = im[s:]; out[h - s:] = im[-1:]
    elif s < 0: out[-s:] = im[:h + s]; out[:-s] = im[:1]
    else: out = im
    return out

def main():
    shift = "--shift" in sys.argv
    k7 = np.array(Image.open(os.path.join(T, "thaitown_katu7.png")).convert("RGBA"))
    st = [load_street(k) for k in (1, 2, 3)]
    if shift: st[1] = vshift(st[1], -2); st[2] = vshift(st[2], 14)
    prev = k7
    for i in range(3):
        st[i] = apply_gain(st[i], gain_rows(prev, st[i])); prev = st[i]
        save_street(i + 1, st[i])
    print("katukuvat korjattu", "(siirrot mukana)" if shift else "")
    si = [np.array(Image.open(os.path.join(T, "thaitown_lentokentta_sisa_%d.png" % n)).convert("RGBA")) for n in (1, 2, 3, 4)]
    for i in range(1, 4): si[i] = apply_gain(si[i], gain_rows(si[i - 1], si[i]))
    for n, im in enumerate(si): Image.fromarray(im).save(os.path.join(T, "thaitown_lentokentta_sisa_%d.png" % (n + 1)))
    wide = np.hstack(si)
    wide = cv2.GaussianBlur(wide, (0, 0), 3.0)   # parallaksina ikkunoiden takana: pehmeä, ei terävä
    Image.fromarray(wide).save(os.path.join(T, "thaitown_lentokentta_sisa.png"))
    print("sisäkuvat tasattu, parallaksikuva sumennettu")

if __name__ == "__main__":
    main()
