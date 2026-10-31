"""Amatsoni: oma heitto (tarttuu, nostaa pään yli ja heittää) -> amatsoni_heitto.png (1000 x 832 ruudut, 6 saraketta, jalkojen keskikohta 330 px vasemmalta).
Ruudut videosta (1080p, 30 fps) 12-110 joka toinen (50 kuvaa). Kuva-ajat ja tarttuminen/heitto-kohdat ovat AmazonBrainissa (ote kuvassa 14, heitto kuvassa 36).
Ajo: python Tools/AmazonAssets/make_amazon_throw.py <video.mov> <ulos>"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import amazon_key as K
import make_amazon_sheets as M
M.CH = 832
v, out = sys.argv[1:3]
F = [M.rescale(f, M.SC) for f in K.load(v)]; fl, ax = M.feet(F[0])
idx = list(range(12, 111, 2))
cells = [M.cell(F[i], ax, fl, 1000, 330) for i in idx]
M.save(cells, 6, os.path.join(out, "amatsoni_heitto.png"))
