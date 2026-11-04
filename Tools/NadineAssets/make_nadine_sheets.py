"""Nadine (NPC, natsilinnakkeen juhlahalli): kolme vihreätaustaista videoa -> Assets/Sprites/Viholliset/nadine_<nimi>.png.
Video A (rauhallinen tanssi: seisoo ja elehtii, tanssi2), B (tanssi: pyörähdykset), C (idle 0-44, kävely 46-94, katsoo oikealle), D (keskustelu: kuvat 6-63 eteen ja takaisin peilattuna = puhe).
Ruudut kuten gestapolla (4 saraketta, hahmo 600 px, jalat 16 px ylös); enintään 40 kuvaa/arkki (kuvan korkeus <= 8192).
Ajo: python Tools/NadineAssets/make_nadine_sheets.py <tanssi2.mov> <tanssi.mov> <kavely.mov> <keskustelu.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 600.0
m.DIMS.update({"n_idle": (640, 320, 768), "n_kavely": (640, 320, 768), "n_puhe": (768, 384, 768), "n_tanssi": (768, 384, 672), "n_tanssi2": (768, 384, 672)})


def main(vA, vB, vC, vD, outs):
    A, B, C, D = m.load(vA), m.load(vB), m.load(vC), m.load(vD)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C); kD, fD = kf(D)
    print("mittakaavat", round(kA, 3), round(kB, 3), round(kC, 3), round(kD, 3))

    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("n_" + name)
        cells = [m.cell(vid[i], mode, k, floor) for i in idx]
        m.save(cells, f"{outs}/nadine_{name}.png"); print(name, len(cells), "kuvaa", list(idx)[:3], "...", list(idx)[-2:])

    sheet("idle", C, m.best_loop(C, 0, 44, 14, 36, "low"), "low", kC, fC)
    sheet("kavely", C, m.best_loop(C, 46, len(C) - 2, 16, 40, "torso"), "torso", kC, fC)
    fw = list(range(6, 64, 3)); sheet("puhe", D, fw + fw[-2::-1], "low", kD, fD)   # eteen ja takaisin peilattuna (alku ja loppu: kädet lanteilla), 4,75 s, 3 kuvaa = 0,125 s
    sheet("tanssi", B, [round(14 + i * (232 - 14) / 47) for i in range(48)], "torso", kB, fB)   # ~9 s, kuvaväli 4,6 videokuvaa = 0,19 s
    sheet("tanssi2", A, [round(i * 239 / 47) for i in range(48)], "torso", kA, fA)   # ~10 s, kuvaväli 5 videokuvaa = 0,21 s

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5] if len(sys.argv) > 5 else "Assets/Sprites/Viholliset")
