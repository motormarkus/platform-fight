$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-jatko-ja-katto'
$final=Join-Path $folder 'tyomaakonttori-kokonaisuus-10240x3688.png'
$src=[Drawing.Bitmap]::new($final)
$sheet=[Drawing.Bitmap]::new(1280,1200,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.Color]::FromArgb(255,35,42,55))
$rects=@(@(4800,1544),@(3100,762),@(8300,1544),@(4800,2950))
for($i=0;$i -lt 4;$i++){
 $r=$rects[$i];$g.DrawImage($src,[Drawing.Rectangle]::new(($i%2)*640,[Math]::Floor($i/2)*600,640,600),[int]$r[0],[int]$r[1],640,600,[Drawing.GraphicsUnit]::Pixel)
}
$g.Dispose();$sheet.Save((Join-Path $folder 'liitokset-100prosenttia.png'),[Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose();$src.Dispose()
$stats=@()
foreach($name in @('02-katu-oikea-5120x1844.png','03-yla-vasen-5120x1844.png','04-yla-oikea-5120x1844.png')){
 $p=Join-Path $folder $name;$w=0;$h=0;$d=[WorksiteDetail]::Load($p,[ref]$w,[ref]$h);$alpha=0
 for($i=3;$i -lt $d.Length;$i+=4){if($d[$i] -eq 0){$alpha++}}
 $stats+=[pscustomobject]@{file=$name;width=$w;height=$h;transparentPixels=$alpha;bytes=(Get-Item -LiteralPath $p).Length}
}
$stats | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $folder 'tiedostotarkistus.json') -Encoding utf8
$stats | Format-Table -AutoSize
