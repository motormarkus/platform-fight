"""Kassakaapin avautuminen (käyttäjän kuva, 3x2 ruutua á 512 px, läpinäkyvä tausta) -> vaakanauha Assets/Sprites/Props/kassakaappi.png.
Ruudut rajataan samasta kohdasta (kaapin runko ja jalat ovat kaikissa samassa paikassa; ovi aukeaa vasemmalle), 6 kuvaa á 464x424.
Ajo: python Tools/MotelAssets/make_safe_sheet.py <kuva.png>"""
import os, sys
import cv2, numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
X0, X1, Y0, Y1 = 16, 480, 56, 480


def main(src):
    im = cv2.imread(src, cv2.IMREAD_UNCHANGED)
    if im.shape[2] != 4: sys.exit("kuvassa ei ole alfaa")
    frames = []
    for r in range(2):
        for c in range(3):
            cell = im[r * 512:(r + 1) * 512, c * 512:(c + 1) * 512].copy()
            fr = cell[Y0:Y1, X0:X1].copy()
            fr[fr[..., 3] < 8] = 0
            frames.append(fr)
    out = np.hstack(frames)
    dst = os.path.join(ROOT, "Assets", "Sprites", "Props", "kassakaappi.png")
    cv2.imwrite(dst, out)
    print("kassakaappi", out.shape[1], "x", out.shape[0], "->", dst)


if __name__ == "__main__":
    main(sys.argv[1])
