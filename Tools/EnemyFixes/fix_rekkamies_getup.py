"""Rekkamiehen ylosnousu- ja alakoukku-kuvat: osa ruuduista oli 10-20 % isompia kuin taistelu/kävely. Skaalataan ruudut pivotin (alareunan keskikohta) ympäri.
Alkuperäinen: Tools/EnemyFixes/orig/rekkamies_ylosnousu.png. Ajo: python Tools/EnemyFixes/fix_rekkamies_getup.py"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CW, CH = 512, 448
JOBS = {
    "rekkamies_ylosnousu.png": [1.0, 0.92, 0.88, 0.85, 0.90, 0.90],
    "rekkamies_alakoukku.png": [1.0, 0.88, 0.86, 0.92, 0.94, 0.95],
}

for name, scales in JOBS.items():
    im = Image.open(os.path.join(ROOT, "Tools", "EnemyFixes", "orig", name)).convert("RGBA")
    out = Image.new("RGBA", im.size, (0, 0, 0, 0))
    for i, s in enumerate(scales):
        cell = im.crop((i * CW, 0, (i + 1) * CW, CH))
        if s != 1.0:
            w, h = round(CW * s), round(CH * s)
            sm = cell.resize((w, h), Image.LANCZOS)
            cell = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
            cell.alpha_composite(sm, ((CW - w) // 2, CH - h))
        out.alpha_composite(cell, (i * CW, 0))
    out.save(os.path.join(ROOT, "Assets", "Sprites", "Viholliset", name))
    print(name, "ok")
