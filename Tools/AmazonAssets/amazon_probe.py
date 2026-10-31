import sys, glob, numpy as np
import amazon_key as K
U="C:/Users/marku/.claude/uploads/3422f5da-b08d-4942-ae8f-cb9c1ccfe0f6/"
for k,p in {"a":"f2da04f3","b":"6928097a","c":"e3e0bb77"}.items():
    fr=K.load(glob.glob(U+p+"*")[0])
    for i in ([0,100,200] if k=="a" else [0,30,50,110,130,150,236]):
        ys,xs=np.where(fr[i][...,3]>128); print(k,i,"x",xs.min(),xs.max(),"y",ys.min(),ys.max(),"h",ys.max()-ys.min())
    if k=="c":
        mn=min(np.where(f[...,3]>128)[1].min() for f in fr[100:215]); mx=max(np.where(f[...,3]>128)[1].max() for f in fr[100:215]); print("c lying x range",mn,mx)
