# Total Fist – Thaitownin hotelli, jatkobriiffi (20.10.2026)

Aloita lukemalla tämä. Taustat: `SCENE-SPLIT-BRIEF-2.md` (scenejako). Unity 6000.3.18f1 / URP 2D. Haara `scene-split`, viimeisin push aed9f89.

## Työskentelyohjeet (käyttäjältä)

- Vastaa lyhyesti suomeksi, selkeästi ja ilman ammattisanastoa (käyttäjä ei ole ohjelmoija). Kerro suunnitelma lyhyesti ennen kuin muutat mitään.
- Claude ei näe Unityä. Älä väitä ulkoasua tai toimintaa valmiiksi ilman käyttäjän testiä. Käyttäjä ajaa valikot, käännökset ja pelitestit itse; pyydä Stop ennen valikkoajoja. Käyttäjä voi liittää kuvakaappauksia.
- Push ja commit vain pyydettäessä. Älä käytä `git add .` / `-A`, lisää tiedostot nimeltä. Älä koske `ääniä ja cubase/*.cpr`, `output/`, `Card.prefab`, `PokerAssets.renderTexture`, `POKER-CLAUDE-BRIEF.md` (käyttäjän paikallisia).
- Editorikoodi: `Assets/Editor/BeatEmUpSetup.cs` (~15 300 riviä). Uudet kohdat tiedoston loppuun (tiedoston rivinvaihto on nyt LF; tarkista aina ennen lisäystä). Kirjoita koodi Write-työkalulla scratchpadiin ja lisää Pythonilla; heredocit, joissa on erikoismerkkejä, voivat kaatua. Tiedostokirjoitus voi epäonnistua hetkellisesti, kun Unity pitää tiedostoa (yritä uudelleen).
- Kuvat tulevat käyttäjältä keskusteluun (1672 x 941); alkuperäiset on tallennettu `Assets/ArtSources/Hotel/` (läpinäkyvät alueet säilyvät alfassa).
- Ei valmistajaviittauksia. Ruoka vain kaupasta (+ pomojen pizza), voimakuvake harvinainen tehtäväpalkinto. Pomojen pitää olla oikeaa vastusta (Deacon vaikein).
- Float-tarkkuus: Thaitownin x ≈ 109000–134000, tarkkuus 0,0078. Hitaat askeleet pyöristyivät nollaksi (puliukko liikkui vain pystysuunnassa); `Enemy.Move` kantaa nyt x-pyöristysjäännöksen (`xCarry`). Sama voi iskeä muuhun hitaaseen liikkeeseen (muisti: float-precision-large-x).

## Mitä on tehty ja testattu (käyttäjä testasi 20.10.: "oli hyvät")

Kaikki alueet ovat `Assets/Scenes/Areas/Thaitown.unity`-scenessä (valikot avaavat sen additiivisesti, tallentavat sen ja nimet lisätään `Thaitown.areas.txt`:hen). Alueiden musiikki = hotellin aulan musiikki.

| Valikko | Mitä | Koordinaatit (x0) |
|---|---|---|
| 177 (vanha) | Hotellin aula 5 kuvaa, ravintola, baari, lava | 109000 |
| 208 | Aulan hissi (E) → hissin sisätila (`ElevatorRide`, ~2,2 s matka) → kattohuoneistokäytävä; käytävän hissi vie takaisin alas | hissi 125000, käytävä 126000 |
| 209 | Sviitti (2 peräkkäistä kuvaa, ei limitystä); käytävän keskimmäinen ovi 1202 vie sinne, sviitin sisäänkäynti takaisin | 128000 |
| 210 | Katto: ovi keskellä, molemmin puolin leveä kuva (ilmastointi, helikopterikenttä, kulma), vasen puoli peilikuva; käytävän päätyovet vievät katolle, katon ovi takaisin samaan päähän (`returnToLastDoor`) | 130000 |
| 211 | Rocco: kombo jab → takasuora → etukoukku → takakäden sivukoukku → etukäden sivukoukku (kaataa); alas + suojaus = alasväistö (pidettynä jää kyykkyyn), väistössä lyönti = nopea takakäden alakoukku, potku = polviisku (kaataa), pusku-nappi = pusku | – |

Käytävä: 5 kuvaa [peilattu pääty, peilattu ovet, hissi, ovet 1201–1203, pääty], limitys 48 px (`PenthouseOverlapPx`), päädyissä ikkunat (läpinäkyvä alfa + yötaivas + kaupungin parallaksi). Huoneovet 1201 ja 1203 sekä peilatun puolen ovet ovat vielä pelkkiä kuvia. Katon taivas on läpinäkyvä, taakse yötaivas + parallaksi.

Muuta samalla jaksolla (testattu): Marcus-sparraus kehässä (valikko 205/206/207; `MarcusBoxer.cs`, YOU LOSE, kanveesiin jääminen, varjonyrkkeily salin kehässä), kenttäsiirtymien tekstuurinvapautus.

Tiedostot:
- Kuvat: `Assets/Sprites/Taustat/thaitown_hissi_sisa.png`, `thaitown_kattokayt_1..5.png`, `thaitown_sviitti_1..2.png`, `thaitown_katto_paa_1..3.png`. Lähteet `Assets/ArtSources/Hotel/hotelli_*.png`.
- Työkalut: `Tools/HotelAssets/make_penthouse_images.py`, `make_suite_images.py`, `make_roof_images.py` (laaja kattokuva oli vain 2000 x 667, suurennettu 1,41 ×; parempi alkuperäinen → `hotelli_katto_laaja.png` ja aja skripti uudelleen). Roccon kuvat: `Tools/RoccoAssets/make_rocco_sheets.py`.
- Koodi: `Assets/Scripts/ElevatorRide.cs` (suunta päätellään `Door.LastUsed`:sta), `Door` (autoTrigger, returnToLastDoor), `PlayerController` (Duck-tila, `quickUppercut`, `Ducking`), `Enemy` (`xCarry`, `sparring`, `DebugInfo`).

## Seuraavaksi: sviitin, katon ja käytävän täyttö

Käyttäjä aloittaa täyttämisen. Ehdotettu järjestys, kysy ensin mitä halutaan:
1. **Sviitti:** huonekalut/esineet (sänky, sohva, pöytä; hajoavat esineet ja ruoka vain sääntöjen mukaan), mahdolliset vihut tai NPC, mahdollinen levon/tallennuksen paikka, ääni (huoneen taustahäly).
2. **Katto:** vihut (helikopterikentän ympärillä), mahdollinen pomo tai tapahtuma (helikopteri), kauppa/esineet, katon taustahäly ja tuuli. Katon kulmat ovat kuvan reunoissa; kulmassa lattian takaraja laskee (`depthLimits`).
3. **Käytävä:** huoneovet 1201/1203 ja peilipuolen ovet (lukittuja? omia huoneita?), vartijat tai vihut, esineet; hissin ovi voisi avautua animaationa.
4. Käytävän ja kuvien sauman tarkistus pelissä; kuvien laadun parannus, jos käyttäjällä on alkuperäiset.
5. Vanhat avoimet: Rocco A+B (nopea jab-takasuora-jab), C ja F zipin ruudut on jätetty käyttämättä; Marcusin iskujen arvot (koukun osumahetki, väistön ajoitus) ovat arvauksia; hotellin Puliukko-vihut taistelevat kaikkia vastaan (`fightsEveryone`, tarkoituksella).
6. Edelliseltä jaksolta: optimoinnit (Resources-kuvat), kaksinpelin, pokerin rahojen ja tallennuksen testaus scenevaihdon yli.

## Pikavihjeet

- Alueen nimi hakuun: `Area.Find("Thaitown sviitti")`, `"Thaitown kattohuoneisto"`, `"Thaitown hotellin hissi"`, `"Thaitown katto"`.
- Uusi ovi: `Door` (here, target, prompt, spawnPoint, halfWidth, maxDistanceFromWall; ruudun reunalle autoTrigger + onGround). Esimerkit valikoissa 208–210.
- Uudet kuvat: `ImportShipSprite(path, ppu, false, SpriteAlignment.Center)`, ppu = 941 / (2 × CamHalf); kuvat limitetään 48 px:llä, ellei ole peräkkäisiä leikkeitä (sviitti).
