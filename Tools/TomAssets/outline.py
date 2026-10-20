"""Tumma ääriviiva Tomin kuviin (alkuperäinen ohut viiva hukkui pienennyksessä ja Tom näytti irralliselta muiden hahmojen rinnalla).
Ensimmäinen versio (2 px, kovasta alfasta) oli liian paksu ja repaleinen. Nyt: alfa suurennetaan 3 ×, pehmennetään (poistaa avainnuksen rosoisuuden),
kynnystetään ja laajennetaan ~1,2 px:llä, pienennetään takaisin (sileä reuna). Käsitellään ruutu kerrallaan (muisti).
add_outline(sheet, cell_w, cell_h): arkki RGBA/BGRA uint8."""
import cv2
import numpy as np

OUTLINE_COLOR = np.array([24.0, 16.0, 14.0])   # lähes musta, lämmin
SS = 3                                          # ylösotto
WIDTH_PX = 1.2                                  # reunan leveys valmiissa kuvassa
SMOOTH = 1.3                                    # alfan pehmennys (pikseliä ylösotetussa kuvassa × 1)

def _outline_cell(cell):
    h, w = cell.shape[:2]
    a = cell[..., 3].astype(np.float32) / 255.0
    if a.max() <= 0: return cell
    up = cv2.resize(a, (w * SS, h * SS), interpolation=cv2.INTER_CUBIC)
    up = cv2.GaussianBlur(up, (0, 0), SMOOTH * SS * 0.55)      # sileä siluetti
    hard = (up > 0.45).astype(np.uint8)
    r = max(1, int(round(WIDTH_PX * SS)))
    dil = cv2.dilate(hard, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1)))
    oa = cv2.resize(dil.astype(np.float32), (w, h), interpolation=cv2.INTER_AREA)
    oa = np.clip(oa, 0, 1)
    out_a = a + oa * (1 - a)
    fg = cell[..., :3].astype(np.float32)
    oc = OUTLINE_COLOR.astype(np.float32)
    rgb = np.where(out_a[..., None] > 1e-4, (fg * a[..., None] + oc * (oa * (1 - a))[..., None]) / np.maximum(out_a[..., None], 1e-4), 0)
    return np.dstack([rgb.clip(0, 255), out_a * 255.0]).clip(0, 255).astype(np.uint8)

def add_outline(sheet, cell_w=768, cell_h=448):
    out = sheet.copy()
    for y in range(0, sheet.shape[0], cell_h):
        for x in range(0, sheet.shape[1], cell_w):
            out[y:y + cell_h, x:x + cell_w] = _outline_cell(sheet[y:y + cell_h, x:x + cell_w])
    return out
