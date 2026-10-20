"""Salivälineet 3D-mallista spriteiksi (gym-pack-props.zip -> src/gym_pack.obj + textures). Käyttö: python make_gym_props.py <ulostulo-kansio>
Tulos: gym_penkkiasema.png, gym_penkki.png, gym_kyykkyrakki.png, gym_painoteline.png, gym_levytanko.png, gym_kasipaino_1..4.png, gym_levy_1..2.png.
Mittakaava: 1 cm = 3.333 px (sprite-PPU 200 -> 1 cm = 0.0167 yksikköä, hahmo 180 cm = 3 yksikköä). Mallin yksikkö on cm."""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(__file__))
from objload import load_obj
from render3d import Tex, render
from select3d import faces_in_box

PPCM = 10.0 / 3.0

def instance(V, VT, VN, F, face_ids, R, center, target):
    """Kopioi faces (kierto R keskipisteen ympäri, siirto kohtaan target); palauttaa uudet (V, VN, F) ja uusien kolmioiden indeksit."""
    vi = sorted({F[i][k][0] for i in face_ids for k in range(3)}); ni = sorted({F[i][k][2] for i in face_ids for k in range(3) if F[i][k][2] >= 0})
    vmap = {o: len(V) + n for n, o in enumerate(vi)}; nmap = {o: len(VN) + n for n, o in enumerate(ni)}
    V2 = np.vstack([V, (V[vi] - center) @ R.T + target]); VN2 = np.vstack([VN, VN[ni] @ R.T])
    F2 = list(F); new = []
    for i in face_ids:
        t = F[i]; F2.append(tuple((vmap[t[k][0]], t[k][1], nmap.get(t[k][2], -1) if t[k][2] >= 0 else -1) for k in range(3))); new.append(len(F2) - 1)
    return V2, VN2, F2, new

def save(img, path):
    n, lab, st, _ = cv2.connectedComponentsWithStats((img[..., 3] > 8).astype(np.uint8), connectivity=8)   # irtonaiset pisteet (viereisten esineiden palat) pois
    keepmask = np.zeros(img.shape[:2], bool)
    big = st[1:, cv2.CC_STAT_AREA].max()
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] > big * 0.02: keepmask |= lab == i
    img = img.copy(); img[~keepmask, 3] = 0
    ys, xs = np.where(img[..., 3] > 8)
    img = img[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(os.path.basename(path), img.shape[1], "x", img.shape[0])

def main(out):
    os.makedirs(out, exist_ok=True)
    V, VT, VN, F = load_obj(os.path.join(os.path.dirname(__file__), "src", "gym_pack.obj"))
    tex = Tex(os.path.join(os.path.dirname(__file__), "src", "textures", "GYM_Base_Color.tga"), os.path.join(os.path.dirname(__file__), "src", "textures", "GYM_AO.tga"))
    def r(ids, yaw=0, pitch=12, V_=None, VN_=None, F_=None, tint=None):
        ft = {i: tint for i in ids} if tint is not None and not isinstance(tint, dict) else tint
        return render(V_ if V_ is not None else V, VT, VN_ if VN_ is not None else VN, F_ if F_ is not None else F, ids, tex, yaw, pitch, ppcm=PPCM, face_tint=ft)
    # värit (kerroin harmaalle pohjalle): käsipainot musta / punainen / sininen / keltainen, levyt värikoodattu (sininen 20 kg, punainen 25 kg)
    DUMB = [(0.30, 0.30, 0.33), (1.35, 0.28, 0.25), (0.28, 0.5, 1.4), (1.4, 1.2, 0.22)]
    BLUE, RED = (0.30, 0.52, 1.35), (1.35, 0.28, 0.25)
    # kiinteät: penkkiasema (teline + penkki + tanko), erillinen penkki, kyykkyräkki, painoteline
    st_ids = sorted(set(faces_in_box(V, F, (-352, -146, 0, 200, -70, 70))) | set(faces_in_box(V, F, (-352, -120, 108, 128, -9, 9))))   # tangon oikea pää (x -170..-128) ylittää laatikon reunan: otetaan mukaan
    save(r(st_ids, yaw=-25, pitch=14), f"{out}/gym_penkkiasema.png")
    save(r(faces_in_box(V, F, (-146, -100, 0, 200, -70, 70)), yaw=90, pitch=16), f"{out}/gym_penkki.png")
    rk_ids = sorted(set(faces_in_box(V, F, (78, 300, 0, 240, -70, 70))) | set(faces_in_box(V, F, (78, 310, 138, 156, -30, -10))))   # tangon oikea pää (x 262..304) ylittää laatikon reunan
    save(r(rk_ids, yaw=-18, pitch=12), f"{out}/gym_kyykkyrakki.png")
    save(r(faces_in_box(V, F, (-256, -148, 0, 80, 115, 232)), pitch=14), f"{out}/gym_painoteline.png")
    # käsipainot (4 kpl, makaavat z-akselin suuntaisesti) sivusta ylhäältä
    for n, (x0, x1) in enumerate([(5, 17), (-11, -1), (24, 38), (45, 60)], 1):
        fi = faces_in_box(V, F, (x0, x1, 0, 14, 205, 250))
        if fi: save(r(fi, yaw=90, pitch=28, tint=DUMB[n - 1]), f"{out}/gym_kasipaino_{n}.png")
    # levyt (makaavat; näytetään päältä kallellaan)
    for n, box in enumerate([(2, 46, 0, 8, 160, 206), (47, 91, 0, 8, 160, 206)], 1):
        fi = faces_in_box(V, F, box)
        if fi: save(r(fi, yaw=180, pitch=50, tint=BLUE if n == 1 else RED), f"{out}/gym_levy_{n}.png")
    # ladattu levytanko: tanko (lattialla, x -111..111, z 145..154) + 2 levyä kummallekin puolelle, levyt kierretty tangon akselille
    bar = faces_in_box(V, F, (-112, 112, 0, 10, 143, 156))
    pl = faces_in_box(V, F, (2, 46, 0, 8, 160, 206))
    ids = list(bar)
    Rz = np.array([[0, 1, 0], [-1, 0, 0], [0, 0, 1.0]])    # levyn paksuus (y) tangon suuntaan (x)
    c = np.array([24.0, 2.5, 183.0]); bar_axis = np.array([0.0, 4.5, 149.5])
    V2, VN2, F2 = V, VN, F
    tints = {}
    for sgn in (-1, 1):
        for off, col in ((95.0, RED), (87.0, BLUE)):          # ulompi levy punainen, sisempi sininen
            V2, VN2, F2, new = instance(V2, VT, VN2, F2, pl, Rz, c, bar_axis + np.array([sgn * off, 0, 0]))
            ids += new
            for i in new: tints[i] = col
    save(r(ids, yaw=-28, pitch=22, V_=V2, VN_=VN2, F_=F2, tint=tints), f"{out}/gym_levytanko.png")

if __name__ == "__main__":
    main(sys.argv[1])
