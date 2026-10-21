# Total Fist – jatkobriiffi uuteen keskusteluun (päivitetty 21.10.2026 illalla)

Aloita lukemalla tämä. Taustat: `GUSTAV-TOM-BRIEF.md` (Gustav, Tom, Candi, counter), `STORY-DRAFT.md` (tarina), `THAITOWN-HOTEL-BRIEF.md`, `SCENE-SPLIT-BRIEF-2.md`. Unity 6000.3.18f1 / URP 2D. Haara `scene-split`. Viimeisin push: katso `git log` (commit "Salsafest ...").

## ENSIMMÄISENÄ: etäohjaus

Käyttäjä ei ole usein kotona ja seuraa sessiota etänä (claude.ai/code tai mobiili). Asetus "Connect new sessions to Remote Control" on kytketty päälle (21.10.), joten uuden session pitäisi yhdistyä automaattisesti. **Tarkista heti alussa**: jos et ole varma, lataa `mcp__ccd_session_mgmt__set_remote_control` (ToolSearch) ja kutsu `session_id: "self", enabled: true`. Kerro käyttäjälle yhdellä rivillä, että etäohjaus on päällä. Käyttäjä ei välttämättä näe Unityä eikä konetta: älä oleta, että hän on valmis ajamaan valikkoja heti.

## Työskentelyohjeet (käyttäjältä, ennallaan)

- Vastaa lyhyesti suomeksi, selkeästi ja ilman ammattisanastoa (käyttäjä ei ole ohjelmoija). Kerro suunnitelma lyhyesti ennen kuin muutat mitään; pienissä korjauksissa voi toimia heti.
- Claude ei näe Unityä. Älä väitä ulkoasua tai toimintaa valmiiksi ilman käyttäjän testiä. Käyttäjä ajaa valikot ja pelitestit itse; pyydä Stop ennen valikkoajoja ja kehota sulkemaan muokatut scenet tallentamatta (Unity voi ylikirjoittaa levyltä muutetun scenen).
- Push ja commit vain pyydettäessä. Älä käytä `git add .` / `-A`, lisää tiedostot nimeltä. Älä koske `ääniä ja cubase/*.cpr`, `output/`, `Card.prefab`, `PokerAssets.renderTexture`, `POKER-CLAUDE-BRIEF.md`, `Tools/PokerAssets/src/*`, `SCENE-SPLIT-BRIEF-2.md`, `THAITOWN-HOTEL-BRIEF.md` (käyttäjän paikallisia).
- Ei valmistajaviittauksia (ei PlayStation/Sony). Ruoka vain kaupasta (+ pomojen pizza), voimakuvake harvinainen tehtäväpalkinto. Pomojen pitää olla oikeaa vastusta (Deacon vaikein).
- Kuvien teko: käyttäjä lähettää videoita/kuvia chattiin (liitteet `C:\Users\marku\.claude\uploads\<sessio>\`). Näytä katselukuvat (violetilla taustalla) ennen kuin ne menevät peliin; käytä `SendUserFile`.
- Pieneen tilaan (sisätila) hahmot saavat olla hieman isompia; ulkona hero-kokoisia.

## Tekniset kompastuskivet (tärkeä!)

- **`\\n` Bash-heredocissa muuttuu oikeaksi rivinvaihdoksi** C#-merkkijonoissa ja rikkoo käännöksen ("Newline in constant"). Tapahtui monta kertaa. Käytä sen sijaan: kirjoita Python-skripti `Write`-työkalulla tiedostoon ja aja se, tai muodosta kenoviiva `chr(92)`:lla / paikanvaraajalla (`{NL}` -> `chr(92)+'n'`). Tarkista aina `grep`illä, ettei merkkijono katkea riville.
- `BeatEmUpSetup.cs` (~16 100 riviä) ja scenet ovat osin CRLF, osin LF: tunnista rivinvaihto skriptissä (`nl = '\r\n' if '\r\n' in s else '\n'`) ja käytä `newline=''`/binäärilukua.
- Muutokset suoraan scene-YAMLiin (UccoLaiva, Aavikko, SampleScene) onnistuvat (poisto/lisäys dokumentteina, `m_Children`-listat), mutta kerro käyttäjälle sulkea scene tallentamatta. Scenejako: alueet eri scene-tiedostoissa (`Assets/Scenes/Areas/*.areas.txt` listaa alueet).
- `rm`-komennot muuttujilla estetään turvatarkistuksessa: kirjoita yli tai käytä kirjaimellisia polkuja.
- Valikkojen uudelleenajo voi tuhota/kopioida olioita: valikot 44 ym. (Uccopulcon rakennus) laskevat paikat kuvan vasemmasta reunasta (+3023 px, katso kommentti `UccoPath`-vakion yläpuolella): älä aja niitä uudelleen.

## Tehty (kaikki pushattu; käyttäjä testaa kotona/koneella)

Aiemmat (Gustav/Tom/Candi, mayhem, Rubyn alasväistö, tapauskansio ja Rexin kirje, Uccopulcon katu + panttilainaamo + Helvi + ulkokahvila, Chinook-ovet jne.) on kuvattu STORY-DRAFT.md:ssä ja aiemmissa commiteissa. Tämän päivän lisäykset:

- **Tarinalukot** (`Clues.cs`: `Open`, `GatesOn`, `BossDown`): Metsurin kaato antaa moottoripyörän avaimet (pyörä ei lähde ilman: `Motorbike`), Horhen kaato antaa laivalipun (ovi "Varaston ovi laivaan", `Door.requiredClue`). Valikko **229** kytkee lukot pois testaukseen.
- **Marcus** (kehän sparraus): kanveesiin lyöminen antaa Mestaruusvyön (`Clues.WinBelt`): vihujen lentomatka +15 % (`Clues.LaunchMul`).
- **Candin rytmitreeni** (`CandiTraining.cs`, valikko **230**): Candi treenipaikalle (x 110034), E aloittaa, isku (J) ja potku (K) nuottien mukaan, 21 tahtia; 16+ pistettä -> Candin treeniohjelma (kuva `Resources/Kirjeet/candi_treeniohjelma.png`, staminan palautuminen +10 %). Kuvat `Sprites/Jumppa/candi_lyonti_1/2.png`.
- **Salsafest** (ranta Uccopulcon kujan takana, UccoLaiva-scene, alue "Salsafest" x 24000): kuva `Taustat/salsafest.png` (3 kuvaa yhdistetty, läpinäkyvä taivas; Tools/make_salsafest.py), kulku kujalta x 3893 px (Uccopulcon katukuvan 3. ruutu, kujaan lisätty hiekkaa), meri parallaksina. Valikot: **232** alue ja ovet, **236** meri (parallaksi, Uccopulcon merikuva), **233** S-Clubin bändi lavalle + S-Clubin musiikki, **234** Maria ja Sohvi tanssivat (kuvat `Sprites/Maria`, `Sprites/Sohvi/sohvi_tanssi_*`), **231** Helvin tanssi lavan edessä, **235** Tom, Puliukko ja Samoalainen rauhassa (`SalsaBrawl.cs`: tappelu alkaa, kun hero lyö jotakuta tai kun alueella on oltu 2 min ja Puliukko vittuilee; sitten kaikki vastaan kaikkia), **237** kaksi rokkimimmiä tanssi-idlellä (`Sprites/Rokkimimmi`). Ajojärjestys uudessa projektissa: 232 -> 236 -> 233 -> 231 -> 234 -> 235 -> 237.
- **Helvin tanssi** (`HelviDance.cs`): kaulakoru (panttilainaamosta 250 mk, Clue `esine_Kaulakoru`) -> E antaa korun -> paritanssi (Rocco+Helvi, `tanssi_pari_1-4`, vain Rocco): 15 tahtia iskunapilla; 7+ osumaa suudelma, 12+ nosto syliin (huipennus, soitetaan lopuksi takaperin), palkintona vihje Horhesta (+100 mk täydellisestä).
- **Rokkimimmin kuvat**: kaatumis-, ylösnousu- ja niskalenkkisarjojen (`rokkari2_*`) maassa/lennossa olevat ruudut kasvatettu (`Tools/rescale_frames.py`, ruutukohtaiset kertoimet), koska video oli kutistanut hahmon; käyttäjä: "näyttää legit". Alkuperäiset varmuuskopiossa (työkansion scratchpad).
- Tytöt (Helvi, Maria, Sohvi) ja Rokkimimmi: ääriviivoja paksunnettu (`Tools/thicken_outline.py`), tyttöjen koko Salsafestissä 1,12.
- Työkalut: `Tools/HelviAssets` (Helvin ja parin tanssit), `Tools/RokkiAssets`, `Tools/SohviAssets/make_sohvi_dance.py`, `Tools/GymAssets/make_candi_lyonti.py`.

## Odottaa käyttäjän testiä / ajoa

Mikään tämän päivän koodi ei ole käännetty Claudella; käyttäjä ajoi valikot 230-237 ja testasi Salsafestin ulkoasun (koko, paikat, ääriviivat OK). Testaamatta: Candin treenin ajoitus ja palkinto, Helvin tanssi (kaulakorun osto -> tanssi -> palkinto), 2 min -tappelun laukeaminen (Puliukko vittuilee -> kaikki vastaan kaikkia), rokkimimmien tanssi-idle, meren parallaksin korkeus (siirrä "Salsafest meri" tarvittaessa), kujan uusi hiekka, tarinalukot (avain/lippu), Marcuksen vyö, kirje Kadun baarissa, mayhem, MemoryLogger ([MUISTI] Editor.logissa; 4 Gt VRAM -arvio).

## Seuraavaksi (käyttäjä päättää)

1. **Prätkäjätkät** Salsafestiin (käyttäjä tekee heille rennot idlet): sama logiikka kuin rokkimimmeillä (`calmIdleSprites`, `wakeDistance=-1`, lisäys `SalsaBrawl.crowd`-listaan, mallina valikko 237). Rokkimimmeille kertyi myös "rennot" kuvat; lisää tarvittaessa.
2. Salsafestin sisältö: kojujen myyjät, sivujuonet, tappelun tyyli; Mariasta/Sohvista puhuvat hahmot.
3. Tarina: Metsurin/Horhen vihjeiden sisältö, lisäpomo ennen Gustav II:ta, Lokin tausta (ks. STORY-DRAFT.md).
4. Esineiden käyttö (kaulakoru on nyt Helvin tanssin avain, sormus ja kitara vielä käyttämättä).
5. Katujen viimeistely ("kadut ensin valmiiksi"); katot/portaat myöhemmin.
6. Vanhat avoimet: Gustavin junakohtaus ja kattojahti, Roccon A+B-iskut, Marcusin arvot, optimoinnit, kaksinpelin ja pokerin testi scenevaihdon yli.
