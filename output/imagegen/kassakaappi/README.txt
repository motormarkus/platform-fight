KASSAKAAPPI

Kuusi 512 x 512 PNG-ruutua, aidosti lapinakyva tausta.
00 = kiinni; 01-04 = avautumisvaiheet; 05 = auki.
Kuvat: Assets/Sprites/Rekvisiitta/Kassakaappi/
Alkuperainen 1536 x 1024 kuvasarja: output/imagegen/kassakaappi/kassakaappi_kuvasarja_3x2.png
Jarjestys kuvasarjassa: vasemmalta oikealle, ensin ylarivi ja sitten alarivi.

Unity-asetukset on tallennettu .meta-tiedostoihin:
Sprite (2D and UI), Single, Full Rect, 200 pixels per unit, Bilinear,
ei mipmappeja, ei pakkausta, alpha transparency, sama custom pivot
(0.61328125, 0.078125) kaikissa ruuduissa. Pivot on kaapin rungon
keskella lattian tasolla, joten ovi voi liikkua vasemmalle.

Animaatiota varten valitse PNG-kuvat jarjestyksessa 00-05 ja veda ne
Scene-ikkunaan. Sopiva aloitusnopeus on 10 ruutua sekunnissa; poista
Loop Time, jotta ovi jaa auki viimeiseen ruutuun.

Menetelma: sisainen image_gen. Alkuperainen alpha on sailytetty;
kuvasarja on ainoastaan leikattu kuudeksi tasakokoiseksi ruuduksi.
Kehote: imagegen-kehote.txt. Tekniset tiedot: manifest.json.