"""Viidakon kiitotie, hangaari ja parallaksit generaattorin kuvista -> Assets/Sprites/Taustat/viidakko_*.png.
Ajo: python Tools/JungleAssets/make_jungle_assets.py <kiitotie> <hangaari> <sisa vasen> <sisa keski> <sisa oikea> <taka (vuoret)> <etu (palmut)>
 kiitotie: 1671 x 941 (alfa, taivas lapinakyva), tyhja kiitotie. hangaari: sama kiitotie vasemmalla, hangaarin ulkoseina ja ovi oikeassa paassa (alfa).
 sisa vasen/keski/oikea: hangaarin sisakuvat vasemmalta oikealle (ikkunat alfa-aukkoina, parallaksi nakyy niista lapi).
Tulosteet: viidakko_kiitotie_1.png + _2.png (toinen peilikuva: keskisauma tasmaa, ja suunta valitaan niin, etta sarjan oikea reuna sopii hangaarikuvan vasempaan reunaan),
 viidakko_hangaari.png (oikea paaty), viidakko_sisa_1..3.png (1672 x 941), viidakko_taka.png (vuoret + peilikuva), viidakko_etu.png (palmurivi + peilikuva, saumaton).
Alfa 240-254 -> 255 (generaattorin kuvat ovat ~1 % lapinakyvia; parallaksi vuotaisi lapi). Lapinakyvien alueiden reunavari korvataan viereisella varilla."""
import os, sys
import numpy as np, cv2

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Sprites", "Taustat")
W, H = 1672, 941


def bleed(bgra, sigma=3.0):
    """Puolilapinakyvien reunapikselien vari otetaan lahelta peittavista pikseleista."""
    f = bgra.astype(np.float32); a = f[..., 3] / 255.0
    solid = (a > 0.97).astype(np.float32)
    num = cv2.GaussianBlur(f[..., :3] * solid[..., None], (0, 0), sigma)
    den = cv2.GaussianBlur(solid, (0, 0), sigma)[..., None]
    fill = num / np.maximum(den, 1e-3)
    w = np.clip((den - 0.02) / 0.1, 0, 1) * (a < 0.97)[..., None]
    f[..., :3] = f[..., :3] * (1 - w) + fill * w
    return f.clip(0, 255).astype(np.uint8)


def solidify(img):
    """Alfa >= 240 -> 255."""
    if img.shape[2] == 4:
        a = img[..., 3]; a[a >= 240] = 255
    return img


def clear_glass(im, n=10):
    """Lähes läpinäkyvät (alfa < 16) ikkunapikselit: alfa 0 ja väri naapureista (generaattorin punainen/musta ei värjää ikkunan reunaa)."""
    a = im[..., 3]; clear = a < 16
    rgb = im[..., :3].copy(); known = ~clear
    for _ in range(n):
        s = cv2.blur(np.where(known[..., None], rgb, 0).astype(np.float32), (3, 3)); c = cv2.blur(known.astype(np.float32), (3, 3))[..., None]
        new = (c > 0) & ~known[..., None]
        if not new.any(): break
        rgb = np.where(new, (s / np.maximum(c, 1e-3)).astype(np.uint8), rgb); known = known | new[..., 0]
    im[..., :3] = rgb; im[..., 3] = np.where(clear, 0, a)
    return im


def rd(p):
    im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if im is None: raise SystemExit("ei luettu: " + p)
    if im.shape[2] == 3: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
    return im


def fit(img):
    if img.shape[1] != W or img.shape[0] != H: img = cv2.resize(img, (W, H), interpolation=cv2.INTER_CUBIC)
    return img


def save(name, img):
    cv2.imwrite(os.path.join(OUT, name), img)


def main(run_p, hang_p, left_p, mid_p, right_p, back_p, front_p):
    B = fit(bleed(solidify(rd(run_p)))); Hg = fit(bleed(solidify(rd(hang_p))))
    # kiitotie_1 + peilikuva _2 + hangaari: valitaan suunta, jossa kiitotie_2:n oikea reuna (= kiitotie_1:n vasen reuna) sopii hangaarikuvan vasempaan reunaan
    row = slice(640, H)   # lattia (taivas on lapinakyva)
    d_dir = np.abs(B[row, 0, :3].astype(float) - Hg[row, 0, :3]).mean()
    d_flip = np.abs(B[row, -1, :3].astype(float) - Hg[row, 0, :3]).mean()
    B1 = B if d_dir <= d_flip else B[:, ::-1].copy()
    print("sauma kiitotie -> hangaari: suora", round(d_dir, 1), "peilattu", round(d_flip, 1))
    save("viidakko_kiitotie_1.png", B1)
    save("viidakko_kiitotie_2.png", B1[:, ::-1].copy())
    save("viidakko_hangaari.png", Hg)
    for i, p in enumerate((left_p, mid_p, right_p), 1):
        save("viidakko_sisa_%d.png" % i, clear_glass(fit(bleed(solidify(rd(p))))))
    # taka: vuoret + peilikuva
    M = cv2.imread(back_p, cv2.IMREAD_UNCHANGED)
    if M.shape[2] == 4: M = M[..., :3]
    M = cv2.resize(M, (W, H), interpolation=cv2.INTER_AREA) if M.shape[1] != W else M
    save("viidakko_taka.png", np.concatenate([M, M[:, ::-1]], axis=1))
    # etu: palmurivi + peilikuva (kuten taka): reunat ovat peilikuvaa, joten sauma ei nay
    F = bleed(solidify(rd(front_p)))
    save("viidakko_etu.png", np.concatenate([F, F[:, ::-1]], axis=1))
    V1 = F
    print("ok", V1.shape)


if __name__ == "__main__":
    main(*sys.argv[1:8])
