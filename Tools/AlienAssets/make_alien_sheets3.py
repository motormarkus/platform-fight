"""Koe-olento, kolmas erä: loikka (syöksy + tallaus maahan). Videosta hännän nuija katoaa kuvasta ~38 alkaen: nuija liitetään takaisin
kuvan 0 nuijasta (kopio kääntäen hännän kärkeen), jotta häntä näyttää samalta kuin idlessä.
Ajo: python Tools/AlienAssets/make_alien_sheets3.py <loikka.mp4> [ulos-kansio] [puraisu.mov]
Ruutu 1536 x 960, pivot 780 (torso-ankkuri: vartalo pysyy ruudussa paikallaan, aivot liikuttavat vaakasuunnassa)."""
import os, sys
import numpy as np
import cv2
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import make_alien_sheets as a
m = a.m

W, H, PIV = 1536, 960, 780
m.DIMS["a_loikka"] = (W, PIV, H)
m.DIMS["a_puraisu"] = (1664, 780, 704)
R = 36                      # nuijan säde videossa (px)
FIRST_PATCH = 38            # tästä kuvasta alkaen nuija puuttuu
IDX = [0, 6, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 68, 72, 76, 80, 84, 88, 92, 96, 100, 106, 112, 120]


def tail(im):
    """Hännän kärki (kauimpana oikeimmasta pisteestä) ja suunta ulospäin."""
    ys, xs = np.where(im[..., 3] > 128)
    ri = np.argmax(xs); d = np.hypot(xs - xs[ri], ys - ys[ri]); ti = np.argmax(d)
    T = np.array([xs[ti], ys[ti]], np.float32)
    near = np.hypot(xs - T[0], ys - T[1]) < 45
    M = np.array([xs[near].mean(), ys[near].mean()], np.float32)
    v = T - M; v /= max(np.hypot(*v), 1e-3)
    return T, v


def make_patch(im0):
    T, v = tail(im0); C = T + v * (4 - R)
    S = 2 * R + 12
    x0, y0 = int(round(C[0])) - S // 2, int(round(C[1])) - S // 2
    p = im0[y0:y0 + S, x0:x0 + S].copy()
    yy, xx = np.mgrid[0:S, 0:S]; r = np.hypot(xx - S // 2, yy - S // 2)
    circ = np.clip((R + 3 - r) / 2.0, 0, 1)
    p[..., 3] = (p[..., 3].astype(np.float32) * circ).astype(np.uint8)
    return p, float(np.arctan2(v[1], v[0]))


def add_club(im, patch, ang0):
    T, v = tail(im); C = T + v * (4 - R)
    S = patch.shape[0]
    rot = cv2.getRotationMatrix2D((S / 2, S / 2), -np.degrees(float(np.arctan2(v[1], v[0])) - ang0), 1.0)
    rp = cv2.warpAffine(patch, rot, (S, S), flags=cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0))
    x0, y0 = int(round(C[0])) - S // 2, int(round(C[1])) - S // 2
    out = im.copy()
    h, w = out.shape[:2]
    sx0, sy0 = max(0, -x0), max(0, -y0); ex, ey = min(S, w - x0), min(S, h - y0)
    src = rp[sy0:ey, sx0:ex].astype(np.float32); dst = out[y0 + sy0:y0 + ey, x0 + sx0:x0 + ex].astype(np.float32)
    sa = src[..., 3:4] / 255.0; da = dst[..., 3:4] / 255.0
    oa = sa + da * (1 - sa)
    rgb = np.where(oa > 1e-3, (src[..., :3] * sa + dst[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-3), 0)
    out[y0 + sy0:y0 + ey, x0 + sx0:x0 + ex] = np.dstack([rgb, oa * 255.0]).clip(0, 255).astype(np.uint8)
    return out


def main(path, outs):
    V = m.load(path)
    patch, ang0 = make_patch(V[0])
    for i in range(FIRST_PATCH, len(V)): V[i] = add_club(V[i], patch, ang0)
    m.setdims("a_loikka")
    cells = [m.cell(V[i], "torso", a.K, a.FLOOR) for i in IDX]
    m.save(cells, f"{outs}/alien_loikka.png")
    print("loikka", len(cells), "kuvaa; valmistelu 0-8, ponnistus 9-12, lento 12-17, lasku 18-23 (tassut iskeytyvät maahan kuvissa 88-96 = indeksit 21-23), palautuminen 24-27")


def puraisu(path, outs):
    """Puraisu (video 97 kuvaa): kyykky 8-21, leuat sulkeutuvat esineen (uhrin sijainen) ympärille 22-23, ravistus 24-76, irtipäästö/karjaisu 79-96.
    Esine (harmaa keihäs) jää kuviin: uhri roikkuu pelissä sen edessä. Kuva 78 (esine lentää pois) jätetty pois."""
    V = m.load(path)
    m.setdims("a_puraisu")
    idx = [8, 12, 16, 19, 21, 23] + list(range(24, 77, 2)) + [79, 80, 82, 84, 88, 92, 96]
    m.save([m.cell(V[i], "low", a.K, 680) for i in idx], f"{outs}/alien_puraisu.png")
    print("puraisu", len(idx), "kuvaa; tartunta indeksi 5, ravistus 6-32, heitto indeksi 33")


if __name__ == "__main__":
    out = sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset"
    if sys.argv[1] != "-": main(sys.argv[1], out)
    if len(sys.argv) > 3: puraisu(sys.argv[3], out)
