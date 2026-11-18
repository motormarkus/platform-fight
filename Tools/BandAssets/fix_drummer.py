"""Rumpalin vyötärön/lantion aukon korjaus alkuperäisestä Viggle-videosta (F:/total fist tavaraa/viggle-a95dbf67-...mp4).
Aukko syntyi, kun videon 'viggle.ai Free AI Video' -vesileima (valkoinen teksti rumpalin vatsan päällä) siivottiin pois ja sen mukana lähti rumpalin vartalo.
Täällä: videon kuvista poistetaan teksti (inpaint) vartalon alueella, kuva muunnetaan arkin ruutuun (skaala 0,714, siirto 86,8 / 37,8) ja aukko täytetään vain, jossa video on vartaloa (ei taustaa).
Lähtö: Tools/BandAssets/bandi_ennen_korjausta.png -> Assets/Sprites/bandi.png (+ valkoisten jalusten siivous)."""
import av, numpy as np, cv2, sys
from PIL import Image
VID = "F:/total fist tavaraa/viggle-a95dbf67-796e-4cdc-a406-7dc583b82c64.mp4"
SRC = "Tools/BandAssets/bandi_ennen_korjausta.png"
DST = "Assets/Sprites/bandi.png"
S, TX, TY = 0.7138, 86.8, 37.8
M = np.float32([[S, 0, TX], [0, S, TY]])
V = [x.to_ndarray(format="rgb24") for x in av.open(VID).decode(video=0)]
sheet = np.array(Image.open(SRC).convert("RGBA")); out = sheet.copy()
# vartalon alue videossa (jakkun alaosa ja lantio vesileiman alla), johon vesileiman valkoiset pikselit kuuluvat vartaloon
RX0, RX1, RY0, RY1 = 20, 200, 335, 432
def clean_video(f):
    g = f.copy(); sub = g[RY0:RY1, RX0:RX1]
    white = (sub.min(2) > 228)
    # vartaloon kuuluvat vain valkoiset, jotka ovat vartalon sisällä: sulkeminen (jättää ulkopuolisen taustan pois)
    body = (sub.min(2) < 200).astype(np.uint8); closed = cv2.morphologyEx(body, cv2.MORPH_CLOSE, np.ones((31, 31), np.uint8))
    mask = (white & (closed > 0)).astype(np.uint8) * 255
    mask = cv2.dilate(mask, np.ones((5, 5), np.uint8))
    sub[:] = cv2.inpaint(np.ascontiguousarray(sub), mask, 6, cv2.INPAINT_TELEA)
    return g
def cell_white(k):
    r, c = divmod(k, 4); x = sheet[r*448:(r+1)*448, c*768:(c+1)*768]; a = x[..., 3:] / 255.0
    return (x[..., :3] * a + 255 * (1 - a)).astype(np.float32), x[..., 3]
# sheet-alue, johon täyttö rajataan (videon vartalon alue arkin koordinaateissa)
fx0, fx1, fy0, fy1 = [int(v) for v in (RX0*S+TX, RX1*S+TX, RY0*S+TY, RY1*S+TY)]
fx1 = min(fx1, 250); fy0 = 258; fy1 = min(fy1, 346)
print("täyttöalue arkissa", fx0, fx1, fy0, fy1)
CV = {}
for k in range(39):
    cw, a = cell_white(k)
    best = (1e18, 0)
    for i in range(0, len(V)):
        wv = cv2.warpAffine(V[i], M, (768, 448), flags=cv2.INTER_AREA, borderValue=(255, 255, 255)).astype(np.float32)
        m = (a > 250); m[:, :] &= True
        m[:, 330:] = False; m[fy0-6:fy1+10, fx0-6:fx1+10] = False   # vertailu vain rumpalin alueelta, ei aukon seutua/vesileimaa
        err = float(np.mean((wv[m] - cw[m]) ** 2))
        if err < best[0]: best = (err, i)
    i = best[1]
    if i not in CV: CV[i] = cv2.warpAffine(clean_video(V[i]), M, (768, 448), flags=cv2.INTER_AREA, borderValue=(255, 255, 255))
    wv = CV[i]
    r, c = divmod(k, 4); cell = out[r*448:(r+1)*448, c*768:(c+1)*768]
    region = np.zeros((448, 768), bool); region[fy0:fy1, fx0:fx1] = True
    body = (wv.min(2) < 200)                      # video: vartaloa (ei valkoista taustaa)
    fill = region & (cell[..., 3] < 200) & body
    cell[fill, :3] = wv[fill]; cell[fill, 3] = 255
    print(k, "video", i, "virhe", round(best[1] if False else best[0], 1), "täytetty", int(fill.sum()))
# valkoisten jalustojen siivous (kuten aiemmin)
boxes = [(104,156,388,440),(154,190,378,432),(284,324,388,428),(118,152,334,354),(160,198,340,368),(210,236,380,398)]
for k in range(39):
    r, c = divmod(k, 4); cell = out[r*448:(r+1)*448, c*768:(c+1)*768]
    rgb = cell[..., :3].astype(int); white = (rgb.min(2) > 178) & ((rgb.max(2) - rgb.min(2)) < 40) & (cell[..., 3] > 40)
    m = np.zeros((448, 768), np.uint8)
    for (x0, x1, y0, y1) in boxes: m[y0:y1, x0:x1] = 1
    kill = white & (m > 0); kill = cv2.dilate(kill.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    kill &= (m > 0) & ((rgb.min(2) > 140) & ((rgb.max(2) - rgb.min(2)) < 48)); cell[kill, 3] = 0
Image.fromarray(out).save(DST); print("valmis")
