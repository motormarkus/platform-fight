param(
    [Parameter(Mandatory=$true)][string]$FramesDir,
    [Parameter(Mandatory=$true)][string]$OutFile,
    [int]$Fps=8
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
if($Fps -lt 1 -or $Fps -gt 50){throw 'Fps must be1..50.'}
$files=@(Get-ChildItem -LiteralPath $FramesDir -Filter '*.png' -File | Sort-Object Name)
if($files.Count -ne 6){throw 'Exactly six PNG preview frames are required.'}
$sources=@()
foreach($file in $files){
    $stream=[System.IO.File]::OpenRead($file.FullName)
    try{$image=[System.Windows.Media.Imaging.BitmapFrame]::Create($stream,[System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,[System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad);$image.Freeze();$sources+=$image}finally{$stream.Dispose()}
}
$width=$sources[0].PixelWidth;$height=$sources[0].PixelHeight
$palette=New-Object System.Windows.Media.Imaging.BitmapPalette($sources[0],256)
$encoder=New-Object System.Windows.Media.Imaging.GifBitmapEncoder
$encoder.Palette=$palette
$delay=[uint16][Math]::Round(100.0/$Fps)
foreach($source in $sources){
    if($source.PixelWidth -ne $width -or $source.PixelHeight -ne $height){throw 'Preview frame dimensions differ.'}
    $indexed=New-Object System.Windows.Media.Imaging.FormatConvertedBitmap($source,[System.Windows.Media.PixelFormats]::Indexed8,$palette,0.0)
    if($indexed.CanFreeze){$indexed.Freeze()}
    $frameMetadata=New-Object System.Windows.Media.Imaging.BitmapMetadata('gif')
    $frameMetadata.SetQuery('/grctlext/Delay',$delay)
    $frameMetadata.SetQuery('/grctlext/Disposal',[byte]1)
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($indexed,$null,$frameMetadata,$null))
}
$outPath=[System.IO.Path]::GetFullPath($OutFile)
$directory=[System.IO.Path]::GetDirectoryName($outPath)
[void][System.IO.Directory]::CreateDirectory($directory)
$output=[System.IO.File]::Create($outPath)
try{$encoder.Save($output)}finally{$output.Dispose()}
# WPF does not reliably retain GIF timing metadata. Add standard loop and
# per-frame graphic controls without changing compressed image pixels.
$gifBytes=[System.IO.File]::ReadAllBytes($outPath)
$tableBytes=if(($gifBytes[10] -band 128) -ne 0){3*[Math]::Pow(2,($gifBytes[10] -band 7)+1)}else{0}
$insertAt=[int](13+$tableBytes)
$loopExtension=[byte[]](@(33,255,11)+[System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0')+@(3,1,0,0,0))
$looped=[System.IO.MemoryStream]::new()
$looped.Write($gifBytes,0,$insertAt)
$looped.Write($loopExtension,0,$loopExtension.Length)
$position=$insertAt
$frameControl=[byte[]]@(33,249,4,4,($delay -band 255),($delay -shr 8),0,0)
while($position -lt $gifBytes.Length){
    $start=$position
    $tag=$gifBytes[$position]
    if($tag -eq 33){
        $label=$gifBytes[$position+1];$position+=2
        do{$blockLength=[int]$gifBytes[$position];$position++;$position+=$blockLength}while($blockLength -ne 0)
        if($label -ne 249){$looped.Write($gifBytes,$start,($position-$start))}
    }elseif($tag -eq 44){
        $packed=$gifBytes[$position+9];$position+=10
        if(($packed -band 128) -ne 0){$position += [int](3*[Math]::Pow(2,($packed -band 7)+1))}
        $position++
        do{$blockLength=[int]$gifBytes[$position];$position++;$position+=$blockLength}while($blockLength -ne 0)
        $looped.Write($frameControl,0,$frameControl.Length)
        $looped.Write($gifBytes,$start,($position-$start))
    }elseif($tag -eq 59){$looped.WriteByte(59);$position++;break}
    else{throw ('Unexpected GIF block at '+$position)}
}
[System.IO.File]::WriteAllBytes($outPath,$looped.ToArray())
$looped.Dispose()
[pscustomobject]@{File=$outPath;Width=$width;Height=$height;Frames=6;DelayMilliseconds=($delay*10);Loop=0;SharedPalette=$true} | ConvertTo-Json -Compress
