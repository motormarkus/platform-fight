import sys, cv2, numpy as np
import amazon_key as K
fr=K.load(sys.argv[1]); S=sys.argv[2]; print(len(fr), fr[0].shape)
allx=[]
for f in fr:
    ys,xs=np.where(f[...,3]>128); allx+=[xs.min(),xs.max()]
x0,x1=max(0,min(allx)-10),min(fr[0].shape[1],max(allx)+10); print("x",x0,x1)
step=int(sys.argv[3]); tiles=[]
for i in range(0,len(fr),step):
    t=fr[i][:,x0:x1].astype(np.float32); al=t[...,3:4]/255
    im=(t[...,:3]*al+90*(1-al)).astype(np.uint8)[...,::-1]; im=cv2.resize(im,(im.shape[1]//4,im.shape[0]//4)); cv2.putText(im,str(i),(4,18),0,0.6,(255,255,255),2); tiles.append(im)
cols=10
while len(tiles)%cols: tiles.append(np.zeros_like(tiles[0]))
rows=[np.hstack(tiles[r:r+cols]) for r in range(0,len(tiles),cols)]
for j in range(0,len(rows),3): cv2.imwrite(f"{S}/v2_{j//3}.jpg",np.vstack(rows[j:j+3]))
