"""Flipperin kuvat: rajaa läpinäkyvyyden mukaan, keskittää neliöön ja pienentää (alkuperäiset Tools/FlipperAssets/orig)."""
import os
from PIL import Image
here = os.path.dirname(__file__)
src = os.path.join(here, "orig"); dst = os.path.join(here, "..", "..", "Assets", "Resources", "Flipper")

def square(name, size):
    im = Image.open(os.path.join(src, name)).convert("RGBA")
    im = im.crop(im.split()[3].getbbox())
    s = max(im.size)
    c = Image.new("RGBA", (s, s), (0, 0, 0, 0)); c.paste(im, ((s - im.width) // 2, (s - im.height) // 2))
    c.resize((size, size), Image.LANCZOS).save(os.path.join(dst, name))

for n in ("pallo.png", "pompp_0.png", "pompp_1.png", "pompp_2.png"):
    square(n, 256)
im = Image.open(os.path.join(src, "maila.png")).convert("RGBA")
im = im.crop(im.split()[3].getbbox())          # 1939 x 784, pyörähdyspiste (229, 366) kuvassa
im.resize((1000, round(1000 * im.height / im.width)), Image.LANCZOS).save(os.path.join(dst, "maila.png"))
bg = Image.open(os.path.join(src, "pohja.png")).convert("RGB")
bg.resize((760, round(760 * bg.height / bg.width)), Image.LANCZOS).save(os.path.join(dst, "pohja.png"))
print("ok")
