"""Kaytava: alkuperainen pano + avoimen oven kuva (olion huone nakyy ovesta) + paatyseina. RGBA (ikkunat lapinakyvia).
Lahteet: src/muuri_kaytava_pano_alkuperainen.png, src/muuri_kaytava_ovi_auki_pano.png (korkeus 941), src/muuri_kaytava2_pano.png (paatyseina).
Ajo: python Tools/MoonAssets/make_corridor_end.py"""
import numpy as np, cv2
S = "Tools/MoonAssets/src/"
c = cv2.imread(S + "muuri_kaytava_pano_alkuperainen.png", cv2.IMREAD_UNCHANGED)
n = cv2.imread(S + "muuri_kaytava_ovi_auki_pano.png", cv2.IMREAD_UNCHANGED)
e = cv2.imread(S + "muuri_kaytava2_pano.png")
SC, NX, NY = 1.015, 2348, 10      # avoimen oven kuva: skaala, x ja y vanhaan panoon (pilarit ja ikkuna osuvat)
n = cv2.resize(n, None, fx=SC, fy=SC, interpolation=cv2.INTER_CUBIC)
def paste(base, im, x, y):
    h = min(im.shape[0], base.shape[0] - y); base[y:y + h, x:x + im.shape[1]] = im[:h]
SEAM, FW = 145, 30     # sauma pilarin kohdalla (kuvan x)
# paatyseina: etsi kohta
W0 = NX + n.shape[1]
tail = np.zeros((941, W0, 4), np.uint8); tail[..., 3] = 255; cw = min(c.shape[1], W0); tail[:, :cw] = c[:, :cw]
nb = tail.copy(); paste(nb, n, NX, NY)
m = cv2.matchTemplate(nb[250:650, NX + 1600:, :3], e[250:650, 130:460], cv2.TM_CCOEFF_NORMED); _, mx, _, loc = cv2.minMaxLoc(m)
ox = NX + 1600 + loc[0] - 130; print("paatykuva alkaa", ox, "vastaavuus", round(mx, 3))
W = ox + e.shape[1]
out = np.zeros((941, W, 4), np.uint8); out[..., 3] = 255
cw = min(c.shape[1], W); out[:, :cw] = c[:, :cw]
# avoin ovi: saumataan vanhaan pilarin kohdalla
s0 = NX + SEAM
paste(out, n, NX, NY)       # rivit 0..NY-1 jaavat vanhasta
w = np.linspace(0, 1, FW, dtype=np.float32)[None, :, None]
old = c[:, s0:s0 + FW].astype(np.float32); new = out[:, s0:s0 + FW].astype(np.float32)
blend = old * (1 - w) + new * w; blend[..., 3] = new[..., 3]
out[:, NX:s0] = c[:, NX:s0]; out[:, s0:s0 + FW] = blend.clip(0, 255).astype(np.uint8)
# paatyseina
sx = 350
t0 = ox + sx
ec = e.astype(np.float32); cc = out[:, :, :3].astype(np.float32)
k = cc[300:600, t0 - 40:t0].reshape(-1, 3).mean(0) / ec[300:600, sx - 40:sx].reshape(-1, 3).mean(0); print("kirkkaus", k.round(3)); ec *= k
w2 = np.linspace(0, 1, FW, dtype=np.float32)[None, :, None]
out[:, t0:t0 + FW, :3] = (cc[:, t0:t0 + FW] * (1 - w2) + ec[:, sx:sx + FW] * w2).clip(0, 255).astype(np.uint8)
out[:, t0:t0 + FW, 3] = 255
out[:, t0 + FW:, :3] = ec[:, sx + FW:].clip(0, 255).astype(np.uint8); out[:, t0 + FW:, 3] = 255
cv2.imwrite("Assets/Sprites/Taustat/muuri_kaytava_pano.png", out); print(out.shape)
cv2.imwrite(S + "_kayt_a.png", cv2.cvtColor(out[:, 2300:4000, :3], cv2.COLOR_BGR2BGR565) if False else out[:, 3300:4800, :3])
cv2.imwrite(S + "_kayt_b.png", out[:, 4000:W, :3])
