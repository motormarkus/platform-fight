$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $root 'kasino-layer-utils.ps1')
$rawRoot='C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472'
$work=Join-Path $root 'kasino-sisatilat-baari-tyo'
$assets=@(
 @('01-kaukotausta','exec-be6e6e05-c802-44ed-a0f6-992dd8fa5034.png',1376),
 @('02-keskitaso','exec-f91536be-0d42-4eab-96d6-270cc18f4dbe.png',1280),
 @('03-etuala','exec-cf3cf1df-1b30-4626-aef3-2ff54b4f7238.png',1376)
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
  for($y=295;$y -le 340;$y++){
   $n=$small.GetPixel(1279,$y);$o=$old.GetPixel(0,$y)
   "y=$y new=$($n.R),$($n.G),$($n.B),$($n.A) old=$($o.R),$($o.G),$($o.B),$($o.A)"
  }
 }
 $im.Dispose();$small.Dispose();$old.Dispose();$preview.Dispose();$g.Dispose()
}

