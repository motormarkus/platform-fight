"""Tonyn arkit uudelleen videosta kevyemmällä reunakäsittelyllä: key_video.key_frame maalaa 9x9-kaistan reunasta tummaksi
ja syö ohuet sormet. Tässä: alfa ilman erodointia, tumma reuna vain 3x3 kaistalla, vihreä hohto pois.
Ajo: python Tools/TonyAssets/rekey_tony.py [uploads-kansio]"""
import os, sys
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
import key_video as kv

def key_soft(bgr, t_in=26.0, t_out=60.0):
    img = bgr.astype(np.float32)
    bg = kv.border_color(bgr).astype(np.float32)
    d = np.sqrt(((img - bg) ** 2).sum(axis=2))
    near = (d < t_in).astype(np.uint8)
    n, lab = cv2.connectedComponents(near, connectivity=4)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])).tolist()) - {0}
    bgm = np.isin(lab, list(edge))
    b, g, r = img[..., 0], img[..., 1], img[..., 2]
    bgm |= (g - r > 110) & (g - b > 80)   # vain selvä taustan vihreä (suljetut aukot); reunan sekapikselit (nenänpää, sormet) jäävät pehmeälle reunalle
    n2, lab2, st, _ = cv2.connectedComponentsWithStats((~bgm).astype(np.uint8), connectivity=8)
    if n2 > 2: bgm = bgm | (lab2 != 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])))
    alpha = np.where(bgm, 0.0, 1.0).astype(np.float32)
    soft = np.clip((d - t_in) / (t_out - t_in), 0, 1)
    ring = cv2.dilate(bgm.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool) & ~bgm
    alpha[ring] = soft[ring]
    alpha = np.clip((cv2.GaussianBlur(alpha, (0, 0), 1.1) - 0.5) * 1.7 + 0.5, 0, 1)   # sileä reuna (ei porrastusta suurennuksessa)
    out = img.copy()
    gmax = np.maximum(out[..., 2], out[..., 0])
    fringe = cv2.dilate((alpha < 0.5).astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool) | (alpha < 0.999)
    out[..., 1] = np.where(fringe, np.minimum(out[..., 1], gmax), out[..., 1])
    band = cv2.dilate((alpha < 0.5).astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & (alpha > 0)
    darkest = cv2.erode(out, np.ones((3, 3), np.uint8))
    out = np.where(band[..., None], darkest, out)
    return np.dstack([out[..., 2], out[..., 1], out[..., 0], alpha * 255.0]).clip(0, 255).astype(np.uint8)

_ORIG_KEY = kv.key_frame
_ORIG_BEST = m.best_loop
COPY_FN = None   # käsitellyn kopion muunnos (esim. mongolin strip_plume): sama tehdään vanhan avainnuksen kuville silmukan valintaan

def use_soft_keys(mod=m):
    global COPY_FN
    """make_king_sheets: load() avainnetaan key_softilla, mutta silmukat (best_loop) valitaan vanhan avainnuksen kuvista,
    jolloin kuvien määrä ja järjestys pysyvät samoina kuin aiemmin tehdyissä arkeissa."""
    import av
    paths, srcs = {}, {}
    def load(path):
        fr = [key_soft(f.to_ndarray(format="bgr24")) for f in av.open(path).decode(video=0)]
        paths[id(fr)] = path; srcs[path] = fr; return fr
    cache = {}
    def best_loop(frames, *a, **k):
        p = paths.get(id(frames))
        if p is None and frames:   # käsitelty kopio (esim. tupsu poistettu): tunnista lähde kuvien määrästä ja sisällöstä
            cands = [(np.abs(f0.astype(np.int16)[..., :3] - frames[len(frames) // 2].astype(np.int16)[..., :3]).mean(), q)
                     for q, f0 in ((q, srcs[q][len(frames) // 2]) for q in srcs if len(srcs[q]) == len(frames) and srcs[q][0].shape == frames[0].shape)]
            if cands: p = min(cands)[1]
        if p is None: return _ORIG_BEST(frames, *a, **k)
        copy = id(frames) not in paths and COPY_FN is not None
        key = (p, copy)
        if key not in cache:
            fr0 = [_ORIG_KEY(f.to_ndarray(format="bgr24")) for f in av.open(p).decode(video=0)]
            cache[key] = [COPY_FN(f) for f in fr0] if copy else fr0
        return _ORIG_BEST(cache[key], *a, **k)
    mod.load = load; mod.best_loop = best_loop; mod.key_frame = lambda bgr, *a, **k: key_soft(bgr)   # muut parametrit (cyan_safe ym.) ohitetaan

if __name__ == "__main__":
    # kaikki arkit uudelleen pehmeällä avainnuksella (paitsi kaatuminen: pölykorjaus käsin, ja suojaus: kuvasta, ei videosta)
    import shutil, tempfile
    U = sys.argv[1] if len(sys.argv) > 1 else "C:/Users/marku/.claude/uploads/04292e93-ed49-4896-966e-72709d802ca8"
    import make_tony_sheets as t
    use_soft_keys()
    tmp = tempfile.mkdtemp()
    t.main(U + "/be422470-gemini_generated_video_E34916BB.mp4", U + "/a47a71be-gemini_generated_video_E1F9C483.mov", U + "/978d9e0e-gemini_generated_video_AC287C34.mp4",
           U + "/ce386b3e-tank-tony-heitto.mp4", os.path.join(HERE, "tony_suojaus_lahde.png"), tmp,
           U + "/23b3f81a-tank-tony-haymaker-videomallista.mp4", U + "/212bc0d5-tank-tony-low-kick-viikate-8-ruutua.mp4", U + "/5165fd90-tank-tony-uppercut-mallivideosta.mp4")
    for f in sorted(os.listdir(tmp)):
        if f in ("tony_kaatuminen.png", "tony_suojaus.png"): continue
        shutil.copy(os.path.join(tmp, f), "Assets/Sprites/Viholliset/" + f); print("->", f)
