"""WTF-areenan julkisivu Vegasin kadulle (käyttäjän kuvat output/imagegen/vegas-wtf-areena-3ruutua, 2089x753).
- skaalataan Vegasin katukuvien kokoon 5120x1844 (sama kuvasuhde)
- rakennus (rivit < 1302) pienennetään (~0,91) niin, että alareuna osuu jalkakäytävän yläreunaan (vegas_tie_patka rivi 1184) ja kyltti mahtuu
- jalkakäytävä + katu otetaan vegas_tie_patka.png:stä (ruudut 1 ja 3 peilattuina, jotta saumat täsmäävät)
- valkoinen taivas (vasen/oikea pääty) läpinäkyväksi (täyttö reunoista, pehmeä reuna)
Tulos: Assets/Sprites/Taustat/vegas_areena_1..3.png"""
import sys
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import numpy as np
SRC = 'output/imagegen/vegas-wtf-areena-3ruutua/'
names = ['vegas_areena_1_vasen', 'vegas_areena_2_sisaankaynti', 'vegas_areena_3_oikea']
W, H = 5120, 1844
CUT, TOP = 1302, 1182          # areenan rakennuksen alareuna -> kadun jalkakäytävän yläreuna
tie = Image.open('Assets/Sprites/Taustat/vegas_tie_patka.png').convert('RGBA')
# 1) kolme ruutua yhdeksi nauhaksi (15360 x 1844)
strip = Image.new('RGB', (3 * W, H))
for i, n in enumerate(names):
    strip.paste(Image.open(SRC + n + '.png').convert('RGB').resize((W, H), Image.LANCZOS), (i * W, 0))
a = np.asarray(strip).astype(np.int16)
SW = 3 * W
# 2) valkoinen taivas läpinäkyväksi (yhteydessä nauhan päätyihin)
white = (a.min(axis=2) >= 232) & ((a.max(axis=2) - a.min(axis=2)) <= 14)
m = Image.fromarray((white * 255).astype(np.uint8)).copy()
for seed in [(2, 2), (SW - 3, 2), (2, CUT - 10), (SW - 3, CUT - 10)]:
    if m.getpixel(seed) == 255: ImageDraw.floodfill(m, seed, 128)
sky = (np.asarray(m) == 128)
near = np.asarray(Image.fromarray((sky * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
lum = a.min(axis=2)
alpha = np.full((H, SW), 255, np.float32); alpha[sky] = 0
edge = near & ~sky
alpha[edge] = np.clip((255 - lum[edge]) / (255 - 150) * 255, 0, 255)
rgb = a.astype(np.float32); kk = alpha[..., None] / 255.0
rgb = np.where(edge[..., None], np.clip((rgb - 255 * (1 - kk)) / np.maximum(kk, 0.05), 0, 255), rgb)
bld = Image.fromarray(np.dstack([rgb.astype(np.uint8), alpha.astype(np.uint8)]), 'RGBA').crop((0, 0, SW, CUT))
# 3) rakennus pienennetään niin, että alareuna (CUT) osuu kadun jalkakäytävään (TOP) ja kyltti mahtuu kokonaan; keskitetään (päädyissä vain taivasta)
k = TOP / CUT
bw, bh = round(SW * k), TOP
bld = bld.resize((bw, bh), Image.LANCZOS)
canvas = Image.new('RGBA', (SW, H), (0, 0, 0, 0))
canvas.alpha_composite(bld, ((SW - bw) // 2, 0))
# 4) jalkakäytävä + katu tie_patkasta (ruudut 1 ja 3 peilattuina -> saumat täsmäävät), rakennus päälle, pilkotaan 3 ruuduksi
for i in range(3):
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    lower = tie.crop((0, TOP, W, H))
    if i % 2 == 0: lower = lower.transpose(Image.FLIP_LEFT_RIGHT)
    out.paste(lower, (0, TOP))
    out.alpha_composite(canvas.crop((i * W, 0, (i + 1) * W, H)))
    dst = 'Assets/Sprites/Taustat/vegas_areena_%d.png' % (i + 1)
    out.save(dst); print(dst)
print('pienennys', round(k, 3), 'taivasta', round(sky.mean() * 100, 1), '%')
