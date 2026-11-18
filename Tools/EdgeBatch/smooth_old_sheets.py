"""Vanhojen (kuvista/aiemmilla avainnuksilla tehtyjen) vihuarkkien rosoinen ääriviiva sileäksi: smooth_edges (1,0 px) kaikille annetun etuliitteen arkeille.
Alkuperäiset: Tools/EdgeBatch/orig_edges/ (otetaan vain kerran, uudelleenajo lähtee niistä). Ajo: python Tools/EdgeBatch/smooth_old_sheets.py"""
import os, sys, shutil
sys.path.insert(0, "Tools/TonyAssets"); import smooth_edges
PREFIX = "amatsoni apuri_nyrkki gustav horhe kapteeni latkafani lenkkari lippis luuranko marcus merirosvo metsuri motoristi pierre pomo portsari puliukko punk rokkari rokkari2 romuukko rosvokapteeni rottakeisari rusty samoa seilori skettari vihu".split()
D, B = "Assets/Sprites/Viholliset/", "Tools/EdgeBatch/orig_edges/"
names = sorted(f for f in os.listdir(D) if f.endswith(".png") and any(f.startswith(p + "_") for p in PREFIX))
for f in names:
    if not os.path.exists(B + f): shutil.copy(D + f, B + f)
smooth_edges.run(names, B, 1.0, D)
print(len(names), "arkkia")
