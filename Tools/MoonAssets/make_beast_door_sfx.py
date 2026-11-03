"""Olion hakkaus- ja karjuntaäänet oven takaa: maltillinen kaiku (synteettinen huoneen impulssivaste) + hieman vaimennettu (ovi välissä).
Ajo: python Tools/MoonAssets/make_beast_door_sfx.py"""
import soundfile as sf, numpy as np, os
def fftconvolve(a, b):
    n = len(a) + len(b) - 1; m = 1 << (n - 1).bit_length()
    return np.fft.irfft(np.fft.rfft(a, m) * np.fft.rfft(b, m), m)[:n]
def lowpass(x, fc, passes=2):
    a = np.exp(-2 * np.pi * fc / SR); y = np.asarray(x, dtype=np.float64)
    for _ in range(passes):
        out = np.empty_like(y); z = 0.0
        for i in range(len(y)): z = (1 - a) * y[i] + a * z; out[i] = z
        y = out
    return y
U = "C:/Users/marku/.claude/uploads/7da1f59e-4305-4433-abdf-9b372a92e21c/"
OUT = "Assets/Resources/Sfx/AlienOvi/"
SR = 48000
rng = np.random.default_rng(7)
def ir(rt60=0.9, pre=0.02):
    n = int(SR * (rt60 * 1.2)); t = np.arange(n) / SR
    h = rng.standard_normal(n) * np.exp(-6.9 * t / rt60)
    h = lowpass(h, 3500)   # tumma kaiku
    h[:int(pre * SR)] = 0
    h[0] = 0
    return h / np.sqrt((h ** 2).sum())
H = ir()
def proc(d, wet=0.30, damp=True, peak=0.6):
    if d.ndim > 1: d = d.mean(1)
    dry = d.copy()
    if damp: dry = lowpass(dry, 5500)   # oven takana vaimeampi
    w = fftconvolve(dry, H)
    out = np.zeros(len(w)); out[:len(dry)] += dry * (1 - wet * 0.4); out += w * wet * 0.9
    out = np.concatenate([out, np.zeros(int(SR * 0.3))])
    out[-int(SR * 0.5):] *= np.linspace(1, 0, int(SR * 0.5))   # häntä pois pehmeästi
    out[:48] *= np.linspace(0, 1, 48)
    return (out / np.abs(out).max() * peak).astype(np.float32)
bangs = ["abca832e-banging_on_vault_doo_4-1793720653491.wav", "f611b33b-metal_banging_door_4-1793720541620.wav", "c43f5814-metal_banging_door_1-1793720494455.wav", "25f9e511-metal_banging_door_4-1793720489306.wav"]
for i, f in enumerate(bangs, 1):
    d, sr = sf.read(U + f); assert sr == SR
    sf.write(OUT + f"paukku{i}.wav", proc(d, 0.30, True, 0.7), SR); 
growls = [U + "99a565f7-peto_pain1.wav", "Assets/Resources/Sfx/Alien/attack1.wav", "Assets/Resources/Sfx/Alien/attack2.wav", "Assets/Resources/Sfx/Alien/idle1.wav"]
for i, f in enumerate(growls, 1):
    d, sr = sf.read(f); assert sr == SR, (f, sr)
    sf.write(OUT + f"karju{i}.wav", proc(d, 0.38, True, 0.6), SR)
print(sorted(os.listdir(OUT)))
