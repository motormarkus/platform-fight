"""Räjähdys vihreän taustan videosta: kuvat Assets/Resources/Jerrycan/boom_NNN.png + ääni Assets/Resources/Sfx/Rajahdys/rajahdys.wav.
Ajo: python Tools/JerrycanAssets/make_explosion.py <video.mp4>
Tulostaa lähdeframet (BoomFrames) ja kuvan koon (Jerrycan.cs käyttää niitä)."""
import sys, cv2, numpy as np, av, soundfile as sf
ROOT = "G:/unity pelit/mopopeli/platform fight/"
OUT = ROOT + "Assets/Resources/Jerrycan/"
SFX = ROOT + "Assets/Resources/Sfx/Rajahdys/"
BG = np.array([35, 238, 36], np.float32)   # bgr (haetaan videon kulmasta, ks. main)
FPS = 30.0
T1, T2 = 25, 115
FADE = 28   # kuvan reunalla pehmeä häivytys (savu menee ruudun yli)

def key(f):
    b, g, r = [f[..., k].astype(np.float32) for k in range(3)]
    gd = g - np.maximum(r, b)
    a = np.clip((T2 - gd) / (T2 - T1), 0, 1)
    a = a * a * (3 - 2 * a)
    af = np.maximum(a, 0.04)[..., None]
    col = np.clip((f.astype(np.float32) - (1 - a)[..., None] * BG) / af, 0, 255)
    col[..., 1] = np.minimum(col[..., 1], np.maximum(col[..., 0], col[..., 2]) + 2)
    h, w = a.shape
    ys = np.minimum(np.arange(h), np.arange(h)[::-1]) / FADE
    xs = np.minimum(np.arange(w), np.arange(w)[::-1]) / FADE
    edge = np.clip(np.minimum(ys[:, None], xs[None, :]), 0, 1)
    a = a * edge
    return col, a

def main(video):
    cap = cv2.VideoCapture(video)
    frames = []
    while True:
        ok, f = cap.read()
        if not ok: break
        frames.append(f)
    n = len(frames)
    global BG
    BG = np.median(frames[0][:20, :20].reshape(-1, 3), axis=0).astype(np.float32)
    FPS_ = cap.get(cv2.CAP_PROP_FPS) or FPS
    keyed = [key(f) for f in frames]
    mass = np.array([k[1].sum() for k in keyed])
    peak = mass.max()
    live = np.where(mass > peak * 0.05)[0]
    first, last = int(live[0]), int(live[-1])
    # kuvat: alku tiheästi (joka toinen), loppu harvemmin
    src = [i for i in range(first, last + 1) if (i - first) < 36 and (i - first) % 2 == 0 or (i - first) >= 36 and (i - first) % 4 == 0]
    H, W = keyed[0][1].shape
    cx, cy = W / 2, H / 2
    hw = hh = 1
    for i in src:
        ys, xs = np.where(keyed[i][1] > 0.1)
        if len(xs) == 0: continue
        hw = max(hw, cx - xs.min(), xs.max() - cx); hh = max(hh, cy - ys.min(), ys.max() - cy)
    hw, hh = int(min(hw + 2, W / 2)), int(min(hh + 2, H / 2))
    scale = 480.0 / (2 * hw)
    for j, i in enumerate(src):
        col, a = keyed[i]
        rgba = np.dstack([col, a * 255]).astype(np.uint8)
        c = rgba[int(cy) - hh:int(cy) + hh, int(cx) - hw:int(cx) + hw]
        c = cv2.resize(c, (int(2 * hw * scale), int(2 * hh * scale)), interpolation=cv2.INTER_AREA)
        cv2.imwrite(OUT + "boom_%03d.png" % j, c)
    print("BoomFrames =", src, "alkuframe", first, "fps", round(FPS_, 2), "; kuvan koko", c.shape[1], "x", c.shape[0], "(ppu 200)")
    # ääni
    cont = av.open(video)
    st = cont.streams.audio[0]
    data = []
    for fr in cont.decode(st):
        data.append(fr.to_ndarray())
    a = np.concatenate(data, axis=1)   # (kanavat, näytteet), planar float
    a = a.mean(axis=0) if a.ndim == 2 else a
    sr = st.rate
    t0 = int(max(0, first / FPS_ - 0.05) * sr)   # ääni alkaa heti animaation alkuhetkestä (ei viivettä)
    a = a[t0:]
    env = np.abs(a)
    idx = np.where(env > 0.02 * env.max())[0]
    a = a[:idx[-1] + int(0.4 * sr)]
    a = a / max(1e-6, np.abs(a).max()) * 0.9
    sf.write(SFX + "rajahdys.wav", a.astype(np.float32), sr, subtype="PCM_16")
    print("ääni", len(a) / sr, "s; ääni alkaa lähdevideosta", t0 / sr, "s; räjähdyksen alku kuvissa", first / FPS_ - t0 / sr, "s äänen alusta")

main(sys.argv[1])
