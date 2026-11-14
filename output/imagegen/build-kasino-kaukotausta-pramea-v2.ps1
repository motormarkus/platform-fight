$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$source = 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-6fc04b25-ad69-46eb-af48-5b418e48701e.png'
$folder = Join-Path $PSScriptRoot 'kasino-kaukotausta-pramea-v2'
[void][System.IO.Directory]::CreateDirectory($folder)
$src = [System.Drawing.Bitmap]::FromFile($source)
$dst = [System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($dst)
$g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$attrs = [System.Drawing.Imaging.ImageAttributes]::new()
$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
# Align the straight wall/carpet boundary with the requested 65 percent height.
$split = 471
$g.DrawImage($src,[System.Drawing.Rectangle]::new(0,0,5120,1199),[single]0,[single]0,[single]$src.Width,[single]$split,[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$g.DrawImage($src,[System.Drawing.Rectangle]::new(0,1199,5120,645),[single]0,[single]$split,[single]$src.Width,[single]($src.Height-$split),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
$attrs.Dispose()
$g.Dispose()
$out = Join-Path $folder 'kasino-kaukotausta-pramea-v2-5120x1844.png'
$dst.Save($out,[System.Drawing.Imaging.ImageFormat]::Png)
$preview = Resize-Canvas $dst 1280 461
$preview.Save((Join-Path $folder 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$pixels = Pixel-Data $dst
$opaque = 0
for($i=3;$i -lt $pixels.Length;$i+=4){if($pixels[$i] -eq 255){$opaque++}}
if($opaque -ne 5120*1844){throw 'Unexpected transparency in opaque background.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'kasino-kaukotausta-pramea-v2-generointiohje.txt') -Destination (Join-Path $folder 'generointiohje.txt')
@'
Kasinon kaukotausta, prameampi versio 2.
Valmis PNG: 5120 x 1844, peittävä taustakuva.
Keskellä rulettipöytä; kaksi suurta yöikkunaa, verhoja, kultakoristeita, kattovalaisimia ja pieniä peliautomaatteja.
Punainen matto alkaa vaakasuoraan noin 65 prosentin korkeudelta (y=1199).
Päätyjä ei ole tasattu tai peilattu keskenään; ne saavat olla erilaiset käyttäjän korjauksen mukaisesti.
Tehty sisäänrakennetulla image_gen-kuvatyökalulla. Natiivikuva 2087 x 753; vientikuva on skaalattu 5120 x 1844 kokoon. Koko ei tarkoita natiivia 5K-generointia.
Ikkunoissa on himmeä yötausta; kuva ei sisällä läpinäkyvyyttä.
Alkuperäinen kasinotausta on säilytetty muuttamattomana. Unity-projektin sceneihin tai asetuksiin ei tehty muutoksia.
'@ | Set-Content -LiteralPath (Join-Path $folder 'LUE-MINUT.txt') -Encoding UTF8
"Dimensions=5120x1844; fully opaque pixels=$opaque; wall/carpet line=1199 (65.0217%); native source=$($src.Width)x$($src.Height)." | Set-Content -LiteralPath (Join-Path $folder 'tarkistus.txt') -Encoding UTF8
$dst.Dispose()
$src.Dispose()
Write-Output $out
