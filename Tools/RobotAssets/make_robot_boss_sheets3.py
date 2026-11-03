"""Kuurobotti-pomo, kolmas erä: peruslyönnit (jab = lyönti 1, ristilyönti toisella kädellä = lyönti 2) -> robotti_pomo_lyonti1.png, robotti_pomo_lyonti2.png.
Videossa 4 lyöntiä (jab 0-15, risti 15-36, jab 42-57, risti 57-84); käytetään kaksi ensimmäistä. Tehosteet (suuliekki, savu) poistetaan.
Lyönti 1: kuvat 0-15 (osuma indeksi 4-6), lyönti 2: kuvat 15-36 (osuma indeksi 3-5).
Ajo: python Tools/RobotAssets/make_robot_boss_sheets3.py <lyonnit.mov> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import make_robot_boss_sheets2 as b2
m = b2.m
m.DIMS.update({"r_pomo_lyonti1": (1024, 400, 768), "r_pomo_lyonti2": (1024, 400, 768), "r_pomo_idle": (768, 384, 768)})

def main(v, outs):
    V = [b2.strip_effects(f) for f in m.load(v)]
    x0, x1, y0, y1 = m.bbox(V[0]); k = m.TARGET_H / (y1 - y0); fl = y1; print("mittakaava", round(k, 3))
    def sheet(name, idx):
        m.setdims("r_pomo_" + name)
        m.save([m.cell(V[i], "low", k, fl) for i in idx], f"{outs}/robotti_pomo_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("idle", [84, 86, 88, 90, 92, 96, 92, 90, 88, 86])   # pomon oma idle: ping-pong lyöntivideon lopun asennosta
    sheet("lyonti1", [0, 2, 4, 5, 6, 7, 9, 11, 13, 15])
    sheet("lyonti2", [15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 36])

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
