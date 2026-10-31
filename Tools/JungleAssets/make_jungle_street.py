"""Viidakon 'katu' hangaarin jälkeen: neljä generaattorin kuvaa (1672 x 941, alfa) -> Assets/Sprites/Taustat/viidakko_katu_1..4.png.
Järjestys vasemmalta oikealle: 1 tyhjä bambumaja (vasemmassa reunassa hangaarin oikea pääty), 2 tyhjä maa ja kasvit, 3 kanikonttori, 4 ravintola, 5 ravintolan jatko (viimeisenä).
Alfa 240-254 -> 255 ja reunavärien korjaus kuten make_jungle_assets.py:ssä.
Ajo: python Tools/JungleAssets/make_jungle_street.py <maja> <tyhjä> <kanikonttori> <ravintola> [<ravintolan jatko>]"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import make_jungle_assets as J
for i, p in enumerate(sys.argv[1:6], 1):
    J.save("viidakko_katu_%d.png" % i, J.fit(J.bleed(J.solidify(J.rd(p)))))
    print("viidakko_katu_%d.png" % i)
