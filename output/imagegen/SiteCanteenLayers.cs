using System;
public static class SiteCanteenLayers {
 static bool Warm(byte[] d,int p) { return d[p+2]>80 && d[p+1]>d[p]*1.13 && d[p+2]>d[p]*1.1; }
 public static byte[] Mask(byte[] source,int w,int h) {
  byte[] m=new byte[w*h];
  int[,] panes={{2725,3174},{3213,4067},{4105,4552},{4836,5119}};
  for(int n=0;n<4;n++)for(int y=466;y<=1012;y++){
   int left=panes[n,0],right=panes[n,1];
   for(int x=left-4;x<=left+6;x++)if(Warm(source,(y*w+x)*4)){left=x;break;}
   if(n==3)right=w-1;else for(int x=right+4;x>=right-6;x--)if(Warm(source,(y*w+x)*4)){right=x;break;}
   for(int x=left;x<=right;x++){
    if(y<=468 && !Warm(source,(y*w+x)*4))continue;
    m[y*w+x]=255;
   }
  }
  return m;
 }
 public static byte[] LocalMask(byte[] mask,int w,int x0,int y0,int cw,int ch){
  byte[] m=new byte[cw*ch];for(int y=0;y<ch;y++)Buffer.BlockCopy(mask,(y+y0)*w+x0,m,y*cw,cw);return m;
 }
 public static byte[] MixReference(byte[] original,byte[] generated,byte[] mask){
  byte[] r=(byte[])generated.Clone();for(int i=0;i<mask.Length;i++)if(mask[i]!=0)Buffer.BlockCopy(original,i*4,r,i*4,4);return r;
 }
 public static byte[] ToneReference(byte[] original,byte[] mask){
  byte[] r=(byte[])original.Clone();for(int i=0;i<mask.Length;i++)if(mask[i]==0)r[i*4+3]=0;return r;
 }
 public static void RestoreVisible(byte[] generated,byte[] original,byte[] mask,int w,int h){
  int count=w*h;int[] nearest=new int[count],distance=new int[count],queue=new int[count];int head=0,tail=0;
  for(int i=0;i<count;i++){nearest[i]=-1;distance[i]=10000;if(mask[i]!=0){nearest[i]=i;distance[i]=0;queue[tail++]=i;}}
  while(head<tail){int p=queue[head++];if(distance[p]>=96)continue;int x=p%w,y=p/w;
   int[] ns={x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1};
   foreach(int n in ns)if(n>=0 && distance[n]>distance[p]+1){distance[n]=distance[p]+1;nearest[n]=nearest[p];queue[tail++]=n;}
  }
  byte[] before=(byte[])generated.Clone();
  for(int i=0;i<count;i++){
   if(mask[i]!=0){Buffer.BlockCopy(original,i*4,generated,i*4,4);continue;}
   if(nearest[i]<0)continue;int q=nearest[i]*4;double t=1-distance[i]/96.0;t=t*t*(3-2*t);
   for(int c=0;c<3;c++)generated[i*4+c]=(byte)Math.Max(0,Math.Min(255,Math.Round(before[i*4+c]+(original[q+c]-before[q+c])*t)));
   generated[i*4+3]=255;
  }
 }
 public static byte[] ExtractForeground(byte[] original,byte[] mask){
  byte[] fg=(byte[])original.Clone();for(int i=0;i<mask.Length;i++)if(mask[i]!=0)for(int c=0;c<4;c++)fg[i*4+c]=0;return fg;
 }
 public static byte[] PlaceBackground(byte[] room,int rw,int rh,int w,int h,int x0,int y0){
  byte[] bg=new byte[w*h*4];for(int y=0;y<rh;y++)Buffer.BlockCopy(room,y*rw*4,bg,((y+y0)*w+x0)*4,rw*4);return bg;
 }
 public static byte[] PadBackground(byte[] bg,int w,int h,int pad){
  int nw=w+pad*2;byte[] result=new byte[nw*h*4];
  for(int y=0;y<h;y++){
   Buffer.BlockCopy(bg,y*w*4,result,(y*nw+pad)*4,w*4);
   for(int x=0;x<pad;x++){
    Buffer.BlockCopy(bg,(y*w+(pad-1-x))*4,result,(y*nw+x)*4,4);
    Buffer.BlockCopy(bg,(y*w+(w-1-x))*4,result,(y*nw+pad+w+x)*4,4);
   }
  }return result;
 }
 public static byte[] Composite(byte[] fg,int w,int h,byte[] bg,int bw,int pad,int shift){
  byte[] result=(byte[])fg.Clone();for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int bx=x+pad-shift;if(bx<0||bx>=bw)continue;int p=(y*w+x)*4,q=(y*bw+bx)*4;
   double fa=fg[p+3]/255.0,ba=bg[q+3]/255.0,a=fa+ba*(1-fa);if(a<=0)continue;
   for(int c=0;c<3;c++)result[p+c]=(byte)Math.Round((fg[p+c]*fa+bg[q+c]*ba*(1-fa))/a);
   result[p+3]=(byte)Math.Round(a*255);
  }return result;
 }
 public static long[] Check(byte[] original,byte[] fg,byte[] recomposed,byte[] mask){
  long outside=0,masked=0,remaining=0,diff=0;
  for(int i=0;i<mask.Length;i++){
   if(mask[i]!=0){masked++;if(fg[i*4+3]!=0)remaining++;}
   for(int c=0;c<4;c++){
    if(mask[i]==0 && original[i*4+c]!=fg[i*4+c])outside++;
    if(original[i*4+c]!=recomposed[i*4+c])diff++;
   }
  }return new long[]{outside,masked,remaining,diff};
 }
 public static int Holes(byte[] padded,int bw,byte[] mask,int w,int h,int pad,int shift){
  int holes=0;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(mask[y*w+x]!=0){int bx=x+pad-shift;if(bx<0||bx>=bw||padded[(y*bw+bx)*4+3]!=255)holes++;}return holes;
 }
}
