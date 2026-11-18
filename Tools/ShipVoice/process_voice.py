"""Aluksen hälytysäänet (oma ääni -> konemainen): kompressori, lievä saturaatio ja lievä rotary TAI transformer. Kaikki efektit tarkoituksella lieviä.
Ajo: python Tools/ShipVoice/process_voice.py <äänitiedosto tai kansio> [ulos-kansio]
Tuottaa jokaisesta <nimi>.wav: kompressori + lievä saturaatio + rengasmodulaatio (55 Hz, 35 %). Muut efektit (rotary, transformer, phaser, flanger) jäävät funktioiksi kokeiluja varten."""
import os, sys, glob, math
import numpy as np, av

SR = 48000
LOPPU = "--loppu" in sys.argv   # loppukuulutukset: lisäksi hajoava (katkeileva) versio
if LOPPU: sys.argv.remove("--loppu")

def load(path):
    c = av.open(path); st = c.streams.audio[0]
    rs = av.AudioResampler(format="fltp", layout="mono", rate=SR)
    out = []
    for fr in c.decode(st):
        for r in rs.resample(fr): out.append(r.to_ndarray()[0])
    for r in rs.resample(None): out.append(r.to_ndarray()[0])
    return np.concatenate(out).astype(np.float64)

def save(path, x):
    x = np.atleast_2d(x)
    if x.shape[0] > x.shape[1]: x = x.T
    x = np.clip(x, -1, 1); pcm = (x.T * 32767).astype(np.int16)
    import wave
    with wave.open(path, "wb") as w:
        w.setnchannels(pcm.shape[1]); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())

def onepole(x, fc):   # alipäästö, 1. aste
    a = math.exp(-2 * math.pi * fc / SR); y = np.empty_like(x); s = 0.0
    for i, v in enumerate(x): s = (1 - a) * v + a * s; y[i] = s
    return y

def compress(x, thr_db=-20.0, ratio=3.0, att=0.005, rel=0.09):
    env = np.empty_like(x); e = 0.0; aa = math.exp(-1 / (att * SR)); ar = math.exp(-1 / (rel * SR))
    ax = np.abs(x)
    for i, v in enumerate(ax): c = aa if v > e else ar; e = c * e + (1 - c) * v; env[i] = e
    db = 20 * np.log10(np.maximum(env, 1e-6)); over = np.maximum(0, db - thr_db)
    gain = 10 ** (-over * (1 - 1 / ratio) / 20)
    return x * gain

def saturate(x, drive=1.8, mix=0.3):
    return (1 - mix) * x + mix * np.tanh(drive * x) / np.tanh(drive)

def normalize(x, peak=0.89):
    m = np.max(np.abs(x)); return x * (peak / m) if m > 0 else x

def rotary(x, horn_hz=6.5, drum_hz=5.2, depth=0.5, vib_ms=0.9):
    """Lievä Leslie: korkeat (torvi) ja matalat (rumpu) pyörivät eri tahtiin: tremolo, pieni vibrato (viiveen modulointi) ja stereopanorointi."""
    lo = onepole(x, 800.0); hi = x - lo
    t = np.arange(len(x)) / SR
    def spin(sig, hz, ph, vib):
        d = vib * 1e-3 * SR * (1 + np.sin(2 * np.pi * hz * t + ph)) + 1
        idx = np.arange(len(sig)) - d; i0 = np.floor(idx).astype(int); fr = idx - i0
        i0 = np.clip(i0, 0, len(sig) - 2); s = sig[i0] * (1 - fr) + sig[i0 + 1] * fr
        am = 1 - depth * 0.5 * (1 + np.sin(2 * np.pi * hz * t + ph + 0.6))
        pan = 0.5 + 0.45 * np.sin(2 * np.pi * hz * t + ph)
        return s * am * np.sqrt(1 - pan), s * am * np.sqrt(pan)
    hl, hr = spin(hi, horn_hz, 0.0, vib_ms); dl, dr = spin(lo, drum_hz, 1.7, vib_ms * 0.4)
    L = hl + dl; R = hr + dr
    return np.vstack([0.88 * L + 0.12 * x, 0.88 * R + 0.12 * x])   # vain vähän kuivaa: efekti kuuluu

def transformer(x, rate_hz=10.0, depth=0.75, duty=0.55, smooth_ms=3.0):
    """Lievä rytminen katko: ääni painuu depth-verran alas joka jaksossa (ei täysin hiljaiseksi), reunat pehmennetty (ei naksahduksia)."""
    t = np.arange(len(x)) / SR
    gate = ((t * rate_hz) % 1.0 < duty).astype(np.float64)
    gate = onepole(onepole(gate, 1000.0 / (2 * math.pi * smooth_ms)), 1000.0 / (2 * math.pi * smooth_ms))
    return x * (1 - depth + depth * gate)

def phaser(x, rate_hz=0.6, stages=6, fmin=300.0, fmax=3200.0, feedback=0.55, mix=0.5):
    """Phaser: all-pass-ketju, jonka taajuus pyyhkii LFO:lla fmin..fmax (logaritmisesti); takaisinkytkentä terävöittää loveja."""
    n = len(x); t = np.arange(n) / SR
    lfo = 0.5 * (1 + np.sin(2 * np.pi * rate_hz * t))
    fc = fmin * (fmax / fmin) ** lfo
    tt = np.tan(np.pi * fc / SR); a = (tt - 1) / (tt + 1)
    z = [0.0] * stages; y = np.empty(n); fb = 0.0
    for i in range(n):
        v = x[i] + feedback * fb; ai = a[i]
        for k in range(stages):
            o = ai * v + z[k]; z[k] = v - ai * o; v = o
        fb = v; y[i] = v
    return (1 - mix) * x + mix * y

def flanger(x, rate_hz=0.35, min_ms=0.6, max_ms=4.5, feedback=0.6, mix=0.5):
    """Flanger: lyhyt moduloitu viive + takaisinkytkentä (metallinen suihkumainen sävy)."""
    n = len(x); t = np.arange(n) / SR
    d = (min_ms + (max_ms - min_ms) * 0.5 * (1 + np.sin(2 * np.pi * rate_hz * t))) * 1e-3 * SR
    buf = np.zeros(n); y = np.empty(n)
    for i in range(n):
        p = i - d[i]; i0 = int(math.floor(p)); f = p - i0
        dl = (buf[i0] * (1 - f) + buf[i0 + 1] * f) if i0 >= 0 and i0 + 1 < i else 0.0
        buf[i] = x[i] + feedback * dl; y[i] = x[i] + dl
    return (1 - mix) * x + mix * y * 0.7

def ringmod(x, freq=55.0, mix=0.35):
    """Rengasmodulaatio (klassinen robottiääni), lievä sekoitus."""
    t = np.arange(len(x)) / SR
    return (1 - mix) * x + mix * x * np.sin(2 * np.pi * freq * t)

def breakup(x, seed=7, gap_ms=(35, 110), every_ms=(140, 380), floor=0.08, stutter=0.3, fade_ms=3.0):
    """Hajoava kuulutus (loppu): epäsäännölliset katkot (ääni putoaa lähes hiljaiseksi 35-110 ms välein 140-380 ms) ja välillä lyhyt toisto (änkytys)."""
    rng = np.random.default_rng(seed); n = len(x); g = np.ones(n); y = x.copy()
    i = int(rng.uniform(*every_ms) * SR / 1000)
    while i < n:
        L = int(rng.uniform(*gap_ms) * SR / 1000)
        if rng.random() < stutter and i - L > 0:      # änkytys: edellinen pätkä toistetaan
            seg = x[i - L:i]; e = min(n, i + L); y[i:e] = seg[:e - i]
        else:
            g[i:min(n, i + L)] = floor
        i += L + int(rng.uniform(*every_ms) * SR / 1000)
    k = max(1, int(fade_ms * SR / 1000)); g = np.convolve(g, np.ones(k) / k, mode="same")
    return y * g

def trim_tail(x, thr=0.2, hold_ms=40, fade_ms=220):
    """Lopun rasahdukset pois: puheen loppu = viimeinen 20 ms ikkuna, jonka RMS > thr * suurin RMS; siitä hold_ms + fade_ms pehmeä häivytys ja loppu leikataan."""
    w = int(0.02 * SR); n = len(x) // w
    if n < 3: return x
    r = np.array([np.sqrt(np.mean(x[k * w:(k + 1) * w] ** 2)) for k in range(n)])
    last = int(np.where(r > thr * r.max())[0][-1])
    end = (last + 1) * w + int(hold_ms * SR / 1000); f = int(fade_ms * SR / 1000)
    y = x[:min(len(x), end + f)].copy()
    if end < len(y): y[end:] *= np.linspace(1, 0, len(y) - end) ** 2
    return y

def process(path, outdir):
    name = os.path.splitext(os.path.basename(path))[0]
    x = load(path); x = x - np.mean(x); x = trim_tail(x)   # lopun rasahdukset/huokaukset pois ennen kompressoria
    base = normalize(saturate(compress(normalize(x))))
    save(os.path.join(outdir, name + ".wav"), normalize(ringmod(base)))
    if LOPPU: save(os.path.join(outdir, name + "_hajoava.wav"), normalize(breakup(ringmod(base))))   # loppukuulutukset: alus hajoaa   # valittu: kompressori + saturaatio + rengasmodulaatio (ei muita modulaatioita)
    print(name, round(len(x) / SR, 2), "s -> rengasmodulaatio")

if __name__ == "__main__":
    src = sys.argv[1]; outdir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")
    os.makedirs(outdir, exist_ok=True)
    files = sorted(glob.glob(os.path.join(src, "*.*"))) if os.path.isdir(src) else [src]
    for f in files:
        if f.lower().endswith((".wav", ".mp3", ".m4a", ".ogg", ".flac", ".aif", ".aiff", ".mov", ".mp4")): process(f, outdir)
