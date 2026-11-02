"""Kuurobotin äänet samalle tasolle kuin muut viholliset (RMS ~ -13 dB, huippu -1 dB) ja pehmeän rajoittimen läpi.
Käyttö: python normalize_robot_sfx.py <alkuperäiset-kansio> <ulos-kansio>   (alkuperäiset: mp3/wav; pan_hit.wav = Pan/pannu_hit_2.wav; speak3.wav = robodance)
High shelf (+4 dB, 3 kHz) vain puheelle (speak1, speak2, glitch*); ei robodancelle."""
import sys, os, glob
import numpy as np, av
sys.path.insert(0, os.path.dirname(__file__))
from eq_robot_sfx import high_shelf, lfilter

# tavoitteet (projektin mittari, dBFS): Roccon äänet (Audio/Pelaaja) ovat pelissä -19...-25, mediaani -21,4; puheet saavat pelissä +2,3 dB (AudioLevels.SpeechBoost), siksi puheet 1 dB hiljaisemmaksi
TARGET = {"default": -21.5, "speak1": -22.5, "speak2": -22.5, "speak3": -23.5, "glitch1": -22.5, "glitch2": -22.5, "hit1": -22.5, "hit2": -22.5, "pan_hit": -17.0}
SHELF = ("speak1", "speak2", "glitch")
CEIL, KNEE = 0.89, 0.6   # -1 dBFS


def load(f):
    c = av.open(f); st = c.streams.audio[0]; fs = st.rate
    fr = [x.to_ndarray() for x in c.decode(st)]
    if st.format.is_planar: a = np.concatenate(fr, axis=1)
    else: a = np.concatenate(fr, axis=1).reshape(-1, st.channels).T   # lomitettu (s16): kanavat erotetaan, muuten kesto ja sävelkorkeus menevät väärin
    if a.dtype.kind in "iu": a = a.astype(np.float32) / np.iinfo(a.dtype).max
    return a.astype(np.float32), fs


def a_weight(x, fs):
    """A-painotus FFT:llä: korva kuulee 2-5 kHz:n vinkumisen paljon kovempana kuin RMS näyttää."""
    f = np.fft.rfftfreq(len(x), 1 / fs); f2 = f ** 2
    ra = (12194.0 ** 2 * f2 ** 2) / ((f2 + 20.6 ** 2) * np.sqrt((f2 + 107.7 ** 2) * (f2 + 737.9 ** 2)) * (f2 + 12194.0 ** 2) + 1e-12)
    return np.fft.irfft(np.fft.rfft(x) * ra / 0.7943, len(x))   # normalisoitu 1 kHz:ssä (~ -2 dB korjaus pois)


def peaking(fs, f0, gain_db, Q):
    A = 10 ** (gain_db / 40); w = 2 * np.pi * f0 / fs; al = np.sin(w) / (2 * Q); cw = np.cos(w)
    b = np.array([1 + al * A, -2 * cw, 1 - al * A]); a = np.array([1 + al / A, -2 * cw, 1 - al / A])
    return b / a[0], a / a[0]


def lowpass(fs, f0, Q=0.707):
    w = 2 * np.pi * f0 / fs; al = np.sin(w) / (2 * Q); cw = np.cos(w)
    b = np.array([(1 - cw) / 2, 1 - cw, (1 - cw) / 2]); a = np.array([1 + al, -2 * cw, 1 - al])
    return b / a[0], a / a[0]


# vinkuvat äänet: kapea vaimennus soinnillisen huipun kohdalle (+ loppusihinä pois alipäästöllä)
FIX = {"hit1": [(1973.0, -12.0, 4.0)], "hit2": [(1100.0, -10.0, 3.0), (3500.0, -4.0, 1.0)], "nousu": [(2200.0, -4.0, 1.0)]}
LOWPASS = {"hit1": 9000.0, "hit2": 9000.0, "nousu": 8000.0}


sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "AudioLevels"))
import measure_audio as _ma


def tool_db(a, fs):
    """Projektin oma voimakkuusmittari (sama kuin AudioLevels.json:n tekijä)."""
    return _ma.loudness_db(a.mean(0).astype(np.float64), fs)[0]


def active_rms(a, fs):
    x = np.stack([a_weight(ch, fs) for ch in a]).mean(0); w = int(fs * 0.05); n = len(x) // w
    r = np.sqrt((x[:n * w].reshape(n, w) ** 2).mean(1)) + 1e-9
    act = r > r.max() * 0.1
    return float(np.sqrt((r[act] ** 2).mean()))


def limit(x):
    m = np.abs(x)
    return np.where(m <= KNEE, x, np.sign(x) * (KNEE + (CEIL - KNEE) * np.tanh((m - KNEE) / (CEIL - KNEE))))


def process(f, out_dir):
    name = os.path.splitext(os.path.basename(f))[0]; ext = os.path.splitext(f)[1]
    a, fs = load(f)
    if name.startswith(SHELF):
        b, aa = high_shelf(fs, 3000.0, 4.0); a = np.stack([lfilter(b, aa, ch) for ch in a])
    for (f0, gdb, q) in FIX.get(name, []):
        b, aa = peaking(fs, f0, gdb, q); a = np.stack([lfilter(b, aa, ch) for ch in a])
    if name in LOWPASS:
        b, aa = lowpass(fs, LOWPASS[name]); a = np.stack([lfilter(b, aa, ch) for ch in a])
    tgt = TARGET.get(name, TARGET["default"])
    g = 1.0
    for _ in range(8):   # rajoitin vie tasoa: haetaan vahvistus, jolla RMS osuu tavoitteeseen rajoittimen jälkeen
        y = limit(a * g); cur = tool_db(y, fs)
        if abs(cur - tgt) < 0.15: break
        g *= 10 ** ((tgt - cur) / 20)
    y = limit(a * g)
    tmp = os.path.join(out_dir, name + ".tmp" + ext); dst = os.path.join(out_dir, name + ext)
    layout = "mono" if y.shape[0] == 1 else "stereo"
    o = av.open(tmp, "w"); s = o.add_stream("pcm_s16le" if ext == ".wav" else "libmp3lame", rate=fs); s.layout = layout
    if ext == ".mp3": s.bit_rate = 128000
    fo = av.AudioFrame.from_ndarray(np.ascontiguousarray((y * 32767).astype(np.int16)), format="s16p", layout=layout); fo.sample_rate = fs
    for p in s.encode(fo): o.mux(p)
    for p in s.encode(None): o.mux(p)
    o.close(); os.replace(tmp, dst)
    print(f"{name+ext:16s} vahvistus {20*np.log10(g):+5.1f} dB  rms {20*np.log10(active_rms(y, fs)):6.1f}  huippu {20*np.log10(np.abs(y).max()):5.1f}")


if __name__ == "__main__":
    os.makedirs(sys.argv[2], exist_ok=True)
    for f in sorted(glob.glob(sys.argv[1] + "/*.wav") + glob.glob(sys.argv[1] + "/*.mp3")):
        if os.path.basename(f).startswith("nousu"): continue   # nousu.wav on tasainen -39 dB:n humina ilman tapahtumaa: ei vahvisteta, vaan odotetaan uutta tiedostoa
        process(f, sys.argv[2])
