"""Kuutie jatkuu kuun tukikohtaan (Kiinan muuri -scene).
- muuri_kuuasema_ulko.png: tukikohdan ulkokuva (korkeus 941, leveys 2385), sovitetaan kuun pinnan kuvan a vasempaan reunaan (kirkkaus liukuen); asetetaan pinnan vasempaan päähän.
- muuri_laboratorio_pano.png: kaksi sisäkuvaa yhdistettynä (kuvat limittyvät 302 px: kuva 3 alkaa kuvan 2 x-kohdasta 1694, 2 px ylempänä), korkeus 941. Ikkunat läpinäkyviä (kuvan 3 alpha).
Ajo: python Tools/MoonAssets/make_lunar_base.py <ulko.png> <sisa1.png> <sisa2.png> [Assets/Sprites/Taustat]"""
import sys, os
import numpy as np
import cv2

H = 941


def load_rgba(p):
    im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 3: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
    return im   # BGRA


def scale_h(im, h=H):
    w = round(im.shape[1] * h / im.shape[0])
    a = im.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    r = cv2.resize(a, (w, h), interpolation=cv2.INTER_CUBIC)
    al = np.clip(r[..., 3:4] / 255.0, 0, 1); r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0); r[..., 3:4] = al * 255.0
    return r.clip(0, 255).astype(np.uint8)


def exterior(src, surfA, out):
    b = scale_h(load_rgba(src))
    A = load_rgba(surfA)
    # kirkkaus: base oikea reuna -> A vasen reuna (maa-alue, rivit 600+)
    def edge(im, cols):
        px = im[600:, cols].reshape(-1, 4); px = px[px[:, 3] > 200]; return px[:, :3].mean(axis=0)
    eb = edge(b, slice(b.shape[1] - 40, b.shape[1])); ea = edge(A, slice(0, 40))
    ratio = np.clip(ea / np.maximum(eb, 1.0), 0.8, 1.25); print("kirkkauskerroin", ratio.round(3))
    span = 500
    k = np.ones(b.shape[1], np.float32)
    ramp = np.linspace(0, 1, span, dtype=np.float32) ** 1.5
    f = b.astype(np.float32)
    for c in range(3): f[:, b.shape[1] - span:, c] *= (1 + (ratio[c] - 1) * ramp)[None, :]
    b[..., :3] = f[..., :3].clip(0, 255).astype(np.uint8)
    cv2.imwrite(out, b); print(out, b.shape[1], "x", b.shape[0])


def interior(i2p, i3p, out, ox=1694, dy=-2):
    a = load_rgba(i2p); b = load_rgba(i3p)
    h0 = a.shape[0]; W = ox + b.shape[1]
    bf = np.zeros((h0, b.shape[1], 4), np.uint8)
    for y in range(b.shape[0]):
        yy = y + dy
        if 0 <= yy < h0: bf[yy] = b[y]
    if dy < 0: bf[h0 + dy:] = bf[h0 + dy - 1]   # kuva 3 on 1 px matalampi: alin rivi toistetaan
    cv = np.zeros((h0, W, 4), np.float32)
    cv[:, :a.shape[1]] = a
    ov = a.shape[1] - ox
    w = np.zeros(W, np.float32); w[ox:ox + ov] = np.linspace(0, 1, ov); w[ox + ov:] = 1.0
    # limitys: kuvan 3 alpha huomioiden (ikkunat läpinäkyviä vain limityksen oikealla puolella)
    b3 = np.zeros((h0, W, 4), np.float32); b3[:, ox:] = bf
    wb = w[None, :, None]
    rgb = cv[..., :3] * (1 - wb) + b3[..., :3] * wb
    alpha = np.where(wb[..., 0] >= 1.0, b3[..., 3], 255.0)   # limityksessä opaakki
    res = np.dstack([rgb, alpha]).clip(0, 255).astype(np.uint8)
    res = scale_h(res)
    cv2.imwrite(out, res); print(out, res.shape[1], "x", res.shape[0])


if __name__ == "__main__":
    ext, i2, i3 = sys.argv[1:4]
    outd = sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Taustat"
    exterior(ext, os.path.join(outd, "muuri_kuupinta_a.png"), os.path.join(outd, "muuri_kuuasema_ulko.png"))
    interior(i2, i3, os.path.join(outd, "muuri_laboratorio_pano.png"))
