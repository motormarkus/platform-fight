"""Härkävideon äänet: Assets/Resources/Sfx/Taikuri/harka_muutos.wav (0,8-4,2 s: napsautus, taikasäteet, muodonmuutos) ja harka_juoksu.wav (4,2-7,2 s: kaviot ja tuhina).
Ajo: python Tools/MagicianAssets/make_bull_audio.py <video>"""
import sys, os, wave
import av, numpy as np
V = sys.argv[1]; c = av.open(V); res = av.AudioResampler(format="s16", layout="stereo", rate=44100); ch = []
for fr in c.decode(audio=0):
    for f in res.resample(fr): ch.append(f.to_ndarray().reshape(-1, 2))
x = np.concatenate(ch).astype(np.float32); sr = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Resources", "Sfx", "Taikuri"); os.makedirs(OUT, exist_ok=True)
def save(name, t0, t1, fin=0.03, fout=0.25):
    seg = x[int(t0 * sr):int(t1 * sr)].copy()
    seg[:int(fin * sr)] *= np.linspace(0, 1, int(fin * sr))[:, None]; seg[-int(fout * sr):] *= np.linspace(1, 0, int(fout * sr))[:, None]
    w = wave.open(os.path.join(OUT, name + ".wav"), "wb"); w.setnchannels(2); w.setsampwidth(2); w.setframerate(sr); w.writeframes(np.clip(seg, -32768, 32767).astype(np.int16).tobytes()); w.close()
save("harka_muutos", 0.8, 4.2); save("harka_juoksu", 4.2, 7.2)
