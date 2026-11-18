using System;
using System.Collections.Generic;
public static class OfficeExtension {
 public static int[] Runs(byte[] d,int w,int y,int max){
  List<int> a=new List<int>();bool on=false;int start=0;
  for(int x=0;x<max;x++){bool clear=d[(y*w+x)*4+3]<32;if(clear&&!on){start=x;on=true;}if((!clear||x==max-1)&&on){int end=clear?x+1:x;if(end-start>20){a.Add(start);a.Add(end);}on=false;}}
  return a.ToArray();
 }
 static double Map(double p,int[] to,int[] from){int i=0;while(i<to.Length-2 && p>to[i+1])i++;double t=(p-to[i])/Math.Max(1.0,to[i+1]-to[i]);return from[i]+t*(from[i+1]-from[i]);}
 public static void AlignUpper(byte[] d,int w,int h,byte[] old,int ow){
  int[] full=Runs(d,w,1844,w),target=Runs(old,ow,0,5120);
  if(full.Length<target.Length)throw new Exception("Upper window topology mismatch: "+full.Length+" vs "+target.Length);
  int[] from=new int[target.Length+1],to=new int[target.Length+1];Array.Copy(full,from,target.Length);Array.Copy(target,to,target.Length);
  from[target.Length-1]=from[target.Length-2]+target[target.Length-1]-target[target.Length-2];from[target.Length]=w;to[target.Length]=w;
  byte[] before=(byte[])d.Clone();
  for(int y=0;y<h-1;y++)for(int x=0;x<w;x++){
   double sx=Math.Max(0,Math.Min(w-1.001,Map(x,to,from)));int ix=(int)sx;double t=sx-ix;int p=(y*w+x)*4,q=(y*w+ix)*4;
   double a=before[q+3]*(1-t),b=before[q+7]*t,alpha=a+b;
   for(int c=0;c<3;c++)d[p+c]=alpha>0?(byte)Math.Round((before[q+c]*a+before[q+4+c]*b)/alpha):(byte)0;d[p+3]=(byte)Math.Round(alpha);
  }
 }
 static void Blend(byte[] d,int p,byte[] old,int q,double t){double a=d[p+3]*(1-t),b=old[q+3]*t,alpha=a+b;for(int c=0;c<3;c++)d[p+c]=alpha>0?(byte)Math.Round((d[p+c]*a+old[q+c]*b)/alpha):(byte)0;d[p+3]=(byte)Math.Round(alpha);}
 public static void RestoreAndCollar(byte[] d,int w,int h,byte[] old,int ow,int oh){
  for(int y=0;y<oh;y++)Buffer.BlockCopy(old,y*ow*4,d,((y+1844)*w)*4,ow*4);
  for(int k=0;k<192;k++){
   double t=Math.Max(0,1-k/191.0);t=t*t*(3-2*t);
   for(int x=0;x<ow;x++)Blend(d,((1843-k)*w+x)*4,old,x*4,t);
   for(int y=0;y<oh;y++)Blend(d,((y+1844)*w+5120+k)*4,old,(y*ow+5119)*4,t);
  }
 }
 public static long[] Check(byte[] d,int w,byte[] old,int ow,int oh){long changed=0,up=0,right=0;for(int y=0;y<oh;y++)for(int x=0;x<ow;x++)for(int c=0;c<4;c++)if(d[((y+1844)*w+x)*4+c]!=old[(y*ow+x)*4+c])changed++;
  for(int x=0;x<ow;x++)for(int c=0;c<4;c++)if(d[(1843*w+x)*4+c]!=old[x*4+c])up++;
  for(int y=0;y<oh;y++)for(int c=0;c<4;c++)if(d[((y+1844)*w+5120)*4+c]!=old[(y*ow+5119)*4+c])right++;
  return new long[]{changed,up,right};
 }
}
