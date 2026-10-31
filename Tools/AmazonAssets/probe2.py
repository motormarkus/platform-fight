import sys,glob,numpy as np
sys.argv=['x']; 
import importlib.util
spec=importlib.util.spec_from_file_location("m","make_amazon_sheets.py"); src=open("make_amazon_sheets.py").read().replace("\nmain()\n","\n")
ns={"__file__":"make_amazon_sheets.py"}; exec(compile(src,"m","exec"),ns)
K=ns["K"]; U="C:/Users/marku/.claude/uploads/3422f5da-b08d-4942-ae8f-cb9c1ccfe0f6/"
SC=ns["SC"]
B=[ns["rescale"](f,SC) for f in K.load(glob.glob(U+"6928097a*")[0])]; fb,ab=ns["feet"](B[0])
C=[ns["rescale"](f,SC) for f in K.load(glob.glob(U+"e3e0bb77*")[0])]; fc,ac=ns["feet"](C[0])
def ext(F,idx,ax,fl):
    xs0=[];xs1=[];ys=[]
    for i in idx:
        y,x=np.where(F[i][...,3]>128); xs0.append(x.min()-ax); xs1.append(x.max()-ax); ys.append(fl-y.min())
    return min(xs0),max(xs1),max(ys)
print("kick",ext(B,range(39,70,2),ab,fb))
for n,r in [("osuma",range(42,62,2)),("kaat",range(100,128,2)),("nousu",range(166,214,3))]: print(n,ext(C,r,ac,fc))
A=[ns["rescale"](f,1.5*SC) for f in K.load(glob.glob(U+"f2da04f3*")[0])]
for n,r in [("idle",range(0,33)),("walk",range(66,99)),("run",range(199,217))]:
    hs=[np.where(A[i][...,3]>128)[0] for i in r]; print(n,"h median",np.median([h.max()-h.min() for h in hs]))
