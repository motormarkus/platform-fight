# Total Fist: Vegas ja Muurin jako — brief seuraavaan keskusteluun (14.11.2026)

Haara `scene-split`. Kaikki alla oleva on kirjoitettu ja käännetty batchmodessa (ei käännösvirheitä), mutta **Vegasia ei ole ajettu eikä pelattu Unityssä**. Käyttäjä ei ole vielä testannut mitään tämän session töistä.

## Tehty tässä sessiossa
- **Melvin** (Vegasin 2. vihu, mikkiständi): valikko 382, sheetit `melvin_*`, ständi `mikkistandi.png`. Pudonnut ständi makaa 1,5 s, välkkyy ja häviää 3,2 s kohdalla (haluttu).
- **Don** (pomo, punapukuinen) + 2 Tankki-Tonya vartijoina: sheetit `don_*` (`Tools/DonAssets/make_don_sheets.py`), valikko 383. `Enemy.cs`: `guardOf`/`guards`/`EscortStep` (vartijat Donin ja pelaajan välissä, Don ~8 yks. päässä kunnes pelaaja lähellä tai vartijat kaatuneet). Donilla ei ole suojauskuvaa.
- **Tankki-Tonyn heitto** säädetty (lento kauas + liuku, lyhyempi kuin Tomilla).
- **Vegasin vihut** valikko 384 (`VegasCrowdSetup.cs`): kasino (2 Donia+4 Tonya, 2 Melvinia, Lätkäfani, 2 Puliukkoa, 2 Pensalenkkaria, 2 Rustya), katu (Melvin, Tony, Tanya), baari (Sohvi+tiski SampleScenestä, 6 pöytää pulloineen, 2 puliukkoa, 2 Donia+4 Tonya, Tom, 2 prätkäjätkää, 2 rokkimimmiä, Lätkäfani, SalsaBrawl 54 s + 4 portsaria). Mallit kloonataan muista sceneistä; 380/382/383 pitää ajaa ensin.
- **Baarin lava** valikko 385: bändi (kopio Aavikkobaarin bändistä; vaihtoehto S-Clubin mariachi), Mary ja Liz tangoissa lavan oikeassa päässä.
- **Violetti** valikko 386 (linnakkeen Violetti, ei juna-aseman Violet): kopio Muuri/Linnake-scenestä, kävelee/tanssii/nojaa tiskiin.
- **Bäkkäri** valikko 387: `vegas_bakkari.png`, alue x 108000, ovi baarin 3. kuvan 92 % kohdalla; sisällä pokeripöytä + Turpaan Vaan, Flipper Flapper, Tähtipyssy (kloonattu pelihallin koneista).
- **Muurin scenejako ajettu** (valikko 349 komentoriviltä): Muuri.unity, **Kuu.unity** (kuuasema, pinta, laboratorio, käytävä + 2, olion huone), **Linnake.unity** (juhlahalli). Jakoskriptiä korjattu: paikattomat juuret (esim. "Juhlahallin tappelu") seuraavat viittaustensa scenea. Ovia korjattu 4, scenet Build Settingsissä.

## Ajojärjestys Unityssä (Vegas)
376 → 378 → 379 → 380 → 382 → 383 → **384** → 385 → 386 → 387 (ja 381 vessat 379:n jälkeen). Pelihallin koneet (254) ja Violetti (336) pitää olla olemassa.
Muurin rakennusvalikot (327/330 ym.) vaativat yhden scenen: aja **350** ennen niitä ja **349** niiden jälkeen. 349/350 voi ajaa myös komentoriviltä: `Unity.exe -batchmode -nographics -projectPath ... -executeMethod BeatEmUpSetup.RunWallSplitBatch -quit` (Unity-editorin pitää olla kiinni; käytä Start-Process -Wait; log -logFile).

## Testaamatta / avoimet
- Muurin jaon toimivuus pelissä: ovet Muuri↔Kuu (kuuhissi, kuuasema) ja Linnake (juhlahalli); Violetin haku 386:ssa jaon jälkeen.
- Kaikki Vegas-valikot ajamatta: sijainnit/koot (Sohvi, bändi, Mary/Liz, koneet, pöydät) ovat arvioita kuvista.
- Melvinin ja Donin arvot ovat arvauksia; hurt/fall-äänet väliaikaisia; Donilla ei suojausta.
- Don-trio: tulkinta "4 Tonya = Donien vartijat" (ei erillisiä); katsottava onko oikein.
- WC:n bäkkärin ovi → bäkkäri tehty (387); panttilainaamon ovi ja kadun jatko oikealle ilman kohdetta. Kasinon pokeri/strip/pomo, Vegasin kopterilento vankilasta puuttuvat.
- Käyttämätön `vegas_palmut_uusi.png`.
- Älä commitoi `output/`-kansion raakakuvia/videoita (esim. `output/imagegen/vegas-strip-baari-tyo/iso-baari-raaka-5120x1844.png`, `jatko-raaka-5120x1844.png`).

## Säännöt (muistista)
Commit/push vain pyydettäessä; älä generoi kuvia itse (käyttäjä toimittaa); ei PlayStation/Sony-viittauksia; älä pudota videon frameja omin päin (kerro jos jotain skipataan); ruoka vain kaupasta; pomojen vaikeus Deaconin tasolle.

## Seuraavaksi (ehdotus)
1. Käyttäjä ajaa Vegas-valikot ja testaa (kuvakaappaukset → säädöt).
2. Tarkista Muurin jako pelissä (ovet, Violetti, Linnake/Kuu-lataus).
3. Muistit: `don-pomo-vegas.md`, `vegas-vessat.md` (bäkkäri, 387), `scene-split-levels.md` voisi päivittää (Kuu/Linnake jaettu).
