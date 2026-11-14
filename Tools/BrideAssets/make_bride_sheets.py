"""Morsian (hääkappeli) yhdestä videosta (240 kuvaa 24 fps, vihreä tausta) -> Assets/Sprites/Morsian/morsian_<nimi>.png.
Videossa: idle 0-100 (kimppu sylissä), nosto 102-112, heitto vasemmalle ~114, loppuliike ja hämmästys 118-148, kääntyminen 150-158, juoksu oikealle 160-203.
Ruutu 512x512 (4 saraketta), hahmo ~480 px, jalat 16 px ylhäällä, pivot x 0.5; hahmo katsoo videolla vasemmalle (idle) ja juoksee oikealle.
Kimppu on heitossa omana spritenä: key_frame jättää vain suurimman yhtenäisen hahmon, joten lentävä kimppu ei tule mukaan.
Ajo: python Tools/BrideAssets/make_bride_sheets.py <video.mp4> [ulos]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 480.0
for k in ("idle", "pako"): m.DIMS["bride_" + k] = (512, 256, 512)
m.DIMS["bride_heitto"] = (512, 256, 640)   # korkeampi ruutu: kimppu nostettuna pään yläpuolella (kuvat 112-116)
A = m.load(sys.argv[1]); outs = sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Morsian"
os.makedirs(outs, exist_ok=True)
x0, x1, y0, y1 = m.bbox(A[0]); K = m.TARGET_H / (y1 - y0); FL = y1
def sheet(name, idx, mode):
    m.setdims("bride_" + name)
    m.save([m.cell(A[i], mode, K, FL) for i in idx], f"{outs}/morsian_{name}.png"); print(name, len(idx), "kuvaa", idx)
sheet("idle", m.best_loop(A, 0, 100, 40, 90, "low")[::2], "low")                 # 12 fps
throw = list(range(100, 130, 2)) + [131, 134, 137, 140]                            # 19 kuvaa; kimppu irtoaa kuvien 114-116 jälkeen: ensimmäinen kimputon kuva 118 = indeksi 9
sheet("heitto", throw, "low"); print("   kimppu irtoaa indeksissä", throw.index(118))
sheet("pako", m.best_loop(A, 160, 203, 10, 24, "torso"), "torso")
