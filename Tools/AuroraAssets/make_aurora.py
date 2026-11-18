# Auroran uudet kuvasarjat videoista (ruudut 512 x 448, pivot alakeskellä, 2x resoluutio, jalat rivillä 447, vihreä tausta avataan)
import av, glob, numpy as np, cv2, sys
from PIL import Image
UP = 'C:/Users/marku/.claude/uploads/1ce608b0-0f6d-41e0-b501-e782c570c2d7/'
OUT = 'Assets/Sprites/'
CW, CH, FLOOR, ANCH = 512, 448, 447, 317
PRE = 'C:/Users/marku/AppData/Local/Temp/claude/a2e04200c6136942/1ce608b0-0f6d-41e0-b501-e782c570c2d7/scratchpad/'
def load(k): return [x.to_ndarray(format='rgb24') for x in av.open(glob.glob(UP + k + '*.mov')[0]).decode(video=0)]
def key(f):
    rgb = f.astype(np.float32)
    bg = np.median(np.concatenate([f[:20, :20].reshape(-1, 3), f[:20, -20:].reshape(-1, 3)]), 0).astype(np.float32)
    dom = rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2]); bgdom = bg[1] - max(bg[0], bg[2])
    a = np.clip(1 - (dom - 0.25 * bgdom) / (0.55 * bgdom), 0, 1)
    a = cv2.GaussianBlur(a, (0, 0), 0.7)
    r, g, b = [rgb[..., i] for i in range(3)]
    g2 = np.minimum(g, np.maximum(r, b) + 6)
    return np.dstack([r, g2, b, a * 255])
def mass(k):
    ys, xs = np.where(k[..., 3] > 128); return xs.mean(), ys.max()
def feet_x(k):
    a = k[..., 3] > 128; ys, xs = np.where(a); y1 = ys.max(); h = ys.max() - ys.min()
    m = a[:, :]; m = m & (np.arange(a.shape[0])[:, None] > y1 - 0.04 * h)
    xx = np.where(m)[1]; return xx.mean()
def cell(k, sc, ax, floor_y):
    # ax = lähdekuvan x, joka tulee ruudun x:ään ANCH; floor_y = lähdekuvan lattiarivi, joka tulee riville FLOOR
    pm = k.copy(); pm[..., :3] *= pm[..., 3:4] / 255
    pm = cv2.resize(pm, None, fx=sc, fy=sc, interpolation=cv2.INTER_AREA)
    pm[..., :3] = np.where(pm[..., 3:4] > 0, pm[..., :3] / np.maximum(pm[..., 3:4] / 255, 1e-3), 0)
    c = np.zeros((CH, CW, 4), np.uint8)
    ox = int(round(ANCH - ax * sc)); oy = int(round(FLOOR - floor_y * sc))
    h, w = pm.shape[:2]
    x0, y0 = max(0, ox), max(0, oy); x1, y1 = min(CW, ox + w), min(CH, oy + h)
    c[y0:y1, x0:x1] = np.clip(pm[y0 - oy:y1 - oy, x0 - ox:x1 - ox], 0, 255).astype(np.uint8)
    if c[0, :, 3].max() > 30 or c[:, 0, 3].max() > 30 or c[:, -1, 3].max() > 30: print('  REUNAAN OSUU')
    return c
def sheet(cells, name, cols=4):
    rows = (len(cells) + cols - 1) // cols
    sh = np.zeros((rows * CH, cols * CW, 4), np.uint8)
    for i, c in enumerate(cells): sh[(i // cols) * CH:(i // cols + 1) * CH, (i % cols) * CW:(i % cols + 1) * CW] = c
    Image.fromarray(sh).save(OUT + name + '.png'); print(name, len(cells), 'kuvaa', sh.shape)
    pv = sh.astype(np.float32); bgc = np.full(pv.shape[:2] + (3,), 70, np.float32)
    pv = pv[..., :3] * pv[..., 3:4] / 255 + bgc * (1 - pv[..., 3:4] / 255)
    s = 0.25; Image.fromarray(cv2.resize(pv.astype(np.uint8), None, fx=s, fy=s, interpolation=cv2.INTER_AREA)).save(PRE + 'prev_' + name + '.png')

RISE_END, LOOP_S, LOOP_P = int(sys.argv[1]) if len(sys.argv) > 1 else 52, None, 56
A = load('24313d51'); KA = [key(f) for f in A]
walkr = range(60, 200)
SA = 369 / np.median([ (lambda a: (np.where(a)[0].max() - np.where(a)[0].min()))(k[..., 3] > 128) for k in [KA[i] for i in walkr] ])
axA = np.median([mass(KA[i])[0] for i in walkr]); flA = np.median([mass(KA[i])[1] for i in walkr])
print('A scale', SA, 'ax', axA, 'floor', flA)
# nousu (ruudut 0..RISE_END-1, joka 2.) + kävelysilmukka (RISE_END..RISE_END+59, joka 2.)
rise = [cell(KA[i], SA, axA, flA) for i in range(0, RISE_END, 2)]
loop = [cell(KA[i], SA, axA, flA) for i in range(RISE_END, RISE_END + LOOP_P, 2)]
sheet(rise, 'turisti_aurora_nousu'); sheet(loop, 'turisti_aurora_kavely')
# istuva idle (video C): ruutu 0 samaan kohtaan kuin nousun ruutu 0
C = load('a4289717'); KC = [key(f) for f in C]
SC = SA * 1.0
# C:n mittakaava: istuva hahmo yhtä iso kuin A:n ruutu 0
hA = lambda k: np.ptp(np.where(k[..., 3] > 128)[0])
SC = SA * hA(KA[0]) / hA(KC[0])
mA0 = mass(KA[0]); mC0 = mass(KC[0])
# C:n piste (mC0) -> sama ruudun kohta kuin A:n (mA0)
axC = mC0[0] - (mA0[0] - axA) * SA / SC; flC = mC0[1] - (mA0[1] - flA) * SA / SC
print('C scale', SC)
sit = [cell(KC[i], SC, axC, flC) for i in range(0, len(KC), 4)]
sheet(sit, 'turisti_aurora_istuu')
# kääntyminen ja seisova idle (video B)
B = load('5de39001'); KB = [key(f) for f in B]
tail = range(140, 190)
axB = np.median([feet_x(KB[i]) for i in tail]); flB = np.median([mass(KB[i])[1] for i in range(40, 193)])
hB = np.median([hA(KB[i]) for i in tail]); SB = 369 / hB
print('B scale', SB, 'feet x', axB, 'floor', flB)
turn = [cell(KB[i], SB, axB, flB) for i in range(48, 140, 3)]
idle = [cell(KB[i], SB, axB, flB) for i in range(140, 185, 3)]
sheet(turn, 'turisti_aurora_kaanto'); sheet(idle, 'turisti_aurora_idle')
