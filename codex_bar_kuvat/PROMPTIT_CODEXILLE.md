# Aavikkobaari: uudet taustakuvat (promptit Codexille)

Pelissä baarin sisätila on kolme vierekkäistä 16:9-kuvaa (1672 x 941), jotka liitetään panoraamaksi (~1155 px ja ~1275 px limitys).
Nykyiset kuvat: `baari_1_ovi_vasen.png`, `baari_2_tiski_keski.png`, `baari_3_lava_oikea.png`.
`baari_panoraama_mitat.png` näyttää mittakaavan: hahmon korkeus lattiaviivalla on 266 px (3,4 yksikköä), tiskin pinta on nyt rivillä 385 ja etureuna rivillä 660.

## Mikä on pielessä
1. **Tiski on liian korkea** hahmoihin nähden (etupaneeli ~265 px = hahmon koko pituus). Pitäisi olla vyötärön korkeudella.
2. **Seiniä ei ole sivuilla**: kuva vain loppuu. Kamera loitontuu nyt ja näkee reunan yli.
3. **Lattiaa on liian vähän alhaalla**: kamera näyttää nyt n. 2,4 yksikköä (≈190 px) kuvan alareunan alapuolelle; sinne on laitettu peilattua lattiaa.

## Yhteiset vaatimukset (kaikki kolme kuvaa)
- Sama tyyli, valaistus, väripaletti ja pikselitaide kuin nykyisissä (tumma puu, lämpimät lamput, messinkiset yksityiskohdat).
- **Korkeus 1130 px** (leveys 1672 px): lisää n. 190 px **lisää lattiaa alareunaan** (jatka samaa lautalattiaa, ei peilausta, ei esineitä lattialla).
- Taustan seinälinjan (seinän ja lattian rajan) pitää pysyä samalla korkeudella kuin nyt (n. rivi 525 kuvissa 1 ja 3), jotta pelin kävelyalueen rajat pysyvät. Tiskin kohdalla tiskin etureuna pysyy rivillä n. 650.
- Saumojen pitää osua: kuvan reunat saavat olla täsmälleen samat kuin nykyisissä (vasemman kuvan oikea osa ja keskikuvan vasen osa ovat samaa seinää; sama keskikuvan oikea reuna ja oikean kuvan vasen reuna: portaat).
- Ei hahmoja eikä pöytiä (ne lisätään pelissä).

## Kuva 1 (vasen, sisäänkäynti)
Pohja: `baari_1_ovi_vasen.png`.
```
Edit this pixel-art saloon interior background (keep the exact style, lighting and the position/size of the double doors, the wall lamps, the speaker, the red leather corner sofa and the first part of the bar on the right edge).
Changes:
1. Make the canvas 1672x1130: extend the wooden plank floor downward by 190 px with the same plank pattern and lighting (no objects).
2. Add a clear LEFT WALL at the far left edge: the back wall turns 90 degrees toward the viewer, shown as a side wall with dark wooden wainscoting and a wall lamp, with the floor/wall corner clearly visible, so the room has a visible boundary on the left (the sofa stays against the back wall).
3. Lower the bar counter visible at the right edge so that the top of the counter is at about waist height of a character that is 266 px tall (counter top ~ 140 px above the floor line; counter front panel ~140 px high, not ~265 px). Keep the brass foot rail. Keep the shelf unit behind it.
Do not add people. Keep the floor line (wall/floor boundary) at row 525.
```

## Kuva 2 (keski, tiski)
Pohja: `baari_2_tiski_keski.png`.
```
Edit this pixel-art saloon interior background (keep style, shelves with bottles, three pendant lamps, left speaker/pillar and the staircase on the right).
Changes:
1. Make the canvas 1672x1130: extend the plank floor downward by 190 px, same pattern, no objects.
2. LOWER THE BAR COUNTER: currently the front panel is ~265 px tall (as tall as a character). Redraw it so the counter top is about 140 px above the floor line and the front panel is ~140 px high (waist height for a 266 px tall character), keep the curved wooden panels, brass foot rail and the bar's left and right ends. Keep the back bar shelf unit but let it sit lower so the bottles on the shelves stay visible above the counter (bartender will be drawn behind the counter in the game, shoulders-up, so leave a free band about 150 px high between the counter top and the lowest shelf).
3. Keep the staircase on the right with its position and size (the foot of the stairs at row ~600, x 1360-1650), only extend the floor in front.
Keep the wall/floor boundary at row 525 on the left and the stair foot at row 600. No people.
```

## Kuva 3 (oikea, lava)
Pohja: `baari_3_lava_oikea.png`.
```
Edit this pixel-art saloon interior background (keep the stage with red curtains and four spotlights, the speakers, the wall lamps, the staircase on the left, and the diagonal wooden wall on the right).
Changes:
1. Make the canvas 1672x1130: extend the plank floor downward by 190 px, same pattern, no objects.
2. Add a clear RIGHT END WALL: the diagonal wall on the right edge continues down to the floor and the room ends with a visible corner (wall + dark wainscoting + a wall lamp), so there is a visible boundary on the right.
3. The staircase foot and the bar counter end on the left edge stay where they are; lower the counter end at the left edge to the new lower counter height (counter top ~140 px above floor line).
Keep the stage front edge at row ~520. No people.
```

## Tiedostot takaisin
Kun kuvat on tehty, laita ne projektiin nimillä `baari_1_ovi_vasen_uusi.png`, `baari_2_tiski_keski_uusi.png`, `baari_3_lava_oikea_uusi.png` ja kerro minulle: teen niistä uuden panoraaman, päivitän takarajat, ovet, portaat, lavan ja bändin paikat ja lasken kameran uudelleen.
