"""WTF-areenan katsomo ja kehä: kolme 5120x2880-masteria staattiseksi taustaksi + yleisön silmukka videoista.
Videot (1280x720, 24 fps, 10 s) ovat masterien animoituja versioita: vain yleisö liikkuu. Videosta otetaan vain liikkuva yleisöalue
(ajallinen keskihajonta -> maski, laajennettu ja pehmennetty), kohdistetaan masteriin (ECC, affiini), värit sovitetaan masteriin
maskin ympäristöstä, ja kuvat pakataan kuvasarjaksi 12 fps. Silmukan sauma: loppu ristihäivytetään alkuun (video 1 ei palaa täysin alkuasentoon).
Ääni: viimeisen (nurkka)videon yleisö -> Resources/Ambienssi/wtf_areenan_katsomo.wav, sauma ristihäivytetty.
Ulos: Assets/Sprites/Taustat/vegas_areena_katsomo_{1,2,3}.png (masterit), vegas_areena_yleiso_{1,2,3}.png (kuvasarjat) + vegas_areena_yleiso.txt
(rivi per paneeli: solu_w solu_h sarakkeet kuvia x y skaala; x,y = solun vasen yläkulma masterin pikseleinä, skaala = masterin px / solun px).
Ajo: python Tools/ArenaAssets/make_arena_crowd.py <master-kansio> <video1> <video2> <video3>"""
import os, sys, shutil, wave
import av, cv2, numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Sprites", "Taustat")
AUD = os.path.join(ROOT, "Assets", "Resources", "Ambienssi", "wtf_areenan_katsomo.wav")
FPS_STEP = 2          # 24 fps -> 12 fps
MAXTEX = 8192


def frames(p):
    return np.stack([f.to_ndarray(format="bgr24") for f in av.open(p).decode(video=0)])


def crowd_mask(v):
    sd = v.astype(np.float32).mean(3).std(0)
    m = (sd > 7).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
    keep = np.zeros(n, bool); keep[1:] = st[1:, 4] >= 60
    m = keep[lab].astype(np.uint8)
    m = cv2.dilate(m, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (15, 15)))
    return cv2.morphologyEx(m, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (21, 21)))


def loop_frames(v, mask):
    """Silmukka: etsi alku/loppu, jossa yleisö on lähimpänä samaa asentoa; loppua ristihäivytetään alkuun."""
    mb = mask[::2, ::2] > 0
    small = v[:, ::2, ::2].astype(np.int16)
    best = min((np.abs(small[b] - small[a])[mb].mean(), a, b) for a in range(0, 25) for b in range(len(v) - 40, len(v)))
    _, a, b = best
    seq = v[a:b + 1].astype(np.float32)
    n = 8 if best[0] < 10 else 16                # huono sauma -> pidempi häivytys
    L = len(seq) - n                             # silmukka: seq[n:] ja loppu sulautuu alkuun seq[0:n]
    out = seq[n:].copy()
    for k in range(n):
        w = (k + 1) / (n + 1)
        out[L - n + k] = out[L - n + k] * (1 - w) + seq[k] * w
    print("   silmukka", a, "->", b, "sauman ero", round(float(best[0]), 1), "häivytys", n, "kuvaa", len(out))
    return out


def main(mdir, vids):
    os.makedirs(OUT, exist_ok=True)
    meta = []
    for i, vp in enumerate(vids):
        mp = os.path.join(mdir, f"master-0{i + 1}-5120x2880.png")
        shutil.copyfile(mp, os.path.join(OUT, f"vegas_areena_katsomo_{i + 1}.png"))
        master = cv2.imread(mp); MH, MW = master.shape[:2]
        v = frames(vp); VH, VW = v.shape[1:3]; sc = MW / VW
        print(os.path.basename(vp), v.shape, "-> master", i + 1)
        ms = cv2.resize(master, (VW, VH), interpolation=cv2.INTER_AREA)
        med = np.median(v[::8], 0).astype(np.uint8)
        W = np.eye(2, 3, dtype=np.float32)
        _, W = cv2.findTransformECC(cv2.cvtColor(ms, cv2.COLOR_BGR2GRAY).astype(np.float32), cv2.cvtColor(med, cv2.COLOR_BGR2GRAY).astype(np.float32),
                                    W, cv2.MOTION_AFFINE, (cv2.TERM_CRITERIA_EPS | cv2.TERM_CRITERIA_COUNT, 200, 1e-6), None, 5)
        warp = lambda f: cv2.warpAffine(f, W, (VW, VH), flags=cv2.INTER_LINEAR | cv2.WARP_INVERSE_MAP, borderMode=cv2.BORDER_REPLICATE)
        v = np.stack([warp(f) for f in v])
        mask = crowd_mask(v)
        # värit: maskin ympärysrengas, kanavittain lineaarinen sovitus videon -> masterin (pienennetyn) väreihin
        ring = (cv2.dilate(mask, np.ones((31, 31), np.uint8)) > 0) & (mask == 0)
        vm = np.median(v[::8], 0).astype(np.float32)
        gain = []
        for c in range(3):
            x, y = vm[..., c][ring], ms[..., c][ring].astype(np.float32)
            A = np.vstack([x, np.ones_like(x)]).T; g, o = np.linalg.lstsq(A, y, rcond=None)[0]; gain.append((g, o))
        print("   värisovitus", [(round(float(g), 3), round(float(o), 1)) for g, o in gain])
        seq = loop_frames(v, mask)[::FPS_STEP]
        for c, (g, o) in enumerate(gain): seq[..., c] = seq[..., c] * g + o
        seq = np.clip(seq, 0, 255).astype(np.uint8)
        # peitto vain siellä, missä kuva oikeasti liikkuu: paikallaan pysyvät kohdat (köydet yleisön edessä, liikkumattomat vartalot,
        # istuimet) jäävät masterista teräviksi; maskin ulkopuolella ei mitään
        sd = cv2.GaussianBlur(v.astype(np.float32).mean(3).std(0), (0, 0), 1.2)
        sd = cv2.dilate(sd, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
        alpha = np.clip((sd - 3.0) / 5.0, 0, 1) * cv2.GaussianBlur(mask.astype(np.float32), (0, 0), 2.0)
        alpha = cv2.GaussianBlur(alpha, (0, 0), 1.0)
        # liike-epäterävyys/videon pehmeys: kevyt terävöitys ennen pakkausta
        blur = np.stack([cv2.GaussianBlur(f, (0, 0), 1.4) for f in seq])
        seq = np.clip(seq.astype(np.float32) * 1.7 - blur.astype(np.float32) * 0.7, 0, 255).astype(np.uint8)
        ys, xs = np.where(alpha > 0.004)
        x0, y0 = xs.min() & ~3, ys.min() & ~3
        x1, y1 = min(VW, (xs.max() + 4) & ~3), min(VH, (ys.max() + 4) & ~3)
        cw, ch = x1 - x0, y1 - y0
        cols = MAXTEX // cw; rows = -(-len(seq) // cols)
        if rows * ch > MAXTEX: raise SystemExit("kuvasarja ei mahdu 8192-tekstuuriin")
        sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
        a8 = (alpha[y0:y1, x0:x1] * 255).astype(np.uint8)
        for k, f in enumerate(seq):
            r, c = divmod(k, cols)
            cell = np.dstack([f[y0:y1, x0:x1], a8]); cell[a8 == 0] = 0
            sheet[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw] = cell
        cv2.imwrite(os.path.join(OUT, f"vegas_areena_yleiso_{i + 1}.png"), sheet)
        meta.append(f"{cw} {ch} {cols} {len(seq)} {x0 * sc:.1f} {y0 * sc:.1f} {sc:.4f}")
        print("   solu", cw, "x", ch, "kuvia", len(seq), "arkki", sheet.shape[1], "x", sheet.shape[0], "paikka masterissa", round(x0 * sc), round(y0 * sc))
        if i == len(vids) - 1: crowd_audio(vp)
    with open(os.path.join(OUT, "vegas_areena_yleiso.txt"), "w") as fh: fh.write("\n".join(meta) + "\n")


def crowd_audio(vp):
    c = av.open(vp); s = c.streams.audio[0]
    res = av.AudioResampler(format="s16", layout="stereo", rate=48000)
    pcm = []
    for fr in c.decode(s):
        for o in res.resample(fr): pcm.append(o.to_ndarray().reshape(-1, 2))
    x = np.concatenate(pcm).astype(np.float32)
    sr, n = 48000, 48000 * 3 // 2                  # 1,5 s ristihäivytys: loppu sulautuu alkuun -> saumaton silmukka
    x = x[int(0.05 * sr):]                         # videon alun mahdollinen napsahdus pois
    head, body = x[:n], x[n:].copy()
    w = np.linspace(0, 1, n)[:, None]
    body[-n:] = body[-n:] * np.sqrt(1 - w) + head * np.sqrt(w)
    peak = np.abs(body).max(); body *= min(1.0, 0.7 * 32767 / max(peak, 1))
    os.makedirs(os.path.dirname(AUD), exist_ok=True)
    with wave.open(AUD, "wb") as wf:
        wf.setnchannels(2); wf.setsampwidth(2); wf.setframerate(sr); wf.writeframes(body.astype(np.int16).tobytes())
    print("ääni", AUD, round(len(body) / sr, 2), "s")


if __name__ == "__main__":
    a = sys.argv[1:]
    main(a[0], a[1:4])
