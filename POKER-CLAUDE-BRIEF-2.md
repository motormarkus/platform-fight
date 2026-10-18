# Total Fist – pokerin jatkobriiffi 2 (18.10.2026, pilvisession → paikallinen)

Jatka paikallisesti haarassa `codex/poker-handoff` (viimeisin pilvessä tehty commit on pushattu). Lue ensin `POKER-CLAUDE-BRIEF.md` (alkuperäinen) ja `Assets/Scripts/Poker/README.md`. Tämä tiedosto kertoo, mitä sen jälkeen tehtiin ja mitä on auki.

## Työskentelyohjeet (käyttäjä)

- Vastaa lyhyesti suomeksi. Käyttäjä ajaa Unityn käännökset ja pelitestit itse (Stop ennen käännöstä). Älä väitä ulkoasua valmiiksi ilman käyttäjän kuvaa pelistä.
- Älä koske `äänet ja cubase/*.cpr`-tiedostoihin eikä `output/`-hakemistoon. Älä käytä `git add .` / `-A`.
- Pilvisessä sessiossa käyttäjä antoi luvan pushata jokainen muutos. Paikallisessa sessiossa kysy tai noudata käyttäjän ohjetta.
- Käyttäjällä on paikallisia, committaamattomia muutoksia (pokerimusiikki `PokerService.cs` + `Resources/Poker/PokerMusic.mp3`, `PokerAssets.renderTexture`, `Card.prefab`, brief). `git pull` valittaa `PokerService.cs`:stä → `git stash push -- Assets/Scripts/Poker/PokerService.cs`, `git pull`, `git stash pop`. Ehdotettu: committaa musiikki pysyvästi.
- Pilvi-Claude ei näe Unityä: kaikki alla oleva ulkoasu on tehty "sokkona" kuvien ja koodin perusteella. Ainoa ajettu todennus: Core-testit (mcs + nunit, 22/22) ja bottisimulaatio.

## Tehty tässä sessiossa (commitit haarassa)

1. **Taustan poisto kuvaan valmiiksi** (ei shaderilla): `Tools/PokerAssets/key_poker_artwork.py` (flood fill reunoilta + isot suljetut aukot, harmaan/valkoisen JPEG-halon syönti, 2x Lanczos jos leveys < 1900). Tulos `Assets/Resources/Poker/Artwork/PanelsKeyed.png`. Vanha `PokerWhiteKey.shader` jäi varapolulle.
2. **Uusi pöytäkuva**: käyttäjän tarkempi 3840x2160 laivabaarikuva → `Assets/Resources/Poker/Artwork/TableScene.jpg` (+ meta, mipmapit, max 4096). `PokerView.Build` piirtää sen koko ruudulle 1280x720-referenssiin; baari ja pöytä ovat samassa kuvassa. Jos kuvaa ei löydy, vanha polku (barBackground + TableOpponentsKeyed) on varalla – sen paikat eivät enää täsmää.
3. **Pöydän litistys poistettu** (ennen 1280x655, nyt 1280x720).
4. **Paneeliatlas** `Assets/ArtSources/Poker/ui-panels-hires.png` (2000x667, kuvakaappauksesta, hieman pakattu) → `PanelsKeyed.png`. `PokerView.Raw` skaalaa atlaksen pikselikoordinaatit `texture.width/1280` ja `texture.height/426`. Alkuperäinen tiedosto käyttäjällä: `output/imagegen/poker-button-frames-redrawn.png` → korvaa `ui-panels-hires.png`, aja skripti uudelleen.
5. **Pelipaikat kiertävät pöydän ympäri myötäpäivään**: Sinä(0), Puliukko(1), Yakuza(2), Prätkäjätkä(3), Rokkimimmi(4). Jakajalla Sinä: SB Puliukko, BB Yakuza, ensin puhuu Prätkäjätkä. Tyylit: Puliukko löysä, Yakuza varovainen, Prätkäjätkä aggressiivinen, Rokkimimmi tasapainoinen. Nimet `PokerCashSession.Join`, tyylit `PokerService.Styles`.
6. **Botin tekoäly** (`Core/PokerBot.cs`): Monte Carlo -equity (64), preflop-aloituskäsien vahvuus (Chen-tyylinen), asema, pot odds, korotusten huomiointi, bluffit vain kun ei vastustusta, vaihteleva panoskoko, all-in vain vahvoilla. Havaintoon lisätty `Street`, `Position`, `Aggressors` (vain julkista tietoa). Simulaatio 1500 jakoa: käytös järkevä, laillisia siirtoja, merkit säilyvät.
7. **Pelin tahti ja selkeys**: botti miettii 1,6–3,2 s (näkyvässä pöydässä), toiminnon jälkeen 1,4 s tauko; puhekupla botin paneelin yläpuolella ("Miettii…" / "Sökö", "Korotti 6", …); kultainen sykkivä hehku vuorossa olevan hahmon ympärillä (runtime-generoitu säteittäinen tekstuuri, `GlowCenter`); SB/BB-merkit paneelin vasemmassa yläkulmassa; jakajan nappi paneelin yläpuolella (`DealerPos`); voittoteksti yhdistää potit ("Sinä voitit 14 mk – Pari"), näytetään vasemmalla alhaalla (110,554).
8. **Oletusvalinta**: omalla vuorolla Sökö/Maksa, jaon välissä Jaa seuraava jako; Poistu pöydästä ei koskaan oletus; botin vuorolla ei valintaa (`SelectFirst`).
9. **Merkit ja kortit**: Blenderin MTL-värit olivat lineaarisia mutta Unityn `_Color` on gamma → kaikki `Chip-*.mat` muunnettu sRGB:ksi (myös `PokerSetSetup.cs` käyttää `.gamma`). `PokerAssetModel.shader`: kirkkaus/kontrasti, saturaatio 1.15, valkoinen kermanväriseksi. Merkit 30 px (ennen 24). Korttikerros piirtyy nyt oman `RenderTexture`n (2560x1440, MSAA 4) – ei assetin 1920x1080/AA1. Oma korttipari pienempi (68 px) ja nimipaneelin yläpuolella.
10. **Jakajan napin leikkautuminen**: `dealer-button.png.meta` `nPOTScale 1 → 0` (NPOT skaalaus rikkoi käsin tehdyn Sprite-rectin).
11. Sääntömoottorin voittoteksti `PokerTable.WinMessage()`.

## Tunnetut ongelmat / auki (käyttäjän palaute)

- **Uusi asettelu on arvattu mock-kuvan mukaan, ei nähty pelissä.** Tarkista Unityssä: paneelit (`SeatPos`), pelaajien kortit (`HolePos`/`Holes`), merkkipinot (`Stacks`/`Bets` PokerCardWorldLayerissä), jakajan nappi, hehkun paikat (`GlowCenter`), puhekuplat (paneelin yläpuolella y−66), voittoteksti. Kaikki koordinaatit ovat 1280x720-referenssissä; Holes/Stacks/Bets ovat korttikerroksessa keskipisteitä.
- **Kortit ja merkit näyttävät käyttäjän mielestä edelleen huonoilta** ("paskalta", merkit harmaita ja epäselviä). Värikorjaus ja suurennus tehty, mutta varjoja, oikeita merkkipinoja (pystysuora pino, ei hajallaan olevia merkkejä), korttien varjoa ja mallien asentoa (merkit nyt `Euler(42,0,-8)`, arvattu) ei ole tehty. Ostettu paketti on yksinkertainen vektorityyli (ei kuvakortteja); käyttäjä kysyi paremmasta paketista → suositus: ensin esitystapa kuntoon, sitten harkitse maalattua pakettia.
- **Rokkimimmin silmä / pupilli "neliö"**: lähdekuvassa silmä on vain ~7 px – uusi 3840x2160-kuva ratkaisee todennäköisesti. Varmista pelissä.
- **Yleisilme epätarkka/pikselöitynyt**: uusi kuva + 2560 RT + MSAA auttavat; tarkista. Seuraava askel jos ei riitä: tarkempi paneeliatlas alkuperäisenä, mipmap-/filteriasetukset, pelin Game-näkymän skaalaus.
- **Jakajan nappi "ei siirry"**: sääntömoottori kiertää oikein (simulaatio), nappi piirretään paikalle `DealerPos[Dealer]`; aiempi piilossa olo johtui korttien/merkkien päällekkäisyydestä ja pienestä koosta. Tarkista uudessa asettelussa.
- Lisäosto-nappi pidetään (käyttäjä: "anna olla").
- Vanhat `TurnOutline*.png` poistettu (eivät sovi uuteen kuvaan); `make_turn_outlines.py` jäi työkaluksi mutta ei käytössä. Vanhat `TableOpponents*.jpg/png` jäivät (varapolku).
- Baarikohtaiset pöytäkuvat: tulevat myöhemmin; suunnitelma: `TableScene` + paikat asetukseen (PokerPresentationSettings) – nyt kovakoodattu Resources-nimi ja koordinaatit.
- Windows Media Foundation -värivaroitus liittymispöydän videosta: ei virhe.

## Työkalut ja testit

- Windows: `Tools/Run-PokerTests.ps1` (Unityn Mono). Pilvessä ajettu: `mcs -out:T.exe -r:nunit.framework.dll Assets/Scripts/Poker/Core/*.cs Assets/Tests/Poker/PokerCoreTests.cs Tools/PokerTests/Runner.cs && mono T.exe` → 22/22. Lisää testejä tarvittaessa botille/voittotekstille (WinMessage).
- Kuvaketju: `python3 Tools/PokerAssets/key_poker_artwork.py` (paneelit). Ympäristömuuttuja `POKER_ART` ohjaa kohdekansion testeihin.
- Poker-demo: `Assets/Scenes/PokerTest.unity` (10 000 mk demo-lompakko). Varsinainen: SampleScene, E pöydän luona.

## Ehdotettu järjestys

1. Pull + Unity-käännös + kuva pelistä (oma vuoro, botin vuoro, flop, showdown). Korjaa asettelu sen perusteella.
2. Merkkien/korttien esitys kuntoon (varjot, pinot, asennot) – tai päätös uudesta paketista.
3. Tarkempi paneeliatlas alkuperäisenä; mahdollisesti tarkemmat napit/kehykset.
4. Pitkän session testit: ohjain, poistuminen (tavallinen + all-in), scenenvaihto, uudelleenkäynnistys, rahan säilyminen.
5. Pokerin musiikki: käyttäjän muutos `PokerService.cs` (`pokerMusicPlaying`) – varmista, että pysäytys tehdään poistuttaessa (näkemättä loppuosaa diffistä).
