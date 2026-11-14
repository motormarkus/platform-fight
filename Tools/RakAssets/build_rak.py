# Raksatyömaan taustakuvat: kokoaa käyttäjän kuvista (src/) kaksi 6000x720 riviä: maa (katu + 1. kerros) ja yläkerrat (kaksi kerrosta + katto).
# Käyttö: python build_rak.py   ->  Assets/Sprites/Taustat/rakennus_maa.png ja rakennus_ylos.png
from PIL import Image
S='src/'; OUT='../../Assets/Sprites/Taustat/'
def L(n): return Image.open(S+n+'.png').convert('RGBA')
L26,M25,R24=L('maa_vasen_paa'),L('maa_keski'),L('maa_oikea_paa')
U23,U21,U22=L('ylakerta_vasen_paa'),L('ylakerta_keski'),L('ylakerta_oikea_paa')
W=2000
# vasen pää: rakennusosa (y<437) venytetään jalkakäytävän reunaan 478, katu alla sellaisenaan -> kaikki kolme palaa samalla jalkakäytävällä
top=L26.crop((0,0,W,437)).resize((W,478),Image.LANCZOS)
bot=L26.crop((0,437,W,670))
left=Image.new('RGBA',(W,720),(0,0,0,0)); left.alpha_composite(top,(0,0)); left.alpha_composite(bot,(0,478))
last=left.crop((0,478+232,W,478+233)).resize((W,720-(478+233)))
left.alpha_composite(last,(0,478+233))
g=Image.new('RGBA',(3*W,720),(0,0,0,0))
for i,p in enumerate([left,M25,R24]): g.alpha_composite(p,(i*W,0))
# yläkerrat: keskipala venytetään 2000 leveäksi ja rajataan 720 korkeaksi
u21=U21.resize((W,int(round(798*W/1971))),Image.LANCZOS).crop((0,0,W,720))
u=Image.new('RGBA',(3*W,720),(0,0,0,0))
for i,p in enumerate([U23,u21,U22]): u.alpha_composite(p,(i*W,0))
# tikas 1. kerroksen ja 2. kerroksen välille: käyttäjän omasta tikaskuvasta (21), sama tikas kahteen kohtaan
lad=U21.crop((930,150,1000,500))   # tikas 70x350
LX=[4665-35, 2330-35]   # tikkaiden vasemmat reunat (pikseliä rivillä)
for lx in LX:
    # ylä: 2.kerroksen laatan päältä (~y 490 yläkuvassa) -> 1.kerroksen laatan päälle (maakuvan y 108 = pino 828)
    u.alpha_composite(lad,(lx,490-20)) if False else None
# yhdistetty pino 6000x1440 (ylä + maa) että tikas voi ylittää saumankin
st=Image.new('RGBA',(3*W,1440),(0,0,0,0)); st.alpha_composite(u,(0,0)); st.alpha_composite(g,(0,720))
for lx in LX:
    top_y=470; bot_y=830
    st.alpha_composite(lad.resize((70,bot_y-top_y),Image.LANCZOS),(lx,top_y))
u=st.crop((0,0,3*W,720)); g=st.crop((0,720,3*W,1440))
import numpy as np
def clean(im):
    a=np.array(im); a[a[...,3]==0]=0; return Image.fromarray(a)
clean(g).save(OUT+'rakennus_maa.png'); clean(u).save(OUT+'rakennus_ylos.png')
st.resize((1500,360)).save('preview.png')
print('ok')
