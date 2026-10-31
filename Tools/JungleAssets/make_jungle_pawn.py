"""Kanikonttorin sisus: kaksi generaattorin kuvaa (1672 x 940, alfa; ovi vasemmalla) -> viidakko_pantti.png (2659 x 941) + tiskin etuosa viidakko_pantti_tiski.png.
Kuvat limittyvät: kuva 2 alkaa x = 987 (mitattu mallintunnistuksella, pisteet 0,85); limitys (685 px) sulatetaan lineaarisesti.
Alfa 240-254 -> 255, lähes läpinäkyvät pikselit (alfa < 16) puhdistetaan. Ajo: python Tools/JungleAssets/make_jungle_pawn.py <kuva 1> <kuva 2>"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_jungle_assets as J
SHIFT = 987
def prep(p): return J.clear_glass(J.fit(J.bleed(J.solidify(J.rd(p)))))
def main(p1, p2):
    a, b = prep(p1), prep(p2); W, H = J.W, J.H
    out = np.zeros((H, SHIFT + W, 4), np.float32)
    out[:, :W] = a.astype(np.float32)
    ov = W - SHIFT                                           # limitysleveys
    # premultiplied-sekoitus: alue [SHIFT, W) sulatetaan a -> b
    t = np.linspace(0, 1, ov, dtype=np.float32)[None, :, None]
    A = a[:, SHIFT:].astype(np.float32); B = b[:, :ov].astype(np.float32)
    aa, ba = A[..., 3:4] / 255, B[..., 3:4] / 255
    wa, wb = (1 - t) * aa, t * ba
    al = wa + wb
    rgb = (A[..., :3] * wa + B[..., :3] * wb) / np.maximum(al, 1e-4)
    out[:, SHIFT:W, :3] = rgb; out[:, SHIFT:W, 3] = np.clip(al[..., 0] * 255, 0, 255) * 1.0
    out[:, W:] = b[:, ov:].astype(np.float32)
    # ikkunoiden/oven läpinäkyvyys: alfa >= 240 -> 255 yhdistelmäalueella
    o = out.clip(0, 255).astype(np.uint8); m = o[..., 3] >= 240; o[..., 3][m] = 255
    o = J.clear_glass(o)
    J.save("viidakko_pantti.png", o)
    J.save("viidakko_pantti_tiski.png", o[462:682, 890:2035])
    print("ok", o.shape)
main(*sys.argv[1:3])
