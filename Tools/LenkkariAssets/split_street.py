# Kadun kuvat kolmeen osaan: rakennus (rivit 0-622, suurennetaan 25 %), jalkakäytävä (622-668, venytetään myös pystyyn) ja katu (668-941, vain vaakaan).
from PIL import Image
T = "Assets/Sprites/Taustat/"
CUT, CURB = 622, 668
names = ["thaitown_katu%d" % i for i in range(1, 8)] + ["thaitown_katu_vasen_%d" % i for i in range(1, 5)]
for n in names:
    im = Image.open(T + n + ".png").convert("RGBA"); w, h = im.size
    im.crop((0, 0, w, CUT)).save(T + n + "_yla.png")
    im.crop((0, CUT, w, CURB)).save(T + n + "_kav.png")
    im.crop((0, CURB, w, h)).save(T + n + "_ala.png")
print("valmis", len(names))
