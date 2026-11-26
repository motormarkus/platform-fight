param(
    [Parameter(Mandatory=$true)][string]$BasePath,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$ZonesPath,
    [string]$AcceptIds,
    [int]$MinimumWidth=24,
    [int]$MaximumWidth=190,
    [int]$MinimumHeight=30,
    [int]$MaximumHeight=220
)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$probe=[System.Drawing.Bitmap]::new(1,1);$pg=[System.Drawing.Graphics]::FromImage($probe);$pg.Clear([System.Drawing.Color]::Black);$pg.Dispose();$probe.Dispose()
$drawingReferences=@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {-not $_.IsDynamic -and $_.GetName().Name -match '^(System\.Drawing|System\.Private\.Windows|System\.Windows\.Extensions)'} | ForEach-Object {$_.Location} | Select-Object -Unique)
$standardReferences=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | Where-Object {$_.Name -notmatch '^System\.Drawing'} | Select-Object -ExpandProperty FullName)
if(-not ('CrowdCandidateDetector' -as [type])) {
Add-Type -ReferencedAssemblies @($drawingReferences+$standardReferences) -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
public sealed class CrowdFaceCandidate {
 public string Id;
 public int X,Y,Width,Height,Pixels;
 public double FillRatio;
}
public static class CrowdCandidateDetector {
 public static CrowdFaceCandidate[] Find(string path,Rectangle[] zones,int minW,int maxW,int minH,int maxH) {
  using(Bitmap loaded=new Bitmap(path))
  using(Bitmap bitmap=new Bitmap(loaded.Width,loaded.Height,PixelFormat.Format24bppRgb)) {
   int w=bitmap.Width,h=bitmap.Height;
   using(Graphics g=Graphics.FromImage(bitmap))g.DrawImageUnscaled(loaded,0,0);
   BitmapData d=bitmap.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
   byte[] rgb=new byte[d.Stride*h];Marshal.Copy(d.Scan0,rgb,0,rgb.Length);int stride=d.Stride;bitmap.UnlockBits(d);
   byte[] skin=new byte[w*h];
   foreach(Rectangle zone in zones) {
    Rectangle z=Rectangle.Intersect(zone,new Rectangle(0,0,w,h));
    for(int y=z.Top;y<z.Bottom;y++)for(int x=z.Left;x<z.Right;x++) {
     int i=y*stride+x*3;double b=rgb[i],g=rgb[i+1],r=rgb[i+2];
     double cb=128-0.168736*r-0.331264*g+0.5*b,cr=128+0.5*r-0.418688*g-0.081312*b;
     double maximum=Math.Max(r,Math.Max(g,b)),minimum=Math.Min(r,Math.Min(g,b));
     double saturation=maximum>0?(maximum-minimum)/maximum:0;
     if(r>40&&g>22&&b>10&&r>g*1.015&&b>g*0.46&&r<g*2.05&&cr>=130&&cr<=184&&cb>=72&&cb<=138&&saturation>0.075&&saturation<0.74)skin[y*w+x]=1;
    }
   }
   List<CrowdFaceCandidate> found=new List<CrowdFaceCandidate>();Queue<int> queue=new Queue<int>();
   for(int sy=0;sy<h;sy++)for(int sx=0;sx<w;sx++) {
    int start=sy*w+sx;if(skin[start]==0)continue;skin[start]=0;queue.Enqueue(start);
    int left=sx,right=sx,top=sy,bottom=sy,n=0;
    while(queue.Count>0) {
     int at=queue.Dequeue(),y=at/w,x=at-y*w;n++;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);
     int[] neighbours={x>0?at-1:-1,x<w-1?at+1:-1,y>0?at-w:-1,y<h-1?at+w:-1};
     foreach(int next in neighbours)if(next>=0&&skin[next]>0){skin[next]=0;queue.Enqueue(next);}
    }
    int cw=right-left+1,ch=bottom-top+1;double ratio=cw/(double)ch,fill=n/(double)(cw*ch);
    if(cw<minW||cw>maxW||ch<minH||ch>maxH||ratio<0.42||ratio>1.45||fill<0.19||fill>0.95||n<180)continue;
    found.Add(new CrowdFaceCandidate {X=left,Y=top,Width=cw,Height=ch,Pixels=n,FillRatio=fill});
   }
   found.Sort((a,b)=> {int rowA=a.Y/70,rowB=b.Y/70;int c=rowA.CompareTo(rowB);return c!=0?c:a.X.CompareTo(b.X);});
   for(int i=0;i<found.Count;i++)found[i].Id="candidate"+(i+1).ToString("000");return found.ToArray();
  }
 }
 public static void Preview(string source,CrowdFaceCandidate[] candidates,string path,int maxWidth,int offsetX,int cropWidth) {
  using(Bitmap original=new Bitmap(source)) {
   int width=cropWidth>0?Math.Min(cropWidth,original.Width-offsetX):original.Width;double s=Math.Min(1,maxWidth/(double)width);
   using(Bitmap p=new Bitmap((int)Math.Round(width*s),(int)Math.Round(original.Height*s),PixelFormat.Format24bppRgb))
   using(Graphics g=Graphics.FromImage(p))
   using(Pen pen=new Pen(Color.Lime,1))
   using(Font font=new Font("Arial",10,FontStyle.Bold)) {
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(original,new Rectangle(0,0,p.Width,p.Height),offsetX,0,width,original.Height,GraphicsUnit.Pixel);
    foreach(CrowdFaceCandidate c in candidates) {
     if(c.X+c.Width<offsetX||c.X>=offsetX+width)continue;
     float x=(float)((c.X-offsetX)*s),y=(float)(c.Y*s);g.DrawRectangle(pen,x,y,(float)(c.Width*s),(float)(c.Height*s));
     g.DrawString(c.Id.Substring(9),font,Brushes.Lime,x,y-14);
    }
    p.Save(path,ImageFormat.Png);
   }
  }
 }
}
'@
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$outputAbsolute=(Resolve-Path -LiteralPath $OutputDirectory).Path
$sourceAbsolute=(Resolve-Path -LiteralPath $BasePath).Path
$source=[System.Drawing.Image]::FromFile($sourceAbsolute);$canvasWidth=$source.Width;$canvasHeight=$source.Height;$source.Dispose()
$zones=[System.Collections.Generic.List[System.Drawing.Rectangle]]::new()
if($ZonesPath) {
    $zoneConfiguration=Get-Content -LiteralPath $ZonesPath -Raw | ConvertFrom-Json
    foreach($zone in $zoneConfiguration.zones){$zones.Add([System.Drawing.Rectangle]::new([int]$zone.x,[int]$zone.y,[int]$zone.width,[int]$zone.height))}
} else {$zones.Add([System.Drawing.Rectangle]::new(0,([int]($canvasHeight*0.12)),$canvasWidth,([int]($canvasHeight*0.54))))}
$candidates=[CrowdCandidateDetector]::Find($sourceAbsolute,$zones.ToArray(),$MinimumWidth,$MaximumWidth,$MinimumHeight,$MaximumHeight)
$candidateRecords=@($candidates | ForEach-Object { [ordered]@{id=$_.Id;accepted=$false;kind='head_or_hand_candidate';bbox=@($_.X,$_.Y,$_.Width,$_.Height);skinPixels=$_.Pixels;fillRatio=$_.FillRatio;ellipse=@(($_.X+$_.Width/2.0),($_.Y+$_.Height/2.0),($_.Width*0.37),($_.Height*0.38))} })
[ordered]@{source=$sourceAbsolute;canvasWidth=$canvasWidth;canvasHeight=$canvasHeight;warning='Skin-tone candidates only, not a semantic people mask. Inspect every accepted ellipse. Never accept unreviewed furniture, gold trim or posters.';candidates=$candidateRecords} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outputAbsolute 'crowd-candidates.json') -Encoding utf8
[CrowdCandidateDetector]::Preview($sourceAbsolute,$candidates,(Join-Path $outputAbsolute 'crowd-candidates-panorama.png'),4000,0,0)
if($canvasWidth -eq15000){for($panel=0;$panel -lt3;$panel++){[CrowdCandidateDetector]::Preview($sourceAbsolute,$candidates,(Join-Path $outputAbsolute ('crowd-candidates-panel-'+($panel+1)+'.png')),3000,($panel*5000),5000)}}
if($AcceptIds) {
    $selected=@($AcceptIds.Split(',') | ForEach-Object {$_.Trim()})
    $people=@()
    foreach($c in $candidateRecords) {if($selected -contains$c.id){$people+=[ordered]@{id=$c.id;regions=@([ordered]@{kind='head';ellipse=$c.ellipse;amplitudeX=1.3;amplitudeY=1.7;rotationDegrees=0.12;feather=7})}}}
    [ordered]@{canvasWidth=$canvasWidth;canvasHeight=$canvasHeight;people=$people;excludePolygons=@()} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $outputAbsolute 'crowd-regions-selected.json') -Encoding utf8
}
Write-Output "Candidate detector found$($candidates.Count) skin-tone components. All require visual review."
