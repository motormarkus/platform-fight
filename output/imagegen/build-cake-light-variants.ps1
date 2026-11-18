$ErrorActionPreference='Stop'
$original=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'build-cake-mess.ps1') -Raw
$folder=Join-Path $PSScriptRoot 'vegas-haakakku-sotku/kevyet-versiot'
$records=Get-Content -LiteralPath (Join-Path $folder 'generoidut.json') -Raw|ConvertFrom-Json
foreach($entry in $records){
 $match=[regex]::Match($entry.hint,'as (C:\\[^\r\n]+?\.png) by default');if(!$match.Success){throw 'Missing raw path'}
 $script=$original.Replace("'vegas-haakakku-sotku'","'vegas-haakakku-sotku/kevyet-versiot'").Replace('C:/Users/marku/.codex/generated_images/01a19137-3c84-7223-9249-58aae914b472/exec-2f15f82a-6f8c-44bc-b2d0-e227a1dba2af.png',$match.Groups[1].Value)
 $script=$script.Replace('haakakku-sotku-natiivi.png',($entry.id+'-natiivi.png')).Replace('haakakku-sotku-1024x256.png',($entry.id+'-1024x256.png')).Replace('esikatselu-ruudukolla.png',($entry.id+'-esikatselu.png')).Replace('tarkistus.json',($entry.id+'-tarkistus.json'))
 $scriptPath=Join-Path $PSScriptRoot ('build-cake-'+$entry.id+'.ps1');Set-Content -LiteralPath $scriptPath -Value $script -Encoding utf8
 & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath
 if($LASTEXITCODE -ne 0){throw ('Build failed '+$entry.id)}
}
Add-Type -AssemblyName System.Drawing
$sheet=[Drawing.Bitmap]::new(1024,768,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$g=[Drawing.Graphics]::FromImage($sheet)
$i=0;foreach($entry in $records){$im=[Drawing.Bitmap]::new((Join-Path $folder ($entry.id+'-esikatselu.png')));$g.DrawImageUnscaled($im,0,256*$i);$im.Dispose();$i++}
$g.Dispose();$sheet.Save((Join-Path $folder 'kaikki-kolme-esikatselu.png'),[Drawing.Imaging.ImageFormat]::Png);$sheet.Dispose()
