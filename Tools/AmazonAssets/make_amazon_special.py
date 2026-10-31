"""Amatsoni: erikoisliike (tuplakyrkkihyppy kuperkeikalla) -> amatsoni_hyppy.png (1000 x 832 ruudut, 6 saraketta, jalkojen keskikohta 330 px vasemmalta, jalat 16 px ylhäällä).
Ruudut videosta (1080p, kiinteä kamera, 30 fps): 22, 24, 26 (2/30 s), 28-44 joka ruutu (1/30 s, osumahetki 30-42), 46-96 joka toinen (2/30 s).
Ajo: python Tools/AmazonAssets/make_amazon_special.py <video.mov> <ulos>"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import amazon_key as K
import make_amazon_sheets as M
M.CH = 832
v, out = sys.argv[1:3]
F = [M.rescale(f, M.SC) for f in K.load(v)]; fl, ax = M.feet(F[0])
idx = [22, 24, 26] + list(range(28, 45)) + list(range(46, 97, 2))
print("kuvia", len(idx), "osumahetki ruudut", idx.index(30), "-", idx.index(42))
cells = [M.cell(F[i], ax, fl, 1000, 330) for i in idx]
M.save(cells, 6, os.path.join(out, "amatsoni_hyppy.png"))
