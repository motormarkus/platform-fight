"""Olion huone (kÃ¤ytÃ¤vÃ¤n varoitusoven takana): kaksi kuvaa perÃ¤kkÃ¤in, korkeus 941.
Kuva 1 (olion_huone_hakki.png): sisÃ¤Ã¤nkÃ¤ynti vasemmalla (varoitusovi) + rikottu hakki; kuva 2 (olion_huone_ovi_oikealla.png): laboratoriotilaa, varoitusovi oikeassa pÃ¤Ã¤ssÃ¤.
Ajo: python Tools/MoonAssets/make_beast_room.py [seam_px]   (seam = limityksen leveys, oletus 24)"""
import sys, numpy as np, cv2
H = 941
S = "Tools/MoonAssets/src/"
OUT = "Assets/Sprites/Taustat/muuri_olion_huone_pano.png"
def sc(p):
    im = cv2.imread(p); w = round(im.shape[1] * H / im.shape[0]); return cv2.resize(im, (w, H), interpolation=cv2.INTER_CUBIC)
a = sc(S + "olion_huone_hakki.png"); b = sc(S + "olion_huone_ovi_oikealla.png")
ov = int(sys.argv[1]) if len(sys.argv) > 1 else 24
CUT = 112   # kuvan 2 vasen pilari jätetään pois (kuvan 1 oikea pilari on jo vieressä)
b = b[:, CUT:]
# kirkkaus: sovita b:n vasen reuna a:n oikeaan
ea = a[300:, -ov:].reshape(-1, 3).mean(0); eb = b[300:, :ov].reshape(-1, 3).mean(0)
print("kirkkaus a/b", (ea / eb).round(3))
w = np.linspace(0, 1, ov, dtype=np.float32)[None, :, None]
seam = a[:, -ov:].astype(np.float32) * (1 - w) + b[:, :ov].astype(np.float32) * w
res = np.concatenate([a[:, :-ov], seam.astype(np.uint8), b[:, ov:]], axis=1)
cv2.imwrite(OUT, res); print(OUT, res.shape[1], "x", res.shape[0], "sauma x =", a.shape[1] - ov)
x = a.shape[1] - ov
cv2.imwrite("Tools/MoonAssets/src/_sauma.png", res[:, max(0, x - 400):x + 400])
