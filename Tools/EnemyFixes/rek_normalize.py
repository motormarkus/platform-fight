"""Rekkamies: kaikki kuvat samaan kokoon (lippiksen leveys silmämääräisesti mitattu sheeteittäin, tavoite 72 px, vaimennettu) ja kiilto pois
(kirkkaimman 15 % kirkkausjakauma sovitetaan rauhallisen idlen (calm) jakaumaan). Alkuperäiset: Tools/EnemyFixes/orig_rekkamies.
Ajo (projektin juuresta): python Tools/EnemyFixes/rek_normalize.py"""
import os, glob
import numpy as np, cv2
SRC = 'Tools/EnemyFixes/orig_rekkamies/'; DST = 'Assets/Sprites/Viholliset/'
CW, CH = 512, 448
SCALE = {   # sheetti: kerroin kaikille ruuduille (ankkuri: ruudun alakeskikohta)
    'calm': 1.04, 'taistelu': 0.93, 'iskut': 1.055, 'poyta': 1.13, 'juoksu': 1.0, 'alakoukku': 1.0, 'kaatuminen': 0.99,
    'piirakkakavely': 0.99, 'ylosnousu': 0.97, 'kavely': 0.965, 'taklaus': 0.95, 'heitto': 0.95, 'heitetty': 0.88,
}
def scale_cell(cell, f):
    if abs(f - 1) < 1e-3: return cell
    a = cell.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    M = np.array([[f, 0, CW * 0.5 * (1 - f)], [0, f, CH * (1 - f)]], np.float32)
    w = cv2.warpAffine(a, M, (CW, CH), flags=cv2.INTER_AREA if f < 1 else cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0))
    al = np.clip(w[..., 3:4], 0, 255); rgb = np.where(al > 1, w[..., :3] / np.maximum(al / 255.0, 1e-3), 0)
    return np.dstack([rgb.clip(0, 255), al]).astype(np.uint8)

def vchan(img):
    return cv2.cvtColor(img[..., :3], cv2.COLOR_BGR2HSV)

def zones(f):
    """ruudun ryhmät: farkut (sininen), huppari (tumma), iho (yläosa), laikut (farkkujen vaalea hankaus/kiilto alaosassa)"""
    hsv = vchan(f); H, S, V = [hsv[..., i].astype(int) for i in range(3)]; a = f[..., 3] > 200
    ys, xs = np.nonzero(a)
    if len(ys) == 0: return None
    top, bot = ys.min(), ys.max(); mid = top + int((bot - top) * 0.50)
    yy = np.arange(f.shape[0])[:, None] * np.ones((1, f.shape[1]), int)
    jeans = a & (H >= 90) & (H <= 130) & (S > 40)
    hood = a & (V < 175) & ~jeans & ((H >= 120) | (S < 45)) & (yy < bot - 40)
    skin = a & (H <= 25) & (S >= 70) & (V >= 100) & (yy < mid)
    blot = a & ~jeans & (V > 105) & (yy >= top + int((bot - top) * 0.40)) & (yy < bot - 70) & (cv2.boxFilter(jeans.astype(np.float32), -1, (27, 27)) > 0.50)   # vain farkkujen ympäröimänä (ei kädet)
    return jeans, hood, skin, blot, hsv

QS = np.linspace(0.0, 1.0, 51)
def calm_ref():
    f = cv2.imread(SRC + 'rekkamies_calm.png', cv2.IMREAD_UNCHANGED); cols = f.shape[1] // CW; ref = {k: ([], []) for k in ('jeans', 'hood', 'skin')}
    for r in range(f.shape[0] // CH):
        for c in range(cols):
            cell = f[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
            z = zones(cell)
            if z is None: continue
            for k, m in zip(('jeans', 'hood', 'skin'), z[:3]):
                ref[k][0].append(z[4][..., 1][m]); ref[k][1].append(z[4][..., 2][m])
    return {k: (np.quantile(np.concatenate(v[0]), QS), np.quantile(np.concatenate(v[1]), QS)) for k, v in ref.items()}

def match_sheet(sheet, ref):
    """kiilto pois: farkkujen, hupparin ja ihon sävyvoimakkuus (S) ja kirkkaus (V) sovitetaan calm-kuvien jakaumaan (koko sheetin jakaumasta), vaaleat hankauslaikut täytetään ympäristöstä"""
    cols = sheet.shape[1] // CW; out = sheet.copy(); cells = []
    for r in range(sheet.shape[0] // CH):
        for c in range(cols):
            cell = sheet[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
            if cell[..., 3].max() == 0: continue
            cells.append((r, c, zones(cell)))
    maps = {}
    for gi, k in enumerate(('jeans', 'hood', 'skin')):
        S_ = [z[4][..., 1][z[gi]] for _, _, z in cells if z is not None]; V_ = [z[4][..., 2][z[gi]] for _, _, z in cells if z is not None]
        S_ = np.concatenate(S_) if S_ else np.array([]); V_ = np.concatenate(V_) if V_ else np.array([])
        if len(S_) < 500: continue
        maps[k] = (np.quantile(S_, QS), np.quantile(V_, QS))
    for r, c, z in cells:
        if z is None: continue
        jeans, hood, skin, blot, hsv = z; hsv = hsv.copy()
        cell = out[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
        for gi, k in enumerate(('jeans', 'hood', 'skin')):
            if k not in maps: continue
            m = z[gi]; own_s, own_v = maps[k]; ref_s, ref_v = ref[k]
            ns = np.interp(hsv[..., 1][m].astype(np.float32), own_s, ref_s) if k != 'skin' else np.minimum(hsv[..., 1][m], np.interp(hsv[..., 1][m].astype(np.float32), own_s, ref_s))
            nv = np.minimum(hsv[..., 2][m].astype(np.float32), np.interp(hsv[..., 2][m].astype(np.float32), own_v, ref_v)) if k == 'skin' else np.interp(hsv[..., 2][m].astype(np.float32), own_v, ref_v)
            hsv[..., 1][m] = np.clip(ns, 0, 255).astype(np.uint8); hsv[..., 2][m] = np.clip(nv, 0, 255).astype(np.uint8)
        rgb = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)
        mask = blot & ~jeans
        if mask.any():
            mk = cv2.dilate(mask.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool) & (cell[..., 3] > 0)
            src = (jeans & ~mk).astype(np.float32); den = cv2.GaussianBlur(src, (0, 0), 9)
            fill = np.stack([cv2.GaussianBlur(rgb[..., ch].astype(np.float32) * src, (0, 0), 9) for ch in range(3)], -1) / np.maximum(den[..., None], 1e-3)
            ok = mk & (den > 0.02)
            # kuviointi: pieni vaihtelu alkuperäisen kirkkauden mukaan, ettei täyttö ole tasainen
            lum = cv2.cvtColor(rgb, cv2.COLOR_BGR2GRAY).astype(np.float32); lum_blur = cv2.GaussianBlur(lum, (0, 0), 3)
            soft = cv2.GaussianBlur(ok.astype(np.float32), (0, 0), 2.5)[..., None]
            rgb = (rgb * (1 - soft) + np.clip(fill + (lum - lum_blur)[..., None] * 0.6, 0, 255) * soft).astype(np.uint8)
        a = cell[..., 3] > 0
        cell[..., :3][a] = rgb[a]
    return out

def main():
    ref = calm_ref()
    for p in sorted(glob.glob(SRC + 'rekkamies_*.png')):
        n = os.path.basename(p)[:-4]; key = n.replace('rekkamies_', '')
        im = cv2.imread(p, cv2.IMREAD_UNCHANGED); H, W = im.shape[:2]; cols = W // CW
        out = im.copy(); f = SCALE.get(key, 1.0)
        if abs(f - 1) > 1e-3:
            for r in range(H // CH):
                for c in range(cols):
                    cell = im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
                    if cell[..., 3].max() == 0: continue
                    out[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = scale_cell(cell, f)
        out = match_sheet(out, ref)
        cv2.imwrite(DST + n + '.png', out); print(n, 'skaala', f)
main()
