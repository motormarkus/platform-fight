# Total Fist – brief seuraavalle keskustelulle

Unity 6000.3.18f1, URP 2D beat-em-up. Repo `G:\unity pelit\mopopeli\platform fight`, haara `scene-split`, viimeisin commit `7ebe6f8e` (pushattu). Käyttäjä on suomalainen ja tiivis; hän testaa Unityssa ja raportoi. Unityä ei voi ajaa täältä, joten kaikki alla oleva on **testaamatta**.

## Ensimmäinen asia
Käyttäjä ajaa Unityssa valikon **311** (rakentaa Viidakon uudelleen ja ajaa vaiheet 319–325; vaihtoehto: 305 ja sen jälkeen 319–325). Scenen `Viidakko.unity` sijainnit päivittyvät vain näillä. Vanhat "Kaivosvaunu"-objektit poistuvat samalla. Odota hänen virheilmoituksensa tai testitulokset. Älä aloita uusia ominaisuuksia ilman hänen ohjeitaan. Virheissä apuna: `C:/Users/marku/AppData/Local/Unity/Editor/Editor.log`.

## Mitä viime jaksossa tehtiin (kaikki testaamatta)
- Kaivos: kuvat 1–2, tumma tausta, kaukainen luolaparallaksi. **Vaunuajo poistettu kokonaan** (MineCartRide.cs ja kaikki sen kuvat pois). Kaivoksen kuvan 2 oikea pää (ovi "Jatka ulos") vie suoraan alueeseen "Kaivoksen ulosmeno" – väliaikaisesti, kunnes käyttäjä keksii kaivokseen siltoja/lisää sisältöä.
- Työmaa (alue, bussipysäkki → kiitotien vasen pää, Viidakko on silmukka), verkkoaidat (pieni takana, matala etuparallaksi FrontParallax.cs), laatikot/jerryt/tynnyrit (laatikkomalli vain nimillä "Laatikko…").
- Mainari-vihollinen (MinerBrain: kuperkeikka myös juoksusta), valikko 323.
- Työmaaravintola (alakerta) + Timpan konttori (yläkerta), valikko 324; väki: Sohvi tiskin takana (jalat piilossa), kalapöydät, 5 mainaria, 3 rosvoa, 4 prätkäjätkää, rekkamies, 3 lippistä, 3 pensalenkkaria.
- Timppa (pomo, TimppaBrain + Enemy-erikoiskäsittely, valikko 325): aggressiivinen, pääpusku, heitto liukuu ruudun reunaan, heitto/pusku ei katkea, nousu korjattu; äänet normalisoitu + high shelf 2–5 kHz.
- Luurankojen korjaukset (äänet hiljaisemmiksi, ilmalento-ääni ~33 %, makuuasento ilmassa, nopeampi potku, luukasa isompi, 30 % lisää energiaa), Amatsonin heitto käyttää kaatumiskuvia (heldLying, thrownBy), Rekkamiehen heittokuvat skaalattu ×1,2.
- Kimalteenpoisto (Tools/MinerAssets/deglitter.py), läpinäkyvyys/parallaksi-läpivuodon korjaukset kuvissa.
- Valikko 326: aloitus laguunin rannalta.

## Avoimet asiat
- Läpinäkyvyys/parallaksi paistaa yhä läpi jossain? Kysy tarkka paikka, jos toistuu.
- Sankarin heitto luurankoihin on vaikea (vastaiskuikkuna vihun lyönnin aikana). Tarjottu: levennetään ikkuna luurangoille – vastausta ei saatu.
- Timpan vaikeus ja hienosäätö; Timpan heitossa ei ole oikeita nostokuvia.
- Työmaaravintolan portsarit puuttuvat. Vuoriravintolan yläkerran sisältö päättämättä.
- Ovien ja rekvisiittojen sijainnit ovat arvioita; korjataan testipalautteen mukaan.
- Poista TEST-lahjoitukset testauksen jälkeen: CaveDarkness.Awake (soihtu), AmazonBrain.Start (haarniska/otsanauha), Jerrycan.Start (tulitikut).
- Myöhemmin: Viidakon jako kolmeen sceneen; Eldoraato on valmis, kun Työmaaravintolan sisältö on kunnossa; kaivoksen sillat/lisäosat (käyttäjä keksii) – siirrä ovi "Jatka ulos" uuden osan loppuun.

## Tekniikka-muistiinpanot
- Rakennus: `Assets/Editor/BeatEmUpSetup.cs` (valikot 305, 311, 319–326). Alueiden x-välit eivät saa mennä päällekkäin aina ladatun SampleScenen kanssa (Kämppä 64001, Makkari 64500). Kaivos 66000, Vuoriravintola 67000, Ulosmeno 71000, Työmaa 72000, Työmaaravintola 73000, Konttori 75000.
- Vihollismoottori: Enemy.cs + IEnemyBrain/IPartialBrain (SkeletonBrain, MinerBrain, TimppaBrain, AmazonBrain), `isBoss`, `grabArmor`, `comboArmor`, `ArmorNow`.
- Kuvatyökalut: `Tools/MinerAssets/` (make_miner_sheets.py, make_timppa_sheets.py, process_timppa_audio.py, deglitter.py), `Tools/SkeletonAssets/`, `Tools/GymAssets/key_video.py`.
- Älä generoi kuvia itse kysymättä; käytä käyttäjän kuvia. Ei valmistajaviittauksia (ei PlayStation/Sony-symboleja). Käytä Write-työkalua skripteille (CRLF/LF-ongelmat).
- Muisti: `C:\Users\marku\.claude\projects\G--unity-pelit-mopopeli-platform-fight\memory\` (MEMORY.md indeksi).
