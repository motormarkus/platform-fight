"""Laboratorion pano uudelleen: alkuperäinen (4408 px) + kuva 2 (rekisteröity: alkaa x=4341, kahvat kohdakkain) + kuva 4 (loppu, limitys 140 px (ks. OV):
litistetään pystysuunnassa niin, että putken yläreuna ja lattiaviiva täsmäävät kuvan 2 reunaan; kirkkaus sovitetaan; kapea sulautus (20 px, leveä tuottaa haamukuvaa)).
Jaetaan kahtia (enimmäisleveys 8192): muuri_laboratorio_pano.png (4408) + muuri_laboratorio_pano_b.png.
Ajo: python Tools/MoonAssets/make_lab_pano.py <kuva2.png> <loppu.png> [Assets/Sprites/Taustat]"""
import sys, os
import numpy as np, cv2
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from make_lab_corridor import load, scale_h, join

def rows(im, c0, c1):
    """(putken yläreunan rivi, lattiaviivan rivi) sarakkeista c0..c1"""
    g = im[:, c0:c1, :3].astype(np.float32)
    lum = g.mean(axis=(1, 2)); grad = np.gradient(lum)
    top = 50 + int(np.argmax(grad[50:140]))          # kattoa vasten kirkas putki: suurin nousu
    b, gg, r = g[..., 0].mean(1), g[..., 1].mean(1), g[..., 2].mean(1)
    yel = (r + gg) * 0.5 - b
    fl = 600 + int(np.argmax(yel[600:740]))
    return top, fl

ext, endp = sys.argv[1:3]
outd = sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Taustat"
o = cv2.imread(os.path.join(HERE, "src", "muuri_laboratorio_pano_alkuperainen.png"), cv2.IMREAD_UNCHANGED)
e = scale_h(load(ext)); n = scale_h(load(endp))
full = join(o, e, 4341)                     # rekisteröity ox; limitys 133 px, rivit 1 px siirretty
e0 = 4341
# ovi: kuvan 2 ovi on kapeampi kuin alkuperäisen (lehdet eivät täsmää) -> oikea ovenlehti tehdään peilaamalla vasen lehti oven keskisauman (x=4328) yli
AX, XE, Y0, Y1, F = 4328, 4613, 285, 690, 6
dst = full.astype(np.float32)
mir = o[Y0:Y1, 2 * AX - XE:AX + 1][:, ::-1].astype(np.float32)          # leveys XE-AX+1
w = mir.shape[1]
mx = np.ones(w, np.float32); mx[-F:] = np.linspace(1, 0, F)
my = np.ones(Y1 - Y0, np.float32); my[:F] = np.linspace(0, 1, F); my[-F:] = np.minimum(my[-F:], np.linspace(1, 0, F))
mk = (my[:, None] * mx[None, :])[..., None]
reg = dst[Y0:Y1, AX:AX + w]
dst[Y0:Y1, AX:AX + w] = reg * (1 - mk) + mir * mk
full = dst.clip(0, 255).astype(np.uint8)
# loppukuva: litistys pystysuunnassa
te, fe = rows(full, full.shape[1] - 40, full.shape[1]); tn, fn = rows(n, 0, 40)
a = (fe - te) / float(fn - tn); b = te - a * tn
print("reunat e:", te, fe, " n:", tn, fn, " -> kerroin", round(a, 4), "siirto", round(b, 1))
M = np.float32([[1, 0, 0], [0, a, b]])
n2 = cv2.warpAffine(n, M, (n.shape[1], n.shape[0]), flags=cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)
# kirkkaus: sovita n:n vasen reuna kuvan 2 oikeaan reunaan
ea = full[100:900, full.shape[1] - 40:, :3].reshape(-1, 3).mean(0); eb = n2[100:900, :40, :3].reshape(-1, 3).mean(0)
ratio = np.clip(ea / np.maximum(eb, 1), 0.85, 1.18); print("kirkkaus", ratio.round(3))
span = 700; ramp = np.linspace(0, 1, span, dtype=np.float32)
f = n2.astype(np.float32)
for c in range(3): f[:, :span, c] *= (1 + (ratio[c] - 1) * (1 - ramp))[None, :]
n2[..., :3] = f[..., :3].clip(0, 255).astype(np.uint8)
OV, FW = 140, 36    # loppukuvassa on limitystä: n alkaa e:n lopusta OV px taaksepäin; sulautus vain FW px keskellä (haamukuvia ei synny)
ox = full.shape[1] - OV; W2 = ox + n2.shape[1]
cv = np.zeros((941, W2, 4), np.float32); cv[:, :full.shape[1], :3] = full[..., :3]; cv[:, :full.shape[1], 3] = 255
nb = np.zeros((941, W2, 4), np.float32); nb[:, ox:] = n2
mid = ox + OV // 2
wt = np.clip((np.arange(W2) - (mid - FW // 2)) / float(FW), 0, 1).astype(np.float32)[None, :, None]
full = (cv * (1 - wt) + nb * wt).clip(0, 255).astype(np.uint8)
print("koko pano", full.shape[1])
cv2.imwrite(os.path.join(outd, "muuri_laboratorio_pano.png"), full[:, :4408]); cv2.imwrite(os.path.join(outd, "muuri_laboratorio_pano_b.png"), full[:, 4408:])
