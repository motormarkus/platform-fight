$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $root 'kasino-layer-utils.ps1')
$rawRoot='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472'
$work=Join-Path $root 'kasino-sisatilat-vasen-tyo'
$assets=@(
 @('01-kaukotausta','exec-47c5484e-0549-4b66-bf48-3d323987e926.png',1376),
 @('02-keskitaso','exec-f5484960-5d69-4d23-962f-fee380544ce7.png',1280),
 @('03-etuala','exec-f0556884-3c32-4aa4-a81f-bedd90edea6a.png',1376)
)
foreach($asset in $assets){
 $im=[System.Drawing.Bitmap]::FromFile((Join-Path $rawRoot $asset[1]))
 $stats=Clean-Alpha $im
 $im.Save((Join-Path $work ($asset[0]+'-raaka.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $small=Resize-Canvas $im $asset[2] 461
 $null=Clean-Alpha $small
 $small.Save((Join-Path $work ($asset[0]+'-raaka-ohjekoko.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $old=[System.Drawing.Bitmap]::FromFile((Join-Path $work ($asset[0]+'-vanha.png')))
 $preview=[System.Drawing.Bitmap]::new(2560,461)
 $g=[System.Drawing.Graphics]::FromImage($preview);$g.Clear([System.Drawing.Color]::FromArgb(55,60,70))
 $g.DrawImage($small,[System.Drawing.Rectangle]::new(0,0,1280,461),0,0,1280,461,[System.Drawing.GraphicsUnit]::Pixel)
 $g.DrawImageUnscaled($old,1280,0)
 $preview.Save((Join-Path $work ($asset[0]+'-raaka-liitos.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 "$($asset[0]) native=$($im.Width)x$($im.Height) $stats"
 if($asset[0] -eq '03-etuala'){
  for($y=295;$y -le 330;$y++){
   $n=$small.GetPixel(1279,$y);$o=$old.GetPixel(0,$y)
   "y=$y new=$($n.R),$($n.G),$($n.B),$($n.A) old=$($o.R),$($o.G),$($o.B),$($o.A)"
  }
 }
 $im.Dispose();$small.Dispose();$old.Dispose();$preview.Dispose();$g.Dispose()
}
