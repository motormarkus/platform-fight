$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$probe=[System.Drawing.Bitmap]::new(1,1);$pg=[System.Drawing.Graphics]::FromImage($probe);$pg.Dispose();$probe.Dispose()
$drawing=@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { -not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)' } | ForEach-Object {$_.Location} | Select-Object -Unique)
$standard=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | Where-Object {$_.Name -notmatch '^System\.Drawing'} | Select-Object -ExpandProperty FullName)
Add-Type -ReferencedAssemblies @($drawing+$standard) -TypeDefinition (Get-Content -LiteralPath (Join-Path $PSScriptRoot 'NativeCompose.cs') -Raw)
$root=$PSScriptRoot
$src='C:/Users/marku/.codex/generated_images/01a207f3-74e0-7c63-b5d8-b78973c50087'
$native=Join-Path $root 'native'
$qa=Join-Path $root 'qa'
New-Item -ItemType Directory -Force -Path $native,$qa | Out-Null
$mapping=@{ 'seam-01.png'='exec-0053c35a-4f5b-43b1-a46a-6ad9eb21accb.png';'seam-02.png'='exec-d893a1c0-b5ff-49eb-835b-65b6a344ec98.png';'roof.png'='exec-e60ad5f7-1eb9-40dc-a39a-aecce08593fa.png';'floor.png'='exec-b060e9e7-c4b8-410f-a033-7e77258ac6f5.png'}
foreach($entry in $mapping.GetEnumerator()){Copy-Item -LiteralPath (Join-Path $src $entry.Value) -Destination (Join-Path $native $entry.Key) -Force}
$padded=[System.Drawing.Bitmap]::new((Join-Path $root 'inputs/world-original-15360x1884.png'))
$core=$padded.Clone([System.Drawing.Rectangle]::new(180,0,15000,1884),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb);$padded.Dispose()
$r1=@([System.Drawing.Rectangle]::new(75,130,445,370),[System.Drawing.Rectangle]::new(180,410,430,310),[System.Drawing.Rectangle]::new(780,270,355,275),[System.Drawing.Rectangle]::new(300,650,475,350))
$r2=@([System.Drawing.Rectangle]::new(380,135,235,220),[System.Drawing.Rectangle]::new(810,135,260,220),[System.Drawing.Rectangle]::new(385,290,285,225),[System.Drawing.Rectangle]::new(805,290,285,225),[System.Drawing.Rectangle]::new(410,505,250,225),[System.Drawing.Rectangle]::new(810,505,260,225),[System.Drawing.Rectangle]::new(85,385,335,345),[System.Drawing.Rectangle]::new(1095,110,300,250),[System.Drawing.Rectangle]::new(1145,290,320,230),[System.Drawing.Rectangle]::new(1040,390,390,340))
$patch=[System.Drawing.Bitmap]::new((Join-Path $native 'seam-01.png'))
$changed1=[Arena16Native]::Repair($core,$patch,4172,256,$r1,@());$patch.Dispose()
$patch=[System.Drawing.Bitmap]::new((Join-Path $native 'seam-02.png'))
$protected=@(497..508)+@(710..719)+@(790..1023)
$changed2=[Arena16Native]::Repair($core,$patch,9292,256,$r2,$protected);$patch.Dispose()
$world=[System.Drawing.Bitmap]::new(15000,2880,[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
$g=[System.Drawing.Graphics]::FromImage($world)
$g.DrawImageUnscaled($core,0,498)
foreach($spec in @(@{Name='roof.png';CropY=0;Y=0},@{Name='floor.png';CropY=526;Y=2382})) {
  $image=[System.Drawing.Bitmap]::new((Join-Path $native $spec.Name))
  if($image.Width -ne 1536 -or $image.Height -ne 1024){throw 'Native extension dimensions changed; do not upscale'}
  $tile=$image.Clone([System.Drawing.Rectangle]::new(0,$spec.CropY,1536,498),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
  for($x=0;$x -lt 15000;$x+=1536){$g.DrawImageUnscaled($tile,$x,$spec.Y);$tile.RotateFlip([System.Drawing.RotateFlipType]::RotateNoneFlipX)}
  $tile.Dispose();$image.Dispose()
}
$g.Dispose();$core.Dispose()
[Arena16Native]::ConnectExtensions($world,498,1884)
$allowed=@()
foreach($r in $r1){$allowed+=[System.Drawing.Rectangle]::new($r.X+4172,$r.Y+256,$r.Width,$r.Height)}
foreach($r in $r2){$allowed+=[System.Drawing.Rectangle]::new($r.X+9292,$r.Y+256,$r.Width,$r.Height)}
$originals=@()
for($i=1;$i -le 3;$i++){$originals+=[System.Drawing.Bitmap]::new((Join-Path (Split-Path $root -Parent) ('vegas-areena-keha-tuotanto/base/static-{0:00}-5000x1884.png' -f $i)))}
$audit=[Arena16Native]::AuditOriginal($world,$originals,$allowed)
foreach($im in $originals){$im.Dispose()}
if($audit[2] -ne 0){throw "Original area unexpectedly changed outside spectator removal regions: $($audit[2]) pixels"}
$world.Save((Join-Path $root 'master-panorama-15000x2880.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$starts=@(0,4940,9880)
$panels=@()
for($i=0;$i -lt 3;$i++) {
  $panel=$world.Clone([System.Drawing.Rectangle]::new($starts[$i],0,5120,2880),[System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
  $name='master-{0:00}-5120x2880.png' -f ($i+1)
  $panel.Save((Join-Path $root $name),[System.Drawing.Imaging.ImageFormat]::Png)
  $preview=[System.Drawing.Bitmap]::new(1280,720)
  $pg=[System.Drawing.Graphics]::FromImage($preview);$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $pg.DrawImage($panel,0,0,1280,720);$pg.Dispose()
  $preview.Save((Join-Path $qa ('preview-{0:00}.jpg' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Jpeg);$preview.Dispose()
  $panels+=@{file=$name;width=5120;height=2880;worldX=$starts[$i];worldY=0;pixelScale=1}
  $panel.Dispose()
}
$diffs=@()
for($i=0;$i -lt 2;$i++) {
  $a=[System.Drawing.Bitmap]::new((Join-Path $root $panels[$i].file));$b=[System.Drawing.Bitmap]::new((Join-Path $root $panels[$i+1].file))
  $n=[Arena16Native]::CompareOverlap($a,$b,180);$a.Dispose();$b.Dispose()
  if($n -ne 0){throw "Overlap verification failed: $n changed pixels"}
  $diffs+=@{left=$i+1;right=$i+2;sharedWidth=180;comparedPixels=180*2880;changedPixels=$n}
  $joint=$starts[$i+1]+90
  $cut=$world.Clone([System.Drawing.Rectangle]::new($joint-700,0,1400,2880),$world.PixelFormat)
  $preview=[System.Drawing.Bitmap]::new(700,1440);$pg=[System.Drawing.Graphics]::FromImage($preview);$pg.DrawImage($cut,0,0,700,1440);$pg.Dispose();$cut.Dispose()
  $preview.Save((Join-Path $qa ('join-{0:00}.jpg' -f ($i+1))),[System.Drawing.Imaging.ImageFormat]::Jpeg);$preview.Dispose()
}
$overview=[System.Drawing.Bitmap]::new(3000,576);$pg=[System.Drawing.Graphics]::FromImage($overview);$pg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic;$pg.DrawImage($world,0,0,3000,576);$pg.Dispose()
$overview.Save((Join-Path $qa 'panorama.jpg'),[System.Drawing.Imaging.ImageFormat]::Jpeg);$overview.Dispose();$world.Dispose()
$report=@{panels=$panels;panoramaWidth=15000;panoramaHeight=2880;originalBandY=498;originalBandHeight=1884;addedTop=498;addedBottom=498;horizontalContentAdded=0;originalImageSpatialResampling=0;nativePatchSize=@(1536,1024);extensions='Native roof and floor modules repeated with alternating horizontal reflection; no enlargement; color blending only at vertical joins';seamRepairChangedPixels=@($changed1,$changed2);originalPixelAudit=@{comparedPixels=$audit[0];changedInSpectatorRemovalRegions=$audit[1];changedOutsideAllowedRegions=$audit[2]};sharedAreaVerification=$diffs;placementStep=4940;overlap=180;safeAreas=@(@{panel=1;localX=@(4820,5120)},@{panel=2;localX=@(0,300);rightLocalX=@(4820,5120)},@{panel=3;localX=@(0,300)})}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $root 'layout-and-verification.json') -Encoding utf8
$report | ConvertTo-Json -Depth 5 -Compress
