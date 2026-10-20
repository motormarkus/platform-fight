"""Counter-iskun 'thud'-äänet (3 vaihtoehtoa): matala jysähdys = combo-breaker. Syntetisoitu: laskeva siniaalto (150 -> 45 Hz), kohinapiikki ja napsahdus.
Tulos: Assets/Audio/sfx/counter_thud1.wav ... counter_thud3.wav (44,1 kHz, 16 bit, mono). Käyttö: python make_thud.py <ulostulo-kansio>"""
import sys, wave
import numpy as np

SR = 44100

def thud(f0, f1, decay, noise, seed, length=0.34):
    rng = np.random.default_rng(seed)
    t = np.arange(int(SR * length)) / SR
    f = f1 + (f0 - f1) * np.exp(-t * 28.0)                 # laskeva taajuus: jysähdys
    phase = 2 * np.pi * np.cumsum(f) / SR
    body = np.sin(phase) * np.exp(-t * decay)
    n = rng.standard_normal(len(t))
    k = 24                                                  # kohina alipäästettynä (liukuva keskiarvo): dumppi, ei sihinä
    n = np.convolve(n, np.ones(k) / k, mode="same")
    burst = n * np.exp(-t * 70.0) * noise
    click = np.zeros_like(t); click[:120] = np.hanning(240)[:120] * 0.9   # lyhyt napsahdus alkuun
    x = body + burst + click * 0.35
    x = np.tanh(x * 1.6)                                    # pehmeä leikkaus: voimaa
    fade = np.minimum(1.0, (len(t) - np.arange(len(t))) / (SR * 0.02))
    x = x * fade
    x /= np.max(np.abs(x))
    return (x * 0.92 * 32767).astype(np.int16)

def save(path, data):
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())

if __name__ == "__main__":
    out = sys.argv[1]
    for i, (f0, f1, dec, noi) in enumerate([(150, 48, 12, 0.55), (170, 55, 14, 0.7), (135, 42, 10, 0.45)], 1):
        save(f"{out}/counter_thud{i}.wav", thud(f0, f1, dec, noi, i))
        print("counter_thud%d.wav" % i)
