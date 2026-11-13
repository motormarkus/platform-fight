"""Warden uppercut-hyppy 1440x1080-videosta (vihreä tausta): kiinteä kamera -> kaikki ruudut samalla mittakaavalla ja samalla lattia/x-ankkurilla (ei ruutukohtaista keskitystä = ei tärinää).
Ajo: python Tools/PrisonAssets/make_warden_uppercut.py <video.mov> [ulos-kansio]"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
CW, CH, FOOT, AX, FLOOR = 896, 1152, 16, 700.0, 1063.0
def place(im, k):
    M = np.float32([[k, 0, CW / 2 - AX * k], [0, k, CH - FOOT - FLOOR * k]])
    im = np.dstack([im[..., :3], im[..., 3]]); out = cv2.warpAffine(im, M, (CW, CH), flags=cv2.INTER_LANCZOS4, borderValue=(0, 0, 0, 0))
    rgb = out[..., :3].astype(np.float32); bl = cv2.GaussianBlur(rgb, (0, 0), 1.6)
    out[..., :3] = np.clip(rgb + 0.7 * (rgb - bl), 0, 255).astype(np.uint8)   # kevyt teroitus
    out[out[..., 3] < 8] = 0
    return out
def key_soft(bgr):
    """Pehmeä vihreän avaus: pehmeä alfa reunoille, vihreän heijastuksen poisto, ja pienet suljetut vihreät alueet (mitalinauhat) säilytetään."""
    f = bgr.astype(np.float32); b, g, r = f[..., 0], f[..., 1], f[..., 2]
    d = g - np.maximum(r, b)                                  # vihreys
    bgm = (d > 60).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(bgm, 4)
    keep = np.zeros(n, bool)
    edge = np.concatenate([f[0], f[-1], f[:, 0], f[:, -1]]); bgc = np.median(edge, axis=0)   # taustan väri reunoilta
    for j in range(1, n):
        x, y, w, h, a = st[j]
        border = x == 0 or y == 0 or x + w >= bgm.shape[1] or y + h >= bgm.shape[0]
        same = np.abs(f[lab == j].mean(axis=0) - bgc).max() < 35      # suljettu alue on taustaa vain jos väri on taustan värinen (mitalinauha ei ole)
        keep[j] = border or a > 700 or same
    bgreal = keep[lab] & (lab > 0)
    bgreal = cv2.dilate(bgreal.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)   # reunan pehmeä vyöhyke mukaan
    alpha = np.where(bgreal, 1.0 - np.clip((d - 25) / 70.0, 0, 1), 1.0)
    g2 = np.where(bgreal, np.minimum(g, np.maximum(r, b) + 4), g)                              # despill
    out = np.dstack([r, g2, b, alpha * 255]).astype(np.uint8)   # RGBA
    out[..., 3] = np.where(alpha < 0.04, 0, out[..., 3])
    return out

def load_soft(path):
    import av
    return [key_soft(fr.to_ndarray(format="bgr24")) for fr in av.open(path).decode(video=0)]

def main(path, outs):
    V = load_soft(path); x0, x1, y0, y1 = m.bbox(V[0]); k = 640.0 / (y1 - y0)   # bbox: alfa > 128
    def ok(i):
        a, b, c, d = m.bbox(V[i]); return c > 0 and d < 1080
    seq = list(range(24, 40, 3)) + list(range(40, 70, 2))           # lataus + ponnistus (efektit mukana)
    for i in (70, 74, 78, 82, 86, 88): seq.append(69 if i < 80 else 90)   # yläreunaan leikkautuvat huippuruudut: pidetään lähin ehjä pose
    seq += list(range(90, 100, 2)) + [104, 106, 108, 110, 112, 114, 116, 118]
    seq = [i for i in seq if i in (69, 90) or ok(i) or 104 <= i <= 112]
    cells = [place(V[i], k) for i in seq]
    bad = [seq[n] for n, c in enumerate(cells) if (c[0, :, 3] > 10).any() or (c[-1, :, 3] > 10).any() or (c[:, 0, 3] > 10).any() or (c[:, -1, 3] > 10).any()]
    print("ruudut", seq, "kuvia", len(seq), "leikkautuvat", bad)
    COLS = 4; rows = (len(cells) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for n, c in enumerate(cells): img[(n // COLS) * CH:(n // COLS + 1) * CH, (n % COLS) * CW:(n % COLS + 1) * CW] = c
    cv2.imwrite(f"{outs}/warden_uppercut.png", cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA))
if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
