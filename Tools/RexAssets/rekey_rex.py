"""Rexin videoista tehdyt arkit uudelleen siistimmällä avainnuksella (Tools/TonyAssets/rekey_tony.key_soft: ohut tumma reuna, sileä alfa,
vihreä hohto pois) ja valmiista kuvista tehdyt (suojaus, yläpotku, matala potku, alakoukku) reunat pehmennetään (smooth_edges).
Alkuperäiset: Tools/RexAssets/orig_edges/. Ajo: python Tools/RexAssets/rekey_rex.py [uploads-kansio]"""
import os, sys, shutil, tempfile
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets"))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
from rekey_tony import key_soft, use_soft_keys
import smooth_edges
use_soft_keys()
U = (sys.argv[1] if len(sys.argv) > 1 else "C:/Users/marku/.claude/uploads/04292e93-ed49-4896-966e-72709d802ca8") + "/"
tmp = tempfile.mkdtemp()
import make_rex_sheets, make_rex_hit, make_rex_kicks
make_rex_sheets.main(U + "420afd89-gemini_generated_video_7BA9B4E8.mp4", U + "6fb19074-gemini_generated_video_A3EE0993.mp4", tmp)
make_rex_hit.main(U + "852caacd-gemini_generated_video_4041063D.mp4", tmp)
make_rex_kicks.main(U + "f7844ed1-gemini_generated_video_F9F027A4.mov", U + "343611ec-copy_FB4AAD31-5025-499C-8EE1-8547D00BB3FB.mov", tmp)
for f in sorted(os.listdir(tmp)):
    shutil.copy(os.path.join(tmp, f), "Assets/Sprites/Viholliset/" + f); print("->", f)
smooth_edges.run(["rex_suojaus.png", "rex_ylapotku.png", "rex_matalapotku.png", "rex_alakoukku.png"], "Tools/RexAssets/orig_edges/")
