"""Rubyn kirvespotku ja kierrepotku peliin: axe_kick/axe_NN.png + spin_kick/spin_NN.png (make_axe_gif.py, make_spin_gif.py)
-> Assets/Resources/Ruby/axe_NN.png, spin_NN.png. Kaikki samalle kankaalle: tukijalka keskelle, pohja alareunaan (pivot 0,5 / 0).
Mittakaava: Rubyn seisova taisteluasento = 322 px (sankaritar_idle), 100 ppu. Ajo: python Tools/RubyAssets/make_ruby_moves.py"""
import os, json
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(os.path.dirname(HERE)), "Assets", "Resources", "Ruby")
os.makedirs(OUT, exist_ok=True)
sets = {"axe": ("axe_kick", "axe_anchors.json", 12), "spin": ("spin_kick", "spin_anchors.json", 9)}
ref = Image.open(os.path.join(HERE, "axe_kick", "axe_11.png")).height      # seisova asento
K = 322.0 / ref
frames = {}
for name, (d, js, n) in sets.items():
    fx = json.load(open(os.path.join(HERE, js)))
    frames[name] = [(Image.open(os.path.join(HERE, d, "%s_%02d.png" % (name, i))), fx[i]) for i in range(n)]
half = max(max(a, im.width - a) for fl in frames.values() for im, a in fl) * K
Hc = max(im.height for fl in frames.values() for im, a in fl) * K
W, H = int(round(2 * half)) + 2, int(round(Hc)) + 2
for name, fl in frames.items():
    for old in os.listdir(OUT):
        if old.startswith(name + "_") and old.endswith(".png"): os.remove(os.path.join(OUT, old))
    for i, (im, a) in enumerate(fl):
        sm = im.resize((max(1, round(im.width * K)), max(1, round(im.height * K))), Image.LANCZOS)
        c = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        c.alpha_composite(sm, (int(round(W / 2 - a * K)), H - 1 - sm.height))
        c.save(os.path.join(OUT, "%s_%02d.png" % (name, i)))
print("ok", W, H, "px, kerroin", round(K, 3))
