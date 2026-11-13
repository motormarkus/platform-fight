$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $root 'kasino-layer-utils.ps1')
$out=Join-Path $root 'kasino-sisatilat-baari-parallaksi-3tasoa'
$old=Join-Path $root 'kasino-sisatilat-vasen-parallaksi-3tasoa'
$names=@('01-kaukotausta','02-keskitaso','03-etuala')
$motion=[System.Drawing.Bitmap]::new(2560,461)
$g=[System.Drawing.Graphics]::FromImage($motion)
$shift=@(-8,-24,0)
$metadata=@()
for($i=0;$i -lt 3;$i++){
 $left=[System.Drawing.Bitmap]::FromFile((Join-Path $out ($names[$i]+'-baari-5120x1844.png')))
 $right=[System.Drawing.Bitmap]::FromFile((Join-Path $old ($names[$i]+'-vasen-5120x1844.png')))
 if($left.Width -ne 5120 -or $left.Height -ne 1844){throw 'Wrong exported dimensions'}
 $lp=Resize-Canvas $left 1280 461;$rp=Resize-Canvas $right 1280 461
 $g.DrawImageUnscaled($lp,$shift[$i],0);$g.DrawImageUnscaled($rp,1280+$shift[$i],0)
 $next=[System.Drawing.Bitmap]::FromFile((Join-Path $root ('kasino-sisatilat-parallaksi-3tasoa/'+$names[$i]+'-5120x1844.png')))
 $np=Resize-Canvas $next 1280 461
 $g.DrawImageUnscaled($np,2560+$shift[$i],0)
 $np.Dispose();$next.Dispose()
 $metadata+=@{file=$names[$i]+'-baari-5120x1844.png';width=5120;height=1844;sortingOrder=$i;transparent=($i -gt 0);pivot=@(0.5,0.5);suggestedRelativeMotion=@(0.2,0.5,1.0)[$i];join=@{newEdge='right';existingEdge='left';existingFile='../kasino-sisatilat-vasen-parallaksi-3tasoa/'+$names[$i]+'-vasen-5120x1844.png'};positionRelativeToExistingInPixels=@(-5120,0);positionRelativeToElevatorPanelInPixels=@(-10240,0)}
 $lp.Dispose();$rp.Dispose();$left.Dispose();$right.Dispose()
}
$motion.Save((Join-Path $out 'liike-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose();$motion.Dispose()
$metadata|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $out 'tasot.json')
$readme=@'
KASINON BAARI JA RUOKAILUTILA – VASEN JATKO KOLMEEN KERROKSEEN

01-kaukotausta-baari-5120x1844.png: kaukainen ruokailusali, pöydät ja loosit, kasinon arkkitehtuuri ja katto. Peittävä.
02-keskitaso-baari-5120x1844.png: ruokailupöydät, tuolit, kattovalaisin, pylväät ja kasvi. Ympäristö aidosti läpinäkyvä.
03-etuala-baari-5120x1844.png: pitkä baaritiski, pullohylly, jakkarat ja kävelylattia. Salin alue läpinäkyvä. Noin 35 % alareunasta avointa kävelyalaa.

SIJOITTELU VASEMMALTA OIKEALLE
Uusi baaripala → aiempi baccarat/rulettipala → alkuperäinen hissipala.
Uuden oikea reuna liittyy baccarat/rulettipalan vasempaan reunaan. Kolme kerrosta vastaavat numeroittain aiemman kasino-sisatilat-vasen-parallaksi-3tasoa-paketin kerroksia.
Käytä kaikissa samaa pixels-per-unit-arvoa ja pivotia (0.5, 0.5). Baaripalan keskipiste on 5120 / pixels-per-unit peliyksikköä baccarat/rulettipalan keskipisteen vasemmalla puolella, Y-koordinaatti sama. Säilytä läpinäkyvät marginaalit.
Piirtojärjestys takaa eteen: 01, 02, 03. Parallaksin suhteelliset lähtöarvot: 0.2, 0.5, 1.0.

ESIKATSELUT
baari-koottu-esikatselu.png: vain uusi baaripala.
koottu-liitos-esikatselu.png: baari vasemmalla, baccarat/rulettipala oikealla.
liitos-lahikuva.png: liitoskohta läheltä.
liike-liitos-esikatselu.png: kerroksille eri sivuttaissiirtymät.
koko-kasino-esikatselu.png: kaikki kolme tähän mennessä tehtyä aluetta yhdessä.
Kerroskohtaisissa esikatseluissa harmaa on läpinäkyvyyden havainnollistus. Harmaata ei ole varsinaisissa pelikerroksissa.

TOTEUTUS
Jokainen pelikerros on 5120 × 1844 PNG. Tiedostot on suurennettu noin 2K:n generointikoosta.
Kuvat luotu sisäänrakennetulla image_gen-työkalulla, kehotteet generointiohjeet.txt. Etualan korkeus kohdistettu. Kaukotaustan liitos käyttää viereisen kuvan peilattua reunakaistaletta ja sisältöä seuraavaa kapeaa saumaa; lattian sävy sovitettu paikallisesti.
Aikaisempia pelipaloja ei ole muutettu. Tarkistukset tiedostossa tarkistus.txt.
'@
Set-Content -LiteralPath (Join-Path $out 'LUE-MINUT.txt') -Value $readme
$overview=[System.Drawing.Bitmap]::new(3840,461)
$og=[System.Drawing.Graphics]::FromImage($overview)
$previewFiles=@((Join-Path $out 'baari-koottu-esikatselu.png'),(Join-Path $old 'vasen-koottu-esikatselu.png'),(Join-Path $root 'kasino-sisatilat-parallaksi-3tasoa/koottu-esikatselu.png'))
for($i=0;$i -lt 3;$i++){$im=[System.Drawing.Bitmap]::FromFile($previewFiles[$i]);$og.DrawImageUnscaled($im,1280*$i,0);$im.Dispose()}
$overview.Save((Join-Path $out 'koko-kasino-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$og.Dispose();$overview.Dispose()
Add-Content -LiteralPath (Join-Path $out 'tarkistus.txt') -Value @('Visual review: assembled new/previous panels, separate far-background join and seam close-up. Foreground border and floor levels are continuous.','Saved RGBA boundary values match for all1844 rows in each of the three corresponding layers.','Existing files are only read as references throughout this workflow.')
Get-ChildItem -LiteralPath $out -Filter '*5120x1844.png'|Select-Object Name,Length
