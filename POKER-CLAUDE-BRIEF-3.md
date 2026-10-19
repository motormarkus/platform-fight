# Total Fist – pokerin jatkobriiffi 3 (19.10.2026, paikallinen sessio)

Jatka haarassa `codex/poker-handoff`. Lue ensin `POKER-CLAUDE-BRIEF.md`, `POKER-CLAUDE-BRIEF-2.md` ja `Assets/Scripts/Poker/README.md`. Tämä kertoo, mitä tehtiin sen jälkeen ja mitä on auki. Käyttäjä pitää tauon pokerista; palatessa aloita testaamalla alla olevat asiat pelissä.

## Työskentelyohjeet (käyttäjä)

- Vastaa lyhyesti suomeksi. Käyttäjä ajaa Unityn käännökset ja pelitestit itse (Stop ennen käännöstä, sitten uusi Play; vanha Play-sessio ei lataa uutta koodia). Ulkoasua ei saa väittää valmiiksi ilman käyttäjän kuvaa pelistä, ja Claude ei näe Unityä: kaikki alla oleva on tehty kuvien ja laskennan perusteella.
- Pushaa vain pyydettäessä. Älä koske `äänet ja cubase/*.cpr`-tiedostoihin, `output/`-hakemistoon, `Card.prefab`- ja `PokerAssets.renderTexture`-muutoksiin (käyttäjän paikalliset, eivät kuulu tähän). Älä käytä `git add .` / `-A`.
- Käyttäjä on tarkka asettelusta: mikään UI-elementti, kortti tai merkkipino ei saa olla toisen päällä eikä hahmojen käsien päällä. Tarkista aina laskemalla 1280x720-referenssikoordinaateissa ennen väitettä.
- Ei valmistajaviittauksia (ei PlayStation/Sony-symboleja tai -nimiä).

## Tehty tässä sessiossa (kaikki commitoitu, pushattu)

1. Käännösvirhe `PokerView.Raw` (`//`-kommentti nieli koodin) korjattu; IndexOutOfRange `Render`issä runoutin aikana (`Actor == -1`) korjattu.
2. **All-in-runout porrastettu**: `PokerView.StartRunoutIfNeeded` / `Runout`-korutiini paljastaa flopin, turnin ja riverin yksi kerrallaan (1,6 s alkuviive, 2,4 s väli). Sääntömoottori ratkaisee jaon edelleen heti; vain näkymä viivästyy (`BoardShown`, `runoutActive`). Pottijaon summat (pinot) näkyvät silti heti – mahdollinen "spoileri", ei korjattu.
3. **Ulkoasu**: kultareunat pois – botilohkot ja napit ovat tasaisia (punainen/sininen/tumma), valintakehys kulta 3 px. `canvas.pixelPerfect = true` tekstin sumeuteen (EI vahvistettu auttavatko; sumeus voi johtua myös Game-näkymän skaalauksesta).
4. **Voittokäden korostus** (`PokerCardWorldLayer.Show(table, boardCount, highlight)`, `PokerCards.BestFive`, `PokerCardPresenter.SetDim`, `PokerMeshCard`): näytöksessä voittajan viisi parasta korttia nostetaan (1,1x, ylös, lähemmäs kameraa), muut himmennetään (MaterialPropertyBlock `_Color`).
5. **Tapahtumahistoria** vasemmalla alhaalla (`UpdateLog`, `logText`): jaon alku, toiminnot, flop/turn/river, voittoteksti; 6 viimeistä riviä.
6. **Äänet**: `Resources/Poker/Sfx/CardDeal.wav` ja `ChipClack.wav` (tehty koodilla, käyttäjä hyväksyi); soivat 3D-korttikerroksessa. **Hahmojen repliikit** `Resources/Poker/Voice/{Yakuza,Puliukko,Motoristi,Rokkimimmi}_{Win,Lose,Fold}.mp3`: voitto ja tappio aina (näytöksen/runoutin jälkeen), luovutus 35 %:n todennäköisyydellä (`FoldVoiceChance`). Rokkimimmillä ja motoristilla vain Fold-tiedosto (käytetään myös tappioon), Yakuzalla ja Puliukolla Lose-tiedosto (käytetään myös luovutukseen). Jonotus `VoiceQueue`.
7. **Jakoanimaatio**: kortit lentävät pöydän keskeltä paikoilleen porrastetusti (0,09 s/kortti, flop 0,12 s), 0,22 s lento + korttiääni.
8. **Pöytäkuva**: ihon kiilto vähennetty (`Tools/PokerAssets/matte_skin.py`, alkuperäinen `Tools/PokerAssets/src/TableScene-original.jpg`, tulos `Resources/Poker/Artwork/TableScene.jpg`, voimakkuus oletus 0,85).
9. **Z-fight**: jokaiselle kortille oma syvyys (`cardOrder`, 0,03/kortti), jotta päällekkäiset kortit eivät vilku läpi. EI vahvistettu pelissä.
10. Pelaajapaikat ja mitat (kaikki 1280x720-referenssissä): botilohkot 140x66, oma 140x54; Yakuzan ja prätkäjätkän lohkot laskettu (SeatPos y 284/286) jotta korteille jää tilaa kätten alle; jakajan napit `DealerPos`; hole-kortit `Holes` (PokerCardWorldLayer): Yakuza (456,254), prätkä (874,254), Rokkimimmi (1180,352), Puliukko (203,395), oma (598,496) välillä 68 px ja kallistus niin että alakulmat ovat lähellä; vastustajat limittäin 28 px, kuvapuoli alas 85 %. Merkkipinot `Stacks`/`Bets`/pottipino (732,330) ja puhekuplat (Yakuza/prätkä y 192, Rokkimimmi y 286) siirretty pois korttien ja käsien päältä. Merkit 22 px, jakajan nappi 60x30.

## Auki / tarkistettavaa pelissä (käyttäjän viimeisin kuva ennen taukoa oli ennen viimeisiä muutoksia)

- Onko mikään yhä päällekkäin (kortit/merkit/lohkot/kuplat/hahmojen kädet)? Viimeisin käyttäjän kuva näytti mm. Rokkimimmin ja Yakuzan korttien paikat; käyttäjä halusi kortit "käsien alle" ja alakulmat omissa korteissa lähelle – juuri korjattu, ei vahvistettu.
- Toimiiko runout (flop/turn/river erikseen) ja voittokorostus Yakuzan/Rokkimimmin korteissa uudella koolla?
- Tapahtumahistoria: riittääkö 6 riviä, näkyykö hyvin; ääniä (repliikit, kortit, merkit) ei ole kuunneltu agentin toimesta.
- Fontti: yhä käyttäjän mielestä epätarkka (ei vahvistettu pixelPerfect-muutos).
- Nimilohkot ovat nyt tummia ilman kultaa; käyttäjä ei ole kommentoinut uutta tyyliä lopullisena.
- Edelleen auki alkuperäisistä briiffeistä: merkkien/korttien esitys ei ole "valmis", pitkän session testit (ohjain, poistuminen, scenenvaihto, uudelleenkäynnistys, rahojen säilyminen), baarikohtaiset taustat.
- Unity luo `.meta`-tiedostot uusille mp3/wav-tiedostoille; ne on commitoitu viimeisessä commitissa.

## Työkalut

- Ydintestit: `powershell -ExecutionPolicy Bypass -File Tools/Run-PokerTests.ps1` → 22/22 läpäisee (ajettu viimeksi tämän session aikana).
- Kuvaketju: `python3 Tools/PokerAssets/key_poker_artwork.py` (paneelit), `python Tools/PokerAssets/matte_skin.py [0..1]` (ihon kiilto).
