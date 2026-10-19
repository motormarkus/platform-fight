"""Arvio tekstuurimuistista scenekohtaisesti: mitä tekstuureja kukin scene viittaa suoraan (guid-viittaukset).
Käyttö projektin juuresta: python Tools/MemoryReportScenes.py
Kertoo, paljonko pääscene lataa alussa ja paljonko kukin kenttäscene lisää vasta kun sinne mennään.
Ei sisällä Resources.Load-kuvia, ääniä eikä Unityn omaa muistia; arvio, ei Profilerin luku."""
import glob
import re
from PIL import Image

Image.MAX_IMAGE_PIXELS = None


def platform(text, target):
    block = text[text.find("buildTarget: " + target):] if "buildTarget: " + target in text else ""
    block = block[:700]
    ms = re.search(r"maxTextureSize: (\d+)", block)
    comp = re.search(r"textureCompression: (\d)", block)
    ov = re.search(r"overridden: (\d)", block)
    return (int(ms.group(1)) if ms else None, int(comp.group(1)) if comp else None, int(ov.group(1)) if ov else 0)


tex = {}  # guid -> (MB, path)
for meta in glob.glob("Assets/**/*.meta", recursive=True):
    path = meta[:-5].replace("\\", "/")
    if not path.lower().endswith((".png", ".jpg", ".jpeg", ".tga", ".psd")):
        continue
    text = open(meta, encoding="utf-8", errors="ignore").read()
    g = re.search(r"^guid: (\w+)", text, re.M)
    if not g:
        continue
    try:
        w, h = Image.open(path).size
    except Exception:
        continue
    d, s = platform(text, "DefaultTexturePlatform"), platform(text, "Standalone")
    ms, comp = (s[0], s[1]) if s[2] == 1 and s[0] else (d[0], d[1])
    ms = ms or 2048
    comp = 1 if comp is None else comp
    mip = re.search(r"enableMipMap: (\d)", text)
    mip = int(mip.group(1)) if mip else 0
    k = min(1.0, ms / max(w, h))
    tex[g.group(1)] = (w * k * h * k * (4 if comp == 0 else 1) * (1.33 if mip else 1) / 1e6, path)

scenes = ["Assets/Scenes/SampleScene.unity"] + sorted(glob.glob("Assets/Scenes/Areas/*.unity"))
used = {}
for sc in scenes:
    t = open(sc, encoding="utf-8", errors="ignore").read()
    used[sc] = {g for g in set(re.findall(r"guid: (\w{32})", t)) if g in tex}


def mb(guids):
    return sum(tex[g][0] for g in guids)


main = used[scenes[0]]
allg = set().union(*used.values())
print("Tekstuurit, jotka scenet viittaavat suoraan (arvio, MB):")
for sc in scenes:
    others = set().union(*[v for k, v in used.items() if k != sc])
    print("  %-45s %6d MB  (%d kuvaa, vain tässä: %d MB)" % (sc.split("/")[-1], mb(used[sc]), len(used[sc]), mb(used[sc] - others)))
print("Yhteensä kaikki scenet: %d MB" % mb(allg))
print("Pelin alussa ladataan vain pääscene: %d MB (%d%% kaikesta)" % (mb(main), 100 * mb(main) / max(1, mb(allg))))
