"""Heitettävä kivi: leikataan heittovideon ilmassa lentävästä kuvasta (kivi irtoaa kädestä, erillinen laikku kuvissa 77–80).
Ajo: python Tools/MongolAssets/cut_rock.py <idle_heitto.mov> [kuva=78] [ulos=Assets/Sprites/Viholliset/mongoli_kivi.png]
Kuvan 78 kivi on ~76x70 px; skaalataan kuten sheetit (0,81) ja keskitetään 96x96-ruutuun."""
import sys, cv2, numpy as np

def main(path, frame=78, out="Assets/Sprites/Viholliset/mongoli_kivi.png", k=0.81):
    c = cv2.VideoCapture(path); c.set(cv2.CAP_PROP_POS_FRAMES, frame); ok, fr = c.read()
    B, G, R = [fr[..., i].astype(int) for i in range(3)]
    fg = (~((G > R + 35) & (G > B + 35))).astype(np.uint8)
    fg = cv2.morphologyEx(fg, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    n, lab, st, cen = cv2.connectedComponentsWithStats(fg)
    comps = sorted([(st[i, 4], i) for i in range(1, n)], reverse=True)
    area, idx = comps[1]                       # suurin = hahmo, toiseksi suurin = kivi
    mask = (lab == idx).astype(np.uint8)
    x, y, w, h = st[idx, 0], st[idx, 1], st[idx, 2], st[idx, 3]
    pad = 3
    sub = fr[y - pad:y + h + pad, x - pad:x + w + pad].copy(); m = mask[y - pad:y + h + pad, x - pad:x + w + pad]
    m = cv2.erode(m, np.ones((3, 3), np.uint8))                      # syö vihreän reunan
    b, g, r = sub[..., 0].astype(int), sub[..., 1].astype(int), sub[..., 2].astype(int)
    sub[..., 1] = np.minimum(g, np.maximum(r, b) + 8).astype(np.uint8)   # vihreän valumisen poisto
    a = cv2.GaussianBlur(m.astype(np.float32), (3, 3), 0.7)
    rgba = np.dstack([cv2.cvtColor(sub, cv2.COLOR_BGR2RGB), (a * 255).astype(np.uint8)])
    f = rgba.astype(np.float32); al = f[..., 3:4] / 255.0; f[..., :3] *= al
    r2 = cv2.resize(f, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al2 = r2[..., 3:4] / 255.0; r2[..., :3] = np.where(al2 > 1e-3, r2[..., :3] / np.maximum(al2, 1e-3), 0)
    r2 = r2.clip(0, 255).astype(np.uint8)
    cell = np.zeros((96, 96, 4), np.uint8); hh, ww = r2.shape[:2]
    cell[48 - hh // 2:48 - hh // 2 + hh, 48 - ww // 2:48 - ww // 2 + ww] = r2
    cv2.imwrite(out, cv2.cvtColor(cell, cv2.COLOR_RGBA2BGRA)); print("kivi", ww, "x", hh, "px ->", out)

if __name__ == "__main__":
    main(sys.argv[1], int(sys.argv[2]) if len(sys.argv) > 2 else 78, sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset/mongoli_kivi.png")
