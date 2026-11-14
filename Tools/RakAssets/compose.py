from PIL import Image
S='src/'
def L(n): return Image.open(S+n+'.png').convert('RGBA')
L26,M25,R24=L('maa_vasen_paa'),L('maa_keski'),L('maa_oikea_paa')
U23,U21,U22=L('ylakerta_vasen_paa'),L('ylakerta_keski'),L('ylakerta_oikea_paa')
# ground row: left end (shift down 42), 2x mid, right end
W=2000
def row(pieces,H):
    im=Image.new('RGBA',(W*len(pieces),H),(0,0,0,0))
    for i,(p,dy) in enumerate(pieces): im.alpha_composite(p,(i*W,dy))
    return im
g=row([(L26,42),(M25,0),(R24,0)],720)
g.save('maa_rivi.png')
u21=U21.resize((2000,int(798*2000/1971)))
u=row([(U23,0),(u21,0),(U22,0)],max(720,u21.size[1]))
u.save('ylakerta_rivi.png')
print(g.size,u.size)
t=Image.new('RGBA',(g.size[0],g.size[1]+u.size[1]),(30,40,70,255))
t.alpha_composite(u,(0,0)); t.alpha_composite(g,(0,u.size[1]))
t.resize((t.size[0]//4,t.size[1]//4)).save('preview.png')
