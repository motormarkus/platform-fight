"""Arvio SampleScenen tekstuurimuistista (vain scenen suoraan viittaamat sprite-tekstuurit; GPU-muisti arvioitu).
Käyttö projektin juuresta: python Tools/MemoryReport.py   (0 = pakkaamaton 4 B/px, muuten ~1 B/px BC7/DXT5)"""
import re,glob
from PIL import Image
Image.MAX_IMAGE_PIXELS=None
s=open('Assets/Scenes/SampleScene.unity',encoding='utf-8',errors='ignore').read()
guids=set(re.findall(r'guid: ([0-9a-f]{32}), type: 3',s))
tot=0;unc=0;n=0;byfolder={}
for m in glob.glob('Assets/**/*.meta',recursive=True):
    t=open(m,encoding='utf-8',errors='ignore').read()
    g=re.search(r'guid: ([0-9a-f]{32})',t)
    if not g or g.group(1) not in guids: continue
    p=m[:-5].replace(chr(92),'/')
    if not p.lower().endswith(('.png','.jpg','.jpeg')): continue
    try: w,h=Image.open(p).size
    except: continue
    ms=int(re.search(r'maxTextureSize: (\d+)',t).group(1)) if 'maxTextureSize' in t else 2048
    comp=int(re.search(r'textureCompression: (\d)',t).group(1)) if 'textureCompression' in t else 1
    mip=int(re.search(r'enableMipMap: (\d)',t).group(1)) if 'enableMipMap' in t else 0
    sc=min(1,ms/max(w,h)); ww,hh=w*sc,h*sc
    bpp=4 if comp==0 else 1
    mb=ww*hh*bpp*(1.33 if mip else 1)/1e6
    tot+=mb; n+=1
    if comp==0: unc+=mb
    key='/'.join(p.split('/')[1:3])
    byfolder[key]=byfolder.get(key,0)+mb
print('tekstuureja',n,'arvio muisti MB',int(tot),'josta pakkaamattomia',int(unc))
for k,v in sorted(byfolder.items(),key=lambda x:-x[1])[:8]: print(' ',k,int(v),'MB')
