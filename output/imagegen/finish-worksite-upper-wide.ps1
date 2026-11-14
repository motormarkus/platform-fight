$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class WorksiteUpperQA {
 public static int H(byte[] l,byte[] r){int e=0;for(int y=0;y<1844;y++)for(int c=0;c<4;c++)if(l[(y*5120+5119)*4+c]!=r[y*5120*4+c])e++;return e;}
 public static int V(byte[] t,byte[] b){int e=0;for(int i=0;i<5120*4;i++)if(t[1843*5120*4+i]!=b[i])e++;return e;}
 public static string Alpha(byte[] p){int clear=0,hidden=0;for(int i=0;i<p.Length;i+=4){if(p[i+3]==0){clear++;if(p[i]!=0||p[i+1]!=0||p[i+2]!=0)hidden++;}}if(hidden>0)throw new Exception("Hidden RGB");return "clear pixels="+clear+", hidden RGB="+hidden;}
}
'@
$folder=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylatasot'
$upperNames=@('01-alku','02-keskiosa','03-paaty')
$lowerNames=@('vegas-strip-tyomaa','vegas-strip-tyomaa-jatko','vegas-strip-tyomaa-paaty')
$upper=@();$lower=@();$ud=@();$ld=@();$us=@();$ls=@();$lines=@()
for($i=0;$i -lt 3;$i++){
 $u=[Drawing.Bitmap]::new((Join-Path $folder ($upperNames[$i]+'-5120x1844.png')))
 $b=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot ($lowerNames[$i]+'\'+$lowerNames[$i]+'-5120x1844.png')))
 if($u.Width -ne 5120 -or $u.Height -ne 1844){throw 'Wrong export size'}
 $upper+=,$u;$lower+=,$b;$ud+=,(Pixel-Data $u);$ld+=,(Pixel-Data $b);$us+=,(Resize-Canvas $u 1280 461);$ls+=,(Resize-Canvas $b 1280 461)
 $v=[WorksiteUpperQA]::V($ud[$i],$ld[$i]);if($v -ne 0){throw 'Vertical mismatch'}
 $lines+="$($upperNames[$i]): 5120x1844 RGBA; vertical seam channel differences=$v; $([WorksiteUpperQA]::Alpha($ud[$i]))"
}
for($i=0;$i -lt 2;$i++){$h=[WorksiteUpperQA]::H($ud[$i],$ud[$i+1]);if($h -ne 0){throw 'Horizontal mismatch'};$lines+="Upper horizontal seam $($i+1): channel differences=$h"}
$originalThird=Join-Path $PSScriptRoot 'vegas-strip-tyomaa-ylajatko\vegas-strip-tyomaa-ylajatko-5120x1844.png'
$h1=(Get-FileHash -LiteralPath $originalThird -Algorithm SHA256).Hash;$h2=(Get-FileHash -LiteralPath (Join-Path $folder '03-paaty-5120x1844.png') -Algorithm SHA256).Hash
if($h1 -ne $h2){throw 'Existing upper end tile changed'};$lines+="Existing upper end tile byte-identical SHA256=$h1"
$grid=[Drawing.Bitmap]::new(3840,922,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($grid);$g.Clear([Drawing.Color]::FromArgb(34,40,58))
for($i=0;$i -lt 3;$i++){$g.DrawImageUnscaled($us[$i],$i*1280,0);$g.DrawImageUnscaled($ls[$i],$i*1280,461)};$g.Dispose()
$grid.Save((Join-Path $folder 'koko-tyomaa-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$top=$grid.Clone([Drawing.Rectangle]::new(0,0,3840,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$top.Save((Join-Path $folder 'ylatasot-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$top.Dispose();$grid.Dispose()
for($i=0;$i -lt 3;$i++){
 $pair=[Drawing.Bitmap]::new(1280,922,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($pair);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($us[$i],0,0);$g.DrawImageUnscaled($ls[$i],0,461);$g.Dispose();$pair.Save((Join-Path $folder ($upperNames[$i]+'-alakuvan-kanssa.png')),[Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
}
$close=[Drawing.Bitmap]::new(1024,400,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($upper[0],[Drawing.Rectangle]::new(0,0,512,400),[Drawing.Rectangle]::new(4608,1444,512,400),[Drawing.GraphicsUnit]::Pixel);$g.DrawImage($upper[1],[Drawing.Rectangle]::new(512,0,512,400),[Drawing.Rectangle]::new(0,1444,512,400),[Drawing.GraphicsUnit]::Pixel);$g.Dispose();$close.Save((Join-Path $folder 'vinotuen-liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$prompts=@()
foreach($name in @('01-alku-generointiohje.txt','02-keskiosa-generointiohje.txt')){$prompts+="`n=== $name ===`n";$prompts+=(Get-Content -LiteralPath (Join-Path $folder $name) -Raw)}
foreach($name in @('vegas-strip-tyomaa-ylajatko-generointiohje.txt','vegas-strip-tyomaa-ylajatko-pilarikorjaus.txt')){$prompts+="`n=== Existing end tile: $name ===`n";$prompts+=(Get-Content -LiteralPath (Join-Path $PSScriptRoot $name) -Raw)}
$prompts | Set-Content -LiteralPath (Join-Path $folder 'generointiohjeet.txt') -Encoding UTF8
@'
TYOMAAN YLAJATKO KOKO LEVEYDELLE

Kolme 5120 x 1844 RGBA PNG -kuvaa, yhteisleveys 15360 pikselia.

Ylarivi vasemmalta oikealle:
01-alku-5120x1844.png | 02-keskiosa-5120x1844.png | 03-paaty-5120x1844.png

Niiden alle samassa jarjestyksessa olemassa olevat katutason kuvat:
vegas-strip-tyomaa | vegas-strip-tyomaa-jatko | vegas-strip-tyomaa-paaty

Kaikki samalla skaalalla ja PPU-arvolla. Samalla keskitettylla pivotilla vaakasuora keskipistevali on 5120/PPU, yla- ja alakuvan pystysuora vali 1844/PPU.
Ylapalojen valiset vaakasaumat ja kaikki kolme alareunan pystysaumaa on tarkistettu kaikista RGBA-kanavista. Liittyvien reunojen kanavaerot 0. Myos rakennuslinjat ja saumojen yli kulkevat vinotuet on tarkistettu esikatseluista.
Tausta on oikeasti lapinakyva. Esikatselujen siniharmaa tausta on vain tarkistusta varten, ei osa pelikuvia.

Alku- ja keskipalat ovat uusia. Paatypala on tasmalleen aiemmin tehty vegas-strip-tyomaa-ylajatko-5120x1844.png, muuttamattomana kopiona. Alakuvia ei muokattu.

Tuotanto: sisaanrakennettu image_gen-kuvatyokalu. Uusien kuvien natiivi 1971 x 798 sisalsi ala- ja oikeassa reunassa liitosviitteet. Viitteet rajattu pois ja varsinainen pelikuva skaalattu 5120 x 1844 vientikokoon. Kuvien geometria kohdistettu olemassa oleviin tasoihin. Alimman avoimen rakennevalin pilarit ja vinotuet jatkettu mekaanisesti alakuvien olemassa olevista rakennepikseleista samoilla linjoilla, jotta ne liittyvat ilman katkoksia. Ei natiivia 5K-generointia.
Ei Unity-scenejen tai asetusten muutoksia. Generointiohjeet tiedostossa generointiohjeet.txt.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
$lines | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
foreach($im in ($upper+$lower+$us+$ls)){$im.Dispose()}
$lines | Write-Output
