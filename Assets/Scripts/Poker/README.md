# Total Fist – Texas Hold'em

Unity 6000.3.18f1, Input System, uGUI. Yksi ihminen ja neljä paikallista bottia.
Käteispeli markoilla; ei oikeaa rahaa, anteja, turnaustasoja tai rakea.

## Testipelin käynnistys

1. Stop. Avaa `Assets/Scenes/PokerTest.unity`, odota Unityn käännös ja paina Play itse.
2. Pöytä avautuu 10 000 markan **erillisellä testilompakolla**, sisäänosto 50 mk, blindit 1/2 mk. Myös lisäosto on 50 mk.
3. Valitse **Jaa seuraava jako**. Seuraava jako alkaa vain tästä napista; jakojen välissä voi tehdä lisäoston.
4. Hiiri toimii kaikkiin nappeihin ja korotusliukuriin. Näppäimistö: vasen/oikea valitsee, ylös/alas muuttaa summaa, Enter/Space vahvistaa, Esc poistuu.
5. P1:n ohjain (`CoopInput.PadFor(0)`): risti/tatti valitsee ja säätää, A vahvistaa, B poistuu. Pelihahmon E/toimintanappi avaa kentän pöydän.

Valikko **191** korjaa puuttuvat oletusasetukset/testiscenen tarvittaessa. Se ei muuta nykyistä kenttää tai avaa Play-tilaa.
Pelattavalle pokerille tehdään **oma erillinen pöytä**. Valitse sen objekti Hierarchysta ja aja valikko **192**, joka lisää siihen `PokerTableEntry`-komponentin. Valitse komponentin `Here`-kenttään pöydän alue, säädä liittymisetäisyys ja tallenna Ctrl+S. Pöydän ulkoasu ja paikka tehdään erikseen; valikko lisää vain liittymistoiminnon.
Nykyistä `SampleScene.unity`-tiedostoa ei ole muutettu tämän toteutuksen mukana.
Vanhan pokerihuoneen pöytään tai sen tappelutapahtumaan ei kytketä pelattavaa pokeria. Valikko 192 ei sijoita uusia objekteja eikä muuta `PokerFight`- tai `SpriteLoop`-komponentteja.

## Omat assetit ja hyväksytty asettelu

`Assets/Resources/Poker/DefaultPoker.asset` sisältää kaiken vaihdettavan esitysmateriaalin.
Kopioi asetusassetti toista baaria varten ja vaihda `barBackground`. Samat säännöt, pöytä ja UI säilyvät.

- `Assets/ArtSources/Poker/approved-layout-reference.jpg` on hyväksytty asettelumalli, ei pelissä piirretty kuva.
- `table-opponents-source.jpg` ja `ui-panels-source.jpg` ovat käyttäjän muuttamattomat lähdekuvat.
  `WhiteKey.mat` / `PokerWhiteKey.shader` tekee niiden valkoisesta taustasta läpinäkyvän esityksessä. Grafiikkaa ei generoitu uudelleen.
- UI-atlaksen lokerot on rajattu kuvan todellisista koordinaateista, ei tasaruudukosta. Kaikki nimet, rahasummat, potti, panokset ja nappitekstit ovat uGUI Text -komponentteja.
- Logo on projektin alkuperäinen `Resources/Valikko/logo.png`. Fontit ovat Luckiest Guy ja Bangers. Tilapäisten korttien maasymbolit käyttävät Unityn sisäänrakennettua fonttia.
- Taustana on projektin nykyinen baarikuva. Konseptin tarkka baaritausta ei ollut erillisenä assettina mukana; sen voi vaihtaa asetuksesta.
- `cardFaces`: 52 erillistä Sprite-viitettä järjestyksessä risti 2–A, ruutu 2–A, hertta 2–A, pata 2–A. `cardBack`, `chip`, `dealerMarker` ovat omat viitteensä. PokerSetin 52 erillistä korttikuvaa ja selkä ovat nyt lähdeasseteissa. Valikko 194 kytkee ne sprite-varaesitykseen ja 3D-korttien materiaaleihin. Uutta korttigrafiikkaa ei tehty.
- `dealSound` ja `chipSound` ovat vapaat käyttäjän omille äänille.
- 3D-kortit: liitä prefabiin `PokerMeshCard` (tai oma `PokerCardPresenter`-toteutus), aseta etu-/takapuolen renderöijät ja 52 materiaalia. Liitä prefabi `cardPrefab`-kenttään ja läpinäkyvä RenderTexture `cardRenderTexture`-kenttään. `cardTableEuler` ja `cardWorldScale` säätävät pöytätasoa/perspektiiviä. Esitys luo erillisen perspektiivikameran ja käyttää layeria **30**. Käytä URP-yhteensopivia, tarvittaessa Unlit-materiaaleja. PokerSetin mallit on normalisoitu yhden yksikön levyisiksi; lopullinen perspektiivi ja suunta pitää vielä varmistaa Unityssä.

## Tiedostot ja vastuut

- `Core/PokerCards.cs`: puhdas C#, viiden kortin vertailu kaikista seitsemän yhdistelmistä ja kickerit.
- `Core/PokerTable.cs`: tilakone, jako, burn-kortit, lailliset toiminnot, minimikorotukset, kumulatiivinen uudelleenavaus, sivupotit, jakopotit, parittomat merkit jakajasta vasemmalle ja maksamattoman panoksen palautus.
- `Core/PokerBot.cs`: neljä tyyliä (varovainen Yakuza, aggressiivinen prätkäjätkä, löysä puliukko, tasapainoinen rokkimimmi). Pelipaikat myötäpäivään: Sinä, Puliukko, Yakuza, Prätkäjätkä, Rokkimimmi. Monte Carlo -equity, preflop-aloituskäsien vahvuus, asema, pot odds, vastustajien korotusten huomiointi (equity laskee, kun joku on korottanut), bluffit vain kun kukaan ei ole näyttänyt voimaa, vaihteleva panoskoko ja all-in vain vahvoilla käsillä. Päätösrajapinta sisältää vain omat kortit ja julkiset tiedot.
- `Core/PokerCashSession.cs`: sisäänosto, lisäosto, poistuminen ja täsmälleen kerran tehtävä tilitys. Tyhjiä botteja täydennetään vain jakojen välissä asetetun sisäänoston mukaan.
- `Core/PokerCheckpointStore.cs`: atominen yhteinen lompakko-/jaon tallennus.
- `PokerService.cs`: `DontDestroyOnLoad`-omistaja, rahasilta, taustasäikeen bottityöt ja unscaled-aika. UI-animaatioiden valmistuminen ei koskaan ohjaa sääntöjä.
- `PokerView.cs`: kuvan mukainen uGUI, vuorokehykset, korttien jako/kääntö ja merkkien siirtymät.
- `PokerTableEntry.cs`, `PokerTestBootstrap.cs`, `Editor/PokerSetup.cs`: kenttäliittymä ja erillinen testikäynnistys.

## Raha ja palautuminen

Projektissa ei ollut pelirahan tallennusta: `PlayerController.money` oli vain muistissa. Pokeri käyttää sitä oikeassa kentässä ja tallentaa ensimmäisestä sisäänostosta alkaen saman lompakon pokerin yhteiseen checkpointiin.
Kun uusi pelihahmo ilmestyy scenen vaihdon/uudelleenkäynnistyksen jälkeen, tallennettu lompakko palautetaan sille. Myös pokerin ulkopuolella kertyvä raha synkronoidaan; muutokset tallennetaan enintään 0,5 s viiveellä ja poistuttaessa.
Tämä tekee pokeria käyttäneen pelaajan rahasta pysyvää myös pelin oman uuden pelin jälkeen. Jos projektiin lisätään myöhemmin varsinainen profiili-/save-järjestelmä, `AttachPlayer`, `SyncWallet` ja `ApplyWallet` ovat sen rahasillan korvauspisteet; checkpoint on pidettävä samassa transaktiossa jaon kanssa.

Tiedostot ovat `Application.persistentDataPath`-kansiossa:

- `total-fist-poker.json`: peliraha ja nykyinen jako.
- `total-fist-poker-demo.json`: erillinen testiraha ja testijako.
- `.bak`: edellinen atomisesti korvattu snapshot. Virheellistä tallennetta ei nollata automaattisesti.

Näkymästä poistuminen palauttaa ohjauksen heti. Tavallinen käsi luovutetaan ja vapaa stäkki palautetaan. All-in pysyy voimassa; palvelu ratkaisee jaon ilman näkymää ja tilittää voiton kerran. Myös näkymän tuhoaminen ja scenen sulkeminen kulkevat samaa poistumisreittiä. Sovelluksen sulkeminen selvittää keskeneräisen jaon; odottamaton keskeytys jatkuu tallennetusta jaosta seuraavalla käynnistyksellä. Snapshot yhdistää lompakon, stäkin ja tilityksen tilan: erillisen hyvityksen ja kuittauksen välissä ei ole kaatumisikkunaa.

## Testit ja editoritarkistus

EditMode: `Assets/Tests/Poker/PokerCoreTests.cs` näkyy Unity Test Runnerissa omana testikokoonpanonaan.
Ilman Unityn käynnistystä: `powershell -File Tools/Run-PokerTests.ps1` käyttää asennuksen Mono/C#-kääntäjää ja ajaa samat NUnit-testimetodit. Ei ulkoisia riippuvuuksia tai verkkolatauksia.

Testit kattavat käsikategoriat/kickerit, ässän matalan suoran, jakopotit/parittoman merkin, erikokoiset all-init/sivupotit/palautuksen, minimikorotukset, vajaan ja kumulatiivisen all-inin, heads-up-blindit ja siirtymän heads-upiin, koko jaon, muiden luovuttamisen, sisään-/uloskirjautumisen, palautumisen levyltä ja epäonnistuvan tallennuksen. Lisäksi 200 satunnaista laillista jakoa sekä 12 bottien pelaamaa jakoa tarkistavat jokaisen toiminnon merkkitasapainon.
Panostuksen uudelleenavautumisen tulkinnan viite: [Poker TDA, Re-opening the Bet](https://www.pokertda.com/view-poker-tda-rules/). Tässä käytetään sen panostussääntöjä ilman turnausmekaniikkoja.

Unityn Play-ajoa ei ole tehty agentin toimesta. Käyttäjän pitää tarkistaa Console/importointi, 16:9/ultrawide/4:3-asettelu, fontit/valkotaustan avainnus, hiiri/ohjain, korttien ja merkkien animaatiot, poistuminen all-inissa sekä kentän E-kehote ja ohjauksen palautuminen. CGTraderin 3D-mallien perspektiivi, mittakaava ja materiaalit tarkistetaan niiden saavuttua.

## Videopöytien sijoitus (valikko 193)

`poker-table-idle.mov` on käyttäjän alkuperäinen 1280×720 / 24 fps / 193 ruudun video. `poker-table-idle-first-frame.png` on siitä dekoodattu ensimmäinen ruutu, jota käytetään editorin esikatselussa ja videon varakuvana. Lähdekuvaa tai videon hahmoja ei muokattu. `PokerGreenKey.shader` poistaa vihreän taustan piirrossa. Videon omaa ääntä ei toisteta.

Aja Stop-tilassa **193. Holdem: omat videopöydät neljään paikkaan**, sitten **Ctrl+S**. Valikko luo tai päivittää neljä omaa pöytää: Samperi'sin pokerihuone alas oikealle, El Loipparin terassin oikea puoli, laivan hytti ja Deacon's Den alas vasemmalle. Vanha pokerihuoneen pöytä ja tapahtumat säilyvät. Kullakin pöydällä on oma alueviite, liittymistoiminto ja kävelyn estävä jalanjälki. Laivan hytin pöytä on hieman pienempi. Baarikohtaiset pokeritaustat tehdään myöhemmin: uudet asetusassetit käyttävät yhteistä väliaikaista taustaa, eikä sijoitusvalikko korvaa käsin asetettuja taustoja.

Video käynnistyy vasta omalla alueella. Pokerin/valikon aikana se pysähtyy; muualla ei dekoodata. Jalat on kohdistettu videosta havaittuun riviin 676. Tarkista Unityssä vihreän reunan puhtaus, hahmojen koko ja pöytien sijoitus. Voit siirtää pöydän juuriobjektia ja säätää sen Visual-lapsen mittakaavaa. Valikon uusi ajo palauttaa oletussijoitukset. Jos MOV-koodekin tuonti epäonnistuu, esikatseluruutu säilyy ja Console ilmoittaa asiasta.

## PokerSetin kortit ja merkit (valikko 194)

Käyttäjän toimittama paketti löytyi Downloads/PokerSet/PokerSet-kansiosta. Siinä on OBJ/Blender-lähde, ei FBX-tiedostoa. Alkuperäinen Blender-tiedosto on säilytetty `Lahdemallit/PokerSet/PokerSet.blend`-polussa, Unityn automaattisen Blender-tuonnin ulkopuolella. Downloads-lähteitä ei muokattu.

`Assets/ArtSources/Poker/ImportedSet` sisältää 52 alkuperäistä PNG-korttia, selät, lähdeatlaksen ja erotellut OBJ-mallit. Kuvia ei pilkottu ruudukoksi eikä maalattu uudelleen. Tuontiskripti `Tools/PokerAssets/import_pokerset.py` erottaa lähteen 2Clubs-kortin kolme osaa, normalisoi pintojen UV:t erillisiä korttitekstuureja varten ja erottaa 1/2/5/10/20/25/50 markan merkit numeroineen. Tiheän merkkirungon pisteitä yhdistetään materiaalialueittain; numeroiden ja kortin geometria säilyy. Kortit ovat 4 224 kolmiota, merkit noin 10 000–14 400 kolmiota kappaleelta. Lähteen tunniste ja mallimäärät ovat `import-manifest.json`-tiedostossa.

**Käyttöönotto:** odota Unityn importointi ja käännös loppuun Stop-tilassa, aja **194. Holdem: ota PokerSetin kortit ja 3D-merkit käyttöön**. Tämä luo kortti- ja merkkiprefabit, 52 pintamateriaalia, merkkien lähdevärit ja läpinäkyvän 1920×1080 RenderTexturen `Assets/Resources/Poker/ImportedSet`-kansioon. Kaikki nykyiset pokeriasetukset kytketään niihin. Myöhemmin valikolla 193 luodut asetusassetit perivät samat assetit oletuksista. Valikko 194 ei muuta blindeja, sisäänostoa, taustoja tai pelisääntöjä.

3D-kameran koordinaatit vastaavat hyväksytyn konseptin UI-koordinaatteja. Mallit jaetaan ja käännetään unscaled-ajassa, ja merkit näkyvät stäkeissä, panoksina sekä kerätyssä potissa. Visuaaliset merkkipinot on rajattu 18 merkkiin per paikka; tarkka kokonaisarvo näkyy aina UI-tekstinä. Raja ei vaikuta rahalogiikkaan. Kamera käyttää layeria 30 ja esityksessä on post-processing pois, jotta RenderTexturen tausta pysyy läpinäkyvänä. Shader käyttää lähdetekstuureja ja lähdevärejä sekä omaa kevyttä muotovalaistusta; baarin valoihin ei tarvitse lisätä 3D-valoja.

Avaa `PokerTest.unity` ja käynnistä Play itse. Tarkista kortin etu-/selkäpuoli, korttien suunta, kaikki maat, merkkien numerot/värit, animaatiot ja perspektiivi. 3D-esityksen varavaihtoehto on tyhjentää `cardPrefab`-kenttä: samoja alkuperäisiä korttikuvia käytetään tällöin UI-spriteinä. Uuden tuonti-/esityskoodin C#-käännös on tarkistettu Unityä käynnistämättä. 52 korttitiedoston SHA-256 vastaa toimitettuja lähteitä, ja kaikkien erotettujen mallien indeksit, UV-rajat sekä normalisoidut mitat on tarkistettu. Unityn Play- tai valikkoajoa ei ole tehty agentin toimesta.

Jakajan nappi: alkuperäinen läpinäkyvä PNG on Assets/ArtSources/Poker/dealer-button.png. Valikko 195 liittää rajatun spriten kaikkiin pokeriasetuksiin; myös 194 tekee tämän. Kuvan pikseleitä ei muokata. Avaa pokerinäkymä uudelleen asetusten päivityksen jälkeen.
