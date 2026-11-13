using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public static class KeyCabinetAnimation {
 public const int W=1493,H=1054;
 const double Pivot=385,Top=122,Bottom=966,DoorWidth=989;
 static readonly double KEnd=1-(Bottom-Top)/(1012.0-53.0);
 static readonly double CosEnd=(39-Pivot)*(1-KEnd)/DoorWidth;
 public static readonly double EndAngle=Math.Acos(CosEnd)*180/Math.PI;
 static readonly double Eye=(Top-(1-KEnd)*53)/KEnd;
 static readonly double Focal=DoorWidth*Math.Sin(EndAngle*Math.PI/180)/KEnd;

 public static void Clean(byte[] d){
  for(int i=0;i<d.Length;i+=4){
   if(d[i+3]<=16)d[i]=d[i+1]=d[i+2]=d[i+3]=0;
   else if(d[i+3]>=240)d[i+3]=255;
  }
 }
 static double Clamp(double x,double a,double b){return Math.Max(a,Math.Min(b,x));}
 static byte Byte(double x){return (byte)Clamp(Math.Round(x),0,255);}
 // Returns premultiplied colour. Alpha is in 0..255, colour in 0..255.
 static void Sample(byte[] source,double x,double y,out double blue,out double green,out double red,out double alpha){
  blue=green=red=alpha=0;
  if(x<0||y<0||x>W-1||y>H-1)return;
  int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);
  double fx=x-ix,fy=y-iy;
  for(int j=0;j<2;j++)for(int k=0;k<2;k++){
   int px=Math.Min(W-1,ix+k),py=Math.Min(H-1,iy+j),p=(py*W+px)*4;
   double weight=(k==0?1-fx:fx)*(j==0?1-fy:fy),a=source[p+3];
   alpha+=a*weight;double premul=a*weight/255.0;
   blue+=source[p]*premul;green+=source[p+1]*premul;red+=source[p+2]*premul;
  }
 }
 static void Over(byte[] image,int p,double b,double g,double r,double a){
  if(a<=0)return;
  double oldA=image[p+3],weight=1-a/255.0,outA=a+oldA*weight;
  if(outA<0.001)return;
  image[p]=Byte((b+image[p]*oldA*weight/255)*255/outA);
  image[p+1]=Byte((g+image[p+1]*oldA*weight/255)*255/outA);
  image[p+2]=Byte((r+image[p+2]*oldA*weight/255)*255/outA);
  image[p+3]=Byte(outA);
 }
 public static byte[] Render(byte[] cabinet,byte[] front,byte[] open,double degrees){
  byte[] output=(byte[])cabinet.Clone();
  double theta=degrees*Math.PI/180,c=Math.Cos(theta),s=Math.Sin(theta),k=DoorWidth*s/Focal;
  bool back=c<0;
  double maxU=back?1.05:1.0;
  double freeX=Pivot+DoorWidth*c*maxU/(1-k*maxU);
  double freeTop=Eye+(Top-Eye)/(1-k*maxU),freeBottom=Eye+(Bottom-Eye)/(1-k*maxU);
  double edgeX=Pivot+DoorWidth*c/(1-k),edgeTop=Eye+(Top-Eye)/(1-k),edgeBottom=Eye+(Bottom-Eye)/(1-k);
  double edgeWidth=8*Math.Abs(s)/(1-k);
  int x0=(int)Clamp(Math.Floor(Math.Min(Pivot,freeX)-12),0,W-1),x1=(int)Clamp(Math.Ceiling(Math.Max(Pivot,freeX)+12),0,W-1);
  int y0=(int)Clamp(Math.Floor(Math.Min(Top,freeTop)-3),0,H-1),y1=(int)Clamp(Math.Ceiling(Math.Max(Bottom,freeBottom)+3),0,H-1);
  // Thin metal edge sampled from the accepted door, visible as the leaf turns.
  if(!back&&degrees>0){
   for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
    double xx=x+0.5,yy=y+0.5;
    if(xx<edgeX||xx>edgeX+edgeWidth||yy<edgeTop||yy>edgeBottom)continue;
    double v=(yy-edgeTop)/(edgeBottom-edgeTop),sb,sg,sr,sa;
    Sample(front,1348,130+v*824,out sb,out sg,out sr,out sa);
    double shade=0.72+0.18*(xx-edgeX)/Math.Max(0.01,edgeWidth);
    Over(output,(y*W+x)*4,sb*shade,sg*shade,sr*shade,sa);
   }
  }
  // Four coverage samples avoid stair-stepping along the rotating edges.
  for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){
   double totalB=0,totalG=0,totalR=0,totalA=0;
   for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++){
    double xx=x+(sx+0.5)/2.0,yy=y+(sy+0.5)/2.0,d=xx-Pivot;
    double denom=DoorWidth*c+d*k;
    if(Math.Abs(denom)<1e-9)continue;
    double u=d/denom,v=((yy-Eye)*(1-k*u)+Eye-Top)/(Bottom-Top);
    if(u<0||u>maxU||v<0||v>1)continue;
    double sampleX,sampleY;
    if(back){
     sampleX=Pivot+DoorWidth*CosEnd*u/(1-KEnd*u);
     sampleY=Eye+(Top+(Bottom-Top)*v-Eye)/(1-KEnd*u);
    }else{sampleX=355+u*998;sampleY=130+v*824;}
    double b,g,r,a;Sample(back?open:front,sampleX,sampleY,out b,out g,out r,out a);
    double shade=back?0.94+0.06*Clamp(-c/-CosEnd,0,1):0.94+0.06*c;
    totalB+=b*shade/4;totalG+=g*shade/4;totalR+=r*shade/4;totalA+=a/4;
   }
   Over(output,(y*W+x)*4,totalB,totalG,totalR,totalA);
  }
  return output;
 }
 public static int OutsideMotionDifferences(byte[] a,byte[] b){
  int count=0;
  // Door sweep never touches right frame, top frame above y=45 or bottom below1020.
  for(int y=0;y<H;y++)for(int x=0;x<W;x++){
   if(x<=1395&&y>=45&&y<=1020)continue;
   int p=(y*W+x)*4;
   for(int c=0;c<4;c++)if(a[p+c]!=b[p+c]){count++;break;}
  }
  return count;
 }
 public static void WriteApng(string[] frames,string destination){
  using(var output=File.Create(destination)){
   output.Write(new byte[]{137,80,78,71,13,10,26,10},0,8);
   byte[] first=File.ReadAllBytes(frames[0]);
   WriteChunk(output,"IHDR",Slice(first,16,13));
   WriteChunk(output,"acTL",Join(U32((uint)frames.Length),U32(0)));
   uint sequence=0;
   for(int n=0;n<frames.Length;n++){
    byte[] png=File.ReadAllBytes(frames[n]),header=Slice(png,16,8);
    ushort delay=(ushort)(n==0?260:n==frames.Length-1?1000:90);
    byte[] control=Join(U32(sequence++),header,U32(0),U32(0),new byte[]{(byte)(delay>>8),(byte)delay,3,232,0,0});
    WriteChunk(output,"fcTL",control);
    for(int p=8;p<png.Length;){
     int len=(int)Read32(png,p);string kind=Encoding.ASCII.GetString(png,p+4,4);
     if(kind=="IDAT"){
      byte[] data=Slice(png,p+8,len);
      if(n==0)WriteChunk(output,"IDAT",data);
      else WriteChunk(output,"fdAT",Join(U32(sequence++),data));
     }
     p+=len+12;
    }
   }
   WriteChunk(output,"IEND",new byte[0]);
  }
 }
 public static string ValidateApng(string path){
  byte[] png=File.ReadAllBytes(path);int frameCount=0,declared=0;uint sequence=0;
  if(png.Length<33||png[0]!=137||png[1]!=80||png[2]!=78||png[3]!=71)throw new Exception("Invalid PNG signature");
  uint width=Read32(png,16),height=Read32(png,20);
  if(png[24]!=8||png[25]!=6)throw new Exception("Preview must be 8-bit RGBA");
  for(int p=8;p<png.Length;){
   int len=(int)Read32(png,p);if(len<0||p+len+12>png.Length)throw new Exception("Truncated PNG chunk");
   string kind=Encoding.ASCII.GetString(png,p+4,4);uint crc=0xffffffff;
   for(int i=p+4;i<p+8+len;i++){crc^=png[i];for(int j=0;j<8;j++)crc=(crc&1)!=0?0xedb88320^(crc>>1):crc>>1;}
   if((crc^0xffffffff)!=Read32(png,p+8+len))throw new Exception("CRC mismatch: "+kind);
   if(kind=="acTL")declared=(int)Read32(png,p+8);
   if(kind=="fcTL"||kind=="fdAT")if(Read32(png,p+8)!=sequence++)throw new Exception("APNG sequence mismatch");
   if(kind=="fcTL"){
    frameCount++;
    if(Read32(png,p+12)!=width||Read32(png,p+16)!=height)throw new Exception("APNG canvas mismatch");
   }
   p+=len+12;
  }
  if(declared!=10||frameCount!=10)throw new Exception("APNG must contain 10 frames");
  return "APNG verified: "+frameCount+" RGBA frames, "+width+" x "+height+", all CRCs and sequence numbers valid.";
 }
 static uint Read32(byte[] d,int o){return (uint)(d[o]<<24|d[o+1]<<16|d[o+2]<<8|d[o+3]);}
 static byte[] U32(uint n){return new byte[]{(byte)(n>>24),(byte)(n>>16),(byte)(n>>8),(byte)n};}
 static byte[] Slice(byte[] d,int p,int n){var r=new byte[n];Buffer.BlockCopy(d,p,r,0,n);return r;}
 static byte[] Join(params byte[][] arrays){int n=0;foreach(var a in arrays)n+=a.Length;var r=new byte[n];int p=0;foreach(var a in arrays){Buffer.BlockCopy(a,0,r,p,a.Length);p+=a.Length;}return r;}
 static void WriteChunk(Stream stream,string kind,byte[] data){
  byte[] type=Encoding.ASCII.GetBytes(kind),length=U32((uint)data.Length);stream.Write(length,0,4);stream.Write(type,0,4);stream.Write(data,0,data.Length);
  uint crc=0xffffffff;
  foreach(byte v in Join(type,data)){crc^=v;for(int j=0;j<8;j++)crc=(crc&1)!=0?0xedb88320^(crc>>1):crc>>1;}
  byte[] c=U32(crc^0xffffffff);stream.Write(c,0,4);
 }
}
