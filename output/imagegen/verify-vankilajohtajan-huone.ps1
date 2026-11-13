$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
$a=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankilajohtajan-huone-vasen-5120x1844.png'))
$b=[System.Drawing.Bitmap]::FromFile((Join-Path $root 'vankilajohtajan-huone-oikea-5120x1844.png'))
if($a.Width -ne $b.Width -or $a.Height -ne $b.Height -or $b.Width -ne 5120 -or $b.Height -ne 1844){throw 'Size mismatch'}
$edgeErrors=0
for($y=0;$y -lt 1844;$y++){if($a.GetPixel(5119,$y).ToArgb() -ne $b.GetPixel(0,$y).ToArgb()){$edgeErrors++}}
if($edgeErrors -ne 0){throw "Saved edge mismatch: $edgeErrors"}
$report=@('Final dimensions: 5120x1844 PNG','Native generated canvas: 2167x726 including overlap; final is resampled.',"Saved joining edge: $edgeErrors RGBA pixel mismatches out of 1844 pixels.")
$windows=@(@(386,99),@(441,99),@(493,99),@(386,180),@(441,180),@(493,180),@(695,99),@(749,99),@(799,99),@(695,180),@(749,180),@(799,180))
foreach($pt in $windows){
 $alpha=$b.GetPixel($pt[0]*4,$pt[1]*4).A
 $report+="Window pane center ($($pt[0]*4),$($pt[1]*4)): alpha=$alpha"
 if($alpha -ne 0){throw 'Window opening not transparent'}
}
$features=@(
 @('mahogany upper trim',940,996),@('upper panel inset',1008,1044),@('lower panel inset',1240,1264),@('baseboard',1268,1308),@('carpet junction',1340,1375)
)
foreach($feature in $features){
 $bestScore=[double]::PositiveInfinity;$bestShift=0
 for($shift=-8;$shift -le 8;$shift++){
  $score=0.0
  for($y=[int]$feature[1];$y -lt [int]$feature[2];$y++){
   $p=$a.GetPixel(5119,$y);$q=$b.GetPixel(64,$y+$shift)
   $diff=([double]$p.R+$p.G+$p.B)-([double]$q.R+$q.G+$q.B)
   $score+=$diff*$diff
  }
  if($score -lt $bestScore){$bestScore=$score;$bestShift=$shift}
 }
 $report+="Feature profile at 64 px inside new tile, $($feature[0]): best vertical offset $bestShift px"
}
$attrs=[System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
$preview=[System.Drawing.Bitmap]::new(1280,461)
$g=[System.Drawing.Graphics]::FromImage($preview)
$g.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.DrawImage($b,[System.Drawing.Rectangle]::new(0,0,1280,461),0,0,5120,1844,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$preview.Save((Join-Path $root 'vankilajohtajan-huone-oikea-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$seam=[System.Drawing.Bitmap]::new(512,922)
$sg=[System.Drawing.Graphics]::FromImage($seam)
$sg.Clear([System.Drawing.Color]::FromArgb(45,90,110))
$sg.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$sg.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$sg.DrawImage($a,[System.Drawing.Rectangle]::new(0,0,256,922),4608,0,512,1844,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$sg.DrawImage($b,[System.Drawing.Rectangle]::new(256,0,256,922),0,0,512,1844,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$seam.Save((Join-Path $root 'vankilajohtajan-huone-liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
Set-Content -LiteralPath (Join-Path $root 'vankilajohtajan-huone-liitos-tarkistus.txt') -Value $report
$report
$attrs.Dispose();$g.Dispose();$preview.Dispose();$sg.Dispose();$seam.Dispose();$a.Dispose();$b.Dispose()

