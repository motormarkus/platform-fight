"""Lumirinteen ennakkovideo ilman Unityä: toistaa valikon 274 kerrokset ja nopeudet (oletus: kameran koko 6 -> 78,4 px/yksikkö).
Ajo: python Tools/SnowAssets/preview_snow.py <ulos.mp4> [sekunnit] [ffmpeg]"""
import math, os, random, subprocess, sys
import numpy as np, cv2

TX = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
W, H = 1672, 941
PPU = 941 / 12.0                      # px / yksikkö (CamHalf 6)
ANG = math.atan(420 / 1672)
CA, SA, TA = math.cos(ANG), math.sin(ANG), math.tan(ANG)
random.seed(4)


def ld(n, flag=cv2.IMREAD_UNCHANGED):
    return cv2.imread(os.path.join(TX, n), flag)


def scaled(img, s):
    return cv2.resize(img, (max(1, int(img.shape[1] * s)), max(1, int(img.shape[0] * s))), interpolation=cv2.INTER_AREA if s < 1 else cv2.INTER_LINEAR)


def blit(dst, src, x, y, mult=1.0):
    x, y = int(round(x)), int(round(y)); h, w = src.shape[:2]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(dst.shape[1], x + w), min(dst.shape[0], y + h)
    if x1 <= x0 or y1 <= y0: return
    s = src[y0 - y:y1 - y, x0 - x:x1 - x]
    if s.shape[2] == 4:
        a = (s[..., 3:4].astype(np.float32) / 255.0) * mult
        dst[y0:y1, x0:x1] = dst[y0:y1, x0:x1] * (1 - a) + s[..., :3] * a
    else:
        dst[y0:y1, x0:x1] = s[..., :3]


def rot_sprite(img, deg):
    """kierto alareunan keskipisteestä (kuten Unityn pivot), positiivinen = myötäpäivään."""
    h, w = img.shape[:2]
    pad = int(max(h, w) * 0.5)
    big = cv2.copyMakeBorder(img, pad, 0, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
    M = cv2.getRotationMatrix2D((pad + w / 2, pad + h), -deg, 1.0)
    out = cv2.warpAffine(big, M, (big.shape[1], big.shape[0]), flags=cv2.INTER_LINEAR, borderValue=(0, 0, 0, 0))
    return out, pad + w / 2, pad + h


sky = ld("lumi_taivas.png", cv2.IMREAD_COLOR)
forest = ld("lumi_metsa.png")
slope = ld("lumi_rinne.png")
surf = ld("lumi_pinta.png"); d1 = ld("lumi_poly_1.png"); d2 = ld("lumi_poly_2.png")
trees = [ld("lumi_puu_%d.png" % i) for i in range(1, 8)]
rocks = [ld("lumi_kivi_%d.png" % i) for i in range(1, 5)]
front = [ld("lumi_hiutale_%d.png" % i) for i in range(1, 4)]
tops = [ld("lumi_latva_%d.png" % i) for i in range(1, 6)]

skyS = scaled(sky, 1.2 * H / 1105 * 1.0)             # 1,2 ruudun korkuinen
forestS = scaled(forest, 0.8)
surfS = scaled(surf, 1.25); d2S = scaled(d2, 1 / 0.75); d1S = scaled(d1, 1 / 0.9)
slopeA = (slope[..., 3].astype(np.float32) / 255.0)[..., None]


class Flow:
    def __init__(self, sprites, count, speed, gain, dmin, dmax, smin, smax, tint=1.0, center=False):
        self.sp, self.n, self.v, self.g = sprites, count, speed * PPU, gain
        self.dmin, self.dmax, self.smin, self.smax, self.tint, self.center = dmin * H, dmax * H, smin, smax, tint, center
        self.margin = 0.3 * W
        self.total = (W + 2 * self.margin) / CA
        self.items = []
        for i in range(count):
            it = {"s": (i + random.uniform(0.1, 0.9)) / count * self.total}
            self.rand(it); self.items.append(it)

    def rand(self, it):
        it["img"] = random.choice(self.sp); it["d"] = random.uniform(self.dmin, self.dmax)
        it["sc"] = random.uniform(self.smin, self.smax); it["flip"] = random.random() < 0.5
        it["k"] = 1 + self.g * (it["d"] - self.dmin) / max(1e-3, self.dmax - self.dmin)
        im = it["img"]
        if it["flip"]: im = im[:, ::-1]
        im = scaled(np.ascontiguousarray(im), it["sc"])
        if self.center:
            h, w = im.shape[:2]; pad = int(max(h, w) * .5)
            big = cv2.copyMakeBorder(im, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
            M = cv2.getRotationMatrix2D((big.shape[1] / 2, big.shape[0] / 2), -math.degrees(ANG), 1.0)
            rot = cv2.warpAffine(big, M, (big.shape[1], big.shape[0]), flags=cv2.INTER_LINEAR, borderValue=(0, 0, 0, 0))
            it["rot"], it["px"], it["py"] = rot, rot.shape[1] / 2, rot.shape[0] / 2
        else:
            it["rot"], it["px"], it["py"] = rot_sprite(im, math.degrees(ANG))

    def step(self, dt):
        for it in self.items:
            it["s"] -= self.v * it["k"] * dt
            if it["s"] < 0: it["s"] += self.total; self.rand(it)

    def draw(self, dst, order_by_y=True):
        lst = []
        for it in self.items:
            x = -self.margin + it["s"] * CA
            y = 274 - self.margin * TA + it["s"] * SA + it["d"]    # kuvan y (alas kasvaa); juuri
            lst.append((y, x, it))
        for y, x, it in sorted(lst, key=lambda t: t[0]):
            blit(dst, it["rot"], x - it["px"], y - it["py"], self.tint)


def tile_layer(img, speed, t, phase, alpha):
    tw, th = img.shape[1], img.shape[0]
    tt = t * speed * PPU + phase * tw
    offx = (-tt * CA) % tw; offy = -(tt * SA) % th    # ylös: y pienenee
    layer = np.zeros((H, W, 4), np.float32)
    cols = int(math.ceil(W / tw)) + 2; rows = int(math.ceil(H / th)) + 2
    out = np.zeros((H, W, 3), np.float32); acc = np.zeros((H, W, 1), np.float32)
    for j in range(-1, rows):
        for i in range(-1, cols):
            x = offx + (i - 1) * tw; y = offy + (j - 1) * th
            x0, y0, x1, y1 = int(max(0, x)), int(max(0, y)), int(min(W, x + tw)), int(min(H, y + th))
            if x1 <= x0 or y1 <= y0: continue
            s = img[y0 - int(y):y1 - int(y), x0 - int(x):x1 - int(x)]
            if s.shape[0] != y1 - y0 or s.shape[1] != x1 - x0: continue
            a = s[..., 3:4].astype(np.float32) / 255.0 * alpha
            out[y0:y1, x0:x1] = s[..., :3]; acc[y0:y1, x0:x1] = a
    return out, acc


def main(outp, secs=6, ff="ffmpeg"):
    fps = 24; n = int(secs * fps)
    back_trees = Flow(trees, 10, 7, 0.4, 0.02, 0.10, 0.38, 0.55, 0.9)
    mid = Flow(trees + rocks + trees, 7, 12, 0.6, 0.10, 0.45, 0.9, 1.3)
    fr = Flow(front, 9, 24, 0.5, 0.05, 0.9, 0.55, 1.3, 0.8, True)
    ft = Flow(tops, 4, 21.6, 0.5, 0.05, 0.85, 1.6, 2.6, 0.9, True)
    frames = []
    for f in range(n):
        t = f / fps; dt = 1 / fps
        canvas = np.zeros((H, W, 3), np.float32)
        # taivas (vaakaliike, keskipiste 0,944 H alareunasta)
        tw = skyS.shape[1]; off = (-t * 0.35 * PPU) % tw
        sy = int(H * (1 - 0.944) - skyS.shape[0] / 2)
        for i in (-1, 0, 1, 2):
            blit(canvas, skyS, off + (i - 1) * tw, sy)
        # metsä
        fw = forestS.shape[1]; foff = (-t * 1.0 * PPU) % fw
        fy = H - 0.10 * H - forestS.shape[0]
        for i in (-1, 0, 1, 2):
            blit(canvas, forestS, foff + (i - 1) * fw, fy)
        blit(canvas, slope, 0, 0)
        # lumipinta maskattuna rinteeseen
        o, a = tile_layer(surfS, 14, t, 0.15, 0.85)
        canvas = canvas * (1 - a * slopeA) + o * a * slopeA
        o, a = tile_layer(d2S, 9, t, 0.6, 0.45)
        canvas = canvas * (1 - a) + o * a
        back_trees.step(dt); back_trees.draw(canvas)
        mid.step(dt); mid.draw(canvas)
        o, a = tile_layer(d1S, 17, t, 0.3, 0.9)
        canvas = canvas * (1 - a) + o * a
        ft.step(dt); ft.draw(canvas)
        fr.step(dt); fr.draw(canvas)
        # hahmon paikka (tilapäinen merkki)
        c8 = np.ascontiguousarray(canvas.clip(0, 255).astype(np.uint8))
        cv2.rectangle(c8, (790, 520), (860, 690), (60, 60, 230), -1)
        cv2.putText(c8, "hahmo", (780, 510), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (60, 60, 230), 2)
        frames.append(cv2.resize(c8, (1280, 720), interpolation=cv2.INTER_AREA))
    tmp = os.path.splitext(outp)[0] + "_raw.avi"
    vw = cv2.VideoWriter(tmp, cv2.VideoWriter_fourcc(*"MJPG"), fps, (1280, 720))
    for fr_ in frames: vw.write(fr_)
    vw.release()
    subprocess.run([ff, "-y", "-i", tmp, "-c:v", "libx264", "-pix_fmt", "yuv420p", "-crf", "20", outp], check=True, capture_output=True)
    os.remove(tmp)
    print("ok", outp)


if __name__ == "__main__":
    main(sys.argv[1], float(sys.argv[2]) if len(sys.argv) > 2 else 6, sys.argv[3] if len(sys.argv) > 3 else "ffmpeg")
