"""Lizin klippi 4: kengät puuttuvat kuvista 10-22 ja 47-50 (mustat kengät on keyattu pois). Kengät liimataan takaisin kuvasta 51 (kengät näkyvät):
jokaiselle jalalle (vasen/oikea) etsitään nilkkahihna ja varpaankärki, ja kengän kuva muunnetaan (kierto, skaala, siirto) niiden mukaan.
Alkuperäiset kuvat kopioidaan kansioon Tools/LizAssets/alkuperaiset (kerran), ajo käyttää aina niitä lähteenä.
Ajo: python Tools/LizAssets/fix_liz_shoes.py [--preview]"""
import os, sys, shutil
import numpy as np, cv2
from PIL import Image
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SRC = os.path.join(ROOT, "Assets", "Sprites", "Liz"); BAK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "alkuperaiset")
BAD = list(range(10, 23)) + [47, 48, 49, 50]; REF = 51
SIDES = {"L": (12, 74), "R": (74, 124)}      # jalan x-alue kuvassa (tanko x>=125)
os.makedirs(BAK, exist_ok=True)
def path(i, d=SRC): return os.path.join(d, f"liz_4_{i:02d}.png")
for i in BAD + [REF]:
    if not os.path.exists(path(i, BAK)): shutil.copy(path(i), path(i, BAK))
def load(i): return np.array(Image.open(path(i, BAK)).convert("RGBA"))
def dark(a): return (a[..., 3] > 150) & (a[..., :3].astype(int).sum(2) < 170)
def strap_and_tip(a, side, ref=False):
    x0, x1 = SIDES[side]; m = dark(a).astype(np.uint8); m[:, :x0] = 0; m[:, x1:] = 0; m[:270] = 0
    n, lab, st, cen = cv2.connectedComponentsWithStats(m, connectivity=8)
    cand = [k for k in range(1, n) if (st[k, 4] >= 12 and (st[k, 4] <= 130 or ref)) and 285 <= cen[k][1] <= 345 and st[k, 2] >= 6]
    if not cand: return None
    if ref: k = max(cand, key=lambda k: st[k, 4])
    else: k = min(cand, key=lambda k: cen[k][1] - 0.001 * st[k, 4])   # ylin riittävän suuri palkki = nilkka
    return k, lab, st, cen
def foot_landmarks_bare(a, side):
    r = strap_and_tip(a, side)
    if r is None: return None
    k, lab, st, cen = r; sx, sy = cen[k]
    alpha = a[..., 3] > 100; ys, xs = np.where(alpha); sel = (abs(xs - sx) <= 28) & (ys >= sy + 3) & (xs >= SIDES[side][0] - 4) & (xs < SIDES[side][1] + 4) & (ys < 346)
    if sel.sum() < 5: return None
    xs, ys = xs[sel], ys[sel]; d = (xs - sx) ** 2 + (ys - sy) ** 2; j = np.argsort(d)[-6:]
    return np.array([sx, sy]), np.array([xs[j].mean(), ys[j].mean()])
LAST_V = {"L": np.array([1.0, 40.0]), "R": np.array([25.0, 38.0])}
def skin_bottom(a, side):
    """Ei hihnaa: nilkka = säären alin kiinteä ihorivi; varvas = nilkka + edellinen jalan vektori."""
    x0, x1 = SIDES[side]; sub = a[:, x0:x1].astype(int)
    skin = (sub[..., 3] > 150) & (sub[..., 0] > 170) & (sub[..., 2] < 175) & (sub[..., 0] - sub[..., 2] > 40)
    last = None
    for y in range(250, 346):
        if skin[y].sum() >= 9: last = y
    if last is None: return None
    xs = np.where(skin[last])[0]; S = np.array([x0 + xs.mean(), float(last)]); return S, S + LAST_V[side]
ref = load(REF)
def ref_shoe(side):
    r = strap_and_tip(ref, side, ref=True); k, lab, st, cen = r
    mask = (lab == k)
    ys, xs = np.where(mask); top = ys.min(); sel = ys <= top + 5; strap = np.array([xs[sel].mean(), ys[sel].mean()])
    d = (xs - strap[0]) ** 2 + (ys - strap[1]) ** 2; j = np.argsort(d)[-8:]; tip = np.array([xs[j].mean(), ys[j].mean()])
    patch = ref.copy(); m2 = cv2.dilate(mask.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)
    patch[~m2] = 0; return patch, strap, tip
shoes = {s: ref_shoe(s) for s in SIDES}
prev = []
for i in BAD:
    a = load(i); out = a.astype(np.float32)
    for side in SIDES:
        lm = foot_landmarks_bare(a, side)
        if lm is None:
            lm = skin_bottom(a, side)
            if lm is None: print(i, side, "ei jalkaa"); continue
            print(i, side, "hihna puuttuu: säären pää")
        else: LAST_V[side] = lm[1] - lm[0]
        (sf, tf), (patch, sr, tr) = lm, shoes[side]
        vf, vr = tf - sf, tr - sr
        ang = np.degrees(np.arctan2(vf[1], vf[0]) - np.arctan2(vr[1], vr[0])); sc = float(np.clip(np.linalg.norm(vf) / max(np.linalg.norm(vr), 1e-3), 0.85, 1.25))
        M = cv2.getRotationMatrix2D((float(sr[0]), float(sr[1])), -ang, sc); M[:, 2] += sf - sr
        w = cv2.warpAffine(patch, M, (a.shape[1], a.shape[0]), flags=cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0)).astype(np.float32)
        al = w[..., 3:4] / 255.0; out[..., :3] = out[..., :3] * (1 - al) + w[..., :3] * al; out[..., 3:4] = np.maximum(out[..., 3:4], w[..., 3:4])
    Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA").save(path(i)); prev.append(i)
    print(i, "korjattu")
if "--preview" in sys.argv:
    S = 3; tiles = []
    for i in BAD:
        im = Image.open(path(i)).convert("RGBA").crop((10, 250, 140, 352)); bg = Image.new("RGBA", im.size, (70, 70, 70, 255)); bg.alpha_composite(im); tiles.append(bg.convert("RGB").resize((130 * S, 102 * S), Image.NEAREST))
    sheet = Image.new("RGB", (130 * S * 6, 102 * S * 3))
    for k, t in enumerate(tiles): sheet.paste(t, ((k % 6) * 130 * S, (k // 6) * 102 * S))
    sheet.save(os.path.join(os.environ.get("TEMP", "."), "liz_fix_preview.png"))
