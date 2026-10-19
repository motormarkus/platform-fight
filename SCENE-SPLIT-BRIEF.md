# Total Fist – scenen jakamisen briiffi (19.10.2026)

Haara `codex/poker-handoff`. Unity 6000.3.18f1 / URP 2D. Tavoite: pilkkoa yksi iso `Assets/Scenes/SampleScene.unity` osiin ennen early accessia, jotta muisti ja latausaika pysyvät kurissa. Lue tämä ensin, ja lue myös `POKER-CLAUDE-BRIEF*.md` (pokerin tausta) tarvittaessa.

## Käyttäjän työskentelyohjeet

- Vastaa lyhyesti suomeksi, selkeästi ja ilman jargonia (käyttäjä ei ole ohjelmoija; edellinen vastaus "hepreaa" kun selitin scenejakoa teknisesti).
- Käyttäjä ajaa Unityn käännökset, valikot ja pelitestit itse. Pyydä Stop ennen valikkoajoja. Claude ei näe Unityä: älä väitä ulkoasua tai toimintaa valmiiksi ilman käyttäjän kuvaa pelistä.
- Push vain pyydettäessä. Älä koske `äänet ja cubase/*.cpr`-tiedostoihin, `output/`-hakemistoon, `Card.prefab`- ja `PokerAssets.renderTexture`-muutoksiin (käyttäjän paikallisia). Älä käytä `git add .` / `-A`; lisää tiedostot nimeltä.
- Editorikoodi: `Assets/Editor/BeatEmUpSetup.cs` (~14 400 riviä, CRLF-rivinvaihdot, valikot "Beat em up/NN. ..."). Kirjoita uudet kohdat tiedoston loppuun. Älä upota `\n`-merkkijonoja Pythonin kautta heredocilla; kirjoita koodi tiedostoon Write-työkalulla ja yhdistä skriptillä.
- Ei valmistajaviittauksia (ei PlayStation/Sony). Pomojen pitää olla oikeaa vastusta (Deacon vaikein).

## Lähtötilanne (mitattu 19.10.)

- `SampleScene.unity`: 14,5 Mt YAML, ~11 900 dokumenttia, ~3 900 GameObjectia (Profilerissa 5 568 pelissä), ~18 400 assettia.
- Kaikki alueet (katu, S-Club, baari, laiva, hotelli, aavikko, Thaitown, viemäri, sali ym.) ovat samassa scenessä, sijoitettuna x-akselille kauas toisistaan (X0 = 1000 … 110000). Float-tarkkuus x≈110000:ssa on ~0,008 yksikköä → mahdollista tärinää.
- Tekstuurit: arvio ~2,6 Gt pakattuna (BC7) koko projektissa (`python Tools/MemoryReportAll.py`). Unityn Profiler (Editor, Play): Textures 1890 kpl / 4,42 Gt, Graphics-muisti 3,2 Gt, koko 5,6 Gt (Editorin luvut ovat isompia kuin buildissa). Pakkaus tehty jo valikolla 199 (kaikki Sprites/Resources-sprite-tekstuurit CompressedHQ; pokerit ja valikot ohitettu). Kaikki alueet latautuvat käynnistyksessä kerralla → siksi iso.
- Suurimmat tekstuuriryhmät: Sprites/Viholliset ~700 Mt, Sprites/Taustat ~400 Mt, Tanya 112, Mary 78, Pan 71, Jumppa 71, Rekvisiitta 66, AjoPari 54, Poker 70+97.
- Aiempi arvio (1,3 Gt) oli väärä: skripti luki maxTextureSizen väärästä kentästä. Oikeat asetukset ovat `platformSettings`-lohkoissa (`DefaultTexturePlatform`, `Standalone`); `Tools/MemoryReportAll.py` lukee ne oikein.

## Rakenne, joka vaikuttaa jakoon

- `Area` (Assets/Scripts/Area.cs): kävelyrajat, kameran rajat, musiikki, ambienssi; `Area.Current`. Alueen vaihto tapahtuu `Door`-komponentilla (`here`, `target`, `spawnPoint`, `returnToLastDoor`) ja pimennyksellä. Ovet ja muut viittaavat alueisiin SUORIIN scene-viittauksiin → viittauksia ei voi tehdä scenejen välillä. Tämä on jaon pääongelma: viittaukset pitää korvata nimen/tunnisteen kautta toimivalla rekisterillä.
- Muut alueisiin sidotut: `ShopCounter.here`, `RideExit`, `GameStart.area`, `InteriorWindowParallax.area`, tappelulaskurit (`Enemy.All`, `BouncerSquad`, `BarBrawl`, `PieWar` jne.), pokerin liittymispöydät (`PokerTableEntry`).
- Pysyvät oliot (pitää säilyä scenejen välillä): pelaaja(t) (`PlayerController`, co-op `CoopManager`/`Players`), kamera (`CameraFollow`), HUD (`GameHUD`), `GameMenu`, `MusicPlayer`, `HitFx`, `PokerService` (DontDestroyOnLoad jo).
- Editorin rakennusvalikot luovat alueet scenen sisälle nimillä (`GameObject.Find("...")`, `ReuseArea`); valikko 29 "Rakenna koko katu uudestaan" ja muut olettavat yhden scenen. Uudet alueet (sali, kehä, jumppatyttö: valikot 196–198) on tehty samalla tavalla.
- Pokerin alue/pöydät: `PokerService`, `PokerTableEntry`, `PokerWorldTablesSetup`.

## Ehdotettu eteneminen

1. **Pilotti pienellä alueella** (esim. Nyrkkeilysali + Nyrkkeilykehä, valikot 196/197): siirrä ne omaan sceneen, ladataan additiivisesti kun pelaaja menee sisään, puretaan kun poistutaan. Mittaa muistin muutos (Profiler → Memory → Simple → Textures) ennen/jälkeen.
2. Tee **aluerekisteri**: `Area` rekisteröityy nimellä (statiikka); `Door` viittaa kohteeseen nimellä/tunnisteella ja lataa scenen tarvittaessa (`SceneManager.LoadSceneAsync(..., Additive)`), siirtää pelaajan `spawnPoint`iin ja purkaa edellisen. Pimennys kattaa latauksen.
3. Päätä, miten alueet sijoitetaan: jokainen alue omaan sceneensä lähelle origoa (ei enää X0-siirtoja) → ratkaisee myös float-tarkkuuden.
4. Siirrä alueet yksi kerrallaan suuruusjärjestyksessä (iso muisti ensin: baari/poker, hotelli, laiva, aavikko, Thaitown, viemäri). Älä riko aloituskohtaa (`GameStart`) tai tallennusta.
5. Päivitä editorin rakennusvalikot kirjoittamaan alueen omaan sceneen (ReuseArea / Find -pohjaisuus) tai dokumentoi, mitkä valikot vanhenevat.
6. Testaa: ovet edestakaisin, co-op, musiikin/ambienssin vaihto, tappelun aikana oven lukitukset, pokerin liittymispöydät, scenenvaihto + pokerin rahat (escrow, checkpoint). `Tools/Run-PokerTests.ps1` ajaa pokerin ydintestit (22/22).

## Mitä tämä sessio teki juuri ennen (committed ja pushattu ennen tätä briiffiä)

- Kadun alku vasemmalle (Boxing & Gym), sali (3 sisäkuvaa limitettynä), nyrkkeilykehä, ikkunaparallaksi, jumppatyttö: valikot 196, 197, 198 (BeatEmUpSetup.cs loppu); kuvat `Assets/Sprites/Taustat/sali_*`, `katu_vasen_sali.png`, `Assets/Sprites/Jumppa/`; kuvatyökalut `Tools/GymAssets/`.
- Osumapysäytys: heitetty pöytä ja tuulimylly/tornado pysäyttävät/täräyttävät vain ensimmäisestä osumasta.
- Tekstuurien BC7-pakkaus (valikko 199, ajettu) ja muistiraportit `Tools/MemoryReport*.py`.
- Vihujen takaperin kävely joukossa korjattu (Enemy.cs: erottelu ei muuta kävelysuuntaa), Sohvin tiski aukeaa vain myyjän kohdalta (ShopCounter.keeperHalfWidth), pokerin Yakuza/Prätkäjätkä-laatikot uusiin paikkoihin ja miettivän botin kolme pyöreää pistettä (PokerView.cs). Näitä ei ole vielä testattu käyttäjän kanssa pelissä.

## Auki / riskit

- Aluevaihto ja ovet ovat suoria viittauksia: iso työ, tee vaiheittain ja testaa joka vaiheen jälkeen.
- Älä lupaa muistisäästöä ilman Profilerin lukua; vertaa aina saman tilanteen mittaus (Play 20–30 s, Stop, Memory-moduulin Simple-näkymä, Textures-rivi). Tarkempi mittaus: asenna Memory Profiler -paketti tai tee Windows Development Build.
- Kuvaus alueiden riippuvuuksista (mikä viittaa mihin) puuttuu; selvitä ensin skriptillä tai koodihaulla ennen kuin siirrät mitään.

## Tilanne 19.10. (päivitys)

- Päätetty jako neljään kenttään: **Bricktown** (katu, S-Club, katto, takakuja, kadun baari, baari, kadun viemäri; jää pääsceneen pelaajan, kameran ja valikoiden kanssa), **UccoLaiva** (valtatie, Uccopulco, El Loippari, laiva, komentosilta, katto, kopterin rahtitila), **Aavikko** (aavikko … juna, aavikon viemäri), **Thaitown**. Sali+kehä on jo omassa scenessään (valikko 200, `Nyrkkeilysali.unity`).
- Kenttien väliset yhteydet: kuja → valtatie (`RideExit`), kopteri → aavikko (`Door`), juna → Thaitown (ei vielä yhteyttä, Thaitownin voi aloittaa vain `GameStart`illa).
- Koodi: `Area.Find(nimi)` (aluerekisteri), `Door.targetName/targetScene/leaveScene`, `GameStart.areaName/sceneName`. `RideExit` ei vielä tue nimeä (tehdään UccoLaiva-vaiheessa).
- Valikko 201 siirtää Thaitownin (`MoveLevelToScene`, yleinen; samaan tapaan UccoLaiva ja Aavikko). Se luokittelee juuriolioiden kentän sijainnin mukaan, keskeyttää jos viittaus ylittäisi rajan ja kirjaa alueet `Assets/Scenes/Areas/<kenttä>.areas.txt`-tiedostoon. Koordinaatteja EI siirretä lähelle origoa (skriptien kovakoodatut x-arvot rikkoutuisivat); float-tarkkuus ratkaistaan erikseen.
- Vanhat rakennusvalikot (`GameObject.Find`) eivät löydä siirrettyjä alueita ennen kuin kentän scene avataan pääscenen rinnalle (Open Scene Additive).

## Tilanne (päivitys, kaikki neljä kenttää jaettu)

- Valmiit ja testatut: valikot 200 (sali+kehä), 201 (Thaitown), 202 (UccoLaiva), 203 (Aavikko+junat), 204 (junan katon oikea reuna -> Thaitownin laituri, `Door.autoTrigger`). Bricktown jää pääsceneen. Siirtymissä vanha kenttä puretaan automaattisesti (`Area.UnloadOtherScenes`).
- Tekstuuriarvio (`python Tools/MemoryReportScenes.py`): pääscene 917 Mt (ennen 2141 Mt). Profilerin mittaus vielä tekemättä.
- Jäljellä: Profiler-mittaus; float-tarkkuus kaukana (x>78000) ratkaisematta; vanhat rakennusvalikot (`GameObject.Find`) eivät löydä siirrettyjä alueita ilman että kenttäscene avataan pääscenen rinnalle; tallennus/co-op/pokerin rahat testaamatta scenevaihdon yli.
