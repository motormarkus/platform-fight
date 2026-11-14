$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'kasino-layer-utils.ps1')
$outDir = Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen'
[void][System.IO.Directory]::CreateDirectory($outDir)
$sourcePath = 'C:\Users\marku\.codex\generated_images\01a19137-3c84-7223-9249-58aae914b472\exec-15a65059-4930-48d1-9918-0c1b2205ab4e.png'
$source = [System.Drawing.Bitmap]::new($sourcePath)
$native = '{0} x {1}' -f $source.Width,$source.Height
$final = Resize-Canvas $source 5120 1844
$finalPath = Join-Path $outDir 'vegas-baari-sisatilat-vasen-5120x1844.png'
$final.Save($finalPath,[System.Drawing.Imaging.ImageFormat]::Png)
$preview = Resize-Canvas $final 1280 461
$preview.Save((Join-Path $outDir 'esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$edge = $final.Clone([System.Drawing.Rectangle]::new(4864,0,256,1844),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$edge.Save((Join-Path $outDir 'oikean-reunan-jatkoviite.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$edge.Dispose()
$data = Pixel-Data $final
Add-Type -TypeDefinition @'
public static class BarInteriorCheck {
 public static int NonOpaque(byte[] b){int n=0;for(int i=3;i<b.Length;i+=4)if(b[i]!=255)n++;return n;}
}
'@
$nonOpaque = [BarInteriorCheck]::NonOpaque($data)
if($nonOpaque -ne 0){throw "Unexpected transparent pixels: $nonOpaque"}
Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $outDir 'natiivi.png')
$first = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen-generointiohje.txt') -Raw
$correction = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'vegas-baari-sisatilat-vasen-lattia-korjaus.txt') -Raw
@("Mode: built-in image_gen",'', 'INITIAL PROMPT', $first, '', 'FINAL TARGETED FLOOR CORRECTION', $correction) | Set-Content -LiteralPath (Join-Path $outDir 'generointiohjeet.txt') -Encoding UTF8
@"
Vegasin baarin sisätilat, vasen pääty

Pelikuva: vegas-baari-sisatilat-vasen-5120x1844.png
Koko: 5120 x 1844 px, PNG. Täysin peittävä sisäkuva.
Natiivikuva: $native px; mukana natiivi.png. Pelikuva on skaalattu vientikoko, ei natiivisti generoitu 5K.

Vasemmalla loivasti viisto sulkeva päätyseinä ja rakennuksen ulkoasuun sopivat pariovet. Jukeboxi, kitara ja vinyylilevyt tuovat hillittyä rock-henkeä. Puu, messinki, viininpunaiset paneelit sekä lämmin valo jatkavat ulkojulkisivun tyyliä.
Lattiaa on noin 40 prosenttia kuvan korkeudesta. Kuva ei sisällä tekstiä tai hahmoja. Tiski alkaa oikealta ja jatkuu katkeamatta kuvan reunan yli seuraavaa palaa varten.
Tulevan oikean jatkopalan pitää jatkaa lattialautoja, tiskiä, hyllyjä sekä seinän vaakasuoria valoja samoilta korkeuksilta. oikean-reunan-jatkoviite.png on suora 256 px leveä rajaus tästä lopullisesta PNG:stä, ei erillinen pelikuva.

Tekotapa: sisäänrakennettu image_gen, ensin uusi sisäkuva ulkojulkisivun tyyliviitteestä, sitten kohdennettu lattiatilan lisäys. Lopuksi koon vienti System.Drawingilla. Kuvan rakenteita ei muutettu viennissä.
Tarkistettu visuaalisesti: pariovet vasemmalla, sulkeva seinä, jukeboxi, avoin lattia, tekstitön sisustus ja oikealle jatkuva tiski. Unityn scenejä tai aiempia kuvia ei muutettu.
"@ | Set-Content -LiteralPath (Join-Path $outDir 'LUE-MINUT.txt') -Encoding UTF8
@("Native=$native",'Final=5120x1844','All pixels opaque; non-opaque=0','Floor: visually approximately 40 percent; left return wall has shallow diagonal projection.','No existing adjacent interior tile yet; this is the initial left end. Right continuation remains open.') | Set-Content -LiteralPath (Join-Path $outDir 'tarkistus.txt') -Encoding UTF8
$final.Dispose();$source.Dispose()
Write-Output $finalPath
Write-Output "Native: $native; final: 5120 x 1844; non-opaque: $nonOpaque"
