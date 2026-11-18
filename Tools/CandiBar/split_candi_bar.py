# Jakaa Candin baarisarjat NpcStrollerin käyttämiin sheetteihin (ruutu 640 x 768): kavely (silmukka), kaanto, selin (edestakaisin), puhe
import numpy as np
from PIL import Image
T='C:/Users/marku/AppData/Local/Temp/cb_'; OUT='Assets/Sprites/Viholliset/violetti_candi_'
CW,CH=640,768
def save(name,cells):
    cols=10; rows=(len(cells)+cols-1)//cols
    sh=np.zeros((rows*CH,cols*CW,4),np.uint8)
    for i,c in enumerate(cells): sh[(i//cols)*CH:(i//cols+1)*CH,(i%cols)*CW:(i%cols+1)*CW]=c
    Image.fromarray(sh).save(OUT+name+'.png'); print(name,len(cells),sh.shape)
K=np.load(T+'kavely.npy'); N=np.load(T+'nojaus.npy'); P=np.load(T+'puhe.npy')
save('kavely',list(K[28:44]))
save('kaanto',list(N[26:38]))
sel=list(N[38:50]); save('selin',sel+sel[-2:0:-1])
save('puhe',list(P))
