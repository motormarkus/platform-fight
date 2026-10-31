"""amatsoni_suudelma.mp3 -> Assets/Resources/Sfx/Amatsoni/suudelma.wav (puhe 0-2,35 s) ja suukko.wav (suukon läiskäys ~2,72-2,90 s). Ajo projektin juuresta."""
import av, numpy as np, wave, os
SRC = r"C:\Users\marku\.claude\uploads\3422f5da-b08d-4942-ae8f-cb9c1ccfe0f6\144e26a6-amatsoni_suudelma.mp3"
OUT = 'Assets/Resources/Sfx/Amatsoni/'
c = av.open(SRC); r = av.AudioResampler(format='fltp', layout='mono', rate=44100); d = []
for fr in c.decode(audio=0):
    for o in r.resample(fr): d.append(o.to_ndarray()[0])
x = np.concatenate(d); sr = 44100
def save(name, a, b, peak=0.85):
    s = x[int(a * sr):int(b * sr)].copy(); s *= peak / max(1e-6, abs(s).max())
    n = int(0.004 * sr); s[:n] *= np.linspace(0, 1, n); s[-n:] *= np.linspace(1, 0, n)
    os.makedirs(OUT, exist_ok=True)
    with wave.open(OUT + name, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(sr); w.writeframes((s * 32767).astype(np.int16).tobytes())
    print(name, '%.2f s' % (len(s) / sr))
save('suudelma.wav', 0.0, 2.35)
save('suukko.wav', 2.70, 2.92)
