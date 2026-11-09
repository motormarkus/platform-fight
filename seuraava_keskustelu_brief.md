# Brief seuraavaan keskusteluun (Total Fist)

Kirjoitettu 2026-11-08. Lähtökohta: haara `scene-split`, viimeisin commit `0b24024a` (pushattu GitHubiin). Uusi haara aloitetaan tästä.

## Mitä seuraavaksi: testaus Unityssä
Mitään alla olevaa ei ole ajettu Unityssä. Aja ensin:
1. Avaa Unity ja odota, että koodi kääntyy (uusi tiedosto `AttackEase.cs` saa tunnistetiedoston silloin).
2. Valikko **343** (Muurin keskitorni: ala-, ylä- ja kattokerros) ja tallenna Muuri-scene.
3. Valikko **341** SampleScenessä ja tallenna (rakennettu jo aiemmin, ei ajettu).

Testattavat asiat:
- **Muurin keskitorni** (`Assets/Editor/WallTowerSetup.cs`, `Assets/Scripts/AreaSplit.cs`):
  - Muuri pysähtyy tornin kohdalla: vasemmalta ei pääse ohi muuten kuin tornin läpi.
  - Vasen ovi vie alakerran vasempaan päähän, ja alakerran oikea pää vie ulos tornin oikealle puolelle.
  - Keskiportaat vievät yläkertaan, yläkerran oikeat portaat katolle, ja katolta pääsee takaisin (katolla ovi on etureunassa).
  - Ikkunoista näkyy parallaksitaivas. Tarkista, onko ikkunoiden reunoissa vaalea kehys.
  - Katon soihtuliekit kahdessa kupissa ja lippu tangossa (`FrameLoop.cs`, kuvat kansioissa `Resources/Muuri/Soihtu` ja `Resources/Muuri/Lippu`). Osuvatko ne kohdalleen ja ovatko koot oikeat?
- **Kiihtyvät iskut** (`AttackEase.cs`, kaikki vihujen aivot): vihujen lyöntien ja potkujen veto alkaa hitaasti ja nopeutuu osumaa kohti. Tuntuuko luontevalta? Osuman ajoitus ei pitäisi muuttua.
- **Horhen läpsykombo**: 12 läpsyä, 5 vahinkoa kukin, viimeinen 18 ja kaataa. Suoja murtuu viidennestä torjutusta läpsystä. Onko liian kova?
- **Pomojen nurkkapurkaus** (aja pomo nurkkaan ja hakkaa): Joulupukki (maha + isku), Rottakeisari (koukku + potku), Pierre (tornado + yläkautta), robottipomo (liukutaklaus), koe-olento (häntähuitaisu), Temppelin kuningas (hyppy), Timppa (pusku). Iskujen pitäisi lennättää hero toiseen laitaan.
- **Äänet**: osuma-, kipu-, gasp- ja puheäänet tasattiin suoraan tiedostoihin (`Tools/AudioLevels/level_hits.py`, alkuperäiset `Tools/AudioLevels/alkuperaiset/`). Kuulostavatko tasaisilta? Robottien puhe3/speak3 jäi noin 5 dB hiljaisemmaksi.
- **Äänenvoimakkuuden asetus** (`SfxVolume.cs` kevennetty): toimiiko tehosteiden voimakkuuden säätö valikosta yhä.
- Aiemmin rakennetut, yhä testaamatta: Lippis (takasuora, JSP-kombo, väistö + counter), Punkkari (takasuora, kombot), pyykkitupa, hissi, Dinan puheet, Metsuri, Rocco-tv, laajennettu Kiinan muuri (valikot 330/327).

## Avoimet asiat sisällössä
- **Tornin katon pomo**: ei päätetty. Ehdotettu Nian-hirviötä tai kivileijonaa. Viimeinen pomo on **Lokki**, ja lohikäärmeen paikka on auki. Käyttäjä tuo kuvat.
- **Taskut ja energia**: ehdotettu, että eväspaikkoja tulee lisää pomoista. Mahdollinen tankkauspaikka tornissa. Ei päätetty.
- Pomohuoneen ovi ja tarinan loppu, Gestapon ja robottipomon äänet (kuolema, puhe) puuttuvat.

## Optimointi (tarkistettu 2026-11-08)
- Eheys kunnossa: ei puuttuvia skriptejä, ei rikkinäisiä viittauksia, viimeisessä käännöksessä ei virheitä.
- Mahdollinen myöhempi säätö: isoimmat hahmosheetit (esim. lentoemäntä 4096x7040, kapteenin iskut, rottakeisarin idle) voi tuoda max 4096 -kokoisina, jos muistia tarvitaan.

## Työskentelysäännöt
- Vastaukset suomeksi, lyhyesti. Älä kyselytä turhaan.
- Älä generoi kuvia tai taustoja itse kysymättä; käytä käyttäjän kuvia ja projektin valmiita.
- Ei PlayStation/Sony-viittauksia, ei hakaristejä tai natsitunnuksia.
- Ruoka vain kaupasta (poikkeus: pomojen ja rekkamiehen pizza).
- Muurin valikot pääluetteloon. Korjaa mieluummin ajonaikaisesti kuin pakota valikkojen uudelleenajo.
- Unityä ei voi ajaa Claudesta: kaikki on testaamatonta, kunnes käyttäjä kertoo.
- Commitin loppuun: `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Kysy ennen pushia.
