$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
Add-Type -TypeDefinition @'
using System;
public static class WorksiteFinalCheck {
 public static int H(byte[] a,byte[] b){int n=0;for(int y=0;y<1844;y++)for(int c=0;c<4;c++)if(a[(y*5120+5119)*4+c]!=b[y*5120*4+c])n++;return n;}
 public static int V(byte[] upper,byte[] lower){int n=0;for(int i=0;i<5120*4;i++)if(upper[1843*5120*4+i]!=lower[i])n++;return n;}
 public static string Alpha(byte[] d,bool ground){int clear=0,hidden=0,holes=0;for(int i=0;i<d.Length;i+=4){if(d[i+3]==0){clear++;if(d[i]!=0||d[i+1]!=0||d[i+2]!=0)hidden++;}if(ground&&i>=1300*5120*4&&d[i+3]!=255)holes++;}if(hidden+holes>0)throw new Exception("Alpha error");return "clear="+clear+", hiddenRGB="+hidden+", floorHoles="+holes;}
}
'@
$root=$PSScriptRoot
$dir=Join-Path $root 'vegas-strip-tyomaa-kokonaisuus';[void][IO.Directory]::CreateDirectory($dir)
$names=@('vegas-strip-tyomaa','vegas-strip-tyomaa-jatko','vegas-strip-tyomaa-paaty','vegas-strip-tyomaa-ylajatko')
$ims=@();$smalls=@();$data=@();$report=@()
foreach($n in $names){$im=[Drawing.Bitmap]::new((Join-Path $root "$n\$n-5120x1844.png"));if($im.Width -ne 5120 -or $im.Height -ne 1844){throw 'Wrong image size'};$ims+=,$im;$smalls+=,(Resize-Canvas $im 1280 461);$data+=,(Pixel-Data $im)}
$h1=[WorksiteFinalCheck]::H($data[0],$data[1]);$h2=[WorksiteFinalCheck]::H($data[1],$data[2]);$v=[WorksiteFinalCheck]::V($data[3],$data[2])
if($h1+$h2+$v -ne 0){throw "Seam mismatch: $h1 $h2 $v"}
$report+="All deliverables 5120x1844 RGBA. Horizontal join 1=$h1; horizontal join 2=$h2; vertical join=$v channel differences."
for($i=0;$i -lt 4;$i++){$report+="$($names[$i]): $([WorksiteFinalCheck]::Alpha($data[$i],$i -lt 3))"}
$street=[Drawing.Bitmap]::new(3840,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($street)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[Drawing.Bitmap]::new((Join-Path $root ('las-vegas-parallaksi-v2\'+$name)));$small=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($small,0,0);$g.DrawImageUnscaled($small,2560,0);$small.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX);$g.DrawImageUnscaled($small,1280,0);$small.Dispose();$layer.Dispose()
}
for($i=0;$i -lt 3;$i++){$g.DrawImageUnscaled($smalls[$i],$i*1280,0)};$g.Dispose()
$street.Save((Join-Path $dir 'katutaso-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$pair=$street.Clone([Drawing.Rectangle]::new(1280,0,2560,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$pair.Save((Join-Path $root 'vegas-strip-tyomaa-paaty\tyomaa-ja-jatko-parallakseilla.png'),[Drawing.Imaging.ImageFormat]::Png);$pair.Dispose()
$single=$street.Clone([Drawing.Rectangle]::new(2560,0,1280,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$single.Save((Join-Path $root 'vegas-strip-tyomaa-paaty\parallakseilla-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$single.Dispose()
$street.Dispose()
$plain=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($plain);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($smalls[1],0,0);$g.DrawImageUnscaled($smalls[2],1280,0);$g.Dispose()
$plain.Save((Join-Path $root 'vegas-strip-tyomaa-paaty\liitos-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$plain.Dispose()
$close=[Drawing.Bitmap]::new(1024,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($ims[1],[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.Rectangle]::new(4608,0,512,1844),[Drawing.GraphicsUnit]::Pixel);$g.DrawImage($ims[2],[Drawing.Rectangle]::new(512,0,512,1844),[Drawing.Rectangle]::new(0,0,512,1844),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $root 'vegas-strip-tyomaa-paaty\liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$prompts=@();foreach($f in @('vegas-strip-tyomaa-jatko-generointiohje.txt','vegas-strip-tyomaa-paaty-generointiohje.txt','vegas-strip-tyomaa-paaty-pilarikorjaus.txt','vegas-strip-tyomaa-ylajatko-generointiohje.txt','vegas-strip-tyomaa-ylajatko-pilarikorjaus.txt')){$prompts+="`n=== $f ===`n";$prompts+=(Get-Content -LiteralPath (Join-Path $root $f) -Raw)}
$prompts | Set-Content -LiteralPath (Join-Path $dir 'generointiohjeet.txt') -Encoding UTF8
@'
Las Vegasin tyomaan uudet palat.
1. vegas-strip-tyomaa-jatko: koko ruudun levyinen tyomaa, aiemman vegas-strip-tyomaa-palan oikealle puolelle.
2. vegas-strip-tyomaa-paaty: edellisen jatkopalan oikealle puolelle. Tyomaa loppuu kuvan puolivalin tuntumassa ja katu jatkuu oikealle.
3. vegas-strip-tyomaa-ylajatko: suoraan paatypalan YLAPUOLELLE, sama x-sijainti. Ei muiden katupalojen paalle.
Kaikki PNG:t 5120 x 1844 ja oikeasti lapinakyva tausta. Sama PPU ja skaala; vierekkaisten palojen keskikohtien vali 5120/PPU vaakaan tai 1844/PPU pystyyn, kun pivotit ovat samat.
Katutason esikatseluun lisatty aiemmat Vegas-parallaksit vain havainnollistamista varten. Varsinaisiin pelikerroksiin niita ei ole yhdistetty.
Liitosten reuna-arvot tarkistettu kaikista RGBA-kanavista. Lisaeksi tarkistettu esikatselut ja rakennuslinjojen kohdistus.
Tuotanto: sisaanrakennettu image_gen, paikalliset liitoskorjaukset ja mekaaninen kohdistus. 5120 x 1844 on skaalattu vientikoko, ei natiivin generoinnin koko. Promptit tiedostossa generointiohjeet.txt.
Ei Unity-scenejen tai asetusten muutoksia.
'@ | Set-Content -LiteralPath (Join-Path $dir 'LUE-MINUT.txt') -Encoding UTF8
$readmePath=Join-Path $root 'vegas-strip-tyomaa-paaty\LUE-MINUT.txt'
$readme=Get-Content -LiteralPath $readmePath -Raw
$readme=$readme.Replace('Aseta vasen reuna vegas-strip-tyomaa-5120x1844.png-kuvan oikeaan reunaan','Aseta vasen reuna vegas-strip-tyomaa-jatko-5120x1844.png-kuvan oikeaan reunaan')
$readme | Set-Content -LiteralPath $readmePath -Encoding UTF8
$report | Set-Content -LiteralPath (Join-Path $dir 'tarkistus.txt') -Encoding UTF8
$report | Set-Content -LiteralPath (Join-Path $root 'vegas-strip-tyomaa-paaty\tarkistus.txt') -Encoding UTF8
foreach($im in $ims){$im.Dispose()};foreach($small in $smalls){$small.Dispose()}
$report | Write-Output
