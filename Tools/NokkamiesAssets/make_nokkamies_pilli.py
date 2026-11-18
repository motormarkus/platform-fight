"""Nokkamiehen pilli (vihellys): koko liike videosta (vartio -> pilli liivin taskusta -> vihellys -> takaisin taskuun -> vartio), joka 4. ruutu.
Sama siisti reuna ja mittakaava kuin muilla Nokkamiehen kuvilla (make_nokkamies_sheets: soft_key, clean_rescale, ääriviiva; 1. ruudun korkeus = 600 px).
Tulos: Assets/Sprites/Viholliset/nokkamies_pilli.png + dims.json (yhdistetään). Pilli suulla noin videoruudusta 84 alkaen (tulostetaan sheetin indeksinä).
Ajo: python Tools/NokkamiesAssets/make_nokkamies_pilli.py <pilli.mp4> [ulos]"""
import os, sys, json
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import make_nokkamies_sheets as nk

def main(video, outs):
    V = nk.load(video)
    x0, x1, y0, y1 = nk.m.bbox(V[0]); k = nk.TARGET_H / (y1 - y0); foot = y1
    idx = list(range(8, len(V) - 8, 8)) + [len(V) - 1]   # joka 8. ruutu: vihellys lyhyt (~1,5 s)
    dims = {}
    s = nk.Sheet("pilli"); [s.add(V[i], "low", k, foot) for i in idx]; s.save(outs, dims)
    mouth = next(n for n, i in enumerate(idx) if i >= 88)
    print("kuvat", idx); print("pilli suulla: sheetin indeksi", mouth)
    p = os.path.join(HERE, "dims.json"); d = json.load(open(p)); d.update(dims); json.dump(d, open(p, "w"), indent=1)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
