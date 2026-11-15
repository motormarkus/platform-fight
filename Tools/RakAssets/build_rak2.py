# Raksatyömaa, yhtenäinen kuva (output/imagegen/.../vegas-tyomaa-yhtenainen-15360x5632.png) -> 3 palaa 4096x4506 (skaala 0.8) Assets/Sprites/Taustat/rakennus_1..3.png
# Lisäksi kopioidaan kuvan oma tikas keskikerroksen (2. ja 3. laatan välin) kohdalle kolmeen tikasakseliin.
import numpy as np, json
from PIL import Image
Image.MAX_IMAGE_PIXELS=None
SRC='../../output/imagegen/vegas-tyomaa-tarkka-v2/vegas-tyomaa-tarkka-15360x5632.png'
OUT='../../Assets/Sprites/Taustat/'
im=Image.open(SRC).convert('RGBA'); a=np.array(im)
import cv2
g=cv2.cvtColor(a[:,:,:3],cv2.COLOR_RGB2GRAY)
tpl=g[1150:2050,13234:13454]
def find(x0,x1):
    reg=g[1150:2050,x0:x1]
    r=cv2.matchTemplate(reg,tpl,cv2.TM_CCOEFF_NORMED); _,mv,_,ml=cv2.minMaxLoc(r)
    return x0+ml[0]+110, mv
cx=[]
for x0,x1 in [(1850,2400),(7600,8200),(13100,13600)]:
    c,mv=find(x0,x1); print('löytyi',c,round(mv,2)); cx.append(c)
print('tikaskeskit',cx)
cx[0]=2110   # vasen tikas ei täsmää mallipohjaan: keskikohta mitattu kuvasta
HW=150
for c in cx:
    c=int(round(c)); lad=im.crop((c-HW,1030,c+HW,2180))
    im.alpha_composite(lad,(c-HW,3484-1150))
json.dump({'ladderX':cx},open('rak2_tikkaat.json','w'))
b=np.array(im); b[b[...,3]==0]=0   # täysi tarkkuus 15360x5632, 4 palaa 3840 leveä
for i in range(4):
    Image.fromarray(b[:,i*3840:(i+1)*3840]).save(OUT+f'rakennus_{i+1}.png')
Image.fromarray(b).resize((1536,563)).save('rak2_preview.png')
print('ok')
