"""WTF-areenan kehän etuosa: kulmapylväät (punainen vasen, sininen oikea), niiden pehmusteet, kolme etuköyttä ja sivuköydet
leikataan katsomon kuvista 2 ja 3 omaksi kuvakseen, joka piirretään kehässä ottelevien eteen (lattialla kulkevat piirtyvät sen eteen).
Kuvat 2 ja 3 ovat 180 px päällekkäin: yhteinen koordinaatisto cx = paneeli * 4940 + x (paneeli 1 = kuva 2, 2 = kuva 3).
Ulos: Assets/Sprites/Taustat/vegas_areena_keha_etu.png + rivi vegas_areena_keha_etu.txt: "cx0 y0 leveys korkeus" (kuvan vasen yläkulma).
Ajo: python Tools/ArenaAssets/make_arena_ring_front.py"""
import os
import cv2, numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
T = os.path.join(ROOT, "Assets", "Sprites", "Taustat")
STEP = 4940
X0, X1, Y0, Y1 = STEP + 1140, 2 * STEP + 4160, 1170, 2292      # rajaus yhteisissä koordinaateissa
P2, P3 = STEP, 2 * STEP

ROPES = [(1241, 1276), (1448, 1497), (1631, 1667)]              # etuköydet (rivit, ääriviivat mukana)
PADS = [(1225, 1336), (1408, 1516), (1594, 1706)]               # pylväiden pehmusteiden rivit


def main():
    m2 = cv2.imread(os.path.join(T, "vegas_areena_katsomo_2.png"), cv2.IMREAD_COLOR)
    m3 = cv2.imread(os.path.join(T, "vegas_areena_katsomo_3.png"), cv2.IMREAD_COLOR)
    W, H = X1 - X0, Y1 - Y0
    img = np.zeros((H, W, 3), np.uint8)
    s2 = X0 - P2                                   # kuvan 2 sarake rajauksen vasemmassa reunassa
    img[:, 0:5120 - s2] = m2[Y0:Y1, s2:5120]
    b = P3 - X0                                    # kuva 3 alkaa tästä sarakkeesta (päällekkäinen alue on sama)
    img[:, b:] = m3[Y0:Y1, 0:W - b]
    mask = np.zeros((H, W), np.uint8)

    def rect(cx0, cx1, r0, r1):
        mask[max(0, r0 - Y0):max(0, r1 - Y0), max(0, cx0 - X0):max(0, cx1 - X0)] = 255

    # pylväät: runko ja pohjalevy
    rect(P2 + 1199, P2 + 1357, 1183, 2242); rect(P2 + 1150, P2 + 1392, 2234, 2292)
    rect(P3 + 3832, P3 + 3999, 1183, 2242); rect(P3 + 3808, P3 + 4048, 2234, 2292)
    # pehmusteet (pylvään sisäpuolella): GrabCut rajauksesta, ettei taustan kulmia jää mukaan
    for r0, r1 in PADS:
        for cx0, cx1 in ((P2 + 1316, P2 + 1582), (P3 + 3618, P3 + 3834)):
            x0, x1, y0, y1 = cx0 - X0 - 12, cx1 - X0 + 12, r0 - Y0 - 12, r1 - Y0 + 12
            sub = np.ascontiguousarray(img[y0:y1, x0:x1]); gm = np.zeros(sub.shape[:2], np.uint8)
            cv2.grabCut(sub, gm, (12, 12, x1 - x0 - 24, y1 - y0 - 24), np.zeros((1, 65), np.float64), np.zeros((1, 65), np.float64), 6, cv2.GC_INIT_WITH_RECT)
            fg = ((gm == cv2.GC_FGD) | (gm == cv2.GC_PR_FGD)).astype(np.uint8)
            n, lab, st, _ = cv2.connectedComponentsWithStats(fg, 8)
            if n > 1: fg = (lab == 1 + int(np.argmax(st[1:, 4]))).astype(np.uint8)
            fg = cv2.morphologyEx(fg, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
            mask[y0:y1, x0:x1][fg > 0] = 255
    # etuköydet pylväältä pylväälle
    for r0, r1 in ROPES:
        rect(P2 + 1360, P3 + 3830, r0, r1)
    # sivuköydet: kirkkaanpunainen köysi vinossa kaistassa pehmusteelta taaksepäin
    f = img.astype(np.int16)
    red = ((f[..., 2] > 150) & (f[..., 1] < 110) & (f[..., 0] < 110)).astype(np.uint8)
    for (x0, y0), (x1, y1) in (((P2 + 1480, 1625), (P2 + 1870, 1455)), ((P3 + 3300, 1455), (P3 + 3700, 1625))):
        band = np.zeros_like(mask)
        cv2.line(band, (x0 - X0, y0 - Y0), (x1 - X0, y1 - Y0), 255, 70)
        side = cv2.morphologyEx(red & (band > 0), cv2.MORPH_CLOSE, np.ones((7, 7), np.uint8))
        n, lab, st, _ = cv2.connectedComponentsWithStats(side, 8)
        side = np.isin(lab, [i for i in range(1, n) if st[i, 4] >= 1500]).astype(np.uint8)   # irtopisteet (messinki, kädet) pois
        side = cv2.dilate(side, np.ones((5, 5), np.uint8))          # tumma ääriviiva mukaan
        mask[side > 0] = 255
    alpha = cv2.GaussianBlur(mask, (3, 3), 0.8)
    out = np.dstack([img, alpha]); out[alpha == 0] = 0
    cv2.imwrite(os.path.join(T, "vegas_areena_keha_etu.png"), out)
    with open(os.path.join(T, "vegas_areena_keha_etu.txt"), "w") as fh: fh.write(f"{X0} {Y0} {W} {H}\n")
    print("kehän etuosa", W, "x", H, "kohdassa", X0, Y0)


if __name__ == "__main__":
    main()
