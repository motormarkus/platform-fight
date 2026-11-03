"""Kuun tukikohdan jatko.
1) Laboratorion pano jatkuu kuvalla 2 (kuvan 2 vasen ovi osuu panon oikeaan reunan oveen): lab_jatko -> muuri_laboratorio_pano.png (korkeus 941)
2) Käytävä: kuvat 3 ja 4 yhdistettynä (limitys 40 px) -> muuri_kaytava_pano.png (korkeus 941); kuvassa 4 ikkuna läpinäkyvä.
Ajo: python Tools/MoonAssets/make_lab_corridor.py <lab_jatko.png> <kaytava1.png> <kaytava2.png> [Assets/Sprites/Taustat]
Huom: laboratorion pano tehdään vain kerran (alkuperäinen 4408 px); jos leveys on jo > 4408, jatkoa ei lisätä uudestaan."""
import sys, os
import numpy as np, cv2
H = 941

def load(p):
    im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 3: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
    return im

def scale_h(im, h=H):
    w = round(im.shape[1] * h / im.shape[0])
    a = im.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    r = cv2.resize(a, (w, h), interpolation=cv2.INTER_CUBIC)
    al = np.clip(r[..., 3:4] / 255.0, 0, 1); r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0); r[..., 3:4] = al * 255.0
    return r.clip(0, 255).astype(np.uint8)

def join(a, b, ox):
    """b alkaa a:n x-kohdasta ox; limitys lineaarinen, limityksessä opaakki."""
    W = ox + b.shape[1]; h = a.shape[0]
    cv = np.zeros((h, W, 4), np.float32); cv[:, :a.shape[1]] = a
    bb = np.zeros((h, W, 4), np.float32); bb[:, ox:] = b
    ov = a.shape[1] - ox
    w = np.zeros(W, np.float32); w[ox:ox + ov] = np.linspace(0, 1, ov); w[ox + ov:] = 1.0
    wb = w[None, :, None]
    rgb = cv[..., :3] * (1 - wb) + bb[..., :3] * wb
    alpha = np.where(wb[..., 0] >= 1.0, bb[..., 3], np.where(wb[..., 0] <= 0.0, cv[..., 3], 255.0))
    return np.dstack([rgb, alpha]).clip(0, 255).astype(np.uint8)

if __name__ == "__main__":
    ext, c1, c2 = sys.argv[1:4]
    outd = sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Taustat"
    lp = os.path.join(outd, "muuri_laboratorio_pano.png")
    lab = cv2.imread(lp, cv2.IMREAD_UNCHANGED)
    if lab.shape[1] <= 4500:
        e = scale_h(load(ext))
        ov = round((1997 - 1694) * H / 787)
        lab = join(lab, e, lab.shape[1] - ov)
        cv2.imwrite(lp, lab); print(lp, lab.shape[1], "x", lab.shape[0])
    else: print("laboratorion pano on jo jatkettu:", lab.shape[1])
    a = scale_h(load(c1)); b = scale_h(load(c2))
    cor = join(a, b, a.shape[1] - round(40 * H / 788))
    op = os.path.join(outd, "muuri_kaytava_pano.png")
    cv2.imwrite(op, cor); print(op, cor.shape[1], "x", cor.shape[0])
