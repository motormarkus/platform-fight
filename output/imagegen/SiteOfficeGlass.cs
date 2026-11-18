using System;
public static class SiteOfficeGlass {
 static bool Interior(byte[] s,int p){return s[p+2]>28 && s[p+1]>s[p]*1.10 && s[p+2]>s[p]*1.12;}
 static void Aperture(byte[] s,byte[] mask,int w,int h,int left,int right,int top,int bottom,bool clippedTop,bool clippedRight){
  int[] tops=new int[right-left+15],bottoms=new int[right-left+15];int bx=left-7;
  for(int x=left-7;x<=Math.Min(w-1,right+7);x++){
   int t=top,b=bottom;
   if(!clippedTop){for(int y=top-5;y<=top+5;y++)if(Interior(s,(y*w+x)*4)){t=y;break;}}
   for(int y=bottom+5;y>=bottom-5;y--)if(Interior(s,(y*w+x)*4)){b=y;break;}
   tops[x-bx]=t;bottoms[x-bx]=b;
  }
  for(int y=top;y<=bottom+5;y++){
   int l=left,r=right;
   for(int x=left-5;x<=left+5;x++)if(Interior(s,(y*w+x)*4)){l=x;break;}
   if(clippedRight)r=w-1;else for(int x=right+5;x>=right-5;x--)if(Interior(s,(y*w+x)*4)){r=x;break;}
   for(int x=l;x<=r;x++){
    if(x-bx<0||x-bx>=tops.Length)continue;
    if(y<tops[x-bx]||y>bottoms[x-bx])continue;
    if((y<top+3&&!clippedTop || y>bottom-2)&&!Interior(s,(y*w+x)*4))continue;
    mask[y*w+x]=255;
   }
  }
 }
 public static byte[] Mask(byte[] source,byte[] guide,int w,int h,out byte[] handles){
  byte[] mask=new byte[w*h];
  int[,] upper={{1588,1837,114},{1874,2170,115},{2208,2545,114},{2998,3344,116},{3381,3738,115},{3777,4170,115},{4778,5023,115},{5061,5119,115}};
  for(int n=0;n<8;n++)Aperture(source,mask,w,h,upper[n,0],upper[n,1],0,upper[n,2],true,n==7);
  Aperture(source,mask,w,h,1633,1916,494,1053,false,false);
  Aperture(source,mask,w,h,2020,2304,494,1051,false,false);
  // Use the generated alpha as a handle guide, then preserve the source's silver
  // handle pixels and their small dark contour instead of replacing their RGB.
  byte[] seeds=new byte[w*h];handles=new byte[w*h];
  for(int y=813;y<=981;y++)for(int x=1875;x<=2058;x++){
   int p=(y*w+x)*4;
   if(guide[p+3]>100 && source[p+2]>60 && source[p]>source[p+2]*0.56 && source[p+1]>source[p+2]*0.66)seeds[y*w+x]=255;
  }
  for(int y=813;y<=981;y++)for(int x=1875;x<=2058;x++)if(seeds[y*w+x]!=0){
   for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)if(dx*dx+dy*dy<=5)handles[(y+dy)*w+x+dx]=255;
  }
  for(int i=0;i<mask.Length;i++)if(handles[i]!=0)mask[i]=0;
  return mask;
 }
 public static byte[] Apply(byte[] original,byte[] mask){byte[] result=(byte[])original.Clone();for(int i=0;i<mask.Length;i++)if(mask[i]!=0)for(int c=0;c<4;c++)result[i*4+c]=0;return result;}
 public static long[] Check(byte[] original,byte[] final,byte[] mask,byte[] handles){
  long changedOutside=0,clear=0,remaining=0,handleChanged=0;
  for(int i=0;i<mask.Length;i++){
   if(mask[i]!=0){clear++;if(final[i*4+3]!=0)remaining++;}
   for(int c=0;c<4;c++){if(mask[i]==0 && original[i*4+c]!=final[i*4+c])changedOutside++;if(handles[i]!=0&&original[i*4+c]!=final[i*4+c])handleChanged++;}
  }return new long[]{changedOutside,clear,remaining,handleChanged};
 }
}
