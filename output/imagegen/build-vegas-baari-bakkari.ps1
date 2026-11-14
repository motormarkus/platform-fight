$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-baari-bakkari'
[void][System.IO.Directory]::CreateDirectory($outDir)
$attach='C:\Users\marku\.codex\codex-remote-attachments\01a19137-3c84-7223-9249-58aae914b472\127E4683-8B49-41EC-8B8E-1124A35338D0'
$logo=[System.Drawing.Bitmap]::new((Join-Path $attach '1-Kuva-1.jpg'))
$portrait=[System.Drawing.Bitmap]::new((Join-Path $attach '2-Kuva-2.jpg'))
$north=[System.Drawing.Bitmap]::new((Join-Path $attach '3-Kuva-3.jpg'))
$raw=[System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-fe6cc7c8-72d3-427e-b281-ca0a00f80572.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$final=Resize-Canvas $raw 5120 1844
$before=Pixel-Data $final
# Preserve the user's actual portrait and wordmark, rather than redrawing them.
$posterWidth=$portrait.Width
$headerHeight=[int][Math]::Round($posterWidth*$logo.Height/[double]$logo.Width)
$portraitTop=200
$poster=[System.Drawing.Bitmap]::new($posterWidth,($headerHeight+$portrait.Height-$portraitTop),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($poster);$g.Clear([System.Drawing.Color]::Black)
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$g.DrawImage($logo,[System.Drawing.Rectangle]::new(0,0,$posterWidth,$headerHeight),[single]0,[single]0,[single]$logo.Width,[single]$logo.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($portrait,[System.Drawing.Rectangle]::new(0,$headerHeight,$posterWidth,($portrait.Height-$portraitTop)),[single]0,[single]$portraitTop,[single]$portrait.Width,[single]($portrait.Height-$portraitTop),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.Dispose()
$poster.Save((Join-Path $outDir 'hms-keelhaul-juliste.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$north.Save((Join-Path $outDir 'northforge-juliste.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$scaleX=5120/[double]$raw.Width;$scaleY=1844/[double]$raw.Height
function Poster-Rect([double]$x,[double]$y,[double]$w,[double]$h){
 return [System.Drawing.Rectangle]::new([int][Math]::Round($x*$scaleX),[int][Math]::Round($y*$scaleY),[int][Math]::Round($w*$scaleX),[int][Math]::Round($h*$scaleY))
}
$hmsRect=Poster-Rect 423 30 271 380
$northRect=Poster-Rect 748 77 282 280
$g=[System.Drawing.Graphics]::FromImage($final);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
function Paste-Print([System.Drawing.Bitmap]$src,[System.Drawing.Rectangle]$rect){
 $g.FillRectangle([System.Drawing.Brushes]::Black,$rect)
 $fit=[Math]::Min($rect.Width/[double]$src.Width,$rect.Height/[double]$src.Height)
 $width=[int][Math]::Round($src.Width*$fit);$height=[int][Math]::Round($src.Height*$fit)
 $destination=[System.Drawing.Rectangle]::new(($rect.X+[int][Math]::Floor(($rect.Width-$width)/2.0)),($rect.Y+[int][Math]::Floor(($rect.Height-$height)/2.0)),$width,$height)
 $g.DrawImage($src,$destination,[single]0,[single]0,[single]$src.Width,[single]$src.Height,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
}
Paste-Print $poster $hmsRect
Paste-Print $north $northRect
$g.Dispose();$attrs.Dispose()
$path=Join-Path $outDir 'vegas-baari-bakkari-5120x1844.png'
$final.Save($path,[System.Drawing.Imaging.ImageFormat]::Png)
$data=Pixel-Data $final
Add-Type -TypeDefinition @'
using System;
using System.Drawing;
public static class BackstageCheck {
 public static string Verify(byte[] before,byte[] after,Rectangle a,Rectangle b){
  int alpha=0,outside=0,changed=0;
  for(int y=0;y<1844;y++)for(int x=0;x<5120;x++){
   int i=(y*5120+x)*4;bool different=false;for(int c=0;c<4;c++)if(before[i+c]!=after[i+c])different=true;
   if(different){changed++;if(!a.Contains(x,y)&&!b.Contains(x,y))outside++;}
   if(after[i+3]!=255)alpha++;
  }
  if(alpha+outside>0)throw new Exception("Non-opaque="+alpha+", changed outside poster frames="+outside);
  return "5120x1844 opaque. Actual supplied artwork composited in two poster frames. Pixels changed outside those frames=0; changed poster pixels="+changed+".";
 }
}
'@
$report=[BackstageCheck]::Verify($before,$data,$hmsRect,$northRect)
$small=Resize-Canvas $final 1280 461
$small.Save((Join-Path $outDir 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png);$small.Dispose()
$posters=$final.Clone([System.Drawing.Rectangle]::new(975,0,1640,1065),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$posters.Save((Join-Path $outDir 'julisteet-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png);$posters.Dispose()
$posterSize='{0}x{1}' -f $poster.Width,$poster.Height
$logo.Dispose();$portrait.Dispose();$north.Dispose();$raw.Dispose();$poster.Dispose();$final.Dispose()
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-bakkari-generointiohje.txt') -Destination (Join-Path $outDir 'generointiohje.txt')
@"
Vegasin baarin bäkkäri

Pelikuva: vegas-baari-bakkari-5120x1844.png. Koko 5120 x 1844 px, täysin peittävä PNG.
Yksi kokonainen huone: ovi vasemmassa päädyssä, sulkevat seinät, bändijulisteet, sohva, seinäkitara ja levyplaketti, peilipöytä sekä siististi seinän vieressä olevat kuljetuskotelot. Alhaalla reilu vapaa kävelylattia. Huoneessa ei ole eläviä hahmoja.

HMS KEELHAUL -julisteessa käytetään käyttäjän toimittamaa henkilökuvaa ja sen YLÄPUOLELLA valkoista HMS KEELHAUL -logoa. Alkuperäisen henkilökuvan vanha ylälogo poistettu rajaamalla 200 px yläreunasta; yhtäkään bändijäsentä ei rajattu pois. Valkoinen logo skaalattu yläotsakkeeksi. Kokonaisjuliste $posterSize px, mukana myös erillisenä PNG:nä.
NORTHFORGE-julisteessa on käyttäjän toimittama leijona-, kirves- ja tekstigrafiikka. Alkuperäistä kuvaa ei piirretty uudelleen. Se on mukana myös erillisenä PNG:nä.
Julisteet sovitettu kehyksiin kuvasuhde säilyttäen. Mustat reunukset ovat kehyksen sisäpuolella. Muu huone ei muuttunut lähdekuvien sijoittamisen yhteydessä.

Tekotapa: sisäänrakennettu image_gen huoneen ja kalusteiden luomiseen, natiivikuva $native. Huone skaalattu 5120 x 1844 vientikokoon. Varsinaiset toimittamasi julistegrafiikat on asetettu syntyneisiin suoriin kehyksiin alkuperäisistä tiedostoista. Vientikoko ei ole generoinnin natiiviresoluutio.
Unity-scenejä tai aiempia baarikuvia ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native room=$native",'Final=5120x1844',$report,"HMS frame=$hmsRect","Northforge frame=$northRect") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $path
Write-Output $report
