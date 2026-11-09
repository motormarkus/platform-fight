"""Kenraali (linnakkeen ensimmäinen pomo): vihreätaustaisista videoista kuvasarjat Assets/Sprites/Viholliset/kenraali_<nimi>.png.
Video 1 (gemini): idle, osuma, lento+kaatuminen, maassa, nousu. Video 2: suorat (3 kpl), potkut ja pyöräytyspotku, alakoukku, sivupotku. Video 3: suoja + syöksysuora (pölypilvi). Video 4: idle (0-56), kävely (58-156), juoksu (158-238).
Ruudut kuten mongolisoturilla (Tools/MongolAssets). Ajo: python Tools/GeneralAssets/make_general_sheets.py <video1> <video2> <video3> <video4> [ulos-sheetit]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 640.0
m.DIMS.update({"g_idle": (640, 320, 768), "g_suoja": (640, 320, 768), "g_suora1": (896, 320, 768), "g_suora2": (896, 320, 768), "g_suora3": (896, 320, 768),
               "g_potku1": (1024, 320, 768), "g_potku2": (1024, 320, 768), "g_potku3": (1280, 440, 768), "g_koukku": (1024, 320, 768), "g_sivupotku": (1152, 320, 768),
               "g_syoksy": (1152, 320, 768), "g_kavely": (640, 320, 768), "g_juoksu": (768, 320, 768), "g_osuma": (896, 420, 768), "g_kaatuminen": (1280, 576, 1024), "g_maahan": (1280, 576, 768), "g_nousu": (1280, 576, 768)})

def main(v1, v2, v3, v4, outs):
    A, B, C, Dv = m.load(v1), m.load(v2), m.load(v3), m.load(v4)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C); kD, fD = kf(Dv)
    print("mittakaavat", round(kA, 3), round(kB, 3), round(kC, 3), round(kD, 3), "kuvia", len(A), len(B), len(C), len(Dv))
    SKIP = {"koukku"}   # alakoukku jätetty pois (käyttäjä: lyönneiksi riittävät suorat); potkut (potku1-3, sivupotku) mukana
    def sheet(name, vid, idx, mode, k, floor):
        if name in SKIP: return
        idx = [i for i in idx if i < len(vid)]
        m.setdims("g_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/kenraali_{name}.png"); print(name, len(cells), "kuvaa", list(idx))
    sheet("idle", Dv, m.best_loop(Dv, 0, 56, 16, 54, "low")[::2], "low", kD, fD)   # joka toinen kuva: pienempi sheet, kuva-aika 1/12 s   # video 4: pitkä idle
    sheet("kavely", Dv, m.best_loop(Dv, 58, 156, 16, 50, "torso")[::2], "torso", kD, fD)
    sheet("juoksu", Dv, m.best_loop(Dv, 158, 238, 10, 30, "torso")[::2], "torso", kD, fD)
    sheet("suoja", C, list(range(4, 36, 3)), "low", kC, fC)
    sheet("suora1", B, list(range(0, 27, 3)), "low", kB, fB); print("suora1: osuma indeksi 4 (kuva 12)")
    sheet("suora2", B, list(range(24, 51, 3)), "low", kB, fB); print("suora2: osuma indeksit 2 ja 6 (kuvat 30, 42)")
    sheet("suora3", B, list(range(48, 75, 3)), "low", kB, fB); print("suora3: osuma indeksi 2 (kuva 54)")
    sheet("potku1", B, [72, 74, 76, 78, 80, 82, 84, 87, 90, 93], "low", kB, fB); print("potku1: osuma indeksit 3-4 (kuvat 78-80)")
    sheet("potku2", B, [126, 128, 130, 132, 134, 136, 138, 141, 144, 147], "low", kB, fB); print("potku2: osuma indeksit 3-4 (kuvat 132-134)")
    sheet("potku3", B, list(range(96, 130, 3)), "low", kB, fB); print("potku3: pyöräytys, osuma indeksit 7-8 (kuvat 117-123)")
    sheet("koukku", B, list(range(150, 171, 3)), "low", kB, fB); print("koukku: osuma indeksi 4 (kuva 162)")
    sheet("sivupotku", B, list(range(168, 190, 3)), "low", kB, fB); print("sivupotku: osuma indeksit 3-4 (kuvat 177-180)")
    sheet("syoksy", C, list(range(32, len(C), 4)), "low", kC, fC); print("syoksy: osuma indeksit 3-6 (kuvat 44-56)")
    sheet("osuma", A, [20, 22, 24, 26, 28, 30], "low", kA, fA)
    sheet("kaatuminen", A, list(range(30, 78, 3)), "com", kA, fA)
    sheet("maahan", A, [80, 92, 104, 116, 128, 138], "com", kA, fA)
    sheet("nousu", A, list(range(140, 192, 3)), "com", kA, fA)

if __name__ == "__main__":
    main(*sys.argv[1:5], sys.argv[5] if len(sys.argv) > 5 else "Assets/Sprites/Viholliset")
