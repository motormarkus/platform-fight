"""Hector: taistelukuvat yhdestä videosta (lyönnit, potkut, hyppypotku, liukutaklaus) -> Assets/Sprites/Viholliset/hector_<nimi>.png.
isku1 = jabi (22-34, osuma kuvassa 4), isku2 = toinen jabi (32-44, osuma 4), isku3 = suora (76-88, osuma 4),
potku1 = korkea sivupotku (48-68, osuma 6), potku2 = etupotku (88-102, osuma 3), hyppypotku (108-138, osuma 8), liukutaklaus (136-170, osuma 6).
Ruudut kuten muilla (hahmo ~640 px, jalat 16 px ylös); hypyn ja liu'un korkeus/matala asento on kuvissa valmiina.
Ajo: python Tools/PrisonAssets/make_hector_fight.py <video.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
# nimi -> (ruudun leveys, ankkurin x, korkeus); sama taulukko BeatEmUpSetup.SkelCell:ssä
CELLS = {"isku1": (1024, 320, 768), "isku2": (1024, 320, 768), "isku3": (1024, 320, 768), "potku1": (1280, 320, 768), "potku2": (1152, 320, 768),
         "hyppypotku": (1408, 384, 1024), "liukutaklaus": (1408, 448, 768)}
m.DIMS.update({"v_" + k: v for k, v in CELLS.items()})

def main(video, outs):
    V = m.load(video); print(len(V), "kuvaa")
    x0, x1, y0, y1 = m.bbox(V[0]); k = m.TARGET_H / (y1 - y0); floor = y1
    def sheet(name, idx, mode):
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, floor) for i in idx], f"{outs}/hector_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("isku1", [22, 24, 26, 28, 30, 32, 34], "low")                   # osuma 3 (kuva 28), käsi suorana 28-30
    sheet("isku2", [32, 34, 35, 36, 38, 40, 42, 44], "low")               # osuma 3 (kuva 36)
    sheet("isku3", [76, 78, 80, 82, 84, 86, 88], "low")                   # osuma 3 (kuva 82)
    sheet("potku1", [48, 50, 52, 54, 56, 58, 60, 62, 64, 66, 68], "low")  # osuma 5 (kuva 58), jalka suorana 56-62
    sheet("potku2", [88, 90, 92, 94, 96, 98, 100, 102], "low")            # osuma 2 (kuva 92), jalka suorana 92-94
    sheet("hyppypotku", list(range(108, 140, 2)), "torso")                # osuma 7 (kuva 122), 16 kuvaa
    sheet("liukutaklaus", list(range(136, 172, 2)), "com")                # osuma 5 (kuva 146), 18 kuvaa

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
