$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'WorksiteDetail.cs') -ReferencedAssemblies System.Drawing
Add-Type -TypeDefinition @'
public static class CreamAirStats {
 public static long[] Alpha(byte[] pixels) {
  long clear=0,solid=0;
  for(int i=3;i<pixels.Length;i+=4) { if(pixels[i]==0)clear++; if(pixels[i]==255)solid++; }
  return new[]{clear,solid};
 }
}
'@
$folder = Join-Path $PSScriptRoot 'vegas-haakakku-sotku/ilmaroiskeet'
$items = Get-Content -LiteralPath (Join-Path $folder 'generoidut.json') -Raw | ConvertFrom-Json
$sheet = [Drawing.Bitmap]::new(1536,512)
$g = [Drawing.Graphics]::FromImage($sheet)
$b1 = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(92,96,105))
$b2 = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(118,122,131))
for($yy=0;$yy -lt 512;$yy+=32) {
 for($xx=0;$xx -lt 1536;$xx+=32) {
  $brush=if((([int]($xx/32)+[int]($yy/32))%2)-eq 0){$b1}else{$b2}
  $g.FillRectangle($brush,$xx,$yy,32,32)
 }
}
$reports = @()
$index=0
foreach($item in $items) {
 Copy-Item -LiteralPath $item.path -Destination (Join-Path $folder ($item.id+'-natiivi.png'))
 $w=0;$h=0
 $d=[WorksiteDetail]::Load($item.path,[ref]$w,[ref]$h)
 $scale=[Math]::Min(512.0/$w,512.0/$h)
 $dw=[int][Math]::Round($w*$scale);$dh=[int][Math]::Round($h*$scale)
 $resized=[WorksiteDetail]::Resize($d,$w,$h,$dw,$dh)
 $canvas=[byte[]]::new(512*512*4)
 $x=[int][Math]::Floor((512-$dw)/2);$y=[int][Math]::Floor((512-$dh)/2)
 for($row=0;$row -lt $dh;$row++) {
  [Buffer]::BlockCopy($resized,$row*$dw*4,$canvas,(($y+$row)*512+$x)*4,$dw*4)
 }
 $final=Join-Path $folder ($item.id+'-512x512.png')
 [WorksiteDetail]::Save($canvas,512,512,$final)
 $im=[WorksiteDetail]::Image($canvas,512,512)
 $g.DrawImageUnscaled($im,$index*512,0)
 $im.Dispose()
 $alpha=[CreamAirStats]::Alpha($canvas)
 if($alpha[0] -lt 150000 -or $alpha[1] -lt 300){throw 'Unexpected alpha contents'}
 $reports += [pscustomobject]@{file=$final;width=512;height=512;nativeWidth=$w;nativeHeight=$h;transparentPixels=$alpha[0];opaquePixels=$alpha[1];bytes=(Get-Item -LiteralPath $final).Length}
 $index++
}
$g.Dispose();$b1.Dispose();$b2.Dispose()
$sheet.Save((Join-Path $folder 'kolme-roisketta-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
$reports | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.json') -Encoding UTF8
$reports | ConvertTo-Json -Depth 4
