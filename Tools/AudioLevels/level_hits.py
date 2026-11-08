# Osuma-, kipu- ja gasp-äänten sekä puheiden tasaus suoraan tiedostoihin (kolme ryhmää, kukin omalle tasolleen).
# Mittaa kaikki hit/isku/osuma- ja pain/kipu/gasp/grunt/aah-äänet (puheet eivät kuulu mukaan) ja skaalaa jokaisen tiedoston
# oman ryhmänsä tavoitetasolle (ryhmän mediaani). Hiljaisia nostetaan enintään MAX_GAIN, mutta huippu pysyy alle 0 dBFS (ei säröä).
# Tasaus tehdään tiedostoihin, koska pelin tasaustaulukko (AudioLevels.json) tunnistaa äänet vain nimen ja keston perusteella:
# monella hahmolla on samannimisiä hit1/pain1-ääniä, jolloin taulukko sekoittaisi ne. Tasattujen äänten rivit poistetaan taulukosta
# (kerroin 1), muut rivit (puheet ym.) jäävät ennalleen.
# Alkuperäiset tiedostot kopioidaan ensin kansioon Tools/AudioLevels/alkuperaiset/ (samassa kansiorakenteessa); ajo käyttää aina niitä
# lähteenä, joten skriptin voi ajaa uudelleen ilman että ääni heikkenee kerta kerralta.
# Käyttö: python Tools/AudioLevels/level_hits.py [--report]
import os, sys, json, glob, shutil, argparse
import numpy as np
import soundfile as sf
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from measure_audio import loudness_db

SCAN = ["Assets/Audio", "Assets/Resources"]
SKIP_DIRS = ("Musiikki", "vehicles")
HIT = ("hit", "isku", "osuma", "slap", "punch", "impact")
PAIN = ("pain", "kipu", "gasp", "grunt", "aah", "hurt", "ouch")
SPEECH = ("speak", "puhe", "welcome", "repliikki", "tervetuloa")   # puheet (oma ryhmä)
NOT = ("lataus", "taunt", "voice", "say")
MAX_GAIN, MIN_GAIN, PEAK_MAX = 4.0, 0.2, 0.97
BACKUP = "Tools/AudioLevels/alkuperaiset"

def group(name):
    n = name.lower()
    if any(k in n for k in SPEECH): return "puhe"
    if any(k in n for k in NOT): return None
    if any(k in n for k in PAIN): return "kipu"
    if any(k in n for k in HIT): return "osuma"
    return None

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--report", action="store_true"); a = ap.parse_args()
    items = []
    for base in SCAN:
        for path in glob.glob(base + "/**/*", recursive=True):
            if not path.lower().endswith((".mp3", ".wav", ".ogg")): continue
            norm = path.replace("\\", "/")
            if any(("/" + s + "/") in norm for s in SKIP_DIRS): continue
            name = os.path.splitext(os.path.basename(norm))[0]
            g = group(name)
            if g is None: continue
            src = BACKUP + "/" + norm if os.path.exists(BACKUP + "/" + norm) else norm   # aina alkuperäisestä
            try: d, sr = sf.read(src, always_2d=True)
            except Exception as e: print("ohitettu (lukuvirhe):", norm, e); continue
            db, peak = loudness_db(d.mean(axis=1), sr)
            if db is None: continue
            items.append(dict(path=norm, src=src, data=d, sr=sr, db=db, peak=float(np.max(np.abs(d))), group=g,
                              key=name + "|" + str(int(round(len(d) / sr * 100)))))
    with open("Assets/Resources/AudioLevels.json", encoding="utf-8") as f: table = json.load(f)
    old = dict(zip(table["names"], table["gains"]))
    for g in ("osuma", "kipu", "puhe"):
        grp = [i for i in items if i["group"] == g]
        if not grp: continue
        target = float(np.median([i["db"] for i in grp]))
        before = [i["db"] + 20 * np.log10(old.get(i["key"], 1.0)) for i in grp]   # nykyinen kuuluvuus pelissä (vanha taulukon kerroin mukana)
        for i in grp:
            i["gain"] = float(np.clip(10 ** ((target - i["db"]) / 20.0), MIN_GAIN, min(MAX_GAIN, PEAK_MAX / max(i["peak"], 1e-4))))
        after = [i["db"] + 20 * np.log10(i["gain"]) for i in grp]
        print("%s: %d ääntä, tavoite %.1f dBFS | nyt pelissä %.1f..%.1f (hajonta %.1f dB) | tasattuna %.1f..%.1f (hajonta %.1f dB)"
              % (g, len(grp), target, min(before), max(before), np.std(before), min(after), max(after), np.std(after)))
        if a.report:
            for i, b in sorted(zip(grp, before), key=lambda z: z[1]):
                print("   %6.1f -> %6.1f  (x%.2f)  %s" % (b, i["db"] + 20 * np.log10(i["gain"]), i["gain"], i["path"]))
    if a.report: return
    removed = 0
    for i in items:
        if "gain" not in i: continue
        bk = BACKUP + "/" + i["path"]
        if not os.path.exists(bk):
            os.makedirs(os.path.dirname(bk), exist_ok=True); shutil.copy2(i["path"], bk)
        info = sf.info(i["src"])
        out = np.clip(i["data"] * i["gain"], -1.0, 1.0)
        if i["path"].lower().endswith(".mp3"): sf.write(i["path"], out, i["sr"], format="MP3", compression_level=0.0)
        else: sf.write(i["path"], out, i["sr"], subtype=info.subtype, format=info.format)
        if i["key"] in old: del old[i["key"]]; removed += 1
    table["names"] = list(old.keys()); table["gains"] = list(old.values())
    with open("Assets/Resources/AudioLevels.json", "w", encoding="utf-8") as f: json.dump(table, f, ensure_ascii=False, indent=1)
    print("tasattu %d tiedostoa (alkuperäiset: %s), taulukosta poistettu %d riviä" % (sum("gain" in i for i in items), BACKUP, removed))

if __name__ == "__main__":
    main()
