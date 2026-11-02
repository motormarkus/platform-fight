"""Kuurobotin äänet: +1,5 dB ja high shelf (+4 dB, 3 kHz, rinne kattaa ~2-5 kHz). Ajo: python eq_robot_sfx.py <kansio> (alkuperäiset kannattaa varmuuskopioida: ajo ylikirjoittaa)."""
import sys, glob, os
import numpy as np, av

def lfilter(b, a, x):
    y = np.zeros_like(x); x1 = x2 = y1 = y2 = 0.0
    for n in range(len(x)):
        xn = float(x[n]); yn = b[0] * xn + b[1] * x1 + b[2] * x2 - a[1] * y1 - a[2] * y2
        x2, x1, y2, y1 = x1, xn, y1, yn; y[n] = yn
    return y

GAIN_DB, SHELF_DB, F0 = 0.0, 4.0, 3000.0   # tason nosto poistettu (käyttäjän sfx-taso oli hiljainen); high shelf jää
SHELF_ONLY = ("speak", "glitch")
EXTRA_DB = {"speak3": -7.5, "pan_hit": 4.0}   # robodance oli liian kova; pannun ääni (kopio) tarvitsee nostoa   # robodance oli liian kova

def high_shelf(fs, f0, gain_db, S=0.8):
    A = 10 ** (gain_db / 40); w = 2 * np.pi * f0 / fs; cw, sw = np.cos(w), np.sin(w)
    al = sw / 2 * np.sqrt((A + 1 / A) * (1 / S - 1) + 2); sq = 2 * np.sqrt(A) * al
    b0 = A * ((A + 1) + (A - 1) * cw + sq); b1 = -2 * A * ((A - 1) + (A + 1) * cw); b2 = A * ((A + 1) + (A - 1) * cw - sq)
    a0 = (A + 1) - (A - 1) * cw + sq; a1 = 2 * ((A - 1) - (A + 1) * cw); a2 = (A + 1) - (A - 1) * cw - sq
    return np.array([b0, b1, b2]) / a0, np.array([a0, a1, a2]) / a0

def load(path):
    c = av.open(path); st = c.streams.audio[0]; fs = st.rate; ch = []
    for fr in c.decode(st): ch.append(fr.to_ndarray().astype(np.float32))
    a = np.concatenate(ch, axis=1)
    if a.dtype != np.float32: a = a.astype(np.float32)
    # planar (kanavat x näytteet); kokonaisluku-muodot skaalataan
    return a, fs

def main(folder):
    for f in sorted(glob.glob(os.path.join(folder, "*.wav")) + glob.glob(os.path.join(folder, "*.mp3"))):
        c = av.open(f); st = c.streams.audio[0]; fs = st.rate; fmt = st.format.name
        frames = [fr.to_ndarray() for fr in c.decode(st)]
        a = np.concatenate(frames, axis=1) if frames[0].ndim == 2 else np.concatenate(frames)[None, :]
        if a.dtype.kind in "iu": a = a.astype(np.float32) / np.iinfo(a.dtype).max
        else: a = a.astype(np.float32)
        if "p" not in fmt and a.shape[0] == 1 and len(frames[0].shape) == 2 and st.channels > 1:   # lomitettu -> kanavat
            a = a.reshape(-1, st.channels).T
        shelf = os.path.basename(f).startswith(SHELF_ONLY) and not os.path.basename(f).startswith("speak3")   # robodance (speak3) ilman shelfiä   # high shelf vain puheelle
        b, aa = high_shelf(fs, F0, SHELF_DB)
        out = np.stack([lfilter(b, aa, ch) if shelf else ch for ch in a]) * 10 ** ((GAIN_DB + next((v for k, v in EXTRA_DB.items() if os.path.basename(f).startswith(k)), 0.0)) / 20)
        if os.path.basename(f).startswith("nousu"): out *= 0.8 / max(np.abs(out).max(), 1e-6)   # alkuperäinen on ~26 dB muita hiljaisempi: nostetaan huippuun 0,8
        t = 0.8; m_ = np.abs(out); out = np.where(m_ < t, out, np.sign(out) * (t + (1 - t) * np.tanh((m_ - t) / (1 - t))))   # pehmeä rajoitin: ei kovaa leikkausta, taso säilyy
        ext = os.path.splitext(f)[1]
        tmp = f + ".tmp" + ext
        o = av.open(tmp, "w"); layout = "mono" if out.shape[0] == 1 else "stereo"
        s = o.add_stream("pcm_s16le" if ext == ".wav" else "libmp3lame", rate=fs); s.layout = layout
        if ext == ".mp3": s.bit_rate = 128000
        fo = av.AudioFrame.from_ndarray(np.ascontiguousarray((out * 32767).astype(np.int16)), format="s16p", layout=layout); fo.sample_rate = fs
        for p in s.encode(fo): o.mux(p)
        for p in s.encode(None): o.mux(p)
        o.close(); c.close(); os.replace(tmp, f)
        print(os.path.basename(f), fs, out.shape, "huippu", round(float(np.abs(out).max()), 3))

if __name__ == "__main__": main(sys.argv[1])
