"""Roccon (pelaaja) idle/kävely/juoksu: kaksinkertainen ääriviiva pois (16.11.2026).
Commit 39d0b73f lisäsi paksun tumman ääriviivan alkuperäisen ohuen viivan ulkopuolelle -> kaksi viivaa. Palautetaan sitä edeltävä versio (c3205f04,
valkoisen reunan siivoukset mukana) ja tehdään myöhempien committien hehkunpoisto (alfa <= 40 -> 0).
Ajo: python Tools/RoccoAssets/restore_rocco_outline.py   (nykyiset tallessa Tools/RoccoAssets/orig_edges/)"""
import subprocess, io
import numpy as np
from PIL import Image
for n in ("idle", "kavely", "juoksu"):
    raw = subprocess.run(["git", "show", "c3205f04:Assets/Sprites/%s.png" % n], capture_output=True, check=True).stdout
    a = np.array(Image.open(io.BytesIO(raw)).convert("RGBA"))
    a[a[..., 3] <= 40] = 0
    Image.fromarray(a, "RGBA").save("Assets/Sprites/%s.png" % n); print(n, a.shape)
# rosoinen reuna sileäksi (smooth_edges, 1,0 px)
import sys, os, shutil, tempfile
sys.path.insert(0, "Tools/TonyAssets"); import smooth_edges
tmp = tempfile.mkdtemp() + "/"
for n in ("idle", "kavely", "juoksu"): shutil.copy("Assets/Sprites/%s.png" % n, tmp)
smooth_edges.run(["idle.png", "kavely.png", "juoksu.png"], tmp, 1.0, "Assets/Sprites/")
