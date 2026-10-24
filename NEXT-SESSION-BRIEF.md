# Total Fist – jatkobriiffi uuteen keskusteluun (päivitetty 23.10.2026)

Aloita lukemalla tämä. Taustat: `GUSTAV-TOM-BRIEF.md` (Gustav, Tom, Candi, counter), `STORY-DRAFT.md` (tarina), `THAITOWN-HOTEL-BRIEF.md`, `SCENE-SPLIT-BRIEF-2.md`. Unity 6000.3.18f1 / URP 2D. Haara `scene-split`. Viimeisin push: katso `git log` (commit "Ekakadun pelihallikohdan uusi kuva ...").

## ENSIMMÄISENÄ: etäohjaus

Käyttäjä ei ole usein kotona ja seuraa sessiota etänä (claude.ai/code tai mobiili). Asetus "Connect new sessions to Remote Control" on kytketty päälle (21.10.), joten uuden session pitäisi yhdistyä automaattisesti. **Tarkista heti alussa**: jos et ole varma, lataa `mcp__ccd_session_mgmt__set_remote_control` (ToolSearch) ja kutsu `session_id: "self", enabled: true`. Kerro käyttäjälle yhdellä rivillä, että etäohjaus on päällä. Käyttäjä ei välttämättä näe Unityä eikä konetta: älä oleta, että hän on valmis ajamaan valikkoja heti.

## Työskentelyohjeet (käyttäjältä, ennallaan)

- Vastaa lyhyesti suomeksi, selkeästi ja ilman ammattisanastoa (käyttäjä ei ole ohjelmoija). Kerro suunnitelma lyhyesti ennen kuin muutat mitään; pienissä korjauksissa voi toimia heti.
- Claude ei näe Unityä. Älä väitä ulkoasua tai toimintaa valmiiksi ilman käyttäjän testiä. Käyttäjä ajaa valikot ja pelitestit itse; pyydä Stop ennen valikkoajoja ja kehota sulkemaan muokatut scenet tallentamatta (Unity voi ylikirjoittaa levyltä muutetun scenen).
- Push ja commit vain pyydettäessä. Älä käytä `git add .` / `-A`, lisää tiedostot nimeltä. Älä koske `ääniä ja cubase/*.cpr` (aina pois commiteista); muut aiemmin "käyttäjän paikalliset" tiedostot on commitoitu käyttäjän luvalla 23.10.
- Ei valmistajaviittauksia (ei PlayStation/Sony). Ruoka vain kaupasta (+ pomojen pizza), voimakuvake harvinainen tehtäväpalkinto. Pomojen pitää olla oikeaa vastusta (Deacon vaikein).
- Kuvien teko: käyttäjä lähettää videoita/kuvia chattiin (liitteet `C:\Users\marku\.claude\uploads\<sessio>\`). Näytä katselukuvat (violetilla taustalla) ennen kuin ne menevät peliin; käytä `SendUserFile`.
- Pieneen tilaan (sisätila) hahmot saavat olla hieman isompia; ulkona hero-kokoisia.

## Tekniset kompastuskivet (tärkeä!)

- **`\\n` Bash-heredocissa muuttuu oikeaksi rivinvaihdoksi** C#-merkkijonoissa ja rikkoo käännöksen ("Newline in constant"). Tapahtui monta kertaa. Käytä sen sijaan: kirjoita Python-skripti `Write`-työkalulla tiedostoon ja aja se, tai muodosta kenoviiva `chr(92)`:lla / paikanvaraajalla (`{NL}` -> `chr(92)+'n'`). Tarkista aina `grep`illä, ettei merkkijono katkea riville.
- `BeatEmUpSetup.cs` (~16 100 riviä) ja scenet ovat osin CRLF, osin LF: tunnista rivinvaihto skriptissä (`nl = '\r\n' if '\r\n' in s else '\n'`) ja käytä `newline=''`/binäärilukua.
- Muutokset suoraan scene-YAMLiin (UccoLaiva, Aavikko, SampleScene) onnistuvat (poisto/lisäys dokumentteina, `m_Children`-listat), mutta kerro käyttäjälle sulkea scene tallentamatta. Scenejako: alueet eri scene-tiedostoissa (`Assets/Scenes/Areas/*.areas.txt` listaa alueet).
- `rm`-komennot muuttujilla estetään turvatarkistuksessa: kirjoita yli tai käytä kirjaimellisia polkuja.
- Valikkojen uudelleenajo voi tuhota/kopioida olioita: valikot 44 ym. (Uccopulcon rakennus) laskevat paikat kuvan vasemmasta reunasta (+3023 px, katso kommentti `UccoPath`-vakion yläpuolella): älä aja niitä uudelleen.

## Tehty aiemmin (21.10. ja ennen; pushattu)

Aiemmat (Gustav/Tom/Candi, mayhem, Rubyn alasväistö, tapauskansio ja Rexin kirje, Uccopulcon katu + panttilainaamo + Helvi + ulkokahvila, Chinook-ovet jne.) on kuvattu STORY-DRAFT.md:ssä ja aiemmissa commiteissa. Tämän päivän lisäykset:

- **Tarinalukot** (`Clues.cs`: `Open`, `GatesOn`, `BossDown`): Metsurin kaato antaa moottoripyörän avaimet (pyörä ei lähde ilman: `Motorbike`), Horhen kaato antaa laivalipun (ovi "Varaston ovi laivaan", `Door.requiredClue`). Valikko **229** kytkee lukot pois testaukseen.
- **Marcus** (kehän sparraus): kanveesiin lyöminen antaa Mestaruusvyön (`Clues.WinBelt`): vihujen lentomatka +15 % (`Clues.LaunchMul`).
- **Candin rytmitreeni** (`CandiTraining.cs`, valikko **230**): Candi treenipaikalle (x 110034), E aloittaa, isku (J) ja potku (K) nuottien mukaan, 21 tahtia; 16+ pistettä -> Candin treeniohjelma (kuva `Resources/Kirjeet/candi_treeniohjelma.png`, staminan palautuminen +10 %). Kuvat `Sprites/Jumppa/candi_lyonti_1/2.png`.
- **Salsafest** (ranta Uccopulcon kujan takana, UccoLaiva-scene, alue "Salsafest" x 24000): kuva `Taustat/salsafest.png` (3 kuvaa yhdistetty, läpinäkyvä taivas; Tools/make_salsafest.py), kulku kujalta x 3893 px (Uccopulcon katukuvan 3. ruutu, kujaan lisätty hiekkaa), meri parallaksina. Valikot: **232** alue ja ovet, **236** meri (parallaksi, Uccopulcon merikuva), **233** S-Clubin bändi lavalle + S-Clubin musiikki, **234** Maria ja Sohvi tanssivat (kuvat `Sprites/Maria`, `Sprites/Sohvi/sohvi_tanssi_*`), **231** Helvin tanssi lavan edessä, **235** Tom, Puliukko ja Samoalainen rauhassa (`SalsaBrawl.cs`: tappelu alkaa, kun hero lyö jotakuta tai kun alueella on oltu 2 min ja Puliukko vittuilee; sitten kaikki vastaan kaikkia), **237** kaksi rokkimimmiä tanssi-idlellä (`Sprites/Rokkimimmi`). Ajojärjestys uudessa projektissa: 232 -> 236 -> 233 -> 231 -> 234 -> 235 -> 237.
- **Helvin tanssi** (`HelviDance.cs`): kaulakoru (panttilainaamosta 250 mk, Clue `esine_Kaulakoru`) -> E antaa korun -> paritanssi (Rocco+Helvi, `tanssi_pari_1-4`, vain Rocco): 15 tahtia iskunapilla; 7+ osumaa suudelma, 12+ nosto syliin (huipennus, soitetaan lopuksi takaperin), palkintona vihje Horhesta (+100 mk täydellisestä).
- **Rokkimimmin kuvat**: kaatumis-, ylösnousu- ja niskalenkkisarjojen (`rokkari2_*`) maassa/lennossa olevat ruudut kasvatettu (`Tools/rescale_frames.py`, ruutukohtaiset kertoimet), koska video oli kutistanut hahmon; käyttäjä: "näyttää legit". Alkuperäiset varmuuskopiossa (työkansion scratchpad).
- Tytöt (Helvi, Maria, Sohvi) ja Rokkimimmi: ääriviivoja paksunnettu (`Tools/thicken_outline.py`), tyttöjen koko Salsafestissä 1,12.
- Työkalut: `Tools/HelviAssets` (Helvin ja parin tanssit), `Tools/RokkiAssets`, `Tools/SohviAssets/make_sohvi_dance.py`, `Tools/GymAssets/make_candi_lyonti.py`.

## Tehty 22.–23.10.2026 (commitit `d7228e78` ... `6ac7435e`, pushattu; käyttäjä testaa Unityssä)

Kaikki alla oleva on käännetty Roslynilla (0 virhettä) mutta **testaamatta Unityssä**, ellei toisin mainita.

- **Pensalenkkari** (valikot 240, 241): uusi vihu (jab, suora, potku, pyörähdyspotku, torjunta, lippishamays, esineiden heitto). Äänet haetaan ajon aikana `Resources/Sfx/pensalenkkari/` tiedostonimien mukaan (ei scenessä). Puhuu rauhallisenakin, kun on ruudulla; äänenvoimakkuus -20 %. `Enemy`: `talksWhenCalm`, `oneSpeakerPerKind` (vain yksi samaa lajia puhuu kerrallaan, `IsKindSpeaker`/`NoteSpeech`), `TryRetort` (Puliukon `speak2` satunnaisesti toisen perään), `SpeakByName`.
- **Salsafest**: kojut ja myyjät (238, 239, 242: Mama ruoka, Pablo cocktail; `LoadSellerFrames`, `Seller(...)`), yömeri sinisävytyksellä (243), Rubyn ja Pablon rytmitanssi suudelmaan (`RubyPabloDance.cs`), puliukko-rosvot ja seilorit.
- **Valtatie** (245, `HighwayEvents.cs`): rahalinja, saarto, hiekkamyrsky. Testipyörä poistettu kadulta (244).
- **Thaitownin katu** (246, 247): katu jatkuu asemalta vasemmalle (kerrostalo, klubi, panttikonttori + sisätila, ulkokahvila). Koko katu skaalattu +25 % ja nostettu (`ThaiStreetScale`, `ThaiStreetRaisePx`); kuvat katkaistu osiin (`Tools/LenkkariAssets/split_street.py` -> `thaitown_katu*_yla/_kav/_ala.png`). `StreetPieceLeft(k)` + `StreetOverlapPx = {0,22,102,186,0}` limittävät kuvat (22 px lisättiin asemaa vasemmalle: kuvan reunassa oli läpinäkyvää); osat leveämmiksi 4 px (`seamGrow`) ettei rakoa jää. Helvi panttikonttorin myyjänä, ulkokahvila + Sohvi. Rullalauta: panttikonttorissa ostettu lauta ilmestyy kadulle (`ShopCounter.boardDoor`, `pendingBoard`, `SpawnPendingBoard`).
- **Thaitownin klubi** (248–252, x 112000; panttikonttorin sisätila x 118000: yli 131072 x:llä tuli tärinää): sisätila 3 kuvaa (sisään oikealta, kulku vasemmalle), `ThaiClubScale 1.1`, pöydät (`ClubTableSpots`), baaritiski + Sohvi, bändi (249, `Tools/LenkkariAssets/make_bandi.py`, edestakainen silmukka), tankotanssijat Liz ja Candi (251, `make_liz.py`, `fill_holes.py`, `Dancer2` + `sizeMul`), väki (250: 2 Tanya, 2 Jakuza, 2 Punkkari, 2 Pensalenkkari, Rekkamies, Tom, 2 Rokkimimmi, 2 Prätkäjätkä, Betty, koviksia) + rähinä 2 min kuluttua (`SalsaBrawl`), 6 portsaria (3/puoli) rähinän alkaessa (`BouncerSquad`), 2 Puliukkoa, wok-koju bändin viereen (252). Sprite-pivot: käytä `EnsureBottomPivotSprite` (pelkkä BottomCenter-asetus antoi Center-pivotin); taustan kokoiset kuvat ppu `941/(2*CamHalf)`, hero-kokoiset ppu 100.
- **Rähinälogiikka** (`Enemy.cs`: `brawlGroup`, `fightsEveryone`, `FightWeight`, `TargetScore`, `UpdateEnemyTarget`): pienet vihut hakeutuvat ensisijaisesti isompien kimppuun; Punkkari+Kovis ja Tanya+Jakuza liittolaisia; Pensalenkkari ja Puliukko tappelevat kaikkia vastaan; Tom, Rekkamies, Betty paljon vahvempia ja hyökkäävät kaikkien päälle. `SalsaBrawl.finalLine = "speak3"` (Puliukko ennen rähinää).
- **Tomin heitot ja iskut** (`TomBrain.cs`): heitetyt vihut käyttävät omia heittokuviaan (`BeginHeldByPlayer`, `SetHeldByPlayer(..., pose)`; ilman heittokuvia pyörivät kuten Roccon heitossa); Tomin iskusta vihuun kuuluu hiljainen osumaääni (`HitFx.OnHitQuiet`). Rekkamiehen/Bettyn heittokuvat voivat puuttua: tarkista pelissä.
- **Äänitasaus** (`AudioLevels.cs`, `Resources/AudioLevels.json`, `Tools/AudioLevels/measure_audio.py` + `tasaa_aanet.bat`): ajon aikainen vahvistustaulu (avain `nimi|kesto`); kaikki `PlayOneShot(` -kutsut -> `PlayOneShotLeveled(` (ei pokerin skripteissä).
- **Muuta**: Mama/Pablo koot (Mama 1.1, Pablo 1.25), Roccon nousu-ppu (`LoadDrunkArt` 115), Roccon juoksuframen valkoinen läiskä korjattu (`juoksu.png`, alkuperäinen `ArtSources/Hahmot/juoksu_alkuperainen.png`), Candi/Liz reikien täyttö.
- **Pot Limit Omaha** El Loipparin pokeripöytään (`Poker/`): E pöydän luona -> valinta Hold'em / Omaha (vasen/oikea, E aloittaa, hyppy peruu). `PokerTable.Variant`, 4 korttia, täsmälleen 2 omaa + 3 pöydän korttia (`PokerCards.EvaluateOmaha`), pot limit -korotus (`Legal`: `CurrentBet + Pot + maksettava`), botit osaavat (`OmahaPreflopStrength`, equity Omaha-säännöillä). Asetus `offerOmaha` + `omahaBuyIn` (100 mk) kirjoitettu `Table-ElLoippari.asset`iin. Testit (25 kpl) menevät läpi `Tools/Run-PokerTests.ps1`:llä. Rokkimimmin neljän kortin viuhkaa siirretty 75 px vasemmalle (arvio, tarkista). Botit jätettiin tarkoituksella ennalleen (Puliukko = löysä, Yakuza = varovainen, Prätkäjätkä = aggressiivinen, Rokkimimmi = tasapainoinen).
- **Ekakadun pelihallikohta** (`katu_jatko.png`): kauppa + pelihalli korvattu uudella kuvalla (alkuperäinen `Assets/ArtSources/Katu/katu_kauppa_pelihalli.jpg`, sovitus x 2658, mittakaava 0.77, y -57; pelihallin ja palotikkaiden paikat ennallaan). Vasen sauma on pilarin reunassa (kova, 14 px), reunan alapuolinen katu sulautettu leveästi. Graffitiluukku (nostoovi) poistui. Sovituskoodi: scratchpadin `compose_jatko.py` (ei repossa); vanha kuva löytyy gitistä.

## Tekniset huomiot (uusia)

- **Float-tarkkuus**: pidä uudet sisätilat x < 131072 (klubi 112000, panttikonttori 118000).
- **`Area.IsHere`** tuhoaa `LooseBoard`-oliot, jotka ovat muissa alueissa (siksi lauta ilmestyy vasta alueelle tullessa).
- **Python-muokkaukset**: lue/kirjoita binäärinä ja tunnista `nl`; hiljaisia epäonnistumisia sattui. `assert count == 1` jokaisessa korvauksessa. Älä kirjoita ei-ASCII-merkkejä Bashin heredocista Pythoniin (merkistö hajosi); käytä `Write`-työkalua skriptille.
- **Roslyn-tarkistus** ennen luovutusta: `csc.dll` polussa `C:/Program Files/Unity/Hub/Editor/6000.3.18f1/Editor/Data/DotNetSdkRoslyn` + `NetCoreRuntime/dotnet.exe`, vastausTiedostot `rt.rsp` (runtime) ja `ed.rsp` + `ms.rsp` (editor) temp-kansiossa (generoi uudelleen Unityn `Library/Bee`-vastaustiedostoista, jos puuttuvat). Poker-ydin käännetään erikseen (`Library/ScriptAssemblies/TotalFist.Poker.Core.dll` on vanha).
- **Commit**: nyt myös aiemmin "käyttäjän paikalliset" tiedostot (`Card.prefab`, `PokerAssets.renderTexture`, briiffit, `Tools/PokerAssets/src`, `output/`, `piirakka_*.meta`) on commitoitu käyttäjän luvalla. Vain `ääniä ja cubase/*.cpr` jää aina pois.

## Odottaa käyttäjän testiä / ajoa (tärkein osa)

**Valikkojen ajojärjestys** (sulje Thaitown ja UccoLaiva tallentamatta ensin; Stop): **246 -> 247 -> 248 -> 250 -> 249 -> 251 -> 252** (Thaitown, tarvitaan limitysten 22 px takia, kaikki paikat lasketaan uudelleen) sekä **242** (Salsafestin myyjät). Tomin heitot/äänet ja pokeri eivät tarvitse valikkoa.

Testaamatta: katujen saumat (aseman vasen puoli, kaikki 4–5 saumaa; klubin vasemmat tolpat), klubin koot (Rocco liian nopea?, bändi/tanssijat/tiski), rähinä ja portsarit, Puliukon repliikit, wok-koju, Pensalenkkarin puhe ja äänet, rullalaudan osto -> ilmestyy kadulle, Tomin heitot (Rekkamies/Betty) ja osumaäänet, äänitasaus, Mama/Pablo koot, Roccon kännin nousukuvat, PLO (valinta, 4 kortin asettelu 3D-korteilla, pot limit -rajat, Rokkimimmin viuhkan paikka), uusi pelihallikohta (pääseekö sisään, sauma pilarissa).

## Seuraavaksi (käyttäjä päättää)

1. Testauksen löydökset (yllä); sauma/koko/paikkakorjaukset ovat tyypillisesti pieniä.
2. Pokeri: bottien teho (Puliukko ei korottanut lähes nutseilla; käyttäjä halusi jättää ennalleen toistaiseksi), mahdollinen PLO-erillinen sisäänosto/blindit.
3. Thaitownin sisältö: klubin musiikki (käyttäjä lisää pian), ulkokahvilan ja klubin sivujuonet, hotelli (`THAITOWN-HOTEL-BRIEF.md`).
4. Salsafest ja Prätkäjätkät, tarinan jatko (STORY-DRAFT.md), esineiden käyttö, katujen viimeistely, vanhat avoimet (Gustavin junakohtaus, Roccon A+B-iskut, optimoinnit, kaksinpelin ja pokerin testi scenevaihdon yli).

## Pelihalli ja Tähtipyssy (23.10.2026, ajamatta Unityssä; käännetty Roslynilla 0 virhettä)

- **Pelihallin sisätila** (valikko **253**, SampleScene): `Assets/Sprites/Taustat/pelihalli_sisa.png` (vasen pääty + kanttiinipääty yhdistetty, `Tools/ArcadeAssets/stitch_pelihalli.py`; lähteet `output/imagegen/arcade-wide-left.png` ja `arcade-wide-right-v2.png`). Alue "Pelihalli" x 56000, neon-ovi kadulla (katu_jatko x 4150 px), vasen ovi ulos, kanttiinin tiski + Sohvi. Halli tyhjä: ei pelikoneita, ei väkeä. Sauma kuvissa näkyy toistuvana neonina/ilmastointina: tarkista pelissä.
- **Tähtipyssy** (avaruusammuntapeli minipelinä): `Assets/Scripts/StarShooter.cs` (koko peli, äänet syntetisoidaan koodilla), `ArcadeMachine.cs` (E koneen luona), `Assets/Editor/StarShooterImport.cs` (tekstuuriasetukset). Kuvat `Assets/Resources/TahtiPyssy/` (tehty `Tools/SpaceGame/slice_sprites.py`:llä lähteistä `Tools/SpaceGame/src/`). Valikko **254** asettaa koneen pelihallin seinälle (aja 253 ensin). Ohjaus: liike, J ampuu (pohjassa), L erikoissäde (mittari täynnä), Esc/Q poistuu. 4 asetasoa, kilpi, 3 vihollista + asteroidit, pomo "Kurittaja" (3 vaihetta, pulssikiekot, tulipallot, liekkisäde), paras tulos PlayerPrefs `tahtipyssy_hi`. Muutokset: `CoopInput.PunchHeld`, `GameMenu.Update` ja `GameHUD.OnGUI` ohittavat kun `StarShooter.Active`.
- Testaamatta: kaikki. Tarkista erityisesti kameran/renderöinnin (oma kamera y 6000), äänet, vaikeus, pomon asemat, otsikkoruutu, poistuminen (pelaajat palaavat). Puuttuu: oikea konekaappi (kone on nyt otsikkokuva kehyksessä), musiikki, neon rally -kone (toinen työ, `Assets/Sprites/Arcade/`).
- **Koneet rikkoutuvat (23.10.)**: valikko **254** tekee Turpaan Vaan, Turboralli ja Flipper Flapper -koneet `Crate`-olioina (kuvat `Assets/Sprites/Pelihalli/kone_<nimi>_0-5.png`, `Tools/ArcadeAssets/slice_machine_break.py`; 3 iskua, sitten hajoaminen, romukasa), koskemattomia (`Crate.immune`/`anchored`) kunnes hallin tappelu alkaa. Valikko **255**: hallin väki (3 Kovista, Rekkamies, Tom, punkkarit, Jakuzat, Tanyat ym.) + 6 portsaria + `SalsaBrawl` (tappelu 2 min kuluttua tai kun joku lyö); `ArcadeBrawlArm` vapauttaa koneet. Kovis, Prätkäjätkä ja Rekkamies saavat `throwsTables` (käyttävät omia nosto/heittokuviaan; Kovisin ja Prätkäjätkän koneenheitto on testaamatta), Tom nostaa Crate-esineitä berserkissä. Ajojärjestys: 253, 254, 255 (255 avaa Thaitownin, UccoLaivan ja Aavikon hetkeksi lukemaan malleja).

## Sini pelihallissa (valikko 256)

- Kuvat: `Assets/Resources/Sini/<sarja>_NN.png` (146 kpl; idle, talk, walk, toCounter, counter, kiss, dance). Leikattu käyttäjän videoista: `Tools/SiniAssets/extract.py` (vihreä tausta pois, PyAV) ja `make_sprites.py` (valinta, tiskin poisto `counter_clean.py`, kankaat bottom-center + 24 px lattiavara, hahmon korkeus 360 px). Videoita ei ole repossa (`~/.claude/uploads`).
- Koodi: `Assets/Scripts/ArcadeSini.cs`. Tilat: idle, kävely, tiski (toCounter -> counter -> takaisin), tanssi, juttelu hallin Enemy-hahmon kanssa, E-juttelu heron kanssa, lentosuukko, väistö tappelun alkaessa. Ei taistelija eikä lyötävissä.
- E-juttelu: kolme kierrosta, kolme vastausta (hyvä +35, ok +12, huono -18; mieliala alkaa 20, läpi >= 70). Läpi: lentosuukko + tanssi, 150 s hyvällä tuulella (E antaa uuden suukon 40 s välein). Ei läpi: 25 s jäähy. Hero lukitaan (pc.enabled=false) jutun ajaksi. Ohjaus: ylös/alas, E/Punch vastaa, K poistuu.
- Valikko 256 asettaa kaistan, tiskipaikat ja tappelun (SalsaBrawl.area == Pelihalli). Aja 253 -> 254 -> 255 -> 256.
- Tuntematonta: tiskin poisto jätti muutaman pikkureiän/jäänteen shortsien vyötärölle tiskikuvissa; koko (Height 3.0), nopeus (1.4) ja kävelyn jalkaliukuma tarkistamatta pelissä; Resources/Sini-tuonti pakkauksella (StarShooterImport) ajamatta.
- Toinen hahmo halliin: tulossa (käyttäjä lähettää).

## Dina pelihallissa (valikko 257)

- Sama koodi kuin Sinillä (`ArcadeSini.cs`, `persona = Dina`, `folder = "Dina"`), kuvat `Assets/Resources/Dina/` (idle, walk, talk, dance, toCounter, counter; ei lentosuukkoa). Dina ja Sini voivat jutella keskenään (toinen kuuntelee, `friend`-kenttä asetetaan valikoissa 256/257: aja molemmat).
- Kuvaputki: `Tools/SiniAssets/dina_extract.py` (taustan väri vaihtuu videoissa vihreästä keltaiseen, joten sävy+reunatäyttö), `dina_counter.py` (tiskivideossa on oikea tiski+ikkuna taustalla: hahmo erotetaan väreillä ja rajataan verhoon; epätäydellinen: pieniä jäänteitä jalkojen välissä ja lantiolla), `make_dina.py`.
- E-juttelu: kertoo Tähtipyssy-ennätyksensä (`dinaRecord`, nyt 30000, ARVAUS: pelin pisteitä ei ole testattu). Kun pelaajan paras (`tahtipyssy_hi`) ylittää sen, hän palkitsee kerran (`dina_record_claimed`): `rewardMoney` (0) ja staattinen tapahtuma `ArcadeSini.RecordBeaten`. Palkinto on käyttäjällä vielä päättämättä.
- Tiskipaikat jaetaan: Sini ja Dina eivät mene samaan kohtaan.

## Tähtipyssy: kenttä 2 ja pienemmät hahmot

- Hahmot, viholliset, ammukset ja pickupit 20 % pienemmiksi (`Sz = 0.8` StarShooter.cs; osumalaatikot skaalattu; pomot, räjähdykset ja tausta eivät muutu).
- Kurittajan kaaduttua alkaa kenttä 2 (`NextStage`): +1 elämä (max 5), kilpi, bonuspisteet, tähtitausta sinertää, viholliset sävytetään, uusi käsikirjoitus `Script2` (~124 s) ja uudet viholliset: drone (kind 4, kääntyy pelaajaa kohti) ja miina (kind 5, räjähtää 8 luodin renkaaksi). Voitto vasta kentän 2 pomon jälkeen.
- Pomo 2 TUHOAJA (950 hp, `UpdateBoss2`): viuhka isoja luoteja, pulssikiekot, droonit, pyyhkivä luotisuihku (vaihe 2+), liekkisäde (vaihe 2+) ja vaiheessa 3 syöksyrynnäkkö (varoitusviiva -> syöksy ruudun yli). Kuvat `Assets/Resources/TahtiPyssy/boss2_0..2.png` (leikattu `Tools/SpaceGame/slice_boss2.py`:llä, lähde `Tools/SpaceGame/src/boss2_sheet.webp`); leveys pelissä 5,6 yks., osumalaatikko 2,5 x 1,2, aseet/säde `bossMuz`-etäisyydeltä. Kentän 2 tausta `stars2.png` häivytetään ristiin (ks. StartStarCrossfade).
- Ei kokeiltu Unityssä; vaikeus/aikataulut arvioita.
- Tuhoajan ammukset (käyttäjän kuvat, `Tools/SpaceGame/slice_boss2_shots.py`, lähteet `src/boss2_shots/`): `e2s` tulipallot (virta), `e2b` salamanuolet (viuhka), `e2arc` kasvava kaari (EnemyShot-tyyppi 4, skaalaa 0,5 -> 1,9 kahdessa sekunnissa, vaihe 2+), `e2ball` iso hidas plasmapallo (tyyppi 5, vaiheessa 3 kaksi), `e2beam` vihreä liekkisäde (värjätty Kurittajan säteestä).

## Turpaan Vaan: pikselikarate (23.10.2026)
- Pelikoneen "Turpaan Vaan" E-näppäin käynnistää `KarateGame` (Assets/Scripts/KarateGame.cs): Rocco/Gustav, 6 vastustajaa, 3 kierroksen ottelut. Ohjaus J isku, K potku, L erikoispotku, I torjunta, Esc poistuu.
- Kuvat: `python Tools/KarateAssets/make_karate.py` -> Assets/Resources/Karate (pikselöity pelin omista kuvista; valmiina).
- Unityssä: odota kuvien tuonti, aja valikko **259** (liittää pelin Turpaan Vaan -koneeseen), tallenna scene. KÄÄNNETTY MUTTA AJAMATTA: tasapaino ja tuntuma testaamatta.

## Flipper Flapper (23.10.2026)
- `FlipperGame.cs`: oma fysiikka (pallo, janat, pomppijat, 2 mailaa, linkoparit, 3 maalitaulua, 3 ohitusvaloa, kerroin, pallonsäästö). Vasen maila nuoli vasen/A/J, oikea nuoli oikea/D/K, laukaisu välilyönti pohjassa+vapautus, Esc poistuu. Hi-score `flipper_hi`.
- Kuvat valinnaisia: Assets/Resources/Flipper/{pohja,pallo,maila,pompp_0,pompp_1,pompp_2}.png (puuttuessa piirretään koodilla). Seinät, linkoparit ja maalitaulut piirretään aina koodilla.
- Unityssä: valikko **260** liittää pelin "Kone: Flipper Flapper" -koneeseen (254 tekee saman uudelleenajettaessa). Käännetty, geometria simuloitu Pythonilla (pallo pääsee kaistalta pöytään), ajamatta Unityssä.

## Pelihalli: Chasedr1ft ja Daccle (23.10.2026)
- Kaksi uutta ei-pelattavaa konetta (kuvat Tools/ArcadeAssets/src, viipaloitu slice_machine_break.py -> Assets/Sprites/Pelihalli/kone_chasedrift_*, kone_daccle_*). Kaikki koneet kestävät nyt 10 iskua; kuusi konetta tasavälein (380, 670, 960, 1250, 1540, Tähtipyssy 1830).
- Unityssä: odota kuvien tuonti, aja 254 (rakentaa koneet + liittää Turpaan Vaan- ja Flipper-pelit) ja 255 (väki), tallenna scene.

## Uudet osumaäänet (23.10.2026)
- Resources/Sfx/Hit/*.wav (Tools/AudioLevels/make_hit_sounds.py, lähde Lataukset): hit_punch_a/b, hit_kick, hit_swoosh_a/b, voice_small_a/b, voice_big_a/b. Alun hiljaisuus leikattu, mono, taso tasattu vanhoihin osumiin.
- HitFx: pelaajan lyönti/potku käyttää uusia osumaääniä (HitFx.OnHit(heavy, kick)), ohi menevä isku soittaa huitaisuäänen (PlayWhiff, kerran per isku), maahan iskeytyminen käyttää vanhoja. Sävelkorkeus vaihtelee joka kerralla.
- Enemy.ApplyGenericVoice: jos vihulla ei ole omia iskuääniä, se saa yleisen iskuäänen (pienet/isot, sävelkorkeus vaihtelee reilusti). Poikkeukset: naiset ja pomot joilla omat äänet. Punkkarin omien iskuäänten todennäköisyys nostettu. Testaamatta Unityssä.
- KORJAUS (23.10.2026): pelaajan osumaäänet (lyönti/potku/huitaisu) palautettiin vanhoiksi, uudet hit_*.wav poistettu. Jäljellä vain vihujen yleiset iskuäänet (Resources/Sfx/Hit/voice_*, Enemy.ApplyGenericVoice) ja punkkarin iskuäänten tiheämpi soitto.
- Viuhahdukset (hitviuh1-5.wav -> Resources/Sfx/Hit/viuh_1-5.wav, rms nostettu 0,07): HitFx.PlayWhoosh soi PlayerController.Enter:ssä joka kerta kun isku/potku alkaa (osui tai ei), ei sävelkorkeuden vaihtelua, ei samaa kahdesti peräkkäin. Testaamatta.

## Sinin ja Dinan kämppä (24.10.2026)
- Kuvat Assets/Sprites/Taustat/kampa_olohuone.png + kampa_keittio.png, yhdistetty kampa.png:ksi (sauma sulautettu). Valikko **Total Fist / Luo kämppä pelihallin yläkertaan** (= Beat em up/261): luo alueen "Kämppä" (x 64000), oven hallin oikeaan reunaan (px 3425, lukossa kunnes Clues "sini_kutsu"), paluuoven (kämpän vasen ovi) ja Sinin ja Dinan kotiin (ArcadeSini.home = true).
- Kutsu: kun Sinin piristys hallissa onnistuu, Clues.Add("sini_kutsu") + Sini sanoo kutsun. Kämpässä Sini (E): kahvikohtaus sivuina (ArcadeSini.BuildCoffeePages), ensimmäinen kerta täysi lepo (Heal + stamina), myöhemmin +40. Dina: ennätyspuhe kuten hallissa.
- Repliikit: Tools/SiniAssets/kahvikohtaus_repliikit.md ja sini_repliikit_halli.md (käyttäjä äänittää; äänien kytkentä koodiin kesken).
- Poistumisovi hallista (vasen) korjattu: ArcadeHallFix.cs (ajaa itsensä).
- Testaamatta Unityssä; hallin oikean oven paikka (px 3425) on arvio kuvasta.

## Kämpän yökaupunki (ikkunoiden parallax)
- `Tools/FlatAssets/make_night_city.py` tekee kolme kerrosta (`kampa_yo_taivas/kaukana/lahella.png`, Assets/Sprites/Taustat) ja leikkaa olohuoneen ikkunalasit läpinäkyviksi `kampa.png`:hen (alkuperäinen `Tools/FlatAssets/orig/kampa.png`).
- `BeatEmUpSetup.CreateFlat` (valikko Total Fist → kämppä) luo kerrokset taustan taakse (`InteriorWindowParallax`, tekijät 0,97 / 0,93 / 0,86). Ajettava uudelleen, jotta kerrokset tulevat sceneen.
- `kampa_makuuhuone.png` (läpinäkyvät ikkunat) on valmiina, mutta makuuhuonetta ei ole vielä rakennettu; sen kamera ei liiku, joten parallax olisi staattinen.

## Kämpän makuuhuonekohtaus (suudelmavideo)
- Valinta "Täältä mää tuun." Sinin puhepuussa (ArcadeSini.cs, CoffeeTree) himmentää ruudun ja toistaa `Assets/StreamingAssets/Kampa/suudelma.mp4` (VideoPlayer → RenderTexture, OnGUI). E ohittaa 1,5 s jälkeen.
- Video tehty `Tools/FlatAssets/make_kiss_video.py <lähde.mov>`: vihreä tausta avattu, tilalle sumennettu makuuhuone + yökaupunki, häivytykset, H.264 + AAC. Lähde oli lähikuva (toinen lähetetyistä; koko vartalon versio jäi käyttämättä: harmaa tausta ja generaattorin roskaa kuvassa).
- Jos videota ei ole, käytetään `Resources/Kampa/suudelma.png` -kuvaa tai pelkkää mustaa ruutua.
- Odottaa: "löhikuvaversio" (käyttäjä lähettää myöhemmin).

### Päivitys: makuuhuonekohtaus = kolme videota peräkkäin
- `Assets/StreamingAssets/Kampa/suudelma_1..3.mp4`: 1 koko vartalo, 2 lähikuva, 3 tanssi + suutelu. Taustat leikattu pois (`Tools/FlatAssets/make_kiss_video.py`: floodFill reunoilta, roskaikonit pois) ja pari on sumennetun makuuhuoneen (yökaupunki ikkunoissa) edessä; tausta himmenee videon 2 alussa.
- Mikä tahansa nappi (E/lyönti/potku/Start) lopettaa kesken ja vie suoraan pelihalliin (`Door.GoNow`, kämpän ovi `returnToLastDoor`). Kahvi (lepo) annetaan lopussa.
- Vanha suudelma.mp4 poistettu.

### Makkari ja kohtauksen päätös
- Videot 1 ja 3 (koko vartalo) pieninä sänkyjen välissä terävää makuuhuonetta vasten, video 2 (lähikuva) täysikokoisena sumean huoneen edessä; video 3 pyörii edestakaisin kunnes musiikki (Resources/Music/rocco_ja_sini) loppuu tai pelaaja keskeyttää.
- Kohtauksen jälkeen peli jatkuu makkarista (Alue: Makkari, `kampa_makuuhuone.png`, yökaupunki ikkunassa). Ovet: "Olohuoneesta makkariin" (olohuoneen harmaa ovi, px 1935) ja "Makkarista olohuoneeseen" (makkarin vasen reuna; ovea ei ole piirretty kuvaan). Sisältö odottaa käyttäjän ideoita.
- Kämpän koko FlatZoom 0,78 (BeatEmUpSetup); Dina ei ole kämpässä; Sini kävelee hallin ovelle sanottuaan "seuraa mua" (S.ToDoor/AtDoor).
