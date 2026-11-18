"""NPC-hahmojen kansioiden kuvat: rosoinen reuna sileäksi (smooth_edges 0,8 px). Alkuperäiset Tools/EdgeBatch/orig_npc/<kansio>/ (otetaan kerran).
Ajo: python Tools/EdgeBatch/smooth_npc.py"""
import os, sys, shutil
sys.path.insert(0, "Tools/TonyAssets"); import smooth_edges
DIRS = ["Sohvi", "Myyjat", "Helvi", "Liz", "Mary", "Maria", "Rokkimimmi", "Bandi", "Joulupukki", "Candi", "Lumilauta"]
for d in DIRS:
    src = "Assets/Sprites/%s/" % d; bak = "Tools/EdgeBatch/orig_npc/%s/" % d; os.makedirs(bak, exist_ok=True)
    names = sorted(f for f in os.listdir(src) if f.endswith(".png"))
    for f in names:
        if not os.path.exists(bak + f): shutil.copy(src + f, bak + f)
    smooth_edges.run(names, bak, 0.8, src); print(d, len(names))
