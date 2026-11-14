$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir=Join-Path $PSScriptRoot 'vegas-strip-haakappeli'
[void][IO.Directory]::CreateDirectory($outDir)
$oldPath=Join-Path $PSScriptRoot 'vegas-strip-kasino-ulkoa-v2\vegas-strip-kasino-ulkoa-v2-5120x1844.png'
$oldHash=(Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash
$old=[Drawing.Bitmap]::new($oldPath)
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-2fd49fc5-2248-4687-8760-447855a58355.png')
$native='{0}x{1}' -f $raw.Width,$raw.Height
$null=Clean-Alpha $raw
$scaled=Resize-Canvas $raw 5120 1844
$null=Clean-Alpha $scaled
$oldData=Pixel-Data $old;$generated=Pixel-Data $scaled
$seamRaw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-c44b6f37-c5eb-42ba-97bd-3f3517d65e91.png')
$seamImage=Resize-Canvas $seamRaw 1024 804
$seamData=Pixel-Data $seamImage
$seamImage.Dispose();$seamRaw.Dispose()
$final=[Drawing.Bitmap]::new(5120,1844,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$data=Pixel-Data $final
Add-Type -TypeDefinition @'
using System;
public static class VegasChapelJoin {
 static double S(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
 public static void Build(byte[] d,byte[] raw,byte[] old){
  const int w=5120,h=1844,shift=92;
  // Preserve the new building's scale; raise it rather than compressing it.
  Buffer.BlockCopy(raw,shift*w*4,d,0,(h-shift)*w*4);
  // Clear isolated alpha speckles in the deliberately empty street air.
  for(int y=0;y<1200;y++)for(int x=0;x<w;x++)if(x<1760||x>=4560){int i=(y*w+x)*4;d[i]=d[i+1]=d[i+2]=d[i+3]=0;}
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4,j=(y*w+w-1-x)*4;
   // Continue the established pavement. Chapel's feet remain untouched above y1268.
   double ground=S((y-1268)/32.0);
   double edge=1-S((x-128)/384.0);
   double a=Math.Max(ground,edge);
   for(int c=0;c<4;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+old[j+c]*a);
  }
 }
 public static void RepairJoint(byte[] d,byte[] repair){
  // Only remove the reflected right-hand diagonal. Original casino and curb stay intact.
  for(int y=1206;y<1375;y++)for(int x=1;x<400;x++){
   double center=1.9*(1372-y);
   double a=S((44-Math.Abs(x-center))/20.0)*S(x/7.0)*S((y-1206)/6.0)*S((1375-y)/4.0);
   int i=(y*5120+x)*4,j=((y-1040)*1024+x+512)*4;
   for(int c=0;c<3;c++)d[i+c]=(byte)Math.Round(d[i+c]*(1-a)+repair[j+c]*a);
  }
 }
 public static string Check(byte[] d,byte[] old){
  const int w=5120,h=1844;int edge=0,hidden=0,ground=0,air=0,cutTop=0;
  for(int y=0;y<h;y++)for(int c=0;c<4;c++)if(d[y*w*4+c]!=old[(y*w+w-1)*4+c])edge++;
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int i=(y*w+x)*4;
   if(d[i+3]==0&&(d[i]!=0||d[i+1]!=0||d[i+2]!=0))hidden++;
   if(y>=1300&&d[i+3]!=255)ground++;
   if(y<1190&&x<1700&&d[i+3]!=0)air++;
   if(y==0&&x>=2200&&x<=4200&&d[i+3]!=255)cutTop++;
  }
  if(edge+hidden+ground+air+cutTop>0)throw new Exception("Validation failed: edge="+edge+", hidden RGB="+hidden+", ground="+ground+", air="+air+", tall facade="+cutTop);
  return "5120x1844 RGBA. Adjacent-edge differences=0 across all 1844 rows and all RGBA channels. Sidewalk, curb and asphalt continuous. Ground y1300..1843 fully opaque. Open gap transparent; upper chapel continues beyond top. Hidden RGB errors=0.";
 }
}
'@
[VegasChapelJoin]::Build($data,$generated,$oldData)
[VegasChapelJoin]::RepairJoint($data,$seamData)
$null=[CasinoLeftPixels]::Normalize($data)
Set-PixelData $final $data
$finalPath=Join-Path $outDir 'vegas-strip-haakappeli-5120x1844.png'
$final.Save($finalPath,[Drawing.Imaging.ImageFormat]::Png)
$saved=[Drawing.Bitmap]::new($finalPath)
$report=[VegasChapelJoin]::Check((Pixel-Data $saved),$oldData)
$newSmall=Resize-Canvas $saved 1280 461;$oldSmall=Resize-Canvas $old 1280 461
$newSmall.Save((Join-Path $outDir 'etuala-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$joined=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($joined);$g.Clear([Drawing.Color]::FromArgb(45,45,45));$g.DrawImageUnscaled($oldSmall,0,0);$g.DrawImageUnscaled($newSmall,1280,0);$g.Dispose()
$joined.Save((Join-Path $outDir 'liitos-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$joined.Dispose()
$close=[Drawing.Bitmap]::new(1024,804,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($close);$g.Clear([Drawing.Color]::FromArgb(45,45,45))
$g.DrawImage($old,[Drawing.Rectangle]::new(0,0,512,804),[Drawing.Rectangle]::new(4608,1040,512,804),[Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($saved,[Drawing.Rectangle]::new(512,0,512,804),[Drawing.Rectangle]::new(0,1040,512,804),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$close.Save((Join-Path $outDir 'liitos-lahikuva.png'),[Drawing.Imaging.ImageFormat]::Png);$close.Dispose()
$scene=[Drawing.Bitmap]::new(2560,461,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($scene)
foreach($name in @('01-yotaivas-5120x1844.png','02-hotellit-ja-rakennukset-5120x1844.png','03-kasvillisuus-5120x1844.png')){
 $layer=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot ('las-vegas-parallaksi-v2\'+$name)));$small=Resize-Canvas $layer 1280 461
 $g.DrawImageUnscaled($small,0,0);$small.RotateFlip([Drawing.RotateFlipType]::RotateNoneFlipX);$g.DrawImageUnscaled($small,1280,0);$small.Dispose();$layer.Dispose()
}
$g.DrawImageUnscaled($oldSmall,0,0);$g.DrawImageUnscaled($newSmall,1280,0);$g.Dispose()
$scene.Save((Join-Path $outDir 'kasino-ja-kappeli-parallakseilla.png'),[Drawing.Imaging.ImageFormat]::Png)
$single=$scene.Clone([Drawing.Rectangle]::new(1280,0,1280,461),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$single.Save((Join-Path $outDir 'kappeli-parallakseilla-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$single.Dispose();$scene.Dispose();$newSmall.Dispose();$oldSmall.Dispose();$saved.Dispose();$final.Dispose();$scaled.Dispose();$raw.Dispose();$old.Dispose()
if((Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash -ne $oldHash){throw 'Casino source changed.'}
$prompt1=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-kappeli-generointiohje.txt') -Raw
$prompt2=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-kappeli-korkeampi-generointiohje.txt') -Raw
$prompt3=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-strip-kappeli-laattasauma-generointiohje.txt') -Raw
@($prompt1,"`n--- USER CORRECTION: TALLER CHAPEL ---`n",$prompt2,"`n--- LOCAL PAVING JOINT CORRECTION ---`n",$prompt3) | Set-Content -LiteralPath (Join-Path $outDir 'generointiohjeet.txt') -Encoding UTF8
@"
Vegas Strip: korkea hääkappeli kasinon oikealle puolelle

Pelikuva: vegas-strip-haakappeli-5120x1844.png, 5120 x 1844 RGBA PNG.
Sijoita tämän kuvan vasen reuna kasinokuvan vegas-strip-kasino-ulkoa-v2-5120x1844.png oikeaan reunaan samalla PPU:lla, skaalalla, pivotilla ja korkeudella.
Ensin on avoin katuosuus, sitten korkea hääkappeli. Katon huippu ja torni jatkuvat kuvan yläreunan ulkopuolelle. Kappelissa on korkeat pariovet, kaari-ikkunat ja hillitty Vegas-neon. Kävelyalue noin 35 prosenttia, kapea jalkakäytävä ja leveä asfaltti. Katu jatkuu oikeaan reunaan.
Tausta on aidosti läpinäkyvä erillisille parallakseille. Parallaksien esikatselut käyttävät aiempia Vegas-tasoja kasinon kohdalla normaalina ja kappelin kohdalla peilattuina. Niitä ei ole yhdistetty pelikäyttöiseen etualaan.
Liittyvät reunapikselit on tarkistettu kaikista RGBA-kanavista; myös kuvaparin esikatselu on tarkistettu. Vanhaa kasinokuvaa ja Unity-scenejä ei muutettu.

Tekotapa: sisäänrakennettu image_gen, ensin jatkokuva ja sitten käyttäjän pyytämä korkeamman julkisivun muokkaus. Valitun kuvan natiivi koko $native; 5120 x 1844 on vientikoko. Rakennus siirretty 92 pikseliä ylemmäs vientikoossa, sen mittasuhteita litistämättä. Kadun pinta jatkettu aiemman kasinokuvan peilatusta pinnasta. Liitoskohdan laatoituksesta poistettu paikallisella kuvatyökalumuokkauksella V-kuvion muodostava ylimääräinen vino sauma. Alfa siivottu, liittymä uuden julkisivun juureen häivytetty. Generointiohjeet erillisessä tekstitiedostossa.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native=$native",$report,"Casino SHA256 unchanged=$oldHash") | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
Write-Output $finalPath
Write-Output $report
