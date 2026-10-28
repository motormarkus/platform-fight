import sys
from PIL import Image, ImageDraw
import numpy as np
S='Assets/Sprites/Viholliset/'
def montage(src,n,idx,path,cols=9,W=170,H=130,cw=512,ch=448):
    im=Image.open(src+n+'.png').convert('RGBA'); c0=im.width//cw
    rows=(len(idx)+cols-1)//cols; out=Image.new('RGBA',(W*cols,(H+12)*rows),(60,70,90,255)); d=ImageDraw.Draw(out)
    for k,i in enumerate(idx):
        r,c=divmod(i,c0); f=im.crop((c*cw,r*ch,(c+1)*cw,(r+1)*ch))
        a=np.asarray(f)[...,3]>128
        if not a.any(): continue
        ys,xs=np.nonzero(a); t=ys.min(); cx=int(np.median(xs[ys<t+45]))
        crop=f.crop((cx-W//2,t-5,cx+W//2,t-5+H)); X=(k%cols)*W; Y=(k//cols)*(H+12)
        out.alpha_composite(crop,(X,Y+12)); d.text((X+3,Y),str(i),fill=(255,255,255,255))
    out.save(path)
if __name__=="__main__":
    n=sys.argv[1]; out=sys.argv[2]; cnt=int(sys.argv[3]); montage(S,n,list(range(cnt)),out)
