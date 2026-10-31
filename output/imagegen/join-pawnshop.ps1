Add-Type -AssemblyName System.Drawing
$root = 'G:\unity pelit\mopopeli\platform fight\output\imagegen'
$source = [System.Drawing.Bitmap]::new('G:\unity pelit\mopopeli\platform fight\output\imagegen\totalfist-panttilainaamo-sisa-01.png')
$generated = [System.Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-aed877db-1645-4385-861d-01464367ba83.png')
$result = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($result)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.DrawImage($generated, 0, 0, $result.Width, $result.Height)
$g.Dispose()
# Reflect a narrow source margin, then smoothly transition into the new artwork.
# The joining column is copied exactly, including alpha.
$transition = 64
for ($x=0; $x -lt $transition; $x++) {
    $t = $x / ($transition - 1.0)
    $weight = $t * $t * (3 - 2 * $t)
    for ($y=0; $y -lt $result.Height; $y++) {
        $a = $source.GetPixel($source.Width - 1 - $x, $y)
        $b = $result.GetPixel($x, $y)
        $aa = [int][Math]::Round($a.A*(1-$weight)+$b.A*$weight)
        $rr = [int][Math]::Round($a.R*(1-$weight)+$b.R*$weight)
        $gg = [int][Math]::Round($a.G*(1-$weight)+$b.G*$weight)
        $bb = [int][Math]::Round($a.B*(1-$weight)+$b.B*$weight)
        $result.SetPixel($x,$y,[System.Drawing.Color]::FromArgb($aa,$rr,$gg,$bb))
    }
}
$result.Save((Join-Path $root 'totalfist-panttilainaamo-sisa-02.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview = [System.Drawing.Bitmap]::new($source.Width*2,$source.Height)
$g = [System.Drawing.Graphics]::FromImage($preview)
$g.Clear([System.Drawing.Color]::White)
$g.DrawImage($source,[System.Drawing.Rectangle]::new(0,0,$source.Width,$source.Height),0,0,$source.Width,$source.Height,[System.Drawing.GraphicsUnit]::Pixel)
$g.DrawImage($result,[System.Drawing.Rectangle]::new($source.Width,0,$source.Width,$source.Height),0,0,$result.Width,$result.Height,[System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$preview.Save((Join-Path $root 'totalfist-panttilainaamo-sisa-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$mismatch = 0
for ($y=0; $y -lt $source.Height; $y++) { if ($source.GetPixel($source.Width-1,$y).ToArgb() -ne $result.GetPixel(0,$y).ToArgb()) { $mismatch++ } }
Write-Output "Size: $($result.Width)x$($result.Height). Mismatching join pixels: $mismatch / $($source.Height)."
$source.Dispose(); $generated.Dispose(); $result.Dispose(); $preview.Dispose()



