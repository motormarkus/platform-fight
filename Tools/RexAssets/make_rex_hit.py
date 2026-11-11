"""Rex: osuma, kaatuminen, nousu yhdestä videosta (vihreä tausta; hahmo suurenee videon aikana, lattia siirtyy alaspäin).
Korjaus: mittakaava kulkee lineaarisesti alun (seisova 549 px -> 640 px) ja lopun (seisova ~585 px -> 640 px) välillä ruudun numeron mukaan,
ja jalat/vartalo asetetaan kunkin kuvan omaan alareunaan (lattia ei pysy paikallaan videossa).
-> Assets/Sprites/Viholliset/rex_osuma.png, rex_kaatuminen.png, rex_nousu.png.
Ajo: python Tools/RexAssets/make_rex_hit.py <video.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_osuma": (896, 420, 768), "v_kaatuminen": (1536, 768, 1024), "v_nousu": (1280, 640, 768)})
END = 210   # tästä eteenpäin hahmo seisoo paikallaan (mittakaava vakio)

def main(video, outs):
    V = m.load(video); print(len(V), "kuvaa")
    h = lambda i: (lambda b: b[3] - b[2])(m.bbox(V[i]))
    kS, kE = m.TARGET_H / h(0), m.TARGET_H / h(END); print("mittakaava alku", round(kS, 3), "loppu", round(kE, 3))
    def k(i): return kS + (kE - kS) * min(1.0, i / float(END))
    def sheet(name, idx, mode):
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k(i), m.bbox(V[i])[3]) for i in idx], f"{outs}/rex_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("osuma", [14, 18, 21, 24, 27, 30, 33, 36], "low")
    sheet("kaatuminen", [36, 42, 46, 50, 54, 58, 62, 66, 70, 76, 84, 90], "com")
    sheet("nousu", [100, 108, 116, 124, 132, 140, 148, 156, 164, 172, 180, 190, 200, 212], "com")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
