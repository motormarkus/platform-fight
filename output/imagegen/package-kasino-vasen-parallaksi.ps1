$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $root 'kasino-layer-utils.ps1')
$out=Join-Path $root 'kasino-sisatilat-vasen-parallaksi-3tasoa'
$old=Join-Path $root 'kasino-sisatilat-parallaksi-3tasoa'
$names=@('01-kaukotausta','02-keskitaso','03-etuala')
$motion=[System.Drawing.Bitmap]::new(2560,461)
$g=[System.Drawing.Graphics]::FromImage($motion)
$shift=@(-8,-24,0)
$metadata=@()
for($i=0;$i -lt 3;$i++){
 $left=[System.Drawing.Bitmap]::FromFile((Join-Path $out ($names[$i]+'-vasen-5120x1844.png')))
 $right=[System.Drawing.Bitmap]::FromFile((Join-Path $old ($names[$i]+'-5120x1844.png')))
 if($left.Width -ne 5120 -or $left.Height -ne 1844){throw 'Wrong exported dimensions'}
 $lp=Resize-Canvas $left 1280 461;$rp=Resize-Canvas $right 1280 461
 $g.DrawImageUnscaled($lp,$shift[$i],0);$g.DrawImageUnscaled($rp,1280+$shift[$i],0)
 $metadata+=@{file=$names[$i]+'-vasen-5120x1844.png';width=5120;height=1844;sortingOrder=$i;transparent=($i -gt 0);pivot=@(0.5,0.5);suggestedRelativeMotion=@(0.2,0.5,1.0)[$i];join=@{newEdge='right';existingEdge='left';existingFile='../kasino-sisatilat-parallaksi-3tasoa/'+$names[$i]+'-5120x1844.png'};positionRelativeToExistingInPixels=@(-5120,0)}
 $lp.Dispose();$rp.Dispose();$left.Dispose();$right.Dispose()
}
$motion.Save((Join-Path $out 'liike-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$motion.Dispose()
$metadata|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $out 'tasot.json')
$readme=@'
KASINON VASEN JATKO – KOLME KERROSTA

01-kaukotausta-vasen-5120x1844.png: kaukainen kasinosali, pieniä pelipöytiä ja automaatteja. Peittävä.
02-keskitaso-vasen-5120x1844.png: baccarat- ja rulettipöytiä, pylväät, palmut ja valaisin. Aito läpinäkyvyys.
03-etuala-vasen-5120x1844.png: lähimmät baccarat- ja rulettipöydät sekä noin 35 % avointa kävelylattiaa. Taustan alue läpinäkyvä.

LIITTÄMINEN
Aseta uusi pala VANHAN VASEMMALLE puolelle. Uuden oikea reuna liittyy vanhan vasempaan reunaan. Kerrokset vastaavat numeroittain aiemman kasino-sisatilat-parallaksi-3tasoa-paketin kerroksia.
Sama pixels-per-unit ja pivot (0.5, 0.5) kaikissa. Uuden palan keskipiste on 5120 / pixels-per-unit peliyksikköä vanhan keskipisteen vasemmalla puolella. Y-koordinaatti sama. Säilytä läpinäkyvät marginaalit.
Piirtojärjestys takaa eteen: 01, 02, 03. Parallaksin aiemmat suhteelliset lähtöarvot 0.2, 0.5, 1.0 sopivat myös uusille paloille.

ESIKATSELUT
koottu-liitos-esikatselu.png: uusi vasemmalla, aiempi hissipala oikealla.
vasen-koottu-esikatselu.png: vain uuden palan kolme kerrosta yhdessä.
liitos-lahikuva.png: liitosalue läheltä.
liike-liitos-esikatselu.png: kerroksille eri sivuttaissiirtymät.
Kerroskohtaisissa esikatseluissa harmaa alue merkitsee läpinäkyvyyttä; harmaata ei ole varsinaisissa PNG-kuvissa.

KOKO JA TOTEUTUS
Jokainen pelikuva on 5120 × 1844 PNG. Nämä ovat suurennettuja vientitiedostoja noin 2K:n generoinneista.
Kuvat luotu sisäänrakennetulla image_gen-työkalulla, kehotteet generointiohjeet.txt. Reunat sovitettu, etualan korkeus kohdistettu ja läpinäkyvyys tarkistettu. Kaukotaustan liitosalue käyttää viereisen kuvan peilattua reunakaistaletta sekä sisältöä seuraavaa kapeaa saumaa.
Alkuperäisiä oikean puolen tiedostoja ei muutettu. Vasemman reunan jatko jää avoimeksi.
'@
Set-Content -LiteralPath (Join-Path $out 'LUE-MINUT.txt') -Value $readme
Add-Content -LiteralPath (Join-Path $out 'tarkistus.txt') -Value @('Visual review: all three assembled layers, separate far-background join and close seam crop. Wide crossfade ghosting removed with a narrow content-aware quilting path.','Saved boundary RGBA values match for all1844 rows in each corresponding layer. Foreground gold border and floor height are visually continuous.','Existing right-hand asset files are read-only inputs throughout this workflow.')
Get-ChildItem -LiteralPath $out -Filter '*5120x1844.png'|Select-Object Name,Length
