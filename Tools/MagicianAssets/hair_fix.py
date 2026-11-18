"""Taikurin tukka: videon hatuttomissa kuvissa 63..74 pään päälaki on litistynyt (hattuun sovitettu), vaikka hattu on irti. Korjaus: kuvan 62 täysi tukka
sovitetaan (ECC, affiini, ihoalueen maskilla) kuvan päähän ja otetaan hiusalueelle (iho + korvan taso ylös). Käytetään make_kaatuminen.py:ssä."""
import numpy as np, cv2
REF = 62; FIRST, LAST = 63, 74
def skin(a):
    r, g, b = [a[..., k].astype(int) for k in range(3)]
    return (a[..., 3] > 200) & (r > 150) & (r > g + 22) & (g > b + 8)
def head_box(a):
    m = a[..., 3] > 100; ys, xs = np.nonzero(m); top = ys.min(); cx = int(np.median(xs[ys < top + 50])); return top, cx
def fix(frame, ref, strength=1.0, debug=None):
    top, cx = head_box(frame); rtop, rcx = head_box(ref)
    H = 170; W = 150
    fy0, fx0 = max(0, top - 10), cx - W // 2; ry0, rx0 = max(0, rtop - 10), rcx - W // 2
    f = frame[fy0:fy0 + H, fx0:fx0 + W]; r = ref[ry0:ry0 + H, rx0:rx0 + W]
    fs = cv2.GaussianBlur(skin(f).astype(np.float32), (0, 0), 2); rs = cv2.GaussianBlur(skin(r).astype(np.float32), (0, 0), 2)
    warp = np.eye(2, 3, dtype=np.float32)
    try: cc, warp = cv2.findTransformECC(fs, rs, warp, cv2.MOTION_AFFINE, (cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 200, 1e-6), None, 5)
    except cv2.error: cc = 0
    rw = cv2.warpAffine(r, warp, (W, H), flags=cv2.INTER_CUBIC + cv2.WARP_INVERSE_MAP, borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
    sk = skin(f); sy, sx = np.nonzero(sk)
    if len(sy) < 50: return frame, 0
    ytop = sy.min(); yz = ytop + int(.22 * (sy.max() - ytop))              # hiusalue: ihon ylärajasta otsan ja korvan tasolle asti, tämän ylä- ja sivupuoli
    zone = np.zeros((H, W), np.float32); zone[:yz] = 1
    zone[sk] = 0; zone = cv2.dilate(zone, np.ones((3, 3), np.uint8)) * (1 - cv2.erode(sk.astype(np.float32), np.ones((3, 3), np.uint8)))
    zone = cv2.GaussianBlur(zone, (0, 0), 1.2)
    wa = rw[..., 3].astype(np.float32) / 255.0
    # hiusten sävy: vedä alkuperäisen kuvan hiusten keskivärin suuntaan, jotta tukka sopii naamaan
    out = f.copy().astype(np.float32); a0 = f[..., 3].astype(np.float32) / 255.0
    hair_f = (a0 > .8) & ~sk & (np.arange(H)[:, None] < yz + 6)
    hair_r = (wa > .8) & (zone > .5)
    gain = np.ones(3, np.float32)
    if hair_f.sum() > 40 and hair_r.sum() > 40:
        gain = np.clip(f[..., :3][hair_f].mean(0) / np.maximum(rw[..., :3][hair_r].mean(0), 1.0), .8, 1.35)
    col = np.clip(rw[..., :3].astype(np.float32) * gain, 0, 255)
    wgt = (zone * wa * strength)[..., None]
    out[..., :3] = out[..., :3] * (1 - wgt) + col * wgt
    new_a = np.maximum(a0, zone * wa * strength)
    # ihon päälle ei kirjoiteta (zone[sk]=0)
    out[..., 3] = new_a * 255.0
    res = frame.copy(); res[fy0:fy0 + H, fx0:fx0 + W] = np.clip(out, 0, 255).astype(np.uint8)
    return res, float(cc)
