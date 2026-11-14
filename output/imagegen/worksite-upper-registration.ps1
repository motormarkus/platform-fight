Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
public static class WorksiteUpperRegistration {
 const int W=5120,H=1844;
 static double S(double v){v=Math.Max(0,Math.Min(1,v));return v*v*(3-2*v);}
 static double Map(double v,double[] src,double[] dst){int k=0;while(k<dst.Length-2&&v>dst[k+1])k++;return src[k]+(v-dst[k])/(dst[k+1]-dst[k])*(src[k+1]-src[k]);}
 static void Sample(byte[] im,double x,double y,byte[] d,int o){
  x=Math.Max(0,Math.Min(W-1,x));y=Math.Max(0,Math.Min(H-1,y));int x0=(int)x,y0=(int)y,x1=Math.Min(W-1,x0+1),y1=Math.Min(H-1,y0+1);double fx=x-x0,fy=y-y0;
  int a=(y0*W+x0)*4,b=(y0*W+x1)*4,c=(y1*W+x0)*4,e=(y1*W+x1)*4;
  double wa=(1-fx)*(1-fy),wb=fx*(1-fy),wc=(1-fx)*fy,we=fx*fy;
  double al=im[a+3]*wa+im[b+3]*wb+im[c+3]*wc+im[e+3]*we;d[o+3]=(byte)Math.Round(al);
  for(int ch=0;ch<3;ch++)d[o+ch]=al>0?(byte)Math.Round((im[a+ch]*im[a+3]*wa+im[b+ch]*im[b+3]*wb+im[c+ch]*im[c+3]*wc+im[e+ch]*im[e+3]*we)/al):(byte)0;
 }
 static List<int[]> Runs(byte[] im,int y){var a=new List<int[]>();int start=-1;for(int x=0;x<W;x++){bool on=im[(y*W+x)*4+3]>16;if(on&&start<0)start=x;if(!on&&start>=0){if(x-start>4)a.Add(new[]{start,x-1});start=-1;}}if(start>=0)a.Add(new[]{start,W-1});return a;}
 public static byte[] Build(byte[] raw,byte[] below,byte[] right,byte[] previous,double[] sy,double[] dy,double[] sx,double[] dx,int bayTop,int emptyLeft,double[] manual){
  var d=new byte[W*H*4];double[] xx=new double[W];for(int x=0;x<W;x++)xx[x]=Map(x,sx,dx);
  for(int y=0;y<H;y++){double yy=Map(y,sy,dy);for(int x=0;x<W;x++)Sample(raw,xx[x],yy,d,(y*W+x)*4);}
  var r0=Runs(below,0);var r48=Runs(below,48);var supports=new List<double[]>();
  if(manual.Length>0){for(int k=0;k<manual.Length;k+=3)supports.Add(new[]{manual[k],manual[k+1],manual[k+2],0.0});}
  else{if(r0.Count!=r48.Count)throw new Exception("Support components require manual review: "+r0.Count+" vs "+r48.Count);for(int k=0;k<r0.Count;k++){double slope=((r48[k][0]+r48[k][1])-(r0[k][0]+r0[k][1]))/96.0;if(Math.Abs(slope)<0.2)slope=0;supports.Add(new[]{(double)r0[k][0],(double)r0[k][1],slope,0.0});}}
  if(previous.Length>0)supports.Add(new[]{-255.0,-213.0,-0.9583333333,1.0});
  var structure=new byte[W*H*4];byte[] sample=new byte[4];
  foreach(var support in supports){
   double start=support[0],end=support[1],slope=support[2];bool incoming=support[3]>0;byte[] source=incoming?previous:below;
   for(int y=bayTop;y<H;y++){
    double distance=H-y,t=Math.Abs(slope)>0.2?Math.Min(16,distance*0.04):(end-start>100?Math.Min(230,distance*0.62):Math.Min(60,distance*0.15));int lo=Math.Max(0,(int)Math.Floor(start-slope*distance)-3),hi=Math.Min(W-1,(int)Math.Ceiling(end-slope*distance)+3);
    for(int x=lo;x<=hi;x++){
     double sourceX=x+slope*(distance+t)+(incoming?W:0);if(sourceX<0||sourceX>=W)continue;Sample(source,sourceX,t,sample,0);if(sample[3]<=16)continue;
     int i=(y*W+x)*4;double a=sample[3]/255.0,oa=structure[i+3]/255.0,na=a+oa*(1-a);
     for(int c=0;c<3;c++)structure[i+c]=(byte)Math.Round((sample[c]*a+structure[i+c]*oa*(1-a))/na);structure[i+3]=(byte)Math.Round(na*255);
    }
   }
  }
  for(int y=bayTop;y<H;y++)for(int x=0;x<W;x++){
   int i=(y*W+x)*4;double blend=1-S((y-bayTop)/64.0);
   if(structure[i+3]>240&&d[i+3]>240)for(int c=0;c<3;c++)structure[i+c]=(byte)Math.Round(structure[i+c]*(1-blend)+d[i+c]*blend);
   Buffer.BlockCopy(structure,i,d,i,4);
  }
  if(emptyLeft>0)for(int y=0;y<H;y++)for(int x=0;x<emptyLeft;x++){int i=(y*W+x)*4;d[i]=d[i+1]=d[i+2]=d[i+3]=0;}
  for(int y=0;y<bayTop;y++)for(int x=W-128;x<W;x++){
   int depth=W-1-x,i=(y*W+x)*4,j=(y*W+depth)*4;double t=1-S((depth-8)/120.0),a=d[i+3]*(1-t)+right[j+3]*t;
   for(int c=0;c<3;c++)d[i+c]=a>0?(byte)Math.Round((d[i+c]*d[i+3]*(1-t)+right[j+c]*right[j+3]*t)/a):(byte)0;d[i+3]=(byte)Math.Round(a);
  }
  return d;
 }
}
'@
