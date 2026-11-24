"""Regina (Vegasin motellin jengiläinen): 3 vihreätaustaista videota (24 fps, 1280x720) + 2 kuvasarjaa (3x2 kuvaa, RGBA) -> Assets/Sprites/Viholliset/regina_<nimi>.png.
A iskut: jabi 4-12 (osuma 8), takasuora 12-22 (osuma 14), (jabi+suora 22-40 ei käytössä), etupotku 48-84 (osuma 63), sivupotku 84-118 (osuma 94),
   askelsuora 118-148 (osuma 130), syöksy 153-213 (osuma 170).
B: idle 0-50, kävely 58-122, juoksu 128-239.
C: osuma 12-34 (efektit; ei käytetä, osuma tulee kuvasarjasta), kaatuminen 36-104, nousu 106-212. Nousussa 132-186 hahmo on selin/vasemmalle -> peilataan.
Kuva 1 (kädet pään suojana) = suojaus, kuva 2 (pää taaksepäin) = osuma. Kaikki ruudut 1152 x 768 (kaatuminen 1152 x 1024), ankkuri keskellä, jalat 16 px ylhäältä alas.
Mittakaava: kaikki videot samassa koossa (seisova hahmo ~514 px); kuvasarjat skaalataan niin, että osumasarjan 1. kuva (perusasento) = idlen korkeus.
Ajo: python Tools/ReginaAssets/make_regina_sheets.py <A.mp4> <B.mp4> <C.mp4> <suojaus.png> <osuma.png> [ulos]"""
import os, sys
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
from key_video import key_frame

m.TARGET_H = 600.0
NAMES = ("idle", "kavely", "juoksu", "jabi", "takasuora", "askelsuora", "etupotku", "sivupotku", "syoksy", "osuma", "suojaus", "kaatuminen", "nousu")
for n in NAMES: m.DIMS["v_" + n] = (1152, 576, 1024 if n == "kaatuminen" else 768)


def despill(im):
    o = im.copy(); r, g, b = (o[..., i].astype(np.int16) for i in range(3))
    o[..., 1] = np.minimum(g, np.maximum(r, b) + 6).astype(np.uint8)
    # liike-epäterävä ääriviiva on sekoittunut vihreään -> oliivinvihreä reuna (kädet, jalat): reunavyöhykkeen kellanvihreät pikselit tummaksi ääriviivaksi
    band = cv2.dilate((o[..., 3] < 128).astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool) & (o[..., 3] > 0)
    r, g, b = (im[..., i].astype(np.float32) for i in range(3))
    olive = band & (g >= 0.88 * r) & (g > b + 22)
    lum = np.minimum(0.3 * r + 0.59 * g + 0.11 * b, 80.0)
    for i, f in enumerate((0.62, 0.48, 0.42)): o[..., i] = np.where(olive, (lum * f).astype(np.uint8), o[..., i])
    return o


def clean(im, frac=0.04):
    """Vihreä heijastus pois ja pienet irralliset alueet (osumaefektin kipinät, roskapisteet) pois."""
    im = despill(im)
    a = (im[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, 8)
    out = im.copy()
    if n > 2:
        big = st[1:, cv2.CC_STAT_AREA].max()
        keep = np.zeros(n, bool); keep[1:] = st[1:, cv2.CC_STAT_AREA] >= frac * big
        near = cv2.dilate(keep[lab].astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
        out[~near, 3] = 0                                     # myös pehmeät reunat irtoalueiden ympäriltä
    out[out[..., 3] < 8] = 0
    return out


def load(p):
    return [key_frame(f.to_ndarray(format="bgr24"), cyan_safe=True) for f in av.open(p).decode(video=0)]   # turkoosi toppi ei ole taustaa


def figures(path):
    """Kuvasarja 3x2: jokainen hahmo omasta ruudustaan, läpinäkyvien pikselien väri nollaksi (punainen haamureuna), roskapisteet pois."""
    im = cv2.cvtColor(cv2.imread(path, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
    H, W = im.shape[:2]; ch, cw = H // 2, W // 3; out = []
    for r in range(2):
        for c in range(3):
            sub = im[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw].copy()
            sub[sub[..., 3] < 24] = 0
            sub = clean(sub)
            pad = np.zeros((ch + 80, cw + 80, 4), np.uint8); pad[40:40 + ch, 40:40 + cw] = sub
            out.append(pad)
    return out


def main(vA, vB, vC, guard, hurt, outs):
    A, B, C = load(vA), load(vB), load(vC)
    x0, x1, y0, y1 = m.bbox(B[0]); k = m.TARGET_H / (y1 - y0); floor = y1
    print("mittakaava", round(k, 4), "seisova korkeus", y1 - y0, "lattia", floor)

    def sheet(name, V, idx, mode, flip=(), fl=None):
        m.setdims("v_" + name)
        cells = []
        for i in idx:
            f = clean(V[i])
            if i in flip: f = f[:, ::-1].copy()
            fli = fl or floor
            ys = np.where(f[..., 3] > 10)[0]
            over = (ys.max() + 1 - fli) * k - (m.FOOT - 1)   # maassa makaava hahmo videolla lattiaviivan alapuolella: nostetaan juuri sen verran, ettei leikkaudu
            if over > 0: fli += over / k
            cells.append(m.cell(f, mode, k, fli))
        m.save(cells, f"{outs}/regina_{name}.png"); print(name, len(idx), "kuvaa", idx); return idx

    sheet("idle", B, m.best_loop(B, 0, 50, 20, 44, "low"), "low")
    sheet("kavely", B, m.best_loop(B, 58, 122, 16, 40, "torso"), "torso")
    sheet("juoksu", B, m.best_loop(B, 128, 239, 10, 24, "torso"), "torso")
    i = sheet("jabi", A, list(range(4, 13)), "low"); print("   osuma indeksi", i.index(8))
    i = sheet("takasuora", A, list(range(12, 23)), "low"); print("   osuma indeksi", i.index(14))
    i = sheet("askelsuora", A, list(range(118, 149)), "low"); print("   osuma indeksi", i.index(130))
    i = sheet("etupotku", A, list(range(48, 85, 2)), "low"); print("   osuma indeksi", i.index(64))
    i = sheet("sivupotku", A, list(range(84, 119, 2)), "low"); print("   osuma indeksi", i.index(94))
    i = sheet("syoksy", A, list(range(153, 214, 2)), "low"); print("   osuma indeksi", i.index(169))
    fall = list(range(36, 105, 3))
    sheet("kaatuminen", C, fall, "com")
    print("   heitetty: alku", [fall.index(x) for x in (36, 39, 42, 45, 48, 51, 54)], "ilma", [fall.index(x) for x in (57, 60, 63, 66, 69, 72, 75, 78, 81, 84)], "lasku", [fall.index(x) for x in fall if x >= 87])
    rise = list(range(106, 213, 3))
    sheet("nousu", C, rise, "com", flip=set(range(132, 187)))

    # kuvasarjat: osuma (pää taaksepäin) ja suojaus (kädet pään edessä); mittakaava osuman 1. kuvasta (perusasento = idle)
    Hf, Gf = figures(hurt), figures(guard)
    _, _, hy0, hy1 = m.bbox(Hf[0]); ki = 0.97 * m.TARGET_H / (hy1 - hy0)   # kuvasarjan hahmolla kapeampi asento ja hieman isommat mittasuhteet (shortsit, kengät ~5 %): kompromissi korkeuden ja mittasuhteiden välillä
    print("kuvasarjojen mittakaava", round(ki, 4), "perusasennon korkeus", hy1 - hy0)
    for name, figs in (("osuma", Hf), ("suojaus", Gf)):
        m.setdims("v_" + name); cells = []
        for f in figs:
            _, _, fy0, fy1 = m.bbox(f); cells.append(m.cell(f, "low", ki, fy1))
        m.save(cells, f"{outs}/regina_{name}.png"); print(name, len(cells), "kuvaa")


if __name__ == "__main__":
    a = sys.argv[1:]
    main(a[0], a[1], a[2], a[3], a[4], a[5] if len(a) > 5 else "Assets/Sprites/Viholliset")
