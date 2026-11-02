"""Luolan luurankosoturi: kolmesta vihreätaustaisesta videosta (A iskut ja potku, B luunheitto, C idle/kävely/juoksu) ja zip-kuvista (osuma, kaatuminen, nousu)
kuvasarjat Assets/Sprites/Viholliset/luuranko_<nimi>.png (ruudut kuten Temppelin kuninkaalla: Tools/KingAssets/make_king_sheets.py) ja Assets/Resources/Luuranko/luu.png (heitettävä luu).
Ajo: python Tools/SkeletonAssets/make_skeleton_sheets.py <iskut.mov> <luunheitto.mov> <liike.mov> <zip-kansio> <jab.png> <rajahdys.png> [ulos-sheetit] [resources/Luuranko]
Jabi: 2x2-ruudukko vihreällä taustalla (ote, alku, ojennus, täysi ojennus); räjähdys: 3x2 ruudukko läpinäkyvällä taustalla (osumapose ... kasa).
Iskukuvista jätetään pois valkoisen osumasalaman kuvat (peli piirtää oman)."""
import os, sys, glob
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
from key_video import key_frame

m.TARGET_H = 640.0
m.DIMS.update({"l_idle": (640, 320, 768), "l_kavely": (640, 320, 768), "l_juoksu": (640, 320, 768), "l_jab": (768, 300, 768), "l_takasuora": (768, 300, 768),
               "l_potku": (1024, 300, 768), "l_luu": (768, 300, 832), "l_osuma": (896, 420, 768), "l_kaatuminen": (1024, 512, 768), "l_maahan": (1024, 512, 768), "l_nousu": (1024, 512, 768), "l_rajahdys": (1024, 512, 768), "l_hyppypotku": (1024, 300, 1024), "l_maasta": (768, 384, 768)})
load = m.load; bbox = m.bbox


def flash_frames(vid):
    """Kuvat, joissa on iso valkoinen osumasalama."""
    out = set()
    for i, k in enumerate(vid):
        rgb = k[..., :3].astype(int); a = k[..., 3] > 128
        w = (rgb.min(2) >= 236) & ((rgb.max(2) - rgb.min(2)) <= 18) & a
        if w.sum() > 800: out.add(i)
    return out


def zip_frames(folder):
    fs = sorted(glob.glob(os.path.join(folder, "*.png")))
    out = []
    for f in fs:
        im = cv2.imread(f, cv2.IMREAD_UNCHANGED)
        out.append(cv2.cvtColor(im, cv2.COLOR_BGRA2RGBA))
    return out


def main(vA, vB, vC, zdir, jabimg, burstimg, outs, resdir, vN=None, vR=None):
    A, B, C = load(vA), load(vB), load(vC)
    fl = flash_frames(A); print("salamakuvat", sorted(fl))
    x0, x1, y0, y1 = bbox(C[0]); k = m.TARGET_H / (y1 - y0); floor = y1
    print("mittakaava", round(k, 3), "hahmon korkeus", y1 - y0, "lattia", floor)

    def sheet(name, vid, idx, mode, kk=None, fl_=None):
        m.setdims("l_" + name)
        cells = [m.cell(vid[i], mode, kk or k, fl_ or floor) for i in idx]
        m.save(cells, f"{outs}/luuranko_{name}.png"); print(name, len(cells), "kuvaa", idx)

    if vN:   # uusi video (Gemini, 1280x720, 24 fps): oikea idle, kävely ja hyppypotku
        N = [key_frame(f.to_ndarray(format="bgr24"), cyan_safe=True) for f in av.open(vN).decode(video=0)]
        nx0, nx1, ny0, ny1 = bbox(N[0]); kn = m.TARGET_H / (ny1 - ny0); print("uusi video: mittakaava", round(kn, 3), "korkeus", ny1 - ny0, "lattia", ny1)
        sheet("idle", N, m.best_loop(N, 0, 44, 14, 40, "low"), "low", kn, ny1)
        sheet("kavely", N, m.best_loop(N, 46, 128, 16, 48, "torso"), "torso", kn, ny1)
        ax0 = m.anchor_x(N[130], "low"); old_ax = m.anchor_x; m.anchor_x = lambda im, mode: ax0   # kiinteä ankkuri: hyppy ei hypi sivusuunnassa
        leap = [132, 135, 138, 141, 144, 147, 150, 153, 156, 159, 162, 165, 168, 172, 176, 182, 188, 194, 198, 202, 206, 210, 214]
        sheet("hyppypotku", N, leap, "low", kn, ny1); m.anchor_x = old_ax
        print("hyppypotku: potkuosuma kuvat 12-17, liike kuvat 5-13")
    else:
        sheet("idle", C, m.best_loop(C, 0, 130, 20, 60, "low"), "low")
        sheet("kavely", C, m.best_loop(C, 146, 232, 14, 44, "torso"), "torso")
    sheet("juoksu", C, m.best_loop(C, 240, 299, 10, 26, "torso"), "torso")
    ok = lambda r: [i for i in r if i not in fl]
    # jabi: 2x2-ruudukko (vihreä tausta); mittakaava ote-kuvan korkeudesta
    K = cv2.imread(jabimg); jh, jw = K.shape[:2]   # jokainen neljännes avataan erikseen (key_frame säilyttää vain suurimman hahmon)
    quads = [key_frame(np.ascontiguousarray(q), cyan_safe=True) for q in (K[:jh // 2, :jw // 2], K[:jh // 2, jw // 2:], K[jh // 2:, :jw // 2], K[jh // 2:, jw // 2:])]
    bb = [bbox(q) for q in quads]; kj = m.TARGET_H / (bb[0][3] - bb[0][2])
    m.setdims("l_jab")
    jc = [m.cell(q, "low", kj, b[3]) for q, b in zip(quads, bb)]
    seq = [0, 1, 2, 3, 3, 2, 1, 0]
    m.save([jc[i] for i in seq], f"{outs}/luuranko_jab.png"); print("jab", len(seq), "kuvaa (osuma = indeksi 3)")
    sheet("takasuora", A, ok(range(56, 73)), "low")
    sheet("potku", A, [93, 95, 97, 99, 101] + ok([132, 136, 140, 148, 156, 160]) + list(range(162, 178, 2)), "low")
    throw = [48, 51, 54, 57, 60, 63, 66, 69, 72, 75, 78, 80, 82, 84, 86, 88, 90, 94, 98, 102, 106]
    sheet("luu", B, throw, "low")
    print("luu: vapautus = indeksi", throw.index(82))
    # luu: kuvassa 72 luu on nyrkissä, näkyvä puolikas (nyrkistä solmuun) peilataan symmetriseksi luuksi
    fr = B[72]; P0 = np.array([688.0, 179.0]); P1 = np.array([762.5, 148.75])
    d = P1 - P0; L = float(np.hypot(*d)); th = np.degrees(np.arctan2(d[1], d[0])); H = 80
    M = cv2.getRotationMatrix2D((float(P0[0]), float(P0[1])), th, 1.0); M[0, 2] += -P0[0]; M[1, 2] += H / 2 - P0[1]
    half = cv2.warpAffine(fr, M, (int(L) + 34, H), flags=cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0))
    a = half[..., 3] > 128; ys, xs = np.where(a)
    half = half[max(0, ys.min() - 3):ys.max() + 4, 10:]   # nyrkin reunan roskat pois
    full = np.concatenate([half[:, ::-1], half], axis=1)
    # reunat siistiksi: alfa pehmeäksi
    os.makedirs(resdir, exist_ok=True)
    cv2.imwrite(f"{resdir}/luu.png", cv2.cvtColor(full, cv2.COLOR_RGBA2BGRA)); print("luu", full.shape)

    if vR:   # nousu maasta: videon alussa hahmo on isompi ja skaalautuu lopulta idleen -> korjataan kuvakohtaisella mittakaavalla
        Rv = [key_frame(f.to_ndarray(format="bgr24"), cyan_safe=True) for f in av.open(vR).decode(video=0)]
        fb = bbox(Rv[132]); k_final = m.TARGET_H / (fb[3] - fb[2]); ax0 = m.anchor_x(Rv[132], "low")
        key = [(0, 0.80), (54, 0.80), (60, 0.835), (66, 0.93), (72, 1.0), (999, 1.0)]
        def sc(i):
            for (a, va), (b, vb) in zip(key, key[1:]):
                if a <= i <= b: return k_final * (va + (vb - va) * (i - a) / max(1, b - a))
        m.setdims("l_maasta"); old_ax = m.anchor_x; m.anchor_x = lambda im, mode: ax0
        idx = list(range(3, 133, 3)); cells = []
        for i in idx:
            al = Rv[i][..., 3] > 128
            if al.sum() < 30: continue
            cells.append(m.cell(Rv[i], "low", sc(i), bbox(Rv[i])[3]))
        m.anchor_x = old_ax
        m.save(cells, f"{outs}/luuranko_maasta.png"); print("maasta", len(cells), "kuvaa, k_final", round(k_final, 3))
    # osuma, kaatuminen, maahan, nousu: zip-kuvat (pienempi resoluutio: oma mittakaava seisovan asennon korkeudesta)
    Z = zip_frames(zdir)
    zx0, zx1, zy0, zy1 = bbox(Z[0]); kz = m.TARGET_H / (zy1 - zy0); print("zip mittakaava", round(kz, 3), "lattia", zy1)
    zi = lambda n: n - 1
    sheet("osuma", Z, [zi(n) for n in range(45, 62, 2)], "low", kz, zy1)
    sheet("kaatuminen", Z, [zi(n) for n in list(range(72, 92, 2)) + [94]], "com", kz, zy1)
    sheet("maahan", Z, [zi(n) for n in (88, 90, 92, 96, 100, 106)], "com", kz, zy1)
    # räjähdys: 3x2 ruudukko, ensimmäinen kuva = osumapose; mittakaava osuma-sarjan viimeisen kuvan korkeudesta
    R = cv2.cvtColor(cv2.imread(burstimg, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA); rh, rw = R.shape[:2]; cw, ch = rw // 3, rh // 2
    rc = [R[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw] for r in range(2) for c in range(3)]
    b0 = bbox(rc[0]); hz = bbox(Z[zi(59)]); kb = (hz[3] - hz[2]) * kz / (b0[3] - b0[2]); print("räjähdyksen mittakaava", round(kb, 3))
    ax, fy = (b0[0] + b0[1]) / 2.0, b0[3]
    m.setdims("l_rajahdys"); bc = []
    for c in rc:
        r2 = m.rescale(c, kb); out = np.zeros((m.CHCUR, m.CW, 4), np.uint8)
        x0 = int(round(m.ANCHOR - ax * kb)); y0 = int(round(m.CHCUR - m.FOOT - fy * kb))
        sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
        ww, hh = min(r2.shape[1] - sx0, m.CW - dx0), min(r2.shape[0] - sy0, m.CHCUR - dy0)
        out[dy0:dy0 + hh, dx0:dx0 + ww] = r2[sy0:sy0 + hh, sx0:sx0 + ww]; bc.append(out)
    m.save(bc, f"{outs}/luuranko_rajahdys.png"); print("rajahdys", len(bc), "kuvaa")
    sheet("nousu", Z, [zi(n) for n in list(range(146, 218, 4)) + [221]], "com", kz, zy1)


if __name__ == "__main__":
    a = sys.argv
    vN = None
    vR = None
    if "--rise" in a:
        k = a.index("--rise"); vR = a[k + 1]; del a[k:k + 2]
    if "--new" in a:
        k = a.index("--new"); vN = a[k + 1]; del a[k:k + 2]
    main(a[1], a[2], a[3], a[4], a[5], a[6], a[7] if len(a) > 7 else "Assets/Sprites/Viholliset", a[8] if len(a) > 8 else "Assets/Resources/Luuranko", vN, vR)
