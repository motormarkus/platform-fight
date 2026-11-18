import numpy as np, cv2, sys
V=np.load('C:/Users/marku/AppData/Local/Temp/V2.npy')
S0,L,STEP=5,48,2
idx=list(range(S0,S0+L,STEP))   # 24 kuvaa
bg=np.array([244,17,233],np.float32)
def key(f):
    d=np.sqrt(((f.astype(np.float32)-bg)**2).sum(2))
    a=np.clip((d-40)/60,0,1)
    a=cv2.GaussianBlur(a,(0,0),0.8)
    rgb=f.astype(np.float32)
    # despill: magenta = high R,B and low G
    spill=np.clip(np.minimum(rgb[...,0],rgb[...,2])-rgb[...,1]-25,0,255)*(1-a*0.0)
    edge=(a<0.98)&(a>0)
    g=rgb[...,1]
    for c in (0,2):
        ch=rgb[...,c]; lim=g+25
        rgb[...,c]=np.where(edge|(a>0), np.minimum(ch,np.maximum(lim,np.minimum(ch,g+25))), ch)
    return np.dstack([np.clip(rgb,0,255),a*255]).astype(np.uint8)
fr=[key(V[i]) for i in idx]
bb=None
for f in fr:
    ys,xs=np.where(f[...,3]>40)
    b=(xs.min(),ys.min(),xs.max(),ys.max()); bb=b if bb is None else (min(bb[0],b[0]),min(bb[1],b[1]),max(bb[2],b[2]),max(bb[3],b[3]))
print('bbox',bb)
w,h=bb[2]-bb[0]+1,bb[3]-bb[1]+1
sc=min(565/w,291/h); print('scale',sc)
CW,CH=768,448
cells=[]
for f in fr:
    c=f[bb[1]:bb[3]+1,bb[0]:bb[2]+1].astype(np.float32); c[...,:3]*=c[...,3:4]/255
    nw,nh=int(round(w*sc)),int(round(h*sc))
    c=cv2.resize(c,(nw,nh),interpolation=cv2.INTER_AREA)
    c[...,:3]=np.where(c[...,3:4]>0,c[...,:3]/np.maximum(c[...,3:4]/255,1e-3),0)
    cell=np.zeros((CH,CW,4),np.uint8)
    x0=(CW-nw)//2; y0=443-nh+1
    cell[y0:y0+nh,x0:x0+nw]=np.clip(c,0,255).astype(np.uint8)
    cells.append(cell)
np.save('C:/Users/marku/AppData/Local/Temp/newband_cells.npy',np.array(cells))
cols=4; rows=(len(cells)+cols-1)//cols
sheet=np.zeros((rows*CH,cols*CW,4),np.uint8)
for i,c in enumerate(cells): sheet[(i//cols)*CH:(i//cols+1)*CH,(i%cols)*CW:(i%cols+1)*CW]=c
cv2.imwrite('Assets/Sprites/bandi_ranta.png',cv2.cvtColor(sheet,cv2.COLOR_RGBA2BGRA))
print(len(cells),'kuvaa',sheet.shape)
