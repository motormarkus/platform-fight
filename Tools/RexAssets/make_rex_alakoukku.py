"""Rex: alakoukku (5 kuvaa vierekkäin, läpinäkyvä tausta) -> Assets/Sprites/Viholliset/rex_alakoukku.png.
Kuvajärjestys arkissa: vartiokanta, kyykky, syvä kyykky, nousu, huippu (nyrkki ylhäällä, osuma kuvassa 4), lisäksi palautus: nousu, vartiokanta (7 kuvaa).
Ajo: python Tools/RexAssets/make_rex_alakoukku.py <alakoukku.png> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from PIL import Image
import make_rex_kicks2 as k2

def main(path, outs):
    img = Image.open(path); W, H = img.size; cw = W // 5
    c = [(i * cw, 0, (i + 1) * cw, H) for i in range(5)]
    k2.build(img, [c[0], c[1], c[2], c[3], c[4], c[3], c[0]], 0, outs, "rex_alakoukku")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
