import sys, glob, os, av, numpy as np
def load(f):
    c = av.open(f); st = c.streams.audio[0]
    fr = [x.to_ndarray() for x in c.decode(st)]
    a = np.concatenate(fr, axis=1)
    if not st.format.is_planar: a = a.reshape(-1, st.channels).T
    if a.dtype.kind in "iu": a = a.astype(np.float32) / np.iinfo(a.dtype).max
    return a.astype(np.float32).mean(0), st.rate
def stats(f):
    x, fs = load(f); pk = np.abs(x).max()
    w = int(fs * 0.05); n = len(x) // w
    r = np.sqrt((x[:n * w].reshape(n, w) ** 2).mean(1)) + 1e-9
    act = r > r.max() * 0.1   # vain aktiivinen osa (hiljaisuus pois)
    rms = np.sqrt((r[act] ** 2).mean())
    return 20 * np.log10(pk), 20 * np.log10(rms)
if __name__ == "__main__":
    for d in sys.argv[1:]:
        print("==", d)
        for f in sorted(glob.glob(d + "/*.wav") + glob.glob(d + "/*.mp3")):
            p, r = stats(f); print(f"{os.path.basename(f):22s} huippu {p:6.1f} dB  rms {r:6.1f} dB")
