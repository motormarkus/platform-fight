import av, numpy as np, cv2, sys, json
from PIL import Image
UP='C:/Users/marku/.claude/uploads/1ce608b0-0f6d-41e0-b501-e782c570c2d7/'
SRC={'kavely':('54638da1-copy_B09E7D33-C5DE-4546-975C-7158A9B4C46B.mov',3),
     'nojaus':('d524f417-copy_51F555FA-51A1-4AE2-9837-E0D5B54B4CD7.mov',3),
     'puhe':('61767f76-gemini_generated_video_BECF836F.mp4',3)}
CW,CH,FEET,FIGH=640,768,751,598
OUT='Assets/Sprites/Viholliset/'
def key(f):
    rgb=f.astype(np.float32)
    # taustan väri kulmista
    bg=np.median(np.concatenate([f[:20,:20].reshape(-1,3),f[:20,-20:].reshape(-1,3)]),0).astype(np.float32)
    dom=rgb[...,1]-np.maximum(rgb[...,0],rgb[...,2]); bgdom=bg[1]-max(bg[0],bg[2])
    a=np.clip(1-(dom-0.25*bgdom)/(0.55*bgdom),0,1)
    a=cv2.GaussianBlur(a,(0,0),0.7)
    r,g,b=[rgb[...,i] for i in range(3)]
    g2=np.minimum(g,np.maximum(r,b)+6)          # vihreän vuodon poisto
    rgb=np.dstack([r,g2,b])
    return np.dstack([rgb,a*255])
res={}
for name,(fn,step) in SRC.items():
    c=av.open(UP+fn); fr=[x.to_ndarray(format='rgb24') for x in c.decode(video=0)]
    fr=fr[::step]; ks=[key(f) for f in fr]
    bbs=[]
    for k in ks:
        ys,xs=np.where(k[...,3]>60); bbs.append((xs.min(),ys.min(),xs.max(),ys.max()))
    h0=bbs[0][3]-bbs[0][1]+1; sc=FIGH/h0
    # jalkojen taso: 1. kuvan alareuna = lattia (kaikissa kuvissa sama)
    floor=bbs[0][3]; cx=(bbs[0][0]+bbs[0][2])/2
    cells=[]
    for k in ks:
        pm=k.copy(); pm[...,:3]*=pm[...,3:4]/255
        pm=cv2.resize(pm,None,fx=sc,fy=sc,interpolation=cv2.INTER_AREA)
        pm[...,:3]=np.where(pm[...,3:4]>0,pm[...,:3]/np.maximum(pm[...,3:4]/255,1e-3),0)
        cell=np.zeros((CH,CW,4),np.uint8)
        ox=int(round(CW/2-cx*sc)); oy=int(round(FEET-floor*sc))
        h,w=pm.shape[:2]
        x0,y0=max(0,ox),max(0,oy); x1,y1=min(CW,ox+w),min(CH,oy+h)
        cell[y0:y1,x0:x1]=np.clip(pm[y0-oy:y1-oy,x0-ox:x1-ox],0,255).astype(np.uint8)
        # leikkautumisvaroitus
        if cell[0,:,3].max()>30 or cell[:,0,3].max()>30 or cell[:,-1,3].max()>30: print(name,'REUNAAN OSUU')
        cells.append(cell)
    cols=10; rows=(len(cells)+cols-1)//cols
    sh=np.zeros((rows*CH,cols*CW,4),np.uint8)
    for i,cc in enumerate(cells): sh[(i//cols)*CH:(i//cols+1)*CH,(i%cols)*CW:(i%cols+1)*CW]=cc
    print(name,len(cells),'kuvaa',sh.shape,'sc',round(sc,3))
    Image.fromarray(sh).save(OUT+f'violetti_candibaari_{name}.png' if False else f'Assets/Sprites/Viholliset/candibaari_{name}.png')
    res[name]=len(cells); np.save(f'C:/Users/marku/AppData/Local/Temp/cb_{name}.npy',np.array(cells))
json.dump(res,open('Tools/CandiBar/counts.json','w'))
