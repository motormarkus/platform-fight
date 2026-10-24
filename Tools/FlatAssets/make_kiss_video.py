"""Makuuhuonekohtauksen videot: lähteiden (HEVC .mov) tausta leikataan pois ja pari asetetaan makuuhuoneen eteen.

Taustat vaihtelevat (vihreä, harmaa vinjetti, värivälähdykset), joten tausta poistetaan alueen täytöllä reunoilta
(floodFill), ei yhdellä avainvärillä. Makuuhuone (yökaupunki ikkunoissa) himmenee, kun toinen video alkaa.

Uusi tanssivideo (vihreä tausta, hidastettu; tekee suudelma_1 ja silmukkaversion suudelma_3, lähikuva _2 säilyy):
  python Tools/FlatAssets/make_kiss_video.py --dance <tanssi.mov> [--test]
Vanha ajo: python Tools/FlatAssets/make_kiss_video.py <1.mov> <2.mov> <3.mov> [--test]
Tulos: Assets/StreamingAssets/Kampa/suudelma_1.mp4, _2, _3 (H.264 + AAC); --test tallentaa vain esikatselukuvat scratchpadiin.
"""
import sys, os
import numpy as np
import cv2
import av
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUTDIR = os.path.join(ROOT, "Assets", "StreamingAssets", "Kampa")
TAUSTAT = os.path.join(ROOT, "Assets", "Sprites", "Taustat")
W, H = 1920, 1080
BRIGHT = [0.85, 0.45, 0.65]     # taustan kirkkaus videoittain (himmenee toisen alussa)
MODES = ["grad", "flat", "flat"]
DIM_TIME = 1.5                  # s


def room(brightness_blur=18):
    city = Image.open(os.path.join(TAUSTAT, "kampa_yo_kaupunki.png")).convert("RGBA").resize((1672, 941), Image.LANCZOS)
    bed = Image.open(os.path.join(TAUSTAT, "kampa_makuuhuone.png")).convert("RGBA")
    base = Image.new("RGBA", bed.size, (0, 0, 0, 255))
    base.alpha_composite(city)
    base.alpha_composite(bed)
    im = base.convert("RGB").resize((W, H), Image.LANCZOS).filter(ImageFilter.GaussianBlur(brightness_blur))
    return np.asarray(im).astype(np.float32)


def bg_alpha(rgb, mode):
    """Etualan peittävyys (0..1): tausta = reunoilta täyttyvä alue (+ umpinaiset taskut samalla värillä).
    mode "grad": vinjetti (naapurivertailu), "flat": tasainen väri (kiinteä toleranssi)."""
    small = cv2.resize(rgb, (960, 540), interpolation=cv2.INTER_AREA)
    sm = cv2.GaussianBlur(small, (3, 3), 0)
    h, w = sm.shape[:2]
    border = np.concatenate([sm[0], sm[-1], sm[:, 0], sm[:, -1]]).astype(np.float32)
    bgc = np.median(border, axis=0)
    if bgc.max() < 30:
        # musta tausta: mustat vaatteet, hiukset ja saappaat eivät erotu väristä -> peitto täytetystä "ei-mustasta" muodosta
        dark = (sm.max(2) > 14).astype(np.uint8)
        dark = cv2.morphologyEx(dark, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (17, 17)))
        dark = cv2.dilate(dark, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7)))
        n, lab, st, _ = cv2.connectedComponentsWithStats(dark, connectivity=8)
        if n > 1:
            big = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
            dark = (lab == big).astype(np.uint8)
        inv = (1 - dark).astype(np.uint8)
        n2, lab2, st2, _ = cv2.connectedComponentsWithStats(inv, connectivity=4)
        for k in range(1, n2):
            if st2[k, cv2.CC_STAT_AREA] < 400:
                dark[lab2 == k] = 1
        fgf = cv2.GaussianBlur(dark.astype(np.float32), (5, 5), 0)
        return cv2.resize(fgf, (W, H), interpolation=cv2.INTER_LINEAR)[..., None]
    mask = np.zeros((h + 2, w + 2), np.uint8)
    seeds = [(x, 0) for x in range(0, w, 24)] + [(x, h - 1) for x in range(0, w, 24)] +             [(0, y) for y in range(0, h, 24)] + [(w - 1, y) for y in range(0, h, 24)]
    tmp = sm.copy()
    if mode == "flat":
        flags = 4 | cv2.FLOODFILL_MASK_ONLY | cv2.FLOODFILL_FIXED_RANGE | (255 << 8)
        tol = (16, 16, 16)
    else:
        flags = 4 | cv2.FLOODFILL_MASK_ONLY | (255 << 8)
        tol = (4, 4, 4)
    for (x, y) in seeds:
        if mask[y + 1, x + 1]:
            continue
        if np.abs(sm[y, x].astype(np.float32) - bgc).max() > 60:
            continue
        cv2.floodFill(tmp, mask, (x, y), 0, tol, tol, flags)
    bg = mask[1:-1, 1:-1] > 0
    fg0 = ~bg
    # umpinaiset taskut (esim. jalkojen välissä): taustan väri myös etualan sisällä
    ring = cv2.dilate(fg0.astype(np.uint8), np.ones((15, 15), np.uint8)).astype(bool) & bg
    ref = np.median(sm[ring], axis=0) if ring.any() else bgc
    dist = np.abs(sm.astype(np.float32) - ref).max(2)
    pocket = (dist < (14 if mode == "flat" else 20)) & fg0
    bg = bg | pocket
    fg = (~bg).astype(np.uint8)
    # vain suurin yhtenäinen etualue (roskaikonit pois)
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg, connectivity=8)
    if n > 1:
        big = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
        fg = (lab == big).astype(np.uint8)
        # täytetään pienet reiät (ei taustaa ihmisen sisään)
        inv = (1 - fg).astype(np.uint8)
        n2, lab2, st2, _ = cv2.connectedComponentsWithStats(inv, connectivity=4)
        for k in range(1, n2):
            if st2[k, cv2.CC_STAT_AREA] < 60:
                fg[lab2 == k] = 1
    fgf = cv2.erode(fg, np.ones((5, 5), np.uint8)).astype(np.float32)     # reunahalo pois
    fgf = cv2.GaussianBlur(fgf, (5, 5), 0)
    fgf = cv2.resize(fgf, (W, H), interpolation=cv2.INTER_LINEAR)
    return fgf[..., None]


SMALL = [True, False, True]      # koko vartalon videot (1, 3) pieninä sänkyjen väliin, lähikuva (2) täysikokoisena
SMALL_SCALE, FEET_X, FEET_Y, SRC_X, SRC_Y = 0.5, 982.0, 925.0, 960.0, 1050.0


def compose(rgb, bgimg, k, mode="flat", small=False):
    a = bg_alpha(rgb, mode)
    f = rgb.astype(np.float32)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 8.0)   # vihreä heijastus pois reunoilta
    if small:
        M = np.array([[SMALL_SCALE, 0, FEET_X - SMALL_SCALE * SRC_X], [0, SMALL_SCALE, FEET_Y - SMALL_SCALE * SRC_Y]], np.float32)
        fa = np.concatenate([f * a, a], 2)                       # esikerrottu väri + peitto
        fa = cv2.warpAffine(fa, M, (W, H), flags=cv2.INTER_AREA if False else cv2.INTER_LINEAR)
        return fa[..., :3] + bgimg * k * (1 - fa[..., 3:4])
    return f * a + bgimg * k * (1 - a)


def convert(src, dst, idx, room_img, pingpong=False):
    """Ei ääntä (musiikki soi pelissä erikseen). pingpong: kuvat eteen- ja taaksepäin, jotta silmukka on saumaton."""
    inp = av.open(src)
    vin = inp.streams.video[0]
    fps = vin.average_rate
    out = av.open(dst, "w")
    vs = out.add_stream("libx264", rate=fps)
    vs.width, vs.height, vs.pix_fmt = W, H, "yuv420p"
    vs.options = {"crf": "22", "preset": "medium"}
    k0, k1 = BRIGHT[max(0, idx - 1)], BRIGHT[idx]
    cache = []

    def put(arr):
        vf = av.VideoFrame.from_ndarray(arr, format="rgb24").reformat(format="yuv420p")
        for p in vs.encode(vf):
            out.mux(p)

    n = 0
    for fr in inp.decode(video=0):
        rgb = fr.to_ndarray(format="rgb24")
        t = n / float(fps)
        k = k1 if idx == 0 else k0 + (k1 - k0) * min(1.0, t / DIM_TIME)
        arr = np.clip(compose(rgb, room_img, k, MODES[idx], SMALL[idx]), 0, 255).astype(np.uint8)
        put(arr)
        if pingpong:
            ok, jpg = cv2.imencode(".jpg", arr[..., ::-1], [cv2.IMWRITE_JPEG_QUALITY, 96])
            cache.append(jpg)
        n += 1
    if pingpong:
        for jpg in reversed(cache[1:-1]):
            put(cv2.imdecode(jpg, cv2.IMREAD_COLOR)[..., ::-1].copy())
            n += 1
    for p in vs.encode():
        out.mux(p)
    out.close()
    print(os.path.basename(dst), n, "kuvaa")


def test(srcs, scratch):
    room_blur, room_sharp = room(18), room(1.5)
    for i, src in enumerate(srcs):
        room_img = room_sharp if SMALL[i] else room_blur
        c = av.open(src)
        frames = [f.to_ndarray(format="rgb24") for f in c.decode(video=0)]
        idxs = [0, 3, 60, 150, 250, 330, 400, 449]
        sheet = Image.new("RGB", (4 * 480, 2 * 270))
        for k, j in enumerate(idxs):
            im = Image.fromarray(np.clip(compose(frames[min(j, len(frames) - 1)], room_img, BRIGHT[i], MODES[i], SMALL[i]), 0, 255).astype(np.uint8))
            sheet.paste(im.resize((480, 270)), ((k % 4) * 480, (k // 4) * 270))
        sheet.save(os.path.join(scratch, "key_test_%d.png" % (i + 1)))


# ---- vihreä tausta (tanssivideo, 1280x720): avainnus + hidastus + ping-pong ----
DANCE_SLOW = 1.4                        # hidastuskerroin (kuvien välinen sekoitus)
DANCE_SCALE, DANCE_FEET_X, DANCE_FEET_Y = 0.92, 982.0, 925.0   # lähdekuva 1280x720 -> sänkyjen väliin
DANCE_SRC_X, DANCE_SRC_Y = 640.0, 715.0


def green_alpha(rgb):
    f = rgb.astype(np.float32)
    d = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = np.clip(1.0 - (d - 30.0) / 60.0, 0.0, 1.0)
    a = cv2.erode(a, np.ones((3, 3), np.uint8))
    return cv2.GaussianBlur(a, (3, 3), 0)[..., None]


def compose_dance(rgb, bgimg, k):
    a = green_alpha(rgb)
    f = rgb.astype(np.float32)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4.0)   # vihreä heijastus pois
    M = np.array([[DANCE_SCALE, 0, DANCE_FEET_X - DANCE_SCALE * DANCE_SRC_X], [0, DANCE_SCALE, DANCE_FEET_Y - DANCE_SCALE * DANCE_SRC_Y]], np.float32)
    fa = cv2.warpAffine(np.concatenate([f * a, a], 2), M, (W, H), flags=cv2.INTER_AREA)
    return fa[..., :3] + bgimg * k * (1 - fa[..., 3:4])


def slowed_frames(src):
    c = av.open(src)
    fr = [f.to_ndarray(format="rgb24").astype(np.float32) for f in c.decode(video=0)]
    n_out = int(len(fr) * DANCE_SLOW)
    for i in range(n_out):
        x = i / DANCE_SLOW
        j = min(int(x), len(fr) - 1); j2 = min(j + 1, len(fr) - 1); t = x - j
        yield fr[j] * (1 - t) + fr[j2] * t


def make_dance(src, scratch=None):
    room_img = room(1.5)
    frames = []
    for rgb in slowed_frames(src):
        frames.append(np.clip(compose_dance(rgb, room_img, BRIGHT[0]), 0, 255).astype(np.uint8))
    if scratch:
        sheet = Image.new("RGB", (4 * 480, 2 * 270))
        for k, j in enumerate(np.linspace(0, len(frames) - 1, 8).astype(int)):
            sheet.paste(Image.fromarray(frames[j]).resize((480, 270)), ((k % 4) * 480, (k // 4) * 270))
        sheet.save(os.path.join(scratch, "dance_test.png")); return
    for name, seq, k_to in (("suudelma_1.mp4", frames, None), ("suudelma_3.mp4", frames + frames[-2:0:-1], None)):
        out = av.open(os.path.join(OUTDIR, name), "w")
        vs = out.add_stream("libx264", rate=24)
        vs.width, vs.height, vs.pix_fmt = W, H, "yuv420p"
        vs.options = {"crf": "22", "preset": "medium"}
        for arr in seq:
            for pk in vs.encode(av.VideoFrame.from_ndarray(arr, format="rgb24").reformat(format="yuv420p")):
                out.mux(pk)
        for pk in vs.encode():
            out.mux(pk)
        out.close()
        print(name, len(seq), "kuvaa")


if __name__ == "__main__":
    if "--dance" in sys.argv:
        make_dance([a for a in sys.argv[1:] if not a.startswith("--")][0], os.environ.get("SCRATCH") if "--test" in sys.argv else None)
        sys.exit()
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if "--test" in sys.argv:
        test(args, os.environ["SCRATCH"])
        sys.exit()
    os.makedirs(OUTDIR, exist_ok=True)
    room_blur, room_sharp = room(18), room(1.5)
    for i, src in enumerate(args):
        convert(src, os.path.join(OUTDIR, "suudelma_%d.mp4" % (i + 1)), i, room_sharp if SMALL[i] else room_blur, pingpong=(i == len(args) - 1))
