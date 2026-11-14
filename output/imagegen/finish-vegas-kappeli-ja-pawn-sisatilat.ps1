$ErrorActionPreference='Stop'
$rooms=@(
 @{Slug='vegas-kappeli';Title='Vegasin hääkappelin sisätila';Content='Vasemmassa päädyssä pariovet sulkevan, viiston päätyseinän tasossa. Penkkirivit jatkuvat kohti oikean puoliskon alttaria. Oikean päädyn seinä sulkee tilan. Kaksi arkkitehtonista kaari-ikkunaa vasemmassa kuvassa: kaikki aukot ovat aidosti läpinäkyviä, jäljellä vain kehykset ja ohuet metallipuitteet. Lattiassa vapaa kävelyala. Ei ihmisiä.';Prompts=@('vegas-kappeli-sisa-vasen-generointiohje.txt','vegas-kappeli-sisa-vasen-ovet-ikkunat-korjaus.txt','vegas-kappeli-sisa-ikkunapuite-generointiohje.txt','vegas-kappeli-sisa-oikea-v2-generointiohje.txt')},
 @{Slug='vegas-panttilainaamo';Title='Vegasin panttilainaamon sisätila';Content='Vasemmassa päädyssä pariovet sulkevan, viiston päätyseinän tasossa. Pitkä korutiski jatkuu kuvien liitoksen yli, ja päättyy oikealla kassaan ja vaakaan. Nähtävillä koruja, kelloja, kitaroita, basso, rullalautoja, kameroita, pieniä työkalulaukkuja ja Vegas-koristeita. Oikean päädyn seinä sulkee tilan. Avoin puulattia, ei ihmisiä. Panttilainaamossa ei ole erillisiä ulkoikkunoita; ovien lasitus ja koruvitriini ovat peittäviä.';Prompts=@('vegas-panttilainaamo-sisa-vasen-generointiohje.txt','vegas-panttilainaamo-sisa-vasen-ovet-korjaus.txt','vegas-panttilainaamo-sisa-oikea-generointiohje.txt','vegas-panttilainaamo-sisa-liitos-generointiohje.txt')}
)
foreach($room in $rooms){
 $slug=$room.Slug;$dir=Join-Path $PSScriptRoot ($slug+'-sisatilat-v2');$work=Join-Path $PSScriptRoot ($slug+'-sisa-tyo')
 $parts=@('Mode: built-in image_gen / sisäänrakennettu kuvatyökalu')
 foreach($name in $room.Prompts){$parts+=@("`n--- $name ---`n",(Get-Content -LiteralPath (Join-Path $PSScriptRoot $name) -Raw))}
 $parts | Set-Content -LiteralPath (Join-Path $dir 'generointiohjeet.txt') -Encoding UTF8
 $ln=(Get-Content -LiteralPath (Join-Path $work 'vasen-v2-natiivikoko.txt') -Raw).Trim()
 $rn=(Get-Content -LiteralPath (Join-Path $work 'oikea-natiivikoko.txt') -Raw).Trim()
 @"
$($room.Title), kaksi vierekkäin liitettävää pelikuvaa

1. $slug-vasen-5120x1844.png
2. $slug-oikea-5120x1844.png

Molempien koko 5120 x 1844 pikseliä, PNG. Aseta tässä järjestyksessä samoilla PPU-, pivot-, skaala- ja korkeusasetuksilla ilman väliä.
$($room.Content)

Liitoksen seinälistat, kalusteet ja lattian korkeus kohdistettu; reunapikselit täsmäävät kaikilla 1844 rivillä jokaisessa RGBA-kanavassa. Kuvapari tarkistettu myös visuaalisesti. Aiemmat versiot ja Unityn scene-/tuontiasetukset jätetty ennalleen.
Esikatselun tumma ikkunaväri tai erillisen läpinäkyvyysesikatselun ruudukko ovat vain esikatselutaustaa. Niitä ei ole tallennettu pelikäyttöisen kappelikuvan ikkuna-aukkoihin.

Tekotapa: sisäänrakennettu image_gen. Vasemman muokatun kuvan natiivi koko $ln; oikean $rn sisältäen apuliuskan. Vientikoko 5120 x 1844 on skaalattu, ei natiivisti generoitu 5K-kuva. Oikean apuliuska poistettu; vaakalinjat kohdistettu, lattian pintaa jatkettu aiemmasta palasta. Kappelin ikkunan puitteet muokattu kuvatyökalulla ja jäljelle jäänyt ruutujen täyttö poistettu alfamaskilla. Panttilainaamon tiskin liitos viimeistelty paikallisella kuvatyökalumuokkauksella. Kehyksiä, puitteita ja sisältöä ei rasteroitu tekstiruudukosta tai vektoripaikkamerkeistä.

Generointiohjeet: generointiohjeet.txt
Tarkistukset: tarkistus.txt
"@ | Set-Content -LiteralPath (Join-Path $dir 'LUE-MINUT.txt') -Encoding UTF8
 Get-ChildItem -LiteralPath $dir -Filter '*.png' | Select-Object Name,Length
}
