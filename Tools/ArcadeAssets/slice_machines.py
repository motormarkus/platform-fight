"""Pelihallin pelikoneet: violetti tausta pois, rajaus -> Assets/Resources/Pelihalli/. Aja: python Tools/ArcadeAssets/slice_machines.py"""
import os, sys
import cv2
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "SpaceGame"))
import slice_sprites as S

OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Pelihalli")
os.makedirs(OUT, exist_ok=True)
for name, scale in (("kone_turpaan", 0.5), ("kone_ralli", 0.5), ("kone_flipper", 0.55)):
    img = cv2.imread(os.path.join(HERE, "src", name + ".png"), cv2.IMREAD_COLOR)
    rgba = S.trim(S.key(img), 2)
    rgba = S.resize(rgba, scale)
    cv2.imwrite(os.path.join(OUT, name + ".png"), rgba)
    print(name, rgba.shape)
