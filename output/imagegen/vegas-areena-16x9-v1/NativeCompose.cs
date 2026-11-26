using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class Arena16Native {
  static byte[] Read(Bitmap b, out int stride) {
    var d=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
    stride=d.Stride;var a=new byte[stride*b.Height];Marshal.Copy(d.Scan0,a,0,a.Length);b.UnlockBits(d);return a;
  }
  static void Write(Bitmap b,byte[] a) {
    var d=b.LockBits(new Rectangle(0,0,b.Width,b.Height),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);
    Marshal.Copy(a,0,d.Scan0,a.Length);b.UnlockBits(d);
  }
  public static long Repair(Bitmap dst,Bitmap patch,int px,int py,Rectangle[] masks,int[] protectedRows) {
    if(patch.Width!=1536 || patch.Height!=1024)throw new Exception("Native repair must remain1536x1024; resizing is forbidden");
    int ds,ps;var d=Read(dst,out ds);var p=Read(patch,out ps);long changed=0;
    for(int y=0;y<patch.Height;y++)for(int x=0;x<patch.Width;x++) {
      double a=0;
      foreach(var r in masks)if(r.Contains(x,y)) {
        int z=Math.Min(Math.Min(x-r.Left,r.Right-1-x),Math.Min(y-r.Top,r.Bottom-1-y));
        a=Math.Max(a,Math.Min(1.0,z/16.0));
      }
      if(a==0 || Array.IndexOf(protectedRows,y)>=0)continue;
      int q=(py+y)*ds+(px+x)*3, v=y*ps+x*3;bool diff=false;
      for(int c=0;c<3;c++){byte n=(byte)Math.Round(d[q+c]*(1-a)+p[v+c]*a);diff|=n!=d[q+c];d[q+c]=n;}
      if(diff)changed++;
    }
    Write(dst,d);return changed;
  }
  public static void ConnectExtensions(Bitmap world,int coreY,int coreH) {
    int s;var a=Read(world,out s);int w=world.Width;
    foreach(bool top in new[]{true,false}) {
      int edge=top?coreY:coreY+coreH-1;
      int ext=top?coreY-1:coreY+coreH;
      var delta=new double[w*3];
      for(int x=0;x<w;x++)for(int c=0;c<3;c++) {
        double old=0,add=0;
        for(int k=0;k<12;k++){old+=a[(edge+(top?k:-k))*s+x*3+c];add+=a[(ext+(top?-k:k))*s+x*3+c];}
        delta[x*3+c]=(old-add)/12;
      }
      var smooth=new double[delta.Length];
      for(int c=0;c<3;c++) {
        double sum=0;int count=0;
        for(int x=-64;x<w+64;x++) {
          int enter=x+64,leave=x-65;
          if(enter>=0&&enter<w){sum+=delta[enter*3+c];count++;}
          if(leave>=0&&leave<w){sum-=delta[leave*3+c];count--;}
          if(x>=0&&x<w)smooth[x*3+c]=sum/count;
        }
      }
      for(int k=0;k<160;k++) {
        int y=ext+(top?-k:k);double f=1.0-k/160.0;
        for(int x=0;x<w;x++)for(int c=0;c<3;c++) {
          int q=y*s+x*3+c;double v=a[q]+smooth[x*3+c]*f;
          // Exact color continuity at the joining row; retain native texture away from the join.
          if(k<12){double t=k/12.0;v=a[edge*s+x*3+c]*(1-t)+v*t;}
          a[q]=(byte)Math.Clamp((int)Math.Round(v),0,255);
        }
      }
    }
    Write(world,a);
  }
  public static long CompareOverlap(Bitmap left,Bitmap right,int overlap) {
    int ls,rs;var l=Read(left,out ls);var r=Read(right,out rs);long n=0;
    for(int y=0;y<left.Height;y++)for(int x=0;x<overlap;x++){
      int a=y*ls+(left.Width-overlap+x)*3,b=y*rs+x*3;
      if(l[a]!=r[b]||l[a+1]!=r[b+1]||l[a+2]!=r[b+2])n++;
    }return n;
  }
  public static long[] AuditOriginal(Bitmap panorama,Bitmap[] originals,Rectangle[] allowed) {
    int ws;var w=Read(panorama,out ws);long outside=0,inside=0,total=0;
    for(int i=0;i<originals.Length;i++) {
      int os;var o=Read(originals[i],out os);
      for(int y=0;y<1884;y++)for(int x=0;x<5000;x++) {
        int wx=i*5000+x,a=(498+y)*ws+wx*3,b=y*os+x*3;
        total++;
        if(w[a]==o[b] && w[a+1]==o[b+1] && w[a+2]==o[b+2])continue;
        bool ok=false;foreach(var r in allowed)if(r.Contains(wx,y)){ok=true;break;}
        if(ok)inside++;else outside++;
      }
    }return new[]{total,inside,outside};
  }
}
