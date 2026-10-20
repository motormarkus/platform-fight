"""Pieni ortografinen ohjelmistorenderöijä (numpy): gym pack -mallin osat spriteiksi. Pohjaväri (GYM_Base_Color) + ambient occlusion + Lambert-valaistus.
Kamera: yaw (kierto pystyakselin ympäri, astetta), pitch (katse alaspäin, astetta); y ylös, yksiköt cm."""
import numpy as np
from PIL import Image
Image.MAX_IMAGE_PIXELS = None

class Tex:
    def __init__(self, base, ao=None):
        self.base = np.asarray(Image.open(base).convert("RGB"))
        self.ao = np.asarray(Image.open(ao).convert("L")) if ao else None
        self.h, self.w = self.base.shape[:2]
    def sample(self, uv):
        u = np.mod(uv[:, 0], 1.0) * (self.w - 1); v = (1.0 - np.mod(uv[:, 1], 1.0)) * (self.h - 1)
        x0 = np.clip(u.astype(int), 0, self.w - 2); y0 = np.clip(v.astype(int), 0, self.h - 2); fx = (u - x0)[:, None]; fy = (v - y0)[:, None]
        b = self.base
        c = (b[y0, x0] * (1 - fx) * (1 - fy) + b[y0, x0 + 1] * fx * (1 - fy) + b[y0 + 1, x0] * (1 - fx) * fy + b[y0 + 1, x0 + 1] * fx * fy)
        if self.ao is not None:
            a = self.ao; ao = (a[y0, x0] * (1 - fx[:, 0]) + a[y0, x0 + 1] * fx[:, 0]) / 255.0
            c = c * (0.35 + 0.65 * ao)[:, None]
        return c

def view_matrix(yaw, pitch):
    ya, pa = np.radians(yaw), np.radians(pitch)
    Ry = np.array([[np.cos(ya), 0, np.sin(ya)], [0, 1, 0], [-np.sin(ya), 0, np.cos(ya)]])
    Rx = np.array([[1, 0, 0], [0, np.cos(pa), -np.sin(pa)], [0, np.sin(pa), np.cos(pa)]])
    return Rx @ Ry

def render(V, VT, VN, F, face_ids, tex, yaw=0.0, pitch=15.0, ppcm=3.0, pad=6, light=(-0.4, 0.8, 0.5), center=None, face_tint=None):
    """face_tint: {kolmion indeksi: (r, g, b) kerroin} -> värjää osia (käsipainot, levyt)."""
    R = view_matrix(yaw, pitch)
    face_ids = np.asarray(face_ids)
    vi = np.array([[F[i][k][0] for k in range(3)] for i in face_ids])
    P = (V[np.unique(vi)] @ R.T)
    mn, mx = P.min(0), P.max(0)
    W = int((mx[0] - mn[0]) * ppcm) + 2 * pad; H = int((mx[1] - mn[1]) * ppcm) + 2 * pad
    img = np.zeros((H, W, 4), np.float32); zb = np.full((H, W), -1e9, np.float32)
    L = np.array(light, float); L /= np.linalg.norm(L)
    for fi in face_ids:
        tri = F[fi]
        p = np.array([V[tri[k][0]] for k in range(3)]) @ R.T
        sx = (p[:, 0] - mn[0]) * ppcm + pad; sy = H - ((p[:, 1] - mn[1]) * ppcm + pad); z = p[:, 2]
        x0, x1 = int(max(0, np.floor(sx.min()))), int(min(W - 1, np.ceil(sx.max())))
        y0, y1 = int(max(0, np.floor(sy.min()))), int(min(H - 1, np.ceil(sy.max())))
        if x1 < x0 or y1 < y0: continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        d = (sy[1] - sy[2]) * (sx[0] - sx[2]) + (sx[2] - sx[1]) * (sy[0] - sy[2])
        if abs(d) < 1e-9: continue
        l0 = ((sy[1] - sy[2]) * (gx - sx[2]) + (sx[2] - sx[1]) * (gy - sy[2])) / d
        l1 = ((sy[2] - sy[0]) * (gx - sx[2]) + (sx[0] - sx[2]) * (gy - sy[2])) / d
        l2 = 1 - l0 - l1
        m = (l0 >= -0.02) & (l1 >= -0.02) & (l2 >= -0.02)
        if not m.any(): continue
        zz = l0 * z[0] + l1 * z[1] + l2 * z[2]
        ys, xs = np.where(m); zv = zz[ys, xs]
        ok = zv > zb[ys + y0, xs + x0]
        if not ok.any(): continue
        ys, xs, zv = ys[ok], xs[ok], zv[ok]
        w0, w1, w2 = l0[ys, xs], l1[ys, xs], l2[ys, xs]
        uv = np.stack([w0, w1, w2], 1) @ np.array([VT[tri[k][1]] if tri[k][1] >= 0 else (0, 0) for k in range(3)])
        col = tex.sample(uv)
        if face_tint is not None and fi in face_tint: col = col * np.array(face_tint[fi])
        if all(tri[k][2] >= 0 for k in range(3)):
            n = np.stack([w0, w1, w2], 1) @ np.array([VN[tri[k][2]] for k in range(3)])
        else:
            n = np.tile(np.cross(p[1] - p[0], p[2] - p[0]), (len(w0), 1))
        n = n @ R.T; n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-9)
        # kamera katsoo -z -> +z suuntaan: pinta kohti kameraa, jos n.z > 0
        lam = np.clip(n @ L, 0, 1)
        shade = 0.55 + 0.45 * lam
        img[ys + y0, xs + x0, :3] = col * shade[:, None]; img[ys + y0, xs + x0, 3] = 255; zb[ys + y0, xs + x0] = zv
    return img.clip(0, 255).astype(np.uint8)
