$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$folder=Join-Path $PSScriptRoot 'vegas-tyomaakonttori-kaikki-ikkunat-tyhjat'
[void][IO.Directory]::CreateDirectory($folder)
$im=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'vegas-tyomaakonttori-ruokala-parallaksi\01-katukuva-ikkunat-lapinakyvat-5120x1844.png'))
foreach($item in @(@('ovi-lahikuva.png',1530,460,800,620),@('ylakerta-lahikuva.png',1500,0,3620,175),@('kahvat-lahikuva.png',1840,790,250,220))){
 $crop=$im.Clone([Drawing.Rectangle]::new($item[1],$item[2],$item[3],$item[4]),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $crop.Save((Join-Path $folder $item[0]),[Drawing.Imaging.ImageFormat]::Png);$crop.Dispose()
}
$im.Dispose()
Copy-Item -LiteralPath 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-a02901bf-80f1-442c-8d31-a7ec6005e35b.png' -Destination (Join-Path $folder 'tyhjennys-natiivi.png')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'vegas-tyomaakonttori-ruokala-parallaksi\kaikki-ikkunat-tyhjaksi-generointiohje.txt') -Destination (Join-Path $folder 'generointiohje.txt')
