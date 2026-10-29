"""Pakkaa Lätkäfanin ruudukkosheetit tiukoiksi atlaksiksi (jokainen kuva leikataan hahmon rajojen mukaan, ei skaalausta eikä pakkausta kuviin).
Jokaiselle sheetille syntyy <nimi>.png ja <nimi>.atlas.txt (rivi: indeksi x y w h ox yb; x,y = suorakulmion vasen yläkulma atlaksessa, ox = suorakulmion keskikohta miinus alkuperäisen ruudun keskikohta (px), yb = suorakulmion alareuna ruudun alareunasta (px)).
Unity tallentaa ox/yb:n spriten borderiin ja Enemy korjaa sillä sijainnin, joten kuva piirtyy täsmälleen kuin ruudukkona.
B: idle, hengitys ja kävely harvennetaan (joka toinen kuva).
Ajo (projektin juuresta): python Tools/LatkaAssets/pack_latka_atlas.py <ruudukko-kansio> <ulos-kansio>"""
import os, sys, glob
import numpy as np
from PIL import Image
PAD, ALIGN = 2, 4
STEP = {"apuri_luistelu": 2, "apuri_jarrutus": 2, "apuri_pudotus": 2, "apuri_osuma": 2, "apuri_kaatuminen": 2, "apuri_maahan": 2, "apuri_nousu": 2, "apuri_nyrkki_idle": 2, "apuri_nyrkki_horjunta": 2, "apuri_nyrkki_maahan": 2, "apuri_nyrkki_nousu": 2, "kapteeni_idle": 2, "kapteeni_kavely": 2, "kapteeni_kaatuminen": 2, "kapteeni_nousu": 2, "kapteeni_taklaus": 2}   # apuri: harvennus (kuvaa joka n:s)
CENTER = ("latkafani_osuma", "latkafani_kaatuminen", "latkafani_maahan")   # lento- ja kaatumiskuvat: hahmon massakeskipiste ruudun keskelle joka kuvassa (ei sivuttaista sätkimistä)
HALVE = ("latkafani_idle", "latkafani_hengitys", "latkafani_kavely")

def cell_size(n):
    if n in ("apuri_osuma", "apuri_kaatuminen", "apuri_maahan", "apuri_nousu", "apuri_pudotus") or n.startswith("apuri_nyrkki_"): return 1280, 768
    if n.startswith("kapteeni_"): return 1280, 768
    if n.startswith("apuri_"): return 1280, 704
    if n in ("latkafani_idle", "latkafani_hengitys"): return 640, 704   # leveämpi ruutu: käsi ei leikkaudu
    if n in ("latkafani_kavely", "latkafani_suoja"): return 512, 704
    if n == "latkafani_juoksu": return 768, 704
    if n in ("latkafani_polvipotku", "latkafani_etupotku", "latkafani_roundhouse"): return 896, 704
    if n == "latkafani_pyorahdys": return 1536, 704
    if n == "latkafani_heitto": return 1024, 896
    if n in ("latkafani_osuma", "latkafani_kaatuminen", "latkafani_maahan", "latkafani_nousu"): return 1280, 704
    return 1024, 704

def pack(src, outdir):
    n = os.path.splitext(os.path.basename(src))[0]; cw, ch = cell_size(n)
    im = Image.open(src).convert("RGBA"); a = np.asarray(im)
    frames = []
    for r in range(a.shape[0] // ch):
        for c in range(a.shape[1] // cw):
            cell = a[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw]
            if cell[..., 3].max() > 0: frames.append(cell)
    if n in HALVE: frames = frames[::2]
    if n in STEP: frames = frames[::STEP[n]]
    if n in CENTER:
        fixed = []
        for cell in frames:
            al = (cell[..., 3] > 8).astype(np.float64); xs = np.where(al.any(0))[0]
            cx = (al.sum(0) * np.arange(cw)).sum() / max(al.sum(), 1.0); sh = int(round(cw / 2 - cx)); sh = max(-xs.min() + 3, min(cw - 4 - xs.max(), sh))
            fixed.append(np.roll(cell, sh, axis=1))
        frames = fixed
    crops = []
    for cell in frames:
        al = cell[..., 3] > 8; ys, xs = np.where(al)
        x0, x1 = max(0, xs.min() - PAD), min(cw, xs.max() + 1 + PAD); y0, y1 = max(0, ys.min() - PAD), min(ch, ys.max() + 1 + PAD)
        if (x1 - x0) % 2: (x1 := x1 + 1) if x1 < cw else (x0 := x0 - 1)   # parillinen leveys: keskikohta osuu pikselirajalle (ei puolen pikselin siirtoa)
        crops.append((cell[y0:y1, x0:x1], x0, ch - y1))   # kuva, vasen reuna ruudussa, alareunan etäisyys ruudun alareunasta
    for W in (4096, 8192):
        order = sorted(range(len(crops)), key=lambda i: -crops[i][0].shape[0]); pos = {}; x = y = rowh = 0; ok = True
        for i in order:
            h, w = crops[i][0].shape[:2]; w4 = -(-w // ALIGN) * ALIGN + ALIGN; h4 = -(-h // ALIGN) * ALIGN + ALIGN
            if x + w4 > W: x = 0; y += rowh; rowh = 0
            pos[i] = (x, y); x += w4; rowh = max(rowh, h4)
        H = -(-(y + rowh) // ALIGN) * ALIGN
        if H <= 8192: break
    atlas = np.zeros((H, W, 4), np.uint8); lines = []
    for i, (cr, x0, yb) in enumerate(crops):
        px, py = pos[i]; h, w = cr.shape[:2]; atlas[py:py + h, px:px + w] = cr
        ox = int(round(x0 + w / 2 - cw / 2)); lines.append(f"{i} {px} {py} {w} {h} {ox} {yb}")
    Image.fromarray(atlas, "RGBA").save(os.path.join(outdir, n + ".png"), optimize=False)
    open(os.path.join(outdir, n + ".atlas.txt"), "w").write("\n".join(lines) + "\n")
    # tarkistus: rekonstruoi ruudut atlaksesta ja vertaa lähteeseen (pikselintarkka)
    for i, cell in enumerate(frames):
        _, px, py, w, h, ox, yb = map(int, lines[i].split()); x0 = int(round(ox + cw / 2 - w / 2))
        rec = np.zeros_like(cell); y1 = ch - yb; rec[y1 - h:y1, x0:x0 + w] = atlas[py:py + h, px:px + w]
        assert np.abs(rec.astype(int) - cell.astype(int))[..., :3][cell[..., 3] > 8].max() == 0 if (cell[..., 3] > 8).any() else True
    print(f"{n}: {len(frames)} kuvaa, {a.shape[1]}x{a.shape[0]} ({a.shape[1]*a.shape[0]/1e6:.1f} Mpx) -> {W}x{H} ({W*H/1e6:.1f} Mpx)")
    return a.shape[1] * a.shape[0], W * H

def main():
    src, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True); t0 = t1 = 0
    for f in sorted(glob.glob(os.path.join(src, sys.argv[3] if len(sys.argv) > 3 else "latkafani_*.png"))):
        a, b = pack(f, out); t0 += a; t1 += b
    print(f"yhteensä {t0/1e6:.1f} Mpx -> {t1/1e6:.1f} Mpx")
main()
