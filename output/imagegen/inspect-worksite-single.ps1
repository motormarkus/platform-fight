$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaa-yhtena'
$raw=[Drawing.Bitmap]::new('C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-e9e545c3-6d81-4182-8a4e-bd850a29c6fe.png')
"Native: $($raw.Width)x$($raw.Height)"
Clean-Alpha $raw
$raw.Save((Join-Path $folder 'tyomaa-natiivi.png'),[Drawing.Imaging.ImageFormat]::Png)
$preview=[Drawing.Bitmap]::new($raw.Width,$raw.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($preview);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImageUnscaled($raw,0,0);$g.Dispose()
$preview.Save((Join-Path $folder 'natiivi-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$edge=[Drawing.Bitmap]::new(300,$raw.Height,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($edge);$g.Clear([Drawing.Color]::FromArgb(34,40,58));$g.DrawImage($raw,[Drawing.Rectangle]::new(0,0,300,$raw.Height),[Drawing.Rectangle]::new(0,0,300,$raw.Height),[Drawing.GraphicsUnit]::Pixel);$g.Dispose()
$edge.Save((Join-Path $folder 'vasen-natiivi-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png)
$rows=for($y=0;$y -lt $raw.Height;$y++){ $p=$raw.GetPixel(0,$y); if($p.A -gt 0){[pscustomobject]@{Y=$y;A=$p.A;R=$p.R;G=$p.G;B=$p.B}} }
$rows | ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $folder 'natiivin-vasen-reuna.json') -Encoding utf8
"Left first visible: $($rows[0].Y)"
$raw.Dispose();$preview.Dispose();$edge.Dispose()
