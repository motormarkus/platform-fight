"""Uudet osumaäänet: poistaa alun tyhjän ja lopun hiljaisuuden, miksaa monoksi ja tasaa voimakkuuden vanhojen osumaäänien (Audio/sfx/hit*) tasolle.
Lähde: Lataukset-kansio. Tulos: Assets/Resources/Sfx/Hit/*.wav (Resources, jotta HitFx ja Enemy lataavat ne koodista).
Aja: python Tools/AudioLevels/make_hit_sounds.py"""
import os
import wave
import av
import numpy as np

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(os.path.expanduser("~"), "Downloads")
OUT = os.path.join(ROOT, "Assets", "Resources", "Sfx", "Hit")
os.makedirs(OUT, exist_ok=True)

FILES = {   # lähde -> kohde (mono wav)
    "littleguy hit.mp3": "voice_small_a",
    "littleguyhit2.mp3": "voice_small_b",
    "big guy hit.mp3": "voice_big_a",
    "Big guy hit2.mp3": "voice_big_b",
    # viuhahdukset (hitviuh1-5.wav) kopioidaan sellaisenaan Resources/Sfx/Hit/viuh_N.wav: käsittely säröytti ne
}


def load(path):
    c = av.open(path)
    sr = c.streams.audio[0].rate
    x = np.concatenate([f.to_ndarray() for f in c.decode(audio=0)], axis=1).astype(np.float32)
    c.close()
    if np.abs(x).max() > 2:
        x /= 32768.0
    return sr, x.mean(axis=0)


def rms(x):
    return float(np.sqrt(np.mean(x * x)) + 1e-9)


# vanhojen osumaäänien taso (tehollisarvo sen jälkeen, kun alku ja loppu on rajattu)
ref = []
for i in (1, 2, 3):
    p = os.path.join(ROOT, "Assets", "Audio", "sfx", f"hit{i}.wav")
    if os.path.exists(p):
        sr, x = load(p)
        m = np.abs(x) > np.abs(x).max() * 0.05
        idx = np.where(m)[0]
        ref.append(rms(x[idx[0]:idx[-1] + 1]))
target = float(np.mean(ref)) if ref else 0.12
print("vertailutaso (rms)", round(target, 3))

for name, dst in FILES.items():
    sr, x = load(os.path.join(SRC, name))
    peak = float(np.abs(x).max())
    thr = peak * 0.04
    idx = np.where(np.abs(x) > thr)[0]
    start = max(0, idx[0] - int(sr * 0.004))      # 4 ms ennen ensimmäistä ääntä (alun tyhjä pois)
    end = min(len(x), idx[-1] + int(sr * 0.03))     # 30 ms hännän jälkeen
    if dst.startswith("viuh_"):
        start, end = 0, len(x)      # viuhahdukset: ei leikkausta, vain voimakkuuden tasaus (alun hiljainen osa kuuluu ääneen)
        y = x.copy()
    else:
        y = x[start:end].copy()
        fade = int(sr * 0.03)
        y[-fade:] *= np.linspace(1, 0, fade)
        y[:8] *= np.linspace(0, 1, 8)
    tgt = 0.10 if dst.startswith("voice_") else 0.07 if dst.startswith("viuh_") else target   # viuhahdukset: hiljaisia lähtötiedostoja nostettu kohtuullisesti (rms 0,07)   # iskuhuudot samalle tasolle kuin hahmojen omat äänet (gasp/attack ~0,05-0,1), osumat vanhojen osumien tasolle
    g = tgt / rms(y)
    y = y * g
    pk = float(np.abs(y).max())
    if pk > 0.95:
        y *= 0.95 / pk
    pcm = (np.clip(y, -1, 1) * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, dst + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr); w.writeframes(pcm.tobytes())
    print(f"{name:26s} -> {dst}.wav  alku leikattu {start / sr * 1000:.0f} ms, kesto {len(y) / sr:.2f} s, vahvistus x{g:.2f}")
