"""Salsafest-alueen kuva: kolme kuvaa (1774 x 887) yhdistettynä, sauma 1-2 pylvään kohdalla (limitys mitattu 1472 px, dy 3), 2-3 suoraan vierekkäin.
Läpinäkyvä taivas täytetään taivaalla ja merellä, joten tulos on peitteinen kuva. Käyttö: python make_salsafest.py <kuva1> <kuva2> <kuva3> <ulos.png> [esikatselu.jpg]"""
import sys
import cv2, numpy as np

def load(p): return cv2.imread(p, cv2.IMREAD_UNCHANGED)

def gain_match(a, b, rows, ax0, bx0, width=80):
    """b sävyyn a:n kanssa (hiekan sävyero) saumakohdassa: a:n sarakkeet ax0..ax0+10 vs b:n bx0..bx0+10; kerroin liukuu 1:een width pikselissä saumasta."""
    ra = a[rows[0]:rows[1], ax0:ax0 + 10, :3].reshape(-1, 3).mean(0); rb = b[rows[0]:rows[1], bx0:bx0 + 10, :3].reshape(-1, 3).mean(0)
    g = np.clip(ra / np.maximum(rb, 1), 0.85, 1.15)
    ramp = np.clip(1 - np.abs(np.arange(b.shape[1]) - bx0) / width, 0, 1)[None, :, None]
    f = b.astype(np.float32); f[..., :3] *= (1 + (g[None, None, :] - 1) * ramp)
    return f.clip(0, 255).astype(np.uint8)

def main(p1, p2, p3, out, prev=None, transparent=True):
    a, b, c = load(p1), load(p2), load(p3)
    H = a.shape[0]
    OFF12, DY, SEAM = 1472, 3, 1677      # sauma pylvään keskellä
    W = OFF12 + b.shape[1] - 0 + c.shape[1]
    # sävyt kohdalleen (hiekka-alue riveillä 560-880)
    b2 = gain_match(a, b, (560, 880), SEAM - 14, SEAM - OFF12 - 2)
    c2 = gain_match(b2, c, (560, 880), b.shape[1] - 12, 2)
    canvas = np.zeros((H, W, 4), np.float32)
    def put(img, x0, dy=0):
        f = img.astype(np.float32); f[..., :3] *= f[..., 3:4] / 255.0   # premult
        if dy:
            f = np.roll(f, -dy, axis=0); f[-dy:] = f[-dy - 1:-dy]   # kuva ylös dy, alareuna toistetaan
        return f
    wa = np.zeros(W, np.float32); wb = np.zeros(W, np.float32); wc = np.zeros(W, np.float32)
    FE = 10
    xs = np.arange(W, dtype=np.float32)
    wa = np.clip((SEAM + FE - xs) / (2 * FE), 0, 1); wa[a.shape[1]:] = 0
    wb_in = np.clip((xs - (SEAM - FE)) / (2 * FE), 0, 1)
    seam23 = OFF12 + b.shape[1]
    wb = wb_in * (xs < seam23)
    wc = (xs >= seam23).astype(np.float32)
    pa, pb, pc = put(a, 0), put(b2, OFF12, DY), put(c2, seam23)
    canvas[:, :a.shape[1]] += pa * wa[None, :a.shape[1], None]
    canvas[:, OFF12:OFF12 + b.shape[1]] += pb * wb[None, OFF12:OFF12 + b.shape[1], None]
    canvas[:, seam23:] += pc * wc[None, seam23:, None]
    A = canvas[..., 3:4]
    wsum = (wa[None, :, None] * 0 + 1)  # painot summautuvat 1:een
    rgb = np.where(A > 1, canvas[..., :3] / np.maximum(A / 255.0, 1e-3), 0)
    fg = np.dstack([rgb.clip(0, 255), A.clip(0, 255)])
    if transparent:   # taivas ja meri tulevat parallaksikerroksesta (Uccopulcon meri): kuva jää läpinäkyväksi
        out_img = fg.clip(0, 255).astype(np.uint8)
        # reunavärin puhdistus: läpinäkyvien reunapikselien väri otetaan lähimmästä peitteisestä
        a8 = out_img[..., 3]
        solid = (a8 >= 250).astype(np.uint8)
        k = cv2.dilate(out_img[..., :3] * solid[..., None], np.ones((5, 5), np.uint8))
        edge = (a8 < 250) & (a8 > 0)
        out_img[..., :3][edge] = np.where((k.sum(axis=2) > 0)[..., None], k, out_img[..., :3])[edge]
        cv2.imwrite(out, out_img)
        print('läpinäkyvä', W, H)
        return
    # tausta: aamu-/iltataivas ja meri horisontissa
    y = np.arange(H, dtype=np.float32)[:, None]
    top = np.array([200, 140, 70], np.float32); mid = np.array([210, 215, 235], np.float32)   # BGR: ylhäällä sininen, horisontissa vaalea lämmin
    t = np.clip(y / 470.0, 0, 1)
    sky = top[None, None, :] * (1 - t[..., None]) + np.array([225, 225, 245], np.float32)[None, None, :] * t[..., None]
    sky = np.repeat(sky, W, axis=1)
    sea = np.zeros((H, W, 3), np.float32); sea[:] = (170, 170, 40)    # turkoosi meri
    seaband = ((y >= 440)).astype(np.float32)
    bgc = sky * (1 - seaband[..., None]) + sea * seaband[..., None]
    # meren kevyt vaihtelu
    rng = np.random.RandomState(3)
    stripes = (np.sin(xs[None, :] / 37.0 + y / 6.0) * 6).astype(np.float32)
    bgc += (stripes * seaband)[..., None]
    al = fg[..., 3:4] / 255.0
    res = (fg[..., :3] * al + bgc * (1 - al))
    res = np.dstack([res.clip(0, 255), np.full((H, W), 255, np.float32)]).astype(np.uint8)
    cv2.imwrite(out, res)
    print("leveys", W, "korkeus", H)
    if prev:
        cv2.imwrite(prev, cv2.resize(res[..., :3], (2400, int(2400 * H / W)), interpolation=cv2.INTER_AREA))
        cv2.imwrite(prev.replace(".jpg", "_s12.jpg"), res[:, 1500:1900, :3]); cv2.imwrite(prev.replace(".jpg", "_s23.jpg"), res[:, seam23 - 200:seam23 + 200, :3])

if __name__ == "__main__": main(*sys.argv[1:])
