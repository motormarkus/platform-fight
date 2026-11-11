"""Rex: potkut. Video A (vihreä tausta, 2 korkeaa potkua), video B (vaalea tausta, etupotku) -> Assets/Sprites/Viholliset/rex_potku1/2/3.png.
potku1 = A 0-33 (korkea potku, osuma kuvassa 4), potku2 = A 54-84 (korkea potku, osuma 4), potku3 = B 31-98 (etupotku, osuma 8). Videon A loppu (112-119, loikkapotku) katkeaa, ei käytetä.
Ruudut kuten rex_isku*: hahmo ~640 px, jalat 16 px ylös.
Ajo: python Tools/RexAssets/make_rex_kicks.py <A.mov> <B.mov> [ulos-kansio]"""
import os, sys
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_potku1": (1280, 320, 768), "v_potku2": (1280, 320, 768), "v_potku3": (1280, 320, 768)})

def key_light(bgr):
    """Vaalea (lähes valkoinen) tausta -> alfa: taustan väri reunoilta, etäisyys, suurin yhtenäinen alue, reiät täytetään."""
    h, w = bgr.shape[:2]
    border = np.concatenate([bgr[:8].reshape(-1, 3), bgr[-8:].reshape(-1, 3), bgr[:, :8].reshape(-1, 3), bgr[:, -8:].reshape(-1, 3)])
    bg = np.median(border, axis=0)
    d = np.abs(bgr.astype(np.float32) - bg).max(axis=2)
    mask = (d > 22).astype(np.uint8)
    n, lab, stats, _ = cv2.connectedComponentsWithStats(mask, 8)
    if n > 1:
        big = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA])); mask = (lab == big).astype(np.uint8)
    inv = (1 - mask).astype(np.uint8)   # tausta ja reiät: vain pienet reiät (esim. ketju) täytetään, jalkojen välinen tausta jää läpinäkyväksi
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats(inv, 4)
    for j in range(1, n2):
        if st2[j, cv2.CC_STAT_AREA] < 150: mask[lab2 == j] = 1
    mask = cv2.erode(mask, np.ones((3, 3), np.uint8), iterations=1)
    a = cv2.GaussianBlur(mask.astype(np.float32), (0, 0), 1.0)
    out = np.dstack([bgr[..., ::-1], (a * 255).astype(np.uint8)])   # m.load antaa RGB-värit
    return out

def main(vA, vB, outs):
    A = m.load(vA)
    B = [key_light(f.to_ndarray(format="bgr24")) for f in av.open(vB).decode(video=0)]
    def kf(v):
        x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); print("mittakaavat", round(kA, 3), round(kB, 3), len(A), len(B))
    def sheet(name, vid, idx, k, floor):
        m.setdims("v_" + name)
        m.save([m.cell(vid[i], "low", k, floor) for i in idx], f"{outs}/rex_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("potku1", A, [0, 3, 6, 9, 12, 15, 18, 21, 24, 27, 30], kA, fA)
    sheet("potku2", A, [54, 57, 60, 63, 66, 69, 72, 75, 78, 81, 84], kA, fA)
    sheet("potku3", B, [31, 36, 41, 46, 51, 56, 62, 67, 72, 77, 82, 87, 93, 98], kB, fB)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
