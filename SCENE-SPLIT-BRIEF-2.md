# Total Fist – scenen jako, jatkobriiffi 2 (19.10.2026)

Aloita lukemalla tämä. Taustat: `SCENE-SPLIT-BRIEF.md` (alkuperäinen suunnitelma ja päivitykset). Unity 6000.3.18f1 / URP 2D.

## Työskentelyohjeet (käyttäjältä)

- Vastaa lyhyesti suomeksi, selkeästi ja ilman ammattisanastoa (käyttäjä ei ole ohjelmoija). Kerro suunnitelma lyhyesti ennen kuin muutat mitään.
- Claude ei näe Unityä. Älä väitä ulkoasua tai toimintaa valmiiksi ilman käyttäjän testiä. Käyttäjä ajaa valikot, käännökset ja pelitestit itse; pyydä Stop ennen valikkoajoja. Tietokoneen hallinta (computer-use) on käytettävissä vain kun käyttäjä erikseen pyytää.
- Push ja commit vain pyydettäessä. Älä käytä `git add .` / `-A`, lisää tiedostot nimeltä. Älä koske `ääniä ja cubase/*.cpr`, `output/`, `Card.prefab`, `PokerAssets.renderTexture`, `POKER-CLAUDE-BRIEF.md` (käyttäjän paikallisia).
- Editorikoodi: `Assets/Editor/BeatEmUpSetup.cs` (~14 800 riviä, CRLF). Uudet kohdat tiedoston loppuun. Kirjoita koodi Write-työkalulla scratchpadiin ja yhdistä Pythonilla (muuta rivinvaihdot CRLF:ksi); älä upota `\n`-merkkejä heredocilla.
- Ei valmistajaviittauksia. Pomojen pitää olla oikeaa vastusta (Deacon vaikein).

## Mitä on tehty (haara `scene-split`, pushattu GitHubiin, viimeisin commit 9a4f059)

Peli on jaettu neljään kenttään, jotka latautuvat additiivisesti vasta kun sinne mennään:

| Kenttä | Scene | Sisältö |
|---|---|---|
| Bricktown | pääscene `SampleScene` (aina ladattu) | katu, S-Club, katto, takakuja, kadun baari, baari, kadun viemäri + pelaaja, kamera, HUD, valikot |
| Uccopulco + laiva | `Assets/Scenes/Areas/UccoLaiva.unity` | valtatie, Uccopulco, El Loippari, laiva, komentosilta, katto, kopterin rahtitila |
| Aavikko + junat | `Areas/Aavikko.unity` | aavikko, peltihalli, tie, katu, diner, baari, romuttamo, juna-asema, junat, aavikon viemäri |
| Thaitown | `Areas/Thaitown.unity` | laituri, asema, katu, hotelli |
| (sali + kehä) | `Areas/Nyrkkeilysali.unity` | nyrkkeilysali ja kehä (pilotti, valikko 200) |

Valikot (kaikki ajettu ja käyttäjän testaamia): 200 sali+kehä, 201 Thaitown, 202 UccoLaiva, 203 Aavikko, 204 junan katon oikea reuna → Thaitownin laituri. Generinen työkalu: `MoveLevelToScene(level)` valikon 201 kohdalla (luokittelee juuriolioiden kentän sijainnin mukaan, keskeyttää jos viittaus ylittäisi kentän rajan, kirjaa alueet `<kenttä>.areas.txt`-tiedostoon).

Koodipohja (kaikki testattu):
- `Area.Find(nimi)`: aluerekisteri (alue ilmoittautuu nimellä Awake:ssa).
- `Door.targetName / targetScene`, `RideExit.targetName / targetScene`, `GameStart.areaName / sceneName`: kohde nimellä; scene ladataan tarvittaessa (pimennyksen aikana).
- `Door.autoTrigger`: siirtymä alkaa itsestään (käytössä vain junan katon oikeassa reunassa).
- Aloitusvalikot (36, 46, 88, 90, 92, 107, 128, 137–139 ym.) käärittynä `WithLevelScenes`-funktioon: avaa kenttäscenet hetkeksi lukemaan alueen ja tallentaa aloituskohdan nimellä.
- Koordinaatteja EI siirretty lähelle origoa (skriptien kovakoodatut x-arvot rikkoutuisivat); float-tarkkuus x>78000 ratkaistaan vain jos tärinää näkyy.

Kenttien väliset yhteydet: kuja → valtatie (`RideExit`), kopteri → aavikko (`Door`), junan katto → Thaitown (`Door.autoTrigger`). Valtatie → Uccopulco on saman kentän sisällä.

## Mittaukset

- Tekstuuriarvio (`python Tools/MemoryReportScenes.py`, ajettava ilman `-I`, tarvitsee PIL): pääscene 917 Mt (ennen jakoa kaikki 2141 Mt); Aavikko 816, UccoLaiva 506, Thaitown 279, sali 81 Mt.
- Unity Profiler (Editor, Play, Memory → Simple): Textures 4,42 Gt → 1,90 Gt; Graphics-muisti 3,2 → 1,91 Gt. Profilerin oma muisti (~1,9 Gt) vääristää kokonaislukua. Editorin luvut ovat isompia kuin buildissa.
- `Resources`-kansio vain ~266 Mt (Pan 71, Pokerin taustat 70 (3 pakkaamatonta kuvaa, ~100 Mt säästettävissä jos pakkaus sopii), AjoPari 54, Termari 18, AutonrengasHero 17).

## KESKENERÄINEN: tallentamaton ja testaamaton muutos (vain käyttäjän koneella)

Löytö: Unity ei vapauta purettujen scenejen tekstuureja itsestään; koodissa ei ollut `UnloadUnusedAssets`-kutsua. Siksi kentän jättäminen ei välttämättä vapauttanut muistia. Korjaus on kirjoitettu mutta ei ajettu eikä commitoitu:
- Uusi `Assets/Scripts/ScreenFader.cs`: pysyvä musta pimennys (`ScreenFader.Alpha`) ja siirtymien isäntä (`ScreenFader.Host`). Doorin siirtymärutiini ajetaan nyt tässä, koska ovi voi olla scenessä, joka puretaan kesken siirtymän (muuten ovi ja rutiini tuhoutuvat ja `busy` jää päälle).
- `Area.FreeOtherScenes(dest, pc)`: purkaa muut ladatut kenttäscenet (paitsi kohteen ja pelaajan scene) ja kutsuu `Resources.UnloadUnusedAssets()` mustan ruudun aikana. `Door.Go` ja `RideExit.Go` kutsuvat sitä ennen sisääntulon valoa. Doorin oma pimennyksen piirto ja `leaveScene`-kenttä poistettu.
- Odotettu sivuvaikutus: pimennys kestää kentästä poistuttaessa hetken pidempään.

Seuraavaksi: (1) anna Unityn kääntyä, kerro virheet; (2) testaa siirtymät (kaikki ovet, kuja → valtatie, kopteri → aavikko, katto → Thaitown, sali ja kehä edestakaisin; ruutu ei saa jäädä mustaksi, ovet toimivat uudelleen); (3) mittaa Textures Profilerista ennen ja jälkeen kentän jättämisen (esim. valikko 139 → katon oikea reuna, tai valikko 46 → laiva → kopteri → aavikko; lopuksi valikko 37, ettei aloituskohta jää pääsceneen commitiin); (4) jos hyvä, commit haaraan `scene-split` ja push pyydettäessä. Jos huono, `git checkout Assets/Scripts/Area.cs Assets/Scripts/Door.cs Assets/Scripts/RideExit.cs` ja poista `ScreenFader.cs(.meta)` palauttaa viimeisimmän toimivan tilan.

## Jäljellä

1. Yllä oleva testaus ja commit.
2. Kaksinpelin, pokerin rahojen (escrow, checkpoint; `PokerService.SceneUnloaded` kutsuu Commit/Leave aina kun mikä tahansa scene puretaan) ja pelin tallennuksen testaus scenevaihdon yli (käyttäjä jätti myöhemmäksi).
3. Optimointi jatkossa: Resources-kuvat (pokerin pakkaamattomat), Sprites/Viholliset ~700 Mt ja Taustat ~400 Mt (pakkaus tehty jo valikolla 199).
4. Vanhat rakennusvalikot (`GameObject.Find`, `ReuseArea`) eivät löydä siirrettyjä alueita, ellei kenttäscene ole avattuna pääscenen rinnalla (hiiren oikea → Open Scene Additive). Dokumentoi tai päivitä tarvittaessa.
5. Junan katon Thaitown-saapumiskohta on laiturin alku (6 yks. vasemmasta reunasta); käyttäjä voi haluta toisen kohdan (Aavikko-scenen olio "Katolta Thaitowniin" → Spawn Point).
