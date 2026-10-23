# Total Fist – pokerin jatkobriiffi Claudelle (19.10.2026)

Jatka toteutusta tässä projektissa, älä aloita pokeria uudelleen. Haara `codex/poker-handoff`, lähtöcommit `01c86a4` haarasta `codex/coop`. Unity 6000.3.18f1 / URP 2D. Lue myös `Temp/MaryPreview/Jatko-briiffi.txt`, jos paikallisesti saatavilla, ja `Assets/Scripts/Poker/README.md`.

## Käyttäjän työskentelyohjeet

- Vastaa lyhyesti suomeksi. Älä käytä työpöytää tai editoria etäohjauksella. Tee muutokset koodiin ja tiedostoihin.
- Käyttäjä tekee Unityn käännökset, valikkoajot ja pelitestit. Pyydä Stop ennen käännöksiä aiheuttavia tallennuksia, jos peli on käynnissä. Viimeksi käyttäjä vahvisti Stop-tilan ennen shaderikorjausta.
- Push vain pyydettäessä. Älä koske muutettuun `äänet ja cubase/beatemup-01.cpr` -tiedostoon tai `output/`-hakemistoon; ne eivät kuulu tähän haaraan.
- Baarikohtaiset pokeritaustat tehdään myöhemmin. Käytä alkuperäistä Total Fist -logoa ja projektin fontteja. Kaikki nimet, summat ja toimintatekstit ovat oikeaa UI-tekstiä.

## Tavoite ja toteutettu runko

Viiden pelaajan No-Limit Texas Hold'em -KÄTEISPELI pelin sisäisellä rahalla: ihminen, Yakuza, prätkäjätkä, puliukko ja rokkimimmi. Käyttäjän hyväksymä sisäänosto/lisäosto 50 mk, blindit 1/2 mk. Pelaajan SampleScene-aloitusraha nostettu testejä varten 2000 markkaan. Pokerin oma tallennus voi myöhemmin palauttaa viimeisen lompakon määrän tämän sijaan.

- `Assets/Scripts/Poker/Core/`: puhdas C#-sääntömoottori, käsien arviointi, panostuskierrokset, minimikorotukset, vajaat all-init / panostuksen avaaminen, sivupotit, tasapelit, palautukset, heads-up-blindit, merkkien säilymisen tarkistus, rajattu bottihavainto ja neljä pelityyliä, käteissession escrow ja atominen checkpoint.
- `PokerService.cs`: DontDestroyOnLoad, rahojen omistaja ja taustalla loppuun pelattava poistuneen all-in-pelaajan jako. Erillinen näkymän sulkeminen ja täsmälleen kerran tehtävä tilitys. Botit Task.Run:lla, tulos vartioitu session ja revision mukaan. Taistelu estetty pokerin aikana; palautuu poistuttaessa.
- `PokerView.cs`: runtime uGUI, konseptin 1280x720-asettelu, aidot tekstit, lailliset toiminnot, hiiri/ohjain. Ohjainvalinnalle viimeksi lisätty erillinen kultakehys nappikuvan PÄÄLLE; aiempi Selectable-väritys jäi kuvan alle. Käyttäjän uusin pelikuva vahvistaa valintakehyksen ja pöytäkuvan nyt näkyvän.
- `PokerCardWorldLayer.cs`, `PokerMeshCard.cs`: erillinen 3D-kamera/RenderTexture, ostetut kortti- ja merkkimallit, korttien jako/kääntö ja merkkisiirtymät. Logiikka ei odota animaatioita.
- `PokerTableEntry.cs`, `PokerTableVideo.cs`: omat E/toimintonapilla avattavat liittymispöydät; vanhaa pokerihuoneen pöytää/PokerFightia EI korvata.
- Muut olemassa olevien peliskriptien muutokset ovat pienet pokerin ohjauslukituksen tarkistukset.

## Ensisijaisesti viimeisteltävä ulkoasu

Hyväksytty konsepti on `Assets/ArtSources/Poker/approved-layout-reference.jpg`. Tyhjä pöytä neljine hahmoineen on `table-opponents-source.jpg`; paneelien atlas `ui-panels-source.jpg`. Älä suunnittele ulkoasua uudelleen.

1. Uusimmassa käyttäjän testissä pöytä ja paneelit viimein näkyvät, mutta hahmojen/pöydän sekä paneelien reunoissa oli paljon valkoista JPEG-jäämää. VIIMEISIN MUUTOS on `PokerWhiteKey.shader`: valkoisen maskin rajat .64/.94, 0.8 lähdepikselin naapuruston avulla reunan kavennus ja valkoisen reunavärin poistaminen ennen alpha-blendausta. Tätä viimeistä muutosta käyttäjä EI OLE VIELÄ vahvistanut pelissä. Tarkista, ettei se syö silmien, ihon, kullan tai hiusten kirkkaita yksityiskohtia. Älä väitä sitä valmiiksi ilman uutta näyttöä.
2. Kortit olivat peilikuvina. 53 korttimateriaaliin (52 etupuolta + selkä) asetettu `_MainTex` scale(-1,1), offset(1,0); myös valikon194 luontikoodi säilyttää korjauksen. Viimeisimmän kuvan näkyvät numerot näyttävät oikein päin, mutta tarkista kaikki suunnat/perspektiivi vielä.
3. Pelimerkit näyttävät vielä litteiltä/harmahtavilta ja pieniltä; viimeistele ostettujen mallien värit, valaistus, pinot ja mittakaava konseptin mukaisiksi. Älä generoi uutta korttigrafiikkaa.
4. Oma korttipari menee osittain oman nimipaneelin päälle. Tarkista korttien/nimien/z-järjestys, pöydän perspektiivi ja toimintopalkin sijoitus.
5. Käytössä on vanhan pokerihuoneen taustakuva väliaikaisena; siinä näkyy pieniä vanhoja hahmoja pöydän takana. Baarikohtaiset lopulliset taustat käyttäjä tuo myöhemmin.

## Ratkaistut importtivirheet – älä palauta niitä

- Useat käsin luodut TextureImporter-asetukset olivat `textureShape: 2` (= cubemap), joten Sprite/Texture2D ei latautunut. Korjattu 55 metatiedostoon `textureShape: 1` sekä editorikoodiin `TextureImporterShape.Texture2D`. Älä sekoita enumia Texture2D:n nimeen.
- Unityn puuttuva komponentti voi olla hallittu null-wrapper: `GetComponent<T>() ?? AddComponent<T>()` ei riittänyt. Pöytien luonti keskeytyi MissingComponentExceptioniin. `PokerWorldTablesSetup.EnsureComponent` käyttää Unityn `== null` -tarkistusta.
- Pöytäkuvan/paneelien asset-viitteiden suora tiedostopäivitys ei yksin näkynyt pelissä. Nyt alkuperäisistä muuttumattomat kopiot löytyvät `Assets/Resources/Poker/Artwork/{TableOpponents,Panels}.jpg` täydellisillä 2D-importtiasetuksilla. `PokerView.Build` lataa ne Resources.Loadilla, jos asetusviite puuttuu. Kaikki viisi PokerPresentationSettings-assetia on kytketty niihin. `PokerSetup.RepairArtwork` korjaa olemassa olevat asetukset valikoissa191/193/194.
- Älä lupaa valikkoajojen tarpeettomuutta tai onnistunutta ulkoasua ilman näyttöä. Useita aiempia korjausyrityksiä ei saatu heti käyttöön, mikä turhautti käyttäjää.

## Assetit ja editorivalikot

- Ostettu PokerSet löytyi Downloadsista OBJ/Blend-muodossa, EI FBX:nä. Kortit52 kpl, sininen selkä ja merkit1/2/5/10/20/25/50 on jo tuotu. `Assets/ArtSources/Poker/ImportedSet`, luodut materiaalit/prefabit `Assets/Resources/Poker/ImportedSet`.
- `Tools/PokerAssets/import_pokerset.py` poimii alkuperäisestä OBJ:sta tarpeelliset mallit/UV:t ja keventää merkkien bevel-geometriaa; manifestissa lähdehashit. Alkuperäinen blend `Lahdemallit/PokerSet/PokerSet.blend`, Downloads ennallaan. Mallin uudelleenpoiminta vaatii alkuperäisen ison OBJ:n paikallisesta Downloadsista.
- Käyttäjän valkoinen D-jakajan nappi `Assets/ArtSources/Poker/dealer-button.png`, siitä Sprite-asset; alkuperäinen PNG muuttumaton.
- Maailman liittymispöydän alkuperäinen video `Assets/ArtSources/Poker/poker-table-idle.mov`, vihreä tausta poistetaan GreenKey-shaderilla. Windows Media Foundation ilmoittaa primaarivärivaroituksesta / ensimmäisen ruudun aikaleimasta; näkyy myös käyttäjän Consolessa, ei todistettu pelivirheeksi.
- 191: valmistele testiscene ja asetukset.
- 192: lisää liittyminen valittuun OMAAN pöytään.
- 193: luo/päivitä neljä videopöytää, sitten käyttäjä Ctrl+S. Samperi'sin pokerihuone alas oikealle; El Loipparin terassi oikealle; laivan hytti; Deacon's Den alas vasemmalle. Sceneen tallennettu neljä pöytää.
- 194: kytke 52 korttia ja 3D-merkit kaikkiin asetuksiin; säilyttää peilauskorjauksen ja korjaa kuvituskytkennät.
- 195: kytke jakajan nappi (myös194 tekee tämän).
- Testikäynnistys `Assets/Scenes/PokerTest.unity`, oma demo-lompakko10000mk. Varsinainen peli `SampleScene.unity`; pöydän lähellä E / toimintonappi.

## Rahat ja testaus

Tallennus `Application.persistentDataPath/total-fist-poker.json`, demo erikseen `total-fist-poker-demo.json`; Windowsissa yleensä `C:/Users/marku/AppData/LocalLow/DefaultCompany/Total Fist/`. Älä nollaa lompakkoa tai keskeneräisen jaon escrow'ta ulkoasun korjauksessa. Projektissa ei ollut aiempaa pelirahan pysyvää tallennusta.

`Assets/Tests/Poker/PokerCoreTests.cs`: 22 automatisoitua testiä. Aiemmin 22/22 läpäissyt, sisältää käsien kickerit/ässän matalan suoran, sidepotit/jaot/palautukset, minimikorotukset/vajaan all-inin, heads-upin, poistumisen tilityksen ja checkpointin virhetilanteet; lisäksi 200 satunnaista laillista jakoa ja12 bottijakoa. Testiajo `Tools/Run-PokerTests.ps1` käyttää asennettua Monoa/csc:tä, ei käynnistä Unityä. Viimeisimpien ulkoasumuutosten jälkeen testejä ei ajettu uudelleen. Viimeisintä shaderia EI käännetty/ajettu agentin toimesta. Aiempi erillinen C#-tarkistus läpäisi ennen viimeisimpiä UI-muutoksia; nykyinen editorikäännös jää käyttäjälle.

Käyttäjä on pelannut muutamia jakoja ja liittynyt oikeasta huoneesta; se ei todista kaikkia sääntö-/tilitystapauksia toimiviksi Unityssä. Varmista myös pitkän session ohjainvalinta, lailliset napit, poistuminen tavallisesti/all-inina, scenenvaihto, sovelluksen uudelleenavaus ja rahojen säilyminen. Lue toteutus kriittisesti ennen valmistumisväitettä.

## Lisäys pushin jälkeen: pokerimusiikki
Käyttäjän poker (1).mp3 kopioitu muuttamattomana Resources/Poker/PokerMusic.mp3. RMS -17,68 dBFS, sample-huippu -3,19 dBFS, stereo 48 kHz, kesto207,56s. PokerService kutsuu MusicPlayer.PlayOverride avattaessa ja StopOverride HideView-tilassa, molemmat 1,5s ristiinhäivytyksellä ja olemassa olevalla unscaled-ajalla. Kenttämusiikki jatkuu taustalla. Ei normalisointia. Tämä lisäys on paikallinen, EI mukana commitissa dbbbef6 tai GitHubissa ennen uutta erillistä push-pyyntöä. Unityn käännös ja kuuntelutesti käyttäjällä.
