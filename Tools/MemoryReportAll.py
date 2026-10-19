"""Arvio KAIKKIEN projektin tekstuurien muistista (myös Resources.Load-kuvat, joita scene ei viittaa suoraan).
Käyttö projektin juuresta: python Tools/MemoryReportAll.py
Lukee oikeat Default/Standalone-alustan asetukset (maxTextureSize, pakkaus); mipmapit +33 %.
Pakkaus: 0 = pakkaamaton 4 B/px, 1-3 = BC7/DXT5 ~1 B/px."""
import glob
import re
from PIL import Image

Image.MAX_IMAGE_PIXELS = None


def platform(text, target):
    """Alustan lohko: (maxTextureSize, textureCompression, overridden) tai None."""
    m = re.search(r"buildTarget: %s\n(.*?)\n    -?\s*serializedVersion|buildTarget: %s\n((?:      .*\n|    .*\n)+)" % (target, target), text, re.S)
    block = text[text.find("buildTarget: " + target):] if "buildTarget: " + target in text else ""
    block = block[:700]
    ms = re.search(r"maxTextureSize: (\d+)", block)
    comp = re.search(r"textureCompression: (\d)", block)
    ov = re.search(r"overridden: (\d)", block)
    return (int(ms.group(1)) if ms else None, int(comp.group(1)) if comp else None, int(ov.group(1)) if ov else 0)


rows, total = [], 0.0
for meta in glob.glob("Assets/**/*.meta", recursive=True):
    path = meta[:-5].replace("\\", "/")
    if not path.lower().endswith((".png", ".jpg", ".jpeg", ".tga", ".psd")):
        continue
    text = open(meta, encoding="utf-8", errors="ignore").read()
    try:
        w, h = Image.open(path).size
    except Exception:
        continue
    d = platform(text, "DefaultTexturePlatform")
    s = platform(text, "Standalone")
    ms, comp = (s[0], s[1]) if s[2] == 1 and s[0] else (d[0], d[1])
    ms = ms or 2048
    comp = 1 if comp is None else comp
    mip = re.search(r"enableMipMap: (\d)", text)
    mip = int(mip.group(1)) if mip else 0
    k = min(1.0, ms / max(w, h))
    mb = w * k * h * k * (4 if comp == 0 else 1) * (1.33 if mip else 1) / 1e6
    total += mb
    rows.append((mb, path, w, h, ms, comp))
rows.sort(reverse=True)
print("kaikki tekstuurit", len(rows), "arvio", int(total), "MB")
by = {}
for r in rows:
    key = "/".join(r[1].split("/")[1:3])
    by[key] = by.get(key, 0) + r[0]
for key, v in sorted(by.items(), key=lambda x: -x[1])[:10]:
    print(" ", key, int(v), "MB")
print("suurimmat:")
for r in rows[:12]:
    print(" ", int(r[0]), "MB", r[1], r[2], "x", r[3], "max", r[4], "pakkaus", r[5])
