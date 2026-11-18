"""Porrastuneen ääriviivan pehmennys valmiiseen arkkiin (kun videosta ei voi tehdä uudelleen, esim. käsin korjattu tony_kaatuminen).
Alfa: 3x suurennos, pehmennys ja kynnys -> sileä reuna, takaisin INTER_AREA. Väri: reunan uudet pikselit lähimmästä peittävästä.
Lähde Tools/TonyAssets/orig_edges/<nimi>.png -> Assets/Sprites/Viholliset/<nimi>.png. Ajo: python Tools/TonyAssets/smooth_edges.py tony_kaatuminen.png ..."""
import sys
import numpy as np, cv2
from PIL import Image
UP = 3
def run(names, src="Tools/TonyAssets/orig_edges/", sigma=0.8, dst="Assets/Sprites/Viholliset/"):
  for n in names:
    im = np.array(Image.open(src + n).convert("RGBA"))
    H, W = im.shape[:2]
    a = im[..., 3].astype(np.float32) / 255
    big = cv2.resize(a, (W * UP, H * UP), interpolation=cv2.INTER_CUBIC)
    big = cv2.GaussianBlur(big, (0, 0), UP * sigma)
    big = np.clip((big - 0.5) * 6 + 0.5, 0, 1)
    na = cv2.resize(big, (W, H), interpolation=cv2.INTER_AREA)
    rgb = im[..., :3].copy()
    core = (im[..., 3] >= 200).astype(np.uint8)
    new = (na > 0.02) & (core == 0)
    _, lbl = cv2.distanceTransformWithLabels(1 - core, cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
    ys, xs = np.nonzero(core); lut = np.zeros((lbl.max() + 1, 3), np.uint8); lut[lbl[ys, xs]] = rgb[ys, xs]
    rgb[new] = lut[lbl[new]]
    r_, g_, b_ = (rgb[..., i].astype(np.int16) for i in range(3))
    rgb[..., 1] = np.minimum(g_, np.maximum(r_, b_) + 4).astype(np.uint8)   # vihreä hohto pois
    Image.fromarray(np.dstack([rgb, (na * 255).astype(np.uint8)]), "RGBA").save(dst + n)
    print(n, "ok")

if __name__ == "__main__":
    run(sys.argv[1:])
