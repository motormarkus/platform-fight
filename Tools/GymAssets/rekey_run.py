"""Hahmon kuvaputki uudelleen siistillä avainnuksella (rekey_tony.key_soft: ohut tumma reuna, sileä alfa, vihreä hohto pois, ohuet osat säilyvät).
Ajaa annetun skriptin funktion niin, että sen avainnusfunktiot (key_frame/_key_frame, myös tuoduissa make_king_sheets-moduuleissa) korvataan,
kirjoittaa tulokset väliaikaiskansioon (OUT-argumentti), varmuuskopioi vanhat kuvat <skriptin kansio>/orig_edges/ ja kopioi uudet Assets/Sprites/Viholliset/.
Ajo: python Tools/GymAssets/rekey_run.py <skripti.py> <funktio tai - (ajetaan skriptinä)> <arg1> <arg2> ... (yksi argumenteista sana OUT); EDGE_SIGMA=1.0 lopun reunapehmennys
Esim: python Tools/GymAssets/rekey_run.py Tools/CaptainAssets/make_captain_sheets.py build <videot> <suoja.png> OUT <heitto.png> <pusku.png>"""
import os, sys, shutil, tempfile, importlib.util
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
for d in ("TonyAssets", "KingAssets", "GymAssets"): sys.path.insert(0, os.path.join(ROOT, "Tools", d))
from rekey_tony import key_soft, use_soft_keys
import smooth_edges
SIGMA = float(os.environ.get("EDGE_SIGMA", "1.0"))   # lopullisen arkin reunan pehmennys (px); pientä videota suurennettaessa isompi

def soft(bgr, *a, **k): return key_soft(bgr)

script, func, args = sys.argv[1], sys.argv[2], sys.argv[3:]
sys.path.insert(0, os.path.dirname(os.path.abspath(script)))
import key_video, make_king_sheets
key_video.key_frame = soft; use_soft_keys(make_king_sheets)
tmp = tempfile.mkdtemp()
if func == "-":   # skripti ilman pääfunktiota: ajetaan sellaisenaan, argumentit sys.argv:ssa
    sys.argv = [script] + [tmp if a == "OUT" else a for a in args]
    spec = importlib.util.spec_from_file_location("__main__", script); mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
else:
    spec = importlib.util.spec_from_file_location("target_mod", script); mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
    for name in ("key_frame", "_key_frame"):
        if hasattr(mod, name): setattr(mod, name, soft)
    import rekey_tony
    if hasattr(mod, "strip_plume"):   # silmukka valitaan samoista (tupsu poistettu) vanhoista kuvista: alkuperäinen strip_plume, jos tallessa
        old = os.path.join(os.path.dirname(os.path.abspath(script)), "_strip_plume_vanha.py")
        if os.path.exists(old):
            sp = importlib.util.spec_from_file_location("old_mod", old); om = importlib.util.module_from_spec(sp); sp.loader.exec_module(om)
            rekey_tony.COPY_FN = om.strip_plume
        else: rekey_tony.COPY_FN = mod.strip_plume
    getattr(mod, func)(*[tmp if a == "OUT" else a for a in args])
pngs = [f for f in os.listdir(tmp) if f.endswith(".png")]
if SIGMA > 0: smooth_edges.run(pngs, tmp + "/", SIGMA, tmp + "/")
bak = os.path.join(os.path.dirname(os.path.abspath(script)), "orig_edges"); os.makedirs(bak, exist_ok=True)
dst = os.path.join(ROOT, os.environ.get("REKEY_DST", "Assets/Sprites/Viholliset"))   # REKEY_DST: muu kohdekansio (esim. Assets/Sprites/Morsian)
for f in sorted(os.listdir(tmp)):
    if not f.endswith(".png"): continue
    old = os.path.join(dst, f)
    if os.path.exists(old) and not os.path.exists(os.path.join(bak, f)): shutil.copy(old, os.path.join(bak, f))
    shutil.copy(os.path.join(tmp, f), old); print("->", f)
