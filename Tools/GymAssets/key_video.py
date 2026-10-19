"""Jumppatytön videoruutujen taustan poisto (vihreä/kerma tausta vaihtelee ruuduittain).
Käyttö: python key_video.py <video.mov> -> palauttaa RGBA-ruudut (numpy) generaattorina."""
import cv2
import numpy as np


def border_color(img):
    b = np.concatenate([img[:10].reshape(-1, 3), img[-10:].reshape(-1, 3),
                        img[:, :10].reshape(-1, 3), img[:, -10:].reshape(-1, 3)])
    return np.median(b, axis=0)


def key_frame(bgr, t_in=26.0, t_out=60.0):
    img = bgr.astype(np.float32)
    bg = border_color(bgr).astype(np.float32)
    d = np.sqrt(((img - bg) ** 2).sum(axis=2))
    near = (d < t_in).astype(np.uint8)
    # tausta = reunoista yhteydessä oleva samanvärinen alue (valkoiset kengät eivät katoa kermatausta vasten)
    n, lab = cv2.connectedComponents(near, connectivity=4)
    edge_labels = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])).tolist()) - {0}
    bgmask = np.isin(lab, list(edge_labels))
    # vihreä tausta myös suljetuissa aukoissa (sydämet, käsivarsien väli)
    b, g, r = img[..., 0], img[..., 1], img[..., 2]
    # taustan ympärillä oleva vaalea hohto (kerma/valkoinen reuna ääriviivan ulkopuolella) kuuluu taustaan; ääriviivan sisällä olevat valkoiset kengät eivät ole yhteydessä
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    light = ((hsv[..., 2] > 195) & (hsv[..., 1] < 70)) | bgmask
    n3, lab3 = cv2.connectedComponents(light.astype(np.uint8), connectivity=4)
    touching = np.unique(lab3[bgmask]); touching = touching[touching > 0]
    bgmask = np.isin(lab3, touching)
    # vihreä/turkoosi tausta myös suljetuissa aukoissa (käsivarsien väli, taustan sävy vaihtuu ruuduittain)
    bgmask |= (g - r > 45)
    # vain suurin yhtenäinen hahmo (irtonaiset piirretyt sydämet ym. pois)
    n2, lab2, st, _ = cv2.connectedComponentsWithStats((~bgmask).astype(np.uint8), connectivity=8)
    if n2 > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
        bgmask = bgmask | (lab2 != keep)
    alpha = np.where(bgmask, 0.0, 1.0).astype(np.float32)
    # pehmeä reuna: lähellä taustaa olevat reunapikselit läpinäkyvämmiksi
    soft = np.clip((d - t_in) / (t_out - t_in), 0, 1)
    ring = cv2.dilate(bgmask.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & ~bgmask
    alpha[ring] = soft[ring]
    alpha = cv2.erode(alpha, np.ones((3, 3), np.uint8))
    # vihreä reunahohto pois
    out = bgr.copy().astype(np.float32)
    fringe = (alpha < 0.999) | cv2.dilate((alpha < 0.5).astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
    gmax = np.maximum(out[..., 2], out[..., 0])
    out[..., 1] = np.where(fringe, np.minimum(out[..., 1], gmax + 8), out[..., 1])
    # reunapikselit saavat lähimmän tumman (ääriviivan) värin: ei vaaleaa tai taustan värjäämää rajaa
    band = cv2.dilate((alpha < 0.5).astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool)
    darkest = cv2.erode(out, np.ones((5, 5), np.uint8))
    out = np.where(band[..., None], darkest, out)
    rgba = np.dstack([out[..., 2], out[..., 1], out[..., 0], alpha * 255.0]).clip(0, 255).astype(np.uint8)
    return rgba


def frames(path):
    cap = cv2.VideoCapture(path)
    while True:
        ok, f = cap.read()
        if not ok:
            break
        yield f
