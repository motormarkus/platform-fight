$ErrorActionPreference='Stop'
$root=Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $root 'kasino-layer-utils.ps1')
$work=Join-Path $root 'kasino-sisatilat-vasen-tyo'
$oldDir=Join-Path $root 'kasino-sisatilat-parallaksi-3tasoa'
$out=Join-Path $root 'kasino-sisatilat-vasen-parallaksi-3tasoa'
$null=New-Item -ItemType Directory -Path $out -Force
Add-Type -TypeDefinition @'
using System;
public static class CasinoLeftJoin {
 static byte Clamp(double v){return (byte)Math.Max(0,Math.Min(255,Math.Round(v)));}
 public static void QuiltReflectedBand(byte[] dst,byte[] old,int w,int h,int band,int exact){
  int n=band-exact-32;
  double[] prev=new double[n],curr=new double[n];
  short[] parent=new short[n*h];
  for(int y=0;y<h;y++){
   for(int k=0;k<n;k++){
    int dist=exact+16+k, x=w-1-dist;
    int a=(y*w+x)*4,b=(y*w+dist)*4;
    double cost=0;
    for(int c=0;c<3;c++){
     double delta=dst[a+c]-old[b+c];cost+=delta*delta;
     double ga=Math.Abs(dst[a+4+c]-dst[a-4+c]);
     double gb=Math.Abs(old[b+4+c]-old[b-4+c]);
     cost+=(ga+gb)*12;
    }
    double centre=(k-n*.55)/n;cost+=centre*centre*180;
    double best=0;int pk=k;
    if(y>0){best=double.PositiveInfinity;for(int q=Math.Max(0,k-2);q<=Math.Min(n-1,k+2);q++){double v=prev[q]+Math.Abs(q-k)*60;if(v<best){best=v;pk=q;}}}
    curr[k]=cost+best;parent[y*n+k]=(short)pk;
   }
   var t=prev;prev=curr;curr=t;
  }
  int last=0;for(int k=1;k<n;k++)if(prev[k]<prev[last])last=k;
  int[] cuts=new int[h];for(int y=h-1;y>=0;y--){cuts[y]=exact+16+last;last=parent[y*n+last];}
  for(int y=0;y<h;y++)for(int dist=0;dist<band;dist++){
   double t=Math.Max(0,Math.Min(1,(dist-cuts[y]+4)/8.0));double a=1-t*t*(3-2*t);
   if(a<=0)continue;int i=(y*w+w-1-dist)*4,j=(y*w+dist)*4;
   for(int c=0;c<4;c++)dst[i+c]=Clamp(dst[i+c]*(1-a)+old[j+c]*a);
  }
 }
 public static void BlendReflectedBand(byte[] dst,byte[] old,int w,int h,int band,int exact){
  for(int y=0;y<h;y++)for(int k=0;k<band;k++){
   int i=(y*w+w-1-k)*4, j=(y*w+k)*4;
   double t=Math.Max(0,Math.Min(1,(double)(k-exact)/(band-exact-1))), a=1-t*t*(3-2*t);
   double oa=dst[i+3]*(1-a)+old[j+3]*a;
   for(int c=0;c<3;c++)dst[i+c]=oa>0?Clamp((dst[i+c]*dst[i+3]*(1-a)+old[j+c]*old[j+3]*a)/oa):(byte)0;
   dst[i+3]=Clamp(oa);
  }
 }
 public static void AdjustFloor(byte[] dst,byte[] old,int w,int h){
  int top=1224;
  double[,] sums=new double[3,h+1];
  for(int y=top;y<h;y++)for(int c=0;c<3;c++)sums[c,y+1]=sums[c,y]+old[(y*w)*4+c]-dst[(y*w+w-1)*4+c];
  for(int y=top;y<h;y++){
   int lo=Math.Max(top,y-72),hi=Math.Min(h,y+73);
   for(int k=0;k<512;k++){
    double t=k/511.0, a=1-t*t*(3-2*t);int i=(y*w+w-1-k)*4;
    for(int c=0;c<3;c++)dst[i+c]=Clamp(dst[i+c]+(sums[c,hi]-sums[c,lo])/(hi-lo)*a);
   }
  }
  BlendReflectedBand(dst,old,w,h,96,16);
 }
 public static string Check(byte[] dst,byte[] old,int w,int h,int layer){
  int edge=0,clear=0,hidden=0,opaqueErr=0,region=0;
  for(int y=0;y<h;y++){
   int i=(y*w+w-1)*4,j=y*w*4;
   for(int c=0;c<4;c++)if(dst[i+c]!=old[j+c])edge++;
   for(int x=0;x<w;x++){
    int p=(y*w+x)*4;byte a=dst[p+3];
    if(a==0){clear++;if(dst[p]!=0||dst[p+1]!=0||dst[p+2]!=0)hidden++;}
    if(layer==0&&a!=255)opaqueErr++;
    if(layer==1&&(x<32||x>w-33||y>h*.62)&&a!=0)region++;
    if(layer==2&&y>h*.69&&a!=255)region++;
   }
  }
  if(edge>0||hidden>0||opaqueErr>0||region>0)throw new Exception("Layer check failed: edge="+edge+", hidden="+hidden+", opaque="+opaqueErr+", region="+region);
  return "RGBA boundary errors="+edge+"; transparent pixels="+clear+"; hidden RGB errors="+hidden+"; region errors="+region;
 }
}
'@
function Warp-Foreground([System.Drawing.Bitmap]$src){
 $dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
 $g=[System.Drawing.Graphics]::FromImage($dst)
 $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
 $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
 $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
 $attrs=[System.Drawing.Imaging.ImageAttributes]::new();$attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
 $sy=@(0,315,316,317,318,319,320,321,323,461)
 $dy=@(0,299,300,301,302,303,304,305,307,461)
 for($j=0;$j -lt $sy.Count-1;$j++){
  $rect=[System.Drawing.Rectangle]::new(0,$dy[$j]*4,5120,($dy[$j+1]-$dy[$j])*4)
  $g.DrawImage($src,$rect,[single]0,[single]($sy[$j]*$src.Height/461.0),[single]($src.Width*1280/1376.0),[single](($sy[$j+1]-$sy[$j])*$src.Height/461.0),[System.Drawing.GraphicsUnit]::Pixel,$attrs)
 }
 $attrs.Dispose();$g.Dispose();return $dst
}
$names=@('01-kaukotausta','02-keskitaso','03-etuala')
$newPreviews=@();$oldPreviews=@();$report=@('Left extension. All three new layers are5120x1844; existing right-side files are unchanged.','Built-in image_gen, followed by mechanical crop, scale registration, alpha cleanup and seam blending.','Far layer uses a reflected registered collar from the existing adjacent layer to continue ceiling/wall patterns. Foreground gold border is vertically registered, carpet tone matched locally, and a narrow reflected collar joins the boundary.','Middle-distance contents are uniformly scaled to84% within the unchanged canvas to preserve depth. Native generated detail is about2K; final files are resampled.')
for($i=0;$i -lt 3;$i++){
 $name=$names[$i]
 $src=[System.Drawing.Bitmap]::FromFile((Join-Path $work ($name+'-raaka.png')))
 $old=[System.Drawing.Bitmap]::FromFile((Join-Path $oldDir ($name+'-5120x1844.png')))
 if($i -eq 0){$dst=Resize-Canvas $src 5120 1844 ($src.Width*1280/1376.0)}
 elseif($i -eq 1){
  $small=Resize-Canvas $src 4301 1549
  $dst=[System.Drawing.Bitmap]::new(5120,1844,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g=[System.Drawing.Graphics]::FromImage($dst);$g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
  $g.DrawImageUnscaled($small,409,0);$g.Dispose();$small.Dispose()
 }else{$dst=Warp-Foreground $src}
 $null=Clean-Alpha $dst
 $bytes=Pixel-Data $dst;$oldBytes=Pixel-Data $old
 if($i -eq 0){[CasinoLeftJoin]::QuiltReflectedBand($bytes,$oldBytes,5120,1844,640,128)}
 elseif($i -eq 2){[CasinoLeftJoin]::AdjustFloor($bytes,$oldBytes,5120,1844)}
 $null=[CasinoLeftPixels]::Normalize($bytes)
 Set-PixelData $dst $bytes
 $validation=[CasinoLeftJoin]::Check($bytes,$oldBytes,5120,1844,$i)
 $file=$name+'-vasen-5120x1844.png'
 $dst.Save((Join-Path $out $file),[System.Drawing.Imaging.ImageFormat]::Png)
 $report+="$name native=$($src.Width)x$($src.Height); $validation"
 $newSmall=Resize-Canvas $dst 1280 461;$oldSmall=Resize-Canvas $old 1280 461
 $null=Clean-Alpha $newSmall;$null=Clean-Alpha $oldSmall
 $newPreviews+=,$newSmall;$oldPreviews+=,$oldSmall
 $preview=[System.Drawing.Bitmap]::new(2560,461)
 $g=[System.Drawing.Graphics]::FromImage($preview);$g.Clear([System.Drawing.Color]::FromArgb(55,60,70))
 $g.DrawImageUnscaled($newSmall,0,0);$g.DrawImageUnscaled($oldSmall,1280,0)
 $preview.Save((Join-Path $out ($name+'-liitos-esikatselu.png')),[System.Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$preview.Dispose();$dst.Dispose();$old.Dispose();$src.Dispose()
}
$joined=[System.Drawing.Bitmap]::new(2560,461)
$jg=[System.Drawing.Graphics]::FromImage($joined)
for($i=0;$i -lt 3;$i++){$jg.DrawImageUnscaled($newPreviews[$i],0,0);$jg.DrawImageUnscaled($oldPreviews[$i],1280,0)}
$joined.Save((Join-Path $out 'koottu-liitos-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$left=[System.Drawing.Bitmap]::new(1280,461)
$lg=[System.Drawing.Graphics]::FromImage($left)
foreach($im in $newPreviews){$lg.DrawImageUnscaled($im,0,0)}
$left.Save((Join-Path $out 'vasen-koottu-esikatselu.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$lg.Dispose();$left.Dispose()
$close=[System.Drawing.Bitmap]::new(640,461)
$cg=[System.Drawing.Graphics]::FromImage($close);$cg.DrawImage($joined,[System.Drawing.Rectangle]::new(0,0,640,461),960,0,640,461,[System.Drawing.GraphicsUnit]::Pixel)
$close.Save((Join-Path $out 'liitos-lahikuva.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$cg.Dispose();$close.Dispose();$jg.Dispose();$joined.Dispose()
foreach($im in $newPreviews+$oldPreviews){$im.Dispose()}
Set-Content -LiteralPath (Join-Path $out 'tarkistus.txt') -Value $report
Copy-Item -LiteralPath (Join-Path $root 'kasino-vasen-generointiohjeet.txt') -Destination (Join-Path $out 'generointiohjeet.txt') -Force
$report
