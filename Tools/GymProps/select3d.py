import numpy as np
def faces_in_box(V, F, box, mode="all"):
    x0, x1, y0, y1, z0, z1 = box
    out = []
    for i, t in enumerate(F):
        P = V[[t[0][0], t[1][0], t[2][0]]]
        c = P.mean(0) if mode == "center" else None
        if mode == "center":
            ok = x0 <= c[0] <= x1 and y0 <= c[1] <= y1 and z0 <= c[2] <= z1
        else:
            ok = (P[:, 0] >= x0).all() and (P[:, 0] <= x1).all() and (P[:, 1] >= y0).all() and (P[:, 1] <= y1).all() and (P[:, 2] >= z0).all() and (P[:, 2] <= z1).all()
        if ok: out.append(i)
    return out
