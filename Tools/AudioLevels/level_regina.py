# Reginan äänet (käyttäjän zip, mp3) -> Assets/Audio/regina/*.wav, tasattuna samoille tavoitetasoille kuin level_hits.py:
# puhe -14, isku/potku/kaatuminen/häviö -15, kipu/nousu -17 dBFS. Alkuperäiset (muunnettuna wav:ksi) -> Tools/AudioLevels/alkuperaiset/.
# Käyttö: python Tools/AudioLevels/level_regina.py <purettu zip-kansio>
import os, sys, shutil, numpy as np, soundfile as sf
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from measure_audio import loudness_db
from level_hits import soft_limit, BACKUP, PEAK_MAX, MAX_GAIN, MIN_GAIN
src_dir = sys.argv[1]
NAMES = {   # zipin tiedosto -> (pelin nimi, tavoite dBFS, puhe?)
 "rEGINAHIT1.mp3": ("regina_hit1", -15, 0), "REGINAHIT2.mp3": ("regina_hit2", -15, 0), "REGINA HIT3.mp3": ("regina_hit3", -15, 0),
 "REGINAHIT4.mp3": ("regina_hit4", -15, 0), "REGINAHIT5.mp3": ("regina_hit5", -15, 0),
 "REgina kick1.mp3": ("regina_kick1", -15, 0), "REGINA KICK2.mp3": ("regina_kick2", -15, 0),
 "reginapain1.mp3": ("regina_pain1", -17, 0), "reginapain2.mp3": ("regina_pain2", -17, 0), "Reginapain3.mp3": ("regina_pain3", -17, 0), "reginapain4.mp3": ("regina_pain4", -17, 0),
 "Regina fall1.mp3": ("regina_fall1", -15, 0), "reginafall2.mp3": ("regina_fall2", -15, 0), "regina loses.mp3": ("regina_loses", -15, 0),
 "regina nousu1.mp3": ("regina_nousu1", -17, 0), "regina nousu2.mp3": ("regina_nousu2", -17, 0), "regina nousu3 (2).mp3": ("regina_nousu3", -17, 0),
 "regina speak1.mp3": ("regina_speak1", -14, 1), "regina speak2.mp3": ("regina_speak2", -14, 1), "regina speak3.mp3": ("regina_speak3", -14, 1),
 "regina taistelu speak.mp3": ("regina_taistelu_speak", -14, 1), "regina welcome speak.mp3": ("regina_welcome_speak", -14, 1),
}
for f, (name, target, speech) in NAMES.items():
    d, sr = sf.read(os.path.join(src_dir, f), always_2d=True)
    path = "Assets/Audio/regina/" + name + ".wav"
    bk = BACKUP + "/" + path; os.makedirs(os.path.dirname(bk), exist_ok=True)
    sf.write(bk, d, sr, subtype="PCM_16")
    db, _ = loudness_db(d.mean(axis=1), sr); peak = float(np.max(np.abs(d)))
    g = float(np.clip(10 ** ((target - db) / 20.0), MIN_GAIN, 8.0 if speech else min(MAX_GAIN, PEAK_MAX / max(peak, 1e-4))))
    out = np.clip(soft_limit(d * g) if speech else d * g, -1, 1)
    sf.write(path, out, sr, subtype="PCM_16")
    db2, _ = loudness_db(out.mean(axis=1), sr)
    print("%-24s %5.2fs  %6.1f -> %6.1f dBFS (x%.2f, tavoite %d)%s" % (name, len(d) / sr, db, db2, g, target, "" if abs(db2 - target) < 1.5 else "  <- huippu rajoittaa"))
