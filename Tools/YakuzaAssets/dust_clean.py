import numpy as np, cv2
def clean_dust(a):
    """Poistaa harmaan/valkoisen pölyn ja savun (keinotekoinen pöly ruudun reunassa): matala saturaatio, ei kermanvärinen paita, kiinni hahmon ulkoreunassa."""
    out = a.copy(); al = a[..., 3]; rgb = a[..., :3].astype(int)
    mn = rgb.min(2); mx = rgb.max(2); sat = (mx - mn) / np.maximum(mx, 1)
    cream = (rgb[..., 0] - rgb[..., 2]) > 32            # kermanvärinen paita ja iho
    cand = (al > 8) & (sat < 0.24) & (mn > 85) & (mn < 208) & (~cream)
    cand |= (al > 8) & (al < 235) & (sat < 0.35) & (mn > 70) & (~cream)   # puolilapinäkyvä sumu
    pink = (al > 8) & (np.abs(rgb[..., 1] - rgb[..., 2]) < 20) & (rgb[..., 1] > 68) & (rgb[..., 1] < 190) & (rgb[..., 0] > rgb[..., 1] + 25) & (rgb[..., 0] - rgb[..., 2] < 120)   # vaaleanpunainen sumu (puku sekoittuu pölyyn)
    ys0, xs0 = np.where(al > 10)
    lower = np.zeros_like(al, bool)
    if len(ys0): lower[ys0.min() + int((ys0.max() - ys0.min()) * 0.45):] = True
    below = np.ones_like(al, bool); below[:-14] = al[14:] < 10   # alapuolella (14 px) on tyhjää: pinta on hahmon alareunan kaista
    pink &= lower & below
    edge = cv2.dilate((al < 10).astype(np.uint8), np.ones((13, 13), np.uint8)) > 0
    n, lab, st, _ = cv2.connectedComponentsWithStats(cand.astype(np.uint8), connectivity=8)
    kill = np.zeros_like(cand)
    for i in range(1, n):
        m = lab == i
        if m.sum() >= 12 and (m & edge).any(): kill |= m
    edge2 = cv2.dilate((al < 10).astype(np.uint8), np.ones((29, 29), np.uint8)) > 0
    n2, lab2, _, _ = cv2.connectedComponentsWithStats(pink.astype(np.uint8), connectivity=8)
    for i in range(1, n2):
        m = lab2 == i
        if m.sum() >= 12 and (m & edge2).any(): kill |= m
    kill = cv2.dilate(kill.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    kill &= (al > 0)
    out[kill, 3] = 0
    return out, int(kill.sum())
