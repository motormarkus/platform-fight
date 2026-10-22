# Liz (tankotanssija): 7 videota -> Assets/Sprites/Liz/liz_<klippi>_NN.png (tanko keskellä, tukipiste alareunassa)
import sys, glob, cv2, numpy as np
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
OUT = "G:/unity pelit/mopopeli/platform fight/Assets/Sprites/Liz"
CLIPS = {0: "f691c766-copy_263BD84D-8CC4-4A1A-BEF0-189AE3883F96.mov", 1: "6922bd99-gemini_generated_video_1800D31E.mp4",
         2: "9ddd86a8-copy_23F60CBD-860A-41C7-8A60-7D526C079FFF.mov", 3: "3b076e9f-copy_6A081462-97C4-4953-8C37-AA127D3D1F16.mov",
         4: "fbb4a75b-copy_4EF69A17-5B0D-4C88-8C2F-F75F69695C8C.mov", 5: "7c300cf4-copy_12509151-467F-4996-B1A8-B39F38EA0C83.mov",
         6: "f203e27e-copy_76C799E1-D174-40CE-B08C-5D4541D13B73.mov"}
STEP = 4
def key(frame, tol=5):
    h, w = frame.shape[:2]
    bl = cv2.GaussianBlur(frame, (0, 0), 1.2)
    mask = np.zeros((h + 2, w + 2), np.uint8)
    seeds = [(2, 2), (w - 3, 2), (2, h - 3), (w - 3, h - 3), (w // 2, 2), (w // 2, h - 3), (2, h // 2), (w - 3, h // 2), (w // 4, 2), (3 * w // 4, 2), (w // 4, h - 3), (3 * w // 4, h - 3)]
    for (x, y) in seeds:
        if mask[y + 1, x + 1] == 0:
            cv2.floodFill(bl.copy(), mask, (x, y), 0, (tol,) * 3, (tol,) * 3, 4 | cv2.FLOODFILL_MASK_ONLY | (255 << 8))
    bg = mask[1:-1, 1:-1] > 0
    fg = (~bg).astype(np.uint8) * 255
    rgbf = frame.astype(np.int32)   # BGR
    fg[(rgbf[..., 1] > rgbf[..., 2] + 60) & (rgbf[..., 1] > rgbf[..., 0] + 60)] = 0   # selvä vihreä pois (taskut)
    # paikallinen taustan arvio (maskattu sumennus) -> taskut (esim. käsivarren ja vartalon välissä)
    bgf = bg.astype(np.float32)
    num = cv2.GaussianBlur(bl.astype(np.float32) * bgf[..., None], (0, 0), 40)
    den = cv2.GaussianBlur(bgf, (0, 0), 40)[..., None] + 1e-3
    est = num / den
    d = np.abs(bl.astype(np.float32) - est).max(axis=2)
    pock = ((d < 14) & (fg > 0)).astype(np.uint8) * 255
    nn, ll, ss, _ = cv2.connectedComponentsWithStats(pock, connectivity=4)
    for i in range(1, nn):
        if ss[i, 4] > 150: fg[ll == i] = 0
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg, connectivity=8)
    keep = np.zeros_like(fg)
    for i in range(1, n):
        if st[i, 4] > 2500: keep[lab == i] = 255
    keep = cv2.morphologyEx(keep, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    keep = cv2.erode(keep, np.ones((3, 3), np.uint8))
    a = cv2.GaussianBlur(keep, (0, 0), 0.8)
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    band = cv2.dilate((a > 128).astype(np.uint8), np.ones((7, 7), np.uint8)) - cv2.erode((a > 128).astype(np.uint8), np.ones((7, 7), np.uint8))
    g = rgb[..., 1].astype(np.int32); m = np.maximum(rgb[..., 0], rgb[..., 2]).astype(np.int32)
    rgb[..., 1] = np.where((band > 0) & (g > m), m, g).astype(np.uint8)
    return np.dstack([rgb, a])
def load(path):
    cap = cv2.VideoCapture(path); fr = []; i = 0
    while True:
        ok, x = cap.read()
        if not ok: break
        if i % STEP == 0: fr.append(x)
        i += 1
    return fr
CANDI = {0: "c011fecd-copy_3FB87865-730C-4D5A-8A1A-2805406B4D6F.mov", 1: "344bb65e-copy_21159032-1A00-4808-94CF-74941C753417.mov",
         2: "cda306c8-copy_FFD95B9B-7552-4D9B-95EC-82F206B58106.mov", 3: "cb010355-copy_B082AC8E-9731-4668-9A63-024DF890CC5A.mov",
         4: "d66b9132-copy_D1AAFB0C-713F-4694-AB78-3E236EB7393B.mov", 5: "5cb31870-copy_A28B63CD-A4A3-43E0-9FFC-D5ADEFF42B5D.mov"}
if __name__ == "__main__":
    PREFIX = "liz"
    if len(sys.argv) > 1 and sys.argv[1] == "candi":
        PREFIX = "candi"; CLIPS = CANDI; OUT = OUT.replace("/Liz", "/Candi"); sys.argv.pop(1)
    which = [int(a) for a in sys.argv[1:]] or sorted(CLIPS)
    if PREFIX == "liz" and len(sys.argv) <= 1: which = [0, 1, 3, 4, 5, 6]
    scale = None
    for c in which:
        raw = load(U + CLIPS[c])
        raw = [cv2.resize(f, (1920, 1080)) if f.shape[1] != 1920 else f for f in raw]
        keyed = [key(f) for f in raw]
        # tangon x: sarake, jossa eniten alfaa kapeassa pystysuorassa kaistassa (eka kuva, hahmo seisoo)
        a0 = (keyed[0][..., 3] > 128).astype(np.float32)
        col = a0.sum(axis=0); px = int(np.argmax(cv2.GaussianBlur(col.reshape(1, -1), (0, 0), 2).ravel()))
        if scale is None:
            ys = np.where(a0.sum(axis=1) > 0)[0]
            scale = 348.0 / (ys.max() - ys.min() + 1)   # tanko = lavan tangon korkeus (348 px kuvassa klubi_2)
            print("tanko x", px, "tangon korkeus", ys.max() - ys.min() + 1, "skaala", round(scale, 3))
        # yhteinen kehys: tanko keskellä, leveys = suurin etäisyys tangosta, korkeus: tangon alapää ylös 350 px (skaalattuna)
        halfw = 0; ybot = 0
        for r in keyed:
            ys, xs = np.where(r[..., 3] > 40)
            halfw = max(halfw, abs(xs.min() - px), abs(xs.max() - px)); ybot = max(ybot, ys.max())
        halfw = int(halfw) + 4
        top = 0
        for n, r in enumerate(keyed):
            canvas = np.zeros((ybot - top + 1, 2 * halfw + 1, 4), np.uint8)
            x0 = max(0, px - halfw); x1 = min(r.shape[1], px + halfw + 1)
            crop = r[top:ybot + 1, x0:x1]
            canvas[:crop.shape[0], (x0 - (px - halfw)):(x0 - (px - halfw)) + crop.shape[1]] = crop
            out = cv2.resize(canvas, (max(1, round(canvas.shape[1] * scale)), max(1, round(canvas.shape[0] * scale))), interpolation=cv2.INTER_AREA)
            if out.shape[1] % 2 == 0: out = np.pad(out, ((0, 0), (0, 1), (0, 0)))
            cv2.imwrite(f"{OUT}/{PREFIX}_{c}_{n:02d}.png", cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
        print("klippi", c, len(keyed), "kuvaa", out.shape)
