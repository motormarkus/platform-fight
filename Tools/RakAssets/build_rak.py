# Raksatyömaan taustakuvat: kokoaa käyttäjän kuvista (src/) kaksi 6000x720 riviä: maa (katu + 1. kerros) ja yläkerrat (kaksi kerrosta + katto).
# Käyttö: python build_rak.py   ->  Assets/Sprites/Taustat/rakennus_maa.png ja rakennus_ylos.png
from PIL import Image
import numpy as np
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

# --- saumojen korjaus: pystysiirto, sävytasaus ja pehmeä ristiinhäivytys (peilatut jatkeet) tiilten liitoskohdissa ---
def seam_fix(im, seams=(2000,4000), Z=56, ramp=240):
    a=np.array(im).astype(np.float32)
    a[...,:3]*=a[...,3:4]/255.0   # premultiply
    for sx in seams:
        Lc=a[:,sx-6:sx-1]; Rc=a[:,sx+1:sx+6]
        best=(1e9,0)
        for dy in range(-6,7):
            Rs=np.roll(Rc,dy,axis=0)
            m=(Lc[...,3]>200)&(Rs[...,3]>200)
            d=(np.abs(Lc[...,:3]-Rs[...,:3]).mean(axis=2)*m).sum(axis=1)/np.maximum(m.sum(axis=1),1)
            ok=m.any(axis=1)
            if ok.sum()>20 and d[ok].mean()<best[0]: best=(d[ok].mean(),dy)
        dy=best[1]
        if dy!=0:
            part=np.roll(a[:,sx:],dy,axis=0)
            if dy>0: part[:dy]=part[dy:dy+1]
            else: part[dy:]=part[dy-1:dy]
            a[:,sx:]=part
        # sävytasaus: rivikohtainen keskiero (pehmennetty) ramppina
        Lm=a[:,sx-30:sx-1]; Rm=a[:,sx+1:sx+30]
        am=(Lm[...,3]>250).mean(axis=1)>0.9; bm=(Rm[...,3]>250).mean(axis=1)>0.9
        ok=am&bm
        if ok.sum()>50:
            diff=(Rm[...,:3].mean(axis=1)-Lm[...,:3].mean(axis=1))
            diff=np.where(ok[:,None],diff,np.nan)
            col=np.nanmedian(diff,axis=0)
            col=np.nan_to_num(col)
            for k in range(ramp):
                w=0.5*(1-k/ramp)
                px=a[:,sx-1-k,:]; v=px[:,3:4]>0
                a[:,sx-1-k,:3]+=np.where(v,col*w,0)
                px=a[:,sx+k,:]; v=px[:,3:4]>0
                a[:,sx+k,:3]-=np.where(v,col*w,0)
        # ristiinhäivytys: vasen pala + sen peilijatke vs oikea pala + sen peilijatke
        L=a[:,sx-Z:sx].copy(); R=a[:,sx:sx+Z].copy()
        Lext=np.concatenate([L,L[:,::-1]],axis=1)   # sx-Z .. sx+Z
        Rext=np.concatenate([R[:,::-1],R],axis=1)
        t=np.linspace(0,1,2*Z,dtype=np.float32)[None,:,None]; t=t*t*(3-2*t)
        a[:,sx-Z:sx+Z]=Lext*(1-t)+Rext*t
    out=a.copy(); al=out[...,3:4]
    out[...,:3]=np.where(al>0,out[...,:3]/np.maximum(al/255.0,1e-3),0)
    out=np.clip(out,0,255).astype(np.uint8)
    out[out[...,3]==0]=0
    return Image.fromarray(out)
g=seam_fix(g); u=seam_fix(u,Z=12,ramp=160)

# --- pilarit: yläkerran alimmat pilarit (2. laatan alla) korvataan maakuvan yläreunan pilareilla samoilla x:illä, jotta pino on suora ---
def runs(a,y0,y1,minw=45):
    col=(a[y0:y1,:,3]>230).mean(axis=0)>0.96
    r=[];st=None
    for x,v in enumerate(col):
        if v and st is None: st=x
        if not v and st is not None:
            if x-st>=minw: r.append((st,x))
            st=None
    return r
def fix_pillars(u,g,y0=574,tol=120):
    ua=np.array(u); ga=np.array(g)
    ur=runs(ua,640,715); gr=runs(ga,4,60)
    for (gs,ge) in gr:
        best=None
        for (us,ue) in ur:
            d=abs((us+ue)/2-(gs+ge)/2)
            if d<tol and (best is None or d<best[0]): best=(d,us,ue)
        if best is None: continue
        _,us,ue=best
        ua[y0:720,max(0,us-2):ue+2]=0           # vanha pilari pois
        h=720-y0
        for k in range(h):                       # uusi pilari: maakuvan pilarin rivit edestakaisin liitoksesta ylöspäin
            period=2*36
            m=k%period; src=4+(m if m<36 else period-1-m)
            ua[719-k,gs:ge]=ga[src,gs:ge]
    return Image.fromarray(ua)
u=fix_pillars(u,g)
def clean(im):
    a=np.array(im); a[a[...,3]==0]=0; return Image.fromarray(a)
clean(g).save(OUT+'rakennus_maa.png'); clean(u).save(OUT+'rakennus_ylos.png')
st.resize((1500,360)).save('preview.png')
print('ok')
