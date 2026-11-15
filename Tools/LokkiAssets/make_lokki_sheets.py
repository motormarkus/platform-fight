"""Lokki (valkopukuinen mestari, viimeinen pomo; ensiesiintyminen juna-asemalla): vihreataustaisista videoista kuvasarjat Assets/Sprites/Viholliset/lokki_<nimi>.png.
Video A (iskut): suorasarjat, etupotku, kaantopotku (selin + korkea sivupotku), pitkat iskusarjat, ylakoukku. Video B: idle (0-78), kavely (84-150), juoksu (156-239). Video C: korkeat potkut + kiertopotku ja kämmeniskun (valkoinen iskuaalto).
Lokkia ei voi lyoda (ei osuma-/kaatumiskuvia). Sama siisti reunankasittely kuin Nokkamiehella (soft_key, sileä alfa, hento ääriviiva): Tools/NokkamiesAssets/make_nokkamies_sheets.py.
Ajo: python Tools/LokkiAssets/make_lokki_sheets.py <iskut.mp4> <liike.mp4> <potkut.mov> [ulos]"""
import os, sys, json
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "NokkamiesAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_nokkamies_sheets as nk
import make_king_sheets as m

TARGET_H = 640.0
nk.KEY_HI, nk.KEY_RANGE = 0.68, 0.56
nk.INK_DEPTH, nk.INK_ERODE = 2.2, 0.8   # siisti musteinen reuna: tärkein vihu, ei vihreää/olivia reunaa eikä rosoa   # tiukempi avain: vihreä reunakehä pois (ei neonliiviä)

class Sheet(nk.Sheet):
    def save(self, outs, dims):
        super().save(outs, dims)
        # nk.Sheet kirjoittaa nokkamies_-etuliitteellä: nimeä uudelleen
        src = f"{outs}/nokkamies_{self.name}.png"; dst = f"{outs}/lokki_{self.name}.png"
        os.replace(src, dst)

def hA2(A):
    b = m.bbox(A[24]); return b[3] - b[2]

def main(vA, vB, vC, outs, vD=None):
    A, B, C = nk.load(vA), nk.load(vB), nk.load(vC)
    def kf(v): x0, x1, y0, y1 = m.bbox(v[0]); return TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C)
    # video C: Lokki on siinä ~10 % isompi (lähempänä kameraa) kuin videoissa A/B; mittakaava vartalon (taisteluasento) korkeuden mukaan: A kuva 24 = C kuva 48
    hA = m.bbox(A[24]); hA = hA[3] - hA[2]; hC = m.bbox(C[48]); fC = hC[3]; hC = hC[3] - hC[2]
    kC = kA * hA / hC
    print("mittakaavat", [round(x, 3) for x in (kA, kB, kC)], "kuvia", len(A), len(B), len(C))
    dims = {}
    def sheet(name, vid, idx, mode, k, floor, noink=()):
        s = Sheet(name); [s.add(vid[i], mode, k, floor, ink=(j not in noink)) for j, i in enumerate(idx) if i < len(vid)]; s.save(outs, dims); print("   kuvat", list(idx))
    sheet("idle", B, m.best_loop(B, 0, 78, 16, 54, "low")[::2], "low", kB, fB)
    sheet("kavely", B, m.best_loop(B, 84, 150, 16, 50, "torso")[::2], "torso", kB, fB)
    sheet("juoksu", B, m.best_loop(B, 156, 239, 10, 30, "torso")[::2], "torso", kB, fB)
    # video A: suorat (jokainen veto omana sheetinaan, osumakuva = ojennettu käsi)
    sheet("suora1", A, list(range(24, 33, 2)), "low", kA, fA); print("   suora1: osuma indeksi 2 (kuva 28)")
    sheet("suora2", A, list(range(30, 43, 2)), "low", kA, fA); print("   suora2: osumat indeksit 2 (kuva 34) ja 5 (kuva 40)")
    sheet("suora3", A, list(range(42, 51, 2)), "low", kA, fA); print("   suora3: osuma indeksi 3 (kuva 48)")
    sheet("suora4", A, list(range(50, 61, 2)), "low", kA, fA); print("   suora4: osuma indeksi 2-3 (kuvat 54-56)")
    sheet("potku1", A, list(range(60, 79, 2)), "low", kA, fA); print("   potku1 (etupotku): osuma indeksit 4-5 (kuvat 68-70)")
    sheet("potku2", A, list(range(78, 99, 2)), "low", kA, fA); print("   potku2 (selin kääntyen, korkea sivupotku): osuma indeksit 4-6 (kuvat 86-90)")
    sheet("sarja1", A, list(range(96, 137, 2)), "low", kA, fA); print("   sarja1 (neljä suoraa): osumat kuvissa 102, 108-110, 116, 124-126")
    sheet("sarja2", A, list(range(136, 173, 2)), "low", kA, fA); print("   sarja2 (suoria): osumat kuvissa 146, 160, 168")
    sheet("yla", A, list(range(172, 197, 2)), "low", kA, fA); print("   yla (nostettu käsi + heilautus): osuma kuvissa 186-190")
    # video C
    sheet("potku3", C, list(range(0, 47, 2)), "low", kC, fC); print("   potku3 (polvi + etupotku, kierto selin, korkea sivupotku): osumat kuvissa 6-10 ja 32-40")
    sheet("aalto", C, list(range(46, 66, 2)) + list(range(66, 97, 6)), "low", kC, fC, noink=(6, 7, 8, 9)); print("   aalto: kämmen ojossa, valkoinen iskuaalto kuvissa 60-64; osuma indeksit 7-9")
    if vD:
        D = nk.load(vD)
        hs = [m.bbox(D[i])[3] - m.bbox(D[i])[2] for i in range(len(D))]
        base = hs[0]; b0 = m.bbox(A[0]); kD = kA * (b0[3] - b0[2]) / base;   # seisova D = seisova A (idle) -> 640 px
        fD = m.bbox(D[0])[3]
        print("video D: seisova", base, "px, mittakaava", round(kD, 3))
        # nousussa hahmo näyttää kasvavan (+4 %: kuvat 176-200): kuvakohtainen korjaus seisoviin asentoihin, jotta koko pysyy samana
        def corr(i):
            return min(1.0, base / hs[i]) if 172 <= i <= 212 and hs[i] > base else 1.0
        def sheetD(name, idx, mode):
            sh = Sheet(name)
            for i in idx: sh.add(D[i], mode, kD * corr(i), fD)
            sh.save(outs, dims); print("   kuvat", list(idx))
        sheetD("osuma", [14, 16, 18, 20, 22], "low")
        sheetD("kaatuminen", list(range(22, 63, 2)), "com")
        sheetD("maahan", [64, 72, 80, 90, 100, 110, 120], "com")
        sheetD("nousu", list(range(120, 207, 4)), "com")
    p = os.path.join(HERE, "dims.json")
    d = json.load(open(p)) if os.path.exists(p) else {}
    d.update(dims); json.dump(d, open(p, "w"), indent=1)

if __name__ == "__main__":
    a = sys.argv
    main(a[1], a[2], a[3], a[4] if len(a) > 4 else "Assets/Sprites/Viholliset", a[5] if len(a) > 5 else None)
