# Total Fist – Gustav, Tom, Candi, salivälineet ja counter: jatkobriiffi (20.10.2026)

Aloita lukemalla tämä. Taustat: `THAITOWN-HOTEL-BRIEF.md` (hotelli), `SCENE-SPLIT-BRIEF-2.md` (scenejako). Unity 6000.3.18f1 / URP 2D. Haara `scene-split`.

## Työskentelyohjeet (käyttäjältä)

- Vastaa lyhyesti suomeksi, selkeästi ja ilman ammattisanastoa (käyttäjä ei ole ohjelmoija). Kerro suunnitelma lyhyesti ennen kuin muutat mitään.
- Claude ei näe Unityä. Älä väitä ulkoasua tai toimintaa valmiiksi ilman käyttäjän testiä. Käyttäjä ajaa valikot ja pelitestit itse; pyydä Stop ennen valikkoajoja. Käyttäjä liittää kuvakaappauksia ja kertoo, mitä tapahtui.
- Push ja commit vain pyydettäessä. Älä käytä `git add .` / `-A`, lisää tiedostot nimeltä. Älä koske `ääniä ja cubase/*.cpr`, `output/`, `Card.prefab`, `PokerAssets.renderTexture`, `POKER-CLAUDE-BRIEF.md`, `Tools/PokerAssets/src/*` (käyttäjän paikallisia).
- Editorikoodi: `Assets/Editor/BeatEmUpSetup.cs` (~15 900 riviä, LF). Uudet kohdat tiedoston loppuun. Käytä Edit-työkalua tai Write + Python; Python-heredocissa `\\n` merkkijonoissa on vaarallinen (kerran jäi oikea rivinvaihto C#-merkkijonoon, korjattu). Bash-luokittelija voi hetkellisesti kaatua: yritä uudelleen.
- Ei valmistajaviittauksia. Ruoka vain kaupasta (+ pomojen pizza), voimakuvake harvinainen tehtäväpalkinto. Pomojen pitää olla oikeaa vastusta (Deacon vaikein).
- Kuvien teko: käyttäjä lähettää videoita/kuvasarjoja chattiin (liitteet `C:\Users\marku\.claude\uploads\<sessio>\`). Ei kuvaviittauksia tiedostonimissä käyttäjälle; näytä katselukuvat ja anna käyttäjän tarkistaa taustajäämät ennen pelikuvia.

## Tehty tässä istunnossa (kaikki kirjoitettu; käyttäjä ajoi valikot 212–219 ja testasi osan)

| Valikko | Mitä |
|---|---|
| 212 | **Gustav**, uusiutuva pomo, Thaitownin sviittiin (Enemy + GustavBrain). 560 elämää, ei kuole: nollassa kaatuu, nousee ja juoksee pois (`Enemy.undying`, `Vanish`). Kombot: jab–takasuora–etupotku–sivupotku(kiertopotku); etupotku–kiertopotku–kirvespotku–kierrepotku; jab–kirvespotku–takasuora–kiertopotku–etupotku–kierrepotku. Uppercut (hidas, myös torjunnan jälkeen), hyppypotku, hiekka (huumaa 2 s, `PlayerController.Daze`; osuma -> heitto), heitto. |
| 213 | **Candi** (Violetin = salin jumppatytön kaveri), `Assets/Sprites/Jumppa/candi_1..3.png`, Dancer. |
| 214 | Roccon ja Rubyn **huumausreaktio** (hiekka): `rocco_huumaus.png` / `ruby_huumaus.png`. |
| 215 | **Tom salissa** (puntti): TomBrain, 350 elämää, herää vain osumasta, poseeraa, jab/suora/voimalyönti kaatavat jos ei suojaa, heitto ottaa suojaankin, väsyy (stamina), 25 % -> juo shakerin -> **berserk** (nopea, punainen hehku, ei väsy, jahtaa kaikkia, nostaa ja heittää kojun/pöydän/laatikon ja painopallot, heittää vihuja). Haavoittumaton juonnin ajan. Omat äänet `Assets/Audio/tom/`. |
| 216 | **Salivälineet**: painoteline, punttipenkki, penkki, kyykkyräkki (3D-mallista renderöity, `Tools/GymProps`), levytanko (Crate, nostettava kuin pöytä), 4 käsipainoa + 2 painolevyä (Bottle ball-tila; `uprightHeld` = kädessä pystyssä nyrkin takana). Paikat arvauksia (osuus kävelyalueesta). |
| 217 | **Tom hotellin ravintolassa** (`Tom (hotelli)`, `attacksEveryone`: käy kaikkien kimppuun, herää itse). |
| 218 | Hissin ovet: 0,9 yks. etäisyys takaseinästä + puuttuvien ovien/ElevatorRiden luonti ja viitteiden korjaus. Käytävän hissin ovi oli kadonnut. |
| 219 | **Counter-isku / combo-breaker**: suojaus + lyönti yhtä aikaa (tai suojassa) kun vihu on kesken hyökkäystä -> nopea (0,3 s) haavoittumaton isku katkaisee sarjan (`Enemy.CounterHit`, `InAttack` +0,25 s anteeksiantoikkuna, painalluksen muisti 0,25 s). Kuvat `rocco_counter.png` / `ruby_counter.png` (5 kuvaa, ruutu 0 jätetty pois). Jysähdysääni `counter_thud1-3.wav` (synteettinen). |

Muita koodimuutoksia: `Enemy` (undying, IsDownOrDead, InAttack, CounterHit, thrownBy: heitetty vihu ei kaada heittäjää, brain-korkeus palaa maahan), `PlayerController` (Daze, BlocksFrom, IsGrabbed, State.Counter), `Bottle` (`uprightHeld`).

## Tiedostot ja työkalut

- Koodi: `Assets/Scripts/GustavBrain.cs`, `GustavSand.cs`, `TomBrain.cs`. Aivot ovat IEnemyBrain-malliin (kuten MarcusBoxer).
- Kuvat: `Assets/Sprites/Viholliset/gustav_*.png` (19 kpl), `tom_*.png` (12), ruutu 768 x 448, hahmo 300 px, jalat 8 px ylös, pivot alhaalla; `Assets/Sprites/Props/gym_*.png`.
- Työkalut: `Tools/GustavAssets` (`key_gustav.py` videot -> ruudut, `split_sheets.py` kuvasarjat, `make_gustav_sheets.py` arkit + värinsovitus idleen, `clean_fx.py` tehosteet/reiät/valkoiset laikut pois, `pick.py`, `scales.py`), `Tools/TomAssets` (`key_tom.py`, `make_tom_sheets.py`, `make_tom_falls.py`, `outline.py` ääriviiva 1,2 px), `Tools/HeroCounter` (`make_counter.py`, `make_thud.py`), `Tools/GymProps` (3D->sprite-renderöijä numpyllä, lähde-zip ja tekstuurit gitignoressa), `Tools/GymAssets/make_candi_sheets.py`. Lähdevideot ja -kuvat ovat käyttäjän chat-liitteissä (polut skriptien alussa); jos tarvitaan uudelleen, pyydä käyttäjää lähettämään.
- Videoiden taustat: vihreä/kerma/musta; `key_video.key_frame(cyan_safe=True)` estää turkoosin/sinisen vaatteen katoamisen.

## Avoimet ja seuraavaksi

1. Käyttäjä viimeistelee testauksen uudessa istunnossa (kerro mitä tapahtui + tilanne): Gustavin tasapaino ja koko, Tomin berserk ja äänet, counterin ajoitus, salivälineiden paikat ja kantokorkeus (`carryLower`), Candin etäisyys (`CandiOffsetX`), hissit.
2. Gustav: junan ravintolavaunu (1. kohtaaminen, `GustavBrain.Appear(pos, hp)` on valmiina), katon jahti, Thaitownin sviitti on lopullinen (560). Kohtaamisen energia kasvaa kohtaamisittain. Gustav ei vielä ilmesty junaan.
3. Tomilta puuttuu vielä: Roccon/Rubyn heittämänä kuvat on (tom_heitetty), äänet on; hotellin Tomin paikka `0,6` ravintolan leveydestä on arvaus.
4. Vanhat avoimet (ks. `THAITOWN-HOTEL-BRIEF.md`): sviitin/katon/käytävän täyttö, Rocco A+B -iskut, Marcusin arvot, optimoinnit, kaksinpelin ja pokerin testi scenevaihdon yli.
5. Gustavin ylösnousun ruudun 3 hanskassa on pieni läpinäkyvä piste (mahdollinen). Uppercutin ruuduissa 1–4 voi olla vaaleita laikkuja maastokuviossa (hole fill).
6. Pelin muistissa: uudet kuvat BC7-pakattuja (`SetupAndSlice`); Props 11 kuvaa, candi ~3 sheetiä.
