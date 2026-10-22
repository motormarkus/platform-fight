# Äänten voimakkuuksien mittaus ja tasaus: mittaa kaikki puheet ja tehosteet (ei musiikkia/ambienssia), laskee jokaiselle vahvistuskertoimen,
# jolla kaikki soivat samalla voimakkuudella, ja kirjoittaa Assets/Resources/AudioLevels.json (peli lukee sen: AudioLevels.cs).
# Käyttö: python Tools/AudioLevels/measure_audio.py [--target -22] [--report]
import os, sys, json, glob, argparse
import numpy as np
import soundfile as sf

ROOT = "Assets"
SCAN = ["Assets/Audio", "Assets/Resources/Sfx", "Assets/Resources/Poker/Voice", "Assets/Resources/Poker/Sfx", "Assets/Resources/Pickups", "Assets/Resources/Syonti"]
SKIP_DIRS = ("Musiikki", "vehicles")                     # musiikki ja ajoneuvojen moottorit eivät kuulu tasaukseen
SKIP_WORDS = ("ambien", "loop", "musiikki", "music", "motor", "engine", "rata", "train", "juna")   # ambienssit ja silmukat
MAX_SECONDS = 12.0                                       # pidemmät ovat tyypillisesti silmukoita/ambienssia
MAX_GAIN = 1.0                                           # PlayOneShot-voimakkuutta ei nosteta yli 1: kovat vaimennetaan tavoitetasolle, hiljaisemmat jäävät ennalleen
MIN_GAIN = 0.15

def load_mono(path):
    d, sr = sf.read(path, always_2d=True)
    return d.mean(axis=1).astype(np.float64), sr

def loudness_db(x, sr):
    """Aktiivisten osien RMS (dBFS): hiljaiset välit ja alku/loppu eivät vaikuta."""
    if len(x) < 64: return None, None
    peak = float(np.max(np.abs(x)))
    if peak < 1e-4: return None, None
    n = max(1, int(sr * 0.05)); m = len(x) // n
    if m < 1: return None, peak
    fr = x[: m * n].reshape(m, n)
    rms = np.sqrt((fr ** 2).mean(axis=1))
    act = rms > (rms.max() * 0.25)
    val = float(np.sqrt((rms[act] ** 2).mean())) if act.any() else float(rms.max())
    return 20 * np.log10(max(val, 1e-6)), peak

def category(name):
    n = name.lower()
    if any(k in n for k in ("speak", "welcome", "taunt", "puhe", "voice", "say", "huuto", "gasp", "grunt", "pain", "fall")): return "puhe"
    if any(k in n for k in ("lyo", "isku", "hit", "punch", "slap", "kick", "osuma", "potku", "smash", "crash")): return "isku"
    return "muu"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--target", type=float, default=None, help="tavoitevoimakkuus dBFS (oletus: mediaani)")
    ap.add_argument("--percentile", type=float, default=50.0, help="jos tavoitetta ei anneta: mikä prosenttipiste otetaan (50 = mediaani, 20 = hiljaisempi)")
    ap.add_argument("--report", action="store_true", help="vain raportti, ei kirjoiteta tiedostoa")
    a = ap.parse_args()
    items = []
    for base in SCAN:
        for path in glob.glob(base + "/**/*", recursive=True):
            if not path.lower().endswith((".mp3", ".wav", ".ogg")): continue
            norm = path.replace("\\", "/")
            if any(("/" + s + "/") in norm for s in SKIP_DIRS): continue
            name = os.path.splitext(os.path.basename(path))[0]
            if any(w in name.lower() for w in SKIP_WORDS): continue
            try:
                x, sr = load_mono(path)
            except Exception as e:
                print("ohitettu (lukuvirhe):", path, e); continue
            if len(x) / sr > MAX_SECONDS: continue
            db, peak = loudness_db(x, sr)
            if db is None: continue
            items.append((name + "|" + str(int(round(len(x) / sr * 100))), norm, db, peak, category(name)))
    dbs = np.array([i[2] for i in items])
    target = a.target if a.target is not None else float(np.percentile(dbs, a.percentile))
    print("ääniä: %d  voimakkuus dBFS: min %.1f, mediaani %.1f, max %.1f  -> tavoite %.1f dBFS" % (len(items), dbs.min(), np.median(dbs), dbs.max(), target))
    out = {}; dup = set()
    for name, path, db, peak, cat in items:
        g = float(np.clip(10 ** ((target - db) / 20.0), MIN_GAIN, MAX_GAIN))
        if name in out and abs(out[name] - g) > 0.01: dup.add(name)
        out[name] = round(g, 3)
    if dup: print("HUOM: sama nimi ja kesto, eri voimakkuus (viimeinen voittaa):", sorted(dup)[:10])
    print("kertoimia: <0.5: %d, 0.5-0.8: %d, >0.8: %d" % (sum(v < 0.5 for v in out.values()), sum(0.5 <= v <= 0.8 for v in out.values()), sum(v > 0.8 for v in out.values())))
    for cat in ("puhe", "isku", "muu"):
        c = [(n, d) for n, p, d, pk, ct in items if ct == cat]
        if c: print("  %s: %d kpl, mediaani %.1f dBFS" % (cat, len(c), np.median([d for n, d in c])))
    if a.report: return
    os.makedirs("Assets/Resources", exist_ok=True)
    data = {"targetDb": round(target, 2), "names": list(out.keys()), "gains": list(out.values())}
    with open("Assets/Resources/AudioLevels.json", "w", encoding="utf-8") as f: json.dump(data, f, ensure_ascii=False, indent=1)
    print("kirjoitettu Assets/Resources/AudioLevels.json")

if __name__ == "__main__":
    main()
