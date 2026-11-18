"""Lentoemäntä uudelleen siistillä avainnuksella (rekey_tony.key_soft + smooth_edges), samat ruudut kuin make_attendant_hd.py.
Vanhat: Tools/AttendantAssets/orig_edges/. Videot: F:/total fist tavaraa (idle 9_19AM, puhe 9_21AM) ja uploads (kävely 2_40PM).
Ajo: python Tools/AttendantAssets/rekey_attendant.py"""
import os, sys, shutil, tempfile
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
for d in ("TonyAssets", "KingAssets", "GymAssets"): sys.path.insert(0, os.path.join(ROOT, "Tools", d))
sys.path.insert(0, HERE)
from rekey_tony import key_soft
import smooth_edges, make_attendant_hd as h
h.key_frame = lambda bgr, *a, **k: key_soft(bgr)
F = "F:/total fist tavaraa/"
idle = h.load(F + "Generated Video October 26, 2026 - 9_19AM.mp4"); talk = h.load(F + "Generated Video October 26, 2026 - 9_21AM.mp4")
walk = h.load("C:/Users/marku/.claude/uploads/738327ec-6328-4b78-9791-c65803258fd9/2b4bbe7f-Generated_Video_October_25_2026_-_2_40PM.mp4")
tmp = tempfile.mkdtemp()
fw = list(range(0, len(idle), 4)); h.sheet(idle, fw + fw[-2:0:-1], os.path.join(tmp, "lentoemanta_idle.png"))
h.sheet(talk, list(range(0, len(talk), 3)), os.path.join(tmp, "lentoemanta_puhe.png"))
h.walk_sheet(walk, os.path.join(tmp, "lentoemanta_kavely.png"))
names = sorted(os.listdir(tmp)); smooth_edges.run(names, tmp + "/", 1.0, tmp + "/")
bak = os.path.join(HERE, "orig_edges"); os.makedirs(bak, exist_ok=True)
for n in names:
    dst = os.path.join(ROOT, "Assets/Sprites/Viholliset", n)
    if not os.path.exists(os.path.join(bak, n)): shutil.copy(dst, os.path.join(bak, n))
    shutil.copy(os.path.join(tmp, n), dst); print("->", n)
