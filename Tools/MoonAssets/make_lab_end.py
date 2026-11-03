"""Laboratorion loppu: kuva 4 (käytäväaukko + kulmaseinä oikealla) liitetään panoraaman (kuvan 2 jälkeen) perään, ei limitystä (30 px sulautus).
Koko pano on yli 8192 px, joten se jaetaan kahtia: muuri_laboratorio_pano.png (4408 px) ja muuri_laboratorio_pano_b.png (loput).
Ajo: python Tools/MoonAssets/make_lab_end.py <loppu.png> [Assets/Sprites/Taustat]   (vain kerran: edellyttää että pano on 6434 px, ts. make_lab_corridor.py ajettu)"""
import sys, os
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from make_lab_corridor import load, scale_h, join
outd = sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Taustat"
lp = os.path.join(outd, "muuri_laboratorio_pano.png")
lab = cv2.imread(lp, cv2.IMREAD_UNCHANGED)
if lab.shape[1] != 6434: sys.exit("pano ei ole 6434 px (" + str(lab.shape[1]) + "): ei jatketa")
e = scale_h(load(sys.argv[1]))
full = join(lab, e, lab.shape[1] - 36)
print("koko pano", full.shape[1])
cv2.imwrite(lp, full[:, :4408]); cv2.imwrite(os.path.join(outd, "muuri_laboratorio_pano_b.png"), full[:, 4408:])
