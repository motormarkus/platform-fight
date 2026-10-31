import sys, cv2, numpy as np, glob
sys.path.insert(0,'.'); import amazon_key as K
U="C:/Users/marku/.claude/uploads/3422f5da-b08d-4942-ae8f-cb9c1ccfe0f6/"
S=sys.argv[1]
for k,p in {"a":"f2da04f3","b":"6928097a","c":"e3e0bb77"}.items():
    fr=K.load(glob.glob(U+p+"*")[0]); print(k,len(fr))
    h,w=fr[0].shape[:2]
    # vaakarajaus: koko hahmon x-alue
    allx=[]; 
    for f in fr:
        ys,xs=np.where(f[...,3]>128); allx+= [xs.min(),xs.max()]
    x0,x1=max(0,min(allx)-10),min(w,max(allx)+10); print(k,"x",x0,x1)
    step=4 if k!="b" else 3
    tiles=[]
    for i in range(0,len(fr),step):
        t=fr[i][:,x0:x1].astype(np.float32); bg=np.full(t.shape[:3][:2]+(3,),90,np.float32)
        al=t[...,3:4]/255; im=(t[...,:3]*al+bg*(1-al)).astype(np.uint8)[...,::-1]
        im=cv2.resize(im,(im.shape[1]//3,im.shape[0]//3)); cv2.putText(im,str(i),(4,18),0,0.6,(255,255,255),2); tiles.append(im)
    cols=10
    while len(tiles)%cols: tiles.append(np.zeros_like(tiles[0]))
    rows=[np.hstack(tiles[r:r+cols]) for r in range(0,len(tiles),cols)]
    for j in range(0,len(rows),3): cv2.imwrite(f"{S}/key_{k}_{j//3}.jpg",np.vstack(rows[j:j+3]))
