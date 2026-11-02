"""Timpan äänet: normalisointi (huippu -1 dBFS) ja kevyt high shelf -nosto (+3 dB, 2,8 kHz) -> Assets/Resources/Sfx/Timppa/<nimi>.mp3 (192 kbit/s, 44,1 kHz stereo).
Ajo: python Tools/MinerAssets/process_timppa_audio.py <lähdekansio> [kohde]
Nimet: 'timppa heitto.mp3' -> heitto.mp3, 'timppa hit1.mp3' -> hit1.mp3 jne."""
import os, sys, glob
import av, numpy as np

SR = 44100


def read(path):
    c = av.open(path); out = []
    rs = av.AudioResampler(format="fltp", layout="stereo", rate=SR)
    for fr in c.decode(audio=0):
        for r in rs.resample(fr): out.append(r.to_ndarray())
    for r in rs.resample(None): out.append(r.to_ndarray())
    return np.concatenate(out, axis=1).astype(np.float64)   # (2, n)


def highshelf(x, f0=2800.0, gain_db=3.0, S=0.8):
    A = 10 ** (gain_db / 40.0); w0 = 2 * np.pi * f0 / SR; cw, sw = np.cos(w0), np.sin(w0)
    alpha = sw / 2 * np.sqrt((A + 1 / A) * (1 / S - 1) + 2); sA = 2 * np.sqrt(A) * alpha
    b0 = A * ((A + 1) + (A - 1) * cw + sA); b1 = -2 * A * ((A - 1) + (A + 1) * cw); b2 = A * ((A + 1) + (A - 1) * cw - sA)
    a0 = (A + 1) - (A - 1) * cw + sA; a1 = 2 * ((A - 1) - (A + 1) * cw); a2 = (A + 1) - (A - 1) * cw - sA
    b = np.array([b0, b1, b2]) / a0; a = np.array([1.0, a1 / a0, a2 / a0])
    y = np.zeros_like(x)
    for ch in range(x.shape[0]):
        x1 = x2 = y1 = y2 = 0.0; xs = x[ch]; ys = y[ch]
        for n in range(xs.shape[0]):
            v = b[0] * xs[n] + b[1] * x1 + b[2] * x2 - a[1] * y1 - a[2] * y2
            ys[n] = v; x2, x1, y2, y1 = x1, xs[n], y1, v
    return y


def write(path, x):
    c = av.open(path, "w"); st = c.add_stream("libmp3lame", rate=SR); st.bit_rate = 192000; st.layout = "stereo"
    fr = av.AudioFrame.from_ndarray(np.ascontiguousarray(x.astype(np.float32)), format="fltp", layout="stereo"); fr.sample_rate = SR
    for p in st.encode(fr): c.mux(p)
    for p in st.encode(None): c.mux(p)
    c.close()


def main(src, dst):
    os.makedirs(dst, exist_ok=True)
    for f in sorted(glob.glob(os.path.join(src, "timppa *.mp3"))):
        name = os.path.basename(f)[len("timppa "):].replace(" ", "_")
        x = highshelf(read(f))
        pk = np.abs(x).max(); x = x * (10 ** (-1 / 20) / pk) if pk > 0 else x
        write(os.path.join(dst, name), x); print(name, round(x.shape[1] / SR, 2), "s, huippu alussa", round(float(pk), 3))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Resources/Sfx/Timppa")
