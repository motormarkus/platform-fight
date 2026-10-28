"""Rekkamies: lippiksen leveyden mittaus silmämääräisesti. Ruudut 3x, viivat 10 px välein (pitkä 50 px välein), nimi + ruudun indeksi."""
import sys
from PIL import Image, ImageDraw
import numpy as np
S='Assets/Sprites/Viholliset/'
def ruler(items,path,cols=4,W=160,H=100,sc=3):
    rows=(len(items)+cols-1)//cols
    out=Image.new('RGBA',(W*sc*cols,(H*sc+14)*rows),(60,70,90,255)); d=ImageDraw.Draw(out)
    for k,(n,i) in enumerate(items):
        im=Image.open(S+f'rekkamies_{n}.png').convert('RGBA'); c0=im.width//512
        r,c=divmod(i,c0); f=im.crop((c*512,r*448,(c+1)*512,(r+1)*448))
        a=np.asarray(f)[...,3]>128; ys,xs=np.nonzero(a); t=ys.min(); cx=int(np.median(xs[ys<t+45]))
        x0=cx-W//2+12
        crop=f.crop((x0,t-8,x0+W,t-8+H)).resize((W*sc,H*sc),Image.NEAREST)
        X=(k%cols)*W*sc; Y=(k//cols)*(H*sc+14); out.alpha_composite(crop,(X,Y+14)); d.text((X+3,Y),f'{n} {i}',fill=(255,255,255,255))
        for x in range(0,W,10):
            long=(x0+x)%50==0
            d.line([(X+x*sc,Y+14),(X+x*sc,Y+14+(14 if long else 6))],fill=(255,255,0,255))
            if long: d.text((X+x*sc+2,Y+28),str(x0+x),fill=(255,255,0,255))
    out.save(path)
if __name__=="__main__":
    items=[tuple([a.split(':')[0],int(a.split(':')[1])]) for a in sys.argv[2:]]
    ruler(items,sys.argv[1])
