using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public sealed class CrowdRegion {
    public string Id;
    public string Kind="head";
    public PointF[] Polygon;
    public double Phase;
    public double AmplitudeX=2.0;
    public double AmplitudeY=2.4;
    public double RotationDegrees=0.25;
    public double Feather=8;
    public Rectangle Bounds;
    public float[] Alpha;
    public double PivotX;
    public double PivotY;
}

public sealed class CrowdIdleRenderer : IDisposable {
    public int Width,Height,Stride;
    public byte[] Source;
    public byte[] Mask;
    public short[] Owner;
    public List<CrowdRegion> Regions;
    public int[] ActivePixels;
    public int ActiveCount { get { return ActivePixels.Length; } }
    static double Smooth(double v) { v=Math.Max(0,Math.Min(1,v)); return v*v*(3-2*v); }
    public static PointF[] Ellipse(double cx,double cy,double rx,double ry) {
        PointF[] p=new PointF[48];
        for(int i=0;i<p.Length;i++) { double t=2*Math.PI*i/p.Length; p[i]=new PointF((float)(cx+rx*Math.Cos(t)),(float)(cy+ry*Math.Sin(t))); }
        return p;
    }
    public CrowdIdleRenderer(string path,List<CrowdRegion> regions,List<PointF[]> exclusions) {
        Regions=regions;
        using(Bitmap loaded=new Bitmap(path)) {
            Width=loaded.Width;Height=loaded.Height;
            using(Bitmap input=new Bitmap(Width,Height,PixelFormat.Format24bppRgb)) {
                using(Graphics g=Graphics.FromImage(input)) { g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(loaded,0,0); }
                BitmapData d=input.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
                Stride=d.Stride;Source=new byte[Stride*Height];Marshal.Copy(d.Scan0,Source,0,Source.Length);input.UnlockBits(d);
            }
        }
        int count=Width*Height;Mask=new byte[count];Owner=new short[count];for(int i=0;i<count;i++)Owner[i]=-1;
        byte[] excluded=null;
        if(exclusions!=null && exclusions.Count>0) {
            excluded=new byte[count];
            using(Bitmap eb=new Bitmap(Width,Height,PixelFormat.Format24bppRgb)) {
                using(Graphics g=Graphics.FromImage(eb)) {g.Clear(Color.Black);foreach(PointF[] p in exclusions)g.FillPolygon(Brushes.White,p);}
                BitmapData d=eb.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
                byte[] b=new byte[d.Stride*Height];Marshal.Copy(d.Scan0,b,0,b.Length);eb.UnlockBits(d);
                for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)excluded[y*Width+x]=b[y*d.Stride+x*3];
            }
        }
        if(regions.Count>32760)throw new ArgumentException("Too many regions");
        for(int n=0;n<regions.Count;n++) {
            CrowdRegion r=regions[n];
            if(r.Polygon==null || r.Polygon.Length<3)throw new ArgumentException("Region lacks polygon: "+r.Id);
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            foreach(PointF p in r.Polygon) {minX=Math.Min(minX,p.X);minY=Math.Min(minY,p.Y);maxX=Math.Max(maxX,p.X);maxY=Math.Max(maxY,p.Y);}
            int left=Math.Max(0,(int)Math.Floor(minX)-2),top=Math.Max(0,(int)Math.Floor(minY)-2);
            int right=Math.Min(Width,(int)Math.Ceiling(maxX)+3),bottom=Math.Min(Height,(int)Math.Ceiling(maxY)+3);
            if(right<=left || bottom<=top)throw new ArgumentException("Region outside master: "+r.Id);
            r.Bounds=new Rectangle(left,top,right-left,bottom-top);
            r.PivotX=(minX+maxX)/2.0;r.PivotY=maxY;
            int rw=r.Bounds.Width,rh=r.Bounds.Height;byte[] binary=new byte[rw*rh];
            using(Bitmap b=new Bitmap(rw,rh,PixelFormat.Format24bppRgb)) {
                using(Graphics g=Graphics.FromImage(b)) {
                    g.Clear(Color.Black);g.SmoothingMode=SmoothingMode.AntiAlias;
                    PointF[] local=new PointF[r.Polygon.Length];for(int k=0;k<local.Length;k++)local[k]=new PointF(r.Polygon[k].X-left,r.Polygon[k].Y-top);
                    g.FillPolygon(Brushes.White,local);
                }
                BitmapData d=b.LockBits(new Rectangle(0,0,rw,rh),ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);
                byte[] raw=new byte[d.Stride*rh];Marshal.Copy(d.Scan0,raw,0,raw.Length);b.UnlockBits(d);
                for(int y=0;y<rh;y++)for(int x=0;x<rw;x++) {
                    int i=y*rw+x;binary[i]=raw[y*d.Stride+x*3];
                    if(excluded!=null && excluded[(y+top)*Width+x+left]>0)binary[i]=0;
                }
            }
            float[] distance=new float[binary.Length];
            for(int i=0;i<distance.Length;i++)distance[i]=binary[i]<250?0:100000;
            const float diagonal=1.41421356f;
            for(int y=1;y<rh-1;y++)for(int x=1;x<rw-1;x++) {
                int i=y*rw+x;float v=distance[i];
                v=Math.Min(v,distance[i-1]+1);v=Math.Min(v,distance[i-rw]+1);
                v=Math.Min(v,distance[i-rw-1]+diagonal);v=Math.Min(v,distance[i-rw+1]+diagonal);distance[i]=v;
            }
            for(int y=rh-2;y>0;y--)for(int x=rw-2;x>0;x--) {
                int i=y*rw+x;float v=distance[i];
                v=Math.Min(v,distance[i+1]+1);v=Math.Min(v,distance[i+rw]+1);
                v=Math.Min(v,distance[i+rw-1]+diagonal);v=Math.Min(v,distance[i+rw+1]+diagonal);distance[i]=v;
            }
            r.Alpha=new float[binary.Length];double feather=Math.Max(2,r.Feather);
            for(int y=0;y<rh;y++)for(int x=0;x<rw;x++) {
                int i=y*rw+x,global=(y+top)*Width+x+left;
                float a=(float)Smooth(distance[i]/feather);r.Alpha[i]=a;
                byte ab=(byte)Math.Round(a*255);
                if(ab>Mask[global]) {Mask[global]=ab;Owner[global]=(short)n;}
            }
        }
        List<int> active=new List<int>();for(int i=0;i<count;i++)if(Mask[i]>0)active.Add(i);ActivePixels=active.ToArray();
        if(ActivePixels.Length==0)throw new ArgumentException("No active crowd pixels. Supply validated polygons inside people.");
    }
    public void SaveMasks(string folder,int tileWidth,string[] names) {
        Directory.CreateDirectory(folder);
        for(int p=0;p<names.Length;p++) {
            using(Bitmap mask=new Bitmap(tileWidth,Height,PixelFormat.Format24bppRgb)) {
                BitmapData d=mask.LockBits(new Rectangle(0,0,tileWidth,Height),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);
                byte[] raw=new byte[d.Stride*Height];
                for(int y=0;y<Height;y++)for(int x=0;x<tileWidth;x++) {
                    byte a=Mask[y*Width+p*tileWidth+x];int i=y*d.Stride+x*3;raw[i]=raw[i+1]=raw[i+2]=a;
                }
                Marshal.Copy(raw,0,d.Scan0,raw.Length);mask.UnlockBits(d);mask.Save(Path.Combine(folder,names[p]+"-mask.png"),ImageFormat.Png);
            }
        }
    }
    byte Sample(double x,double y,int channel) {
        x=Math.Max(0,Math.Min(Width-1.0001,x));y=Math.Max(0,Math.Min(Height-1.0001,y));
        int ix=(int)x,iy=(int)y;double fx=x-ix,fy=y-iy;
        int i=iy*Stride+ix*3+channel,j=i+Stride;
        double v=Source[i]*(1-fx)*(1-fy)+Source[i+3]*fx*(1-fy)+Source[j]*(1-fx)*fy+Source[j+3]*fx*fy;
        return (byte)Math.Max(0,Math.Min(255,Math.Round(v)));
    }
    public long Render(int frame,int frameCount,string folder,int tileWidth,string[] names,bool saveMaster) {
        byte[] result=(byte[])Source.Clone();
        double t=2*Math.PI*frame/frameCount;
        double[] dx=new double[Regions.Count],dy=new double[Regions.Count],angle=new double[Regions.Count];
        for(int n=0;n<Regions.Count;n++) {
            CrowdRegion r=Regions[n];double phase=t+r.Phase;
            dx[n]=r.AmplitudeX*Math.Sin(phase);
            dy[n]=r.AmplitudeY*Math.Cos(phase);
            angle[n]=r.RotationDegrees*Math.PI/180*Math.Sin(phase+0.35);
        }
        long changed=0;
        foreach(int index in ActivePixels) {
            int n=Owner[index];CrowdRegion r=Regions[n];int y=index/Width,x=index-y*Width;
            double a=Mask[index]/255.0;
            double ox=(dx[n]-(y-r.PivotY)*angle[n])*a;
            double oy=(dy[n]+(x-r.PivotX)*angle[n])*a;
            int output=y*Stride+x*3;bool different=false;
            for(int c=0;c<3;c++) {
                byte v=(byte)Math.Round(Source[output+c]*(1-a)+Sample(x-ox,y-oy,c)*a);
                result[output+c]=v;if(v!=Source[output+c])different=true;
            }
            if(different)changed++;
        }
        Directory.CreateDirectory(folder);
        for(int p=0;p<names.Length;p++) {
            string frameDir=Path.Combine(folder,"frames",names[p]);Directory.CreateDirectory(frameDir);
            string overlayDir=Path.Combine(folder,"crowd",names[p]);Directory.CreateDirectory(overlayDir);
            using(Bitmap b=new Bitmap(tileWidth,Height,PixelFormat.Format24bppRgb)) {
                BitmapData d=b.LockBits(new Rectangle(0,0,tileWidth,Height),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);
                byte[] raw=new byte[d.Stride*Height];
                for(int y=0;y<Height;y++)Buffer.BlockCopy(result,y*Stride+p*tileWidth*3,raw,y*d.Stride,tileWidth*3);
                Marshal.Copy(raw,0,d.Scan0,raw.Length);b.UnlockBits(d);b.Save(Path.Combine(frameDir,names[p]+"-frame-"+(frame+1).ToString("00")+".png"),ImageFormat.Png);
            }
            using(Bitmap b=new Bitmap(tileWidth,Height,PixelFormat.Format32bppArgb)) {
                BitmapData d=b.LockBits(new Rectangle(0,0,tileWidth,Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
                byte[] raw=new byte[d.Stride*Height];
                foreach(int index in ActivePixels) {
                    int y=index/Width,x=index-y*Width;if(x<p*tileWidth||x>=(p+1)*tileWidth)continue;
                    int n=Owner[index];CrowdRegion r=Regions[n];double a=Mask[index]/255.0;
                    double ox=(dx[n]-(y-r.PivotY)*angle[n])*a,oy=(dy[n]+(x-r.PivotX)*angle[n])*a;
                    int outIndex=y*d.Stride+(x-p*tileWidth)*4;
                    raw[outIndex]=Sample(x-ox,y-oy,0);raw[outIndex+1]=Sample(x-ox,y-oy,1);raw[outIndex+2]=Sample(x-ox,y-oy,2);raw[outIndex+3]=Mask[index];
                }
                Marshal.Copy(raw,0,d.Scan0,raw.Length);b.UnlockBits(d);b.Save(Path.Combine(overlayDir,"crowd-frame-"+(frame+1).ToString("00")+".png"),ImageFormat.Png);
            }
        }
        if(saveMaster) {
            string masterDir=Path.Combine(folder,"panorama-frames");Directory.CreateDirectory(masterDir);
            using(Bitmap b=new Bitmap(Width,Height,PixelFormat.Format24bppRgb)) {
                BitmapData d=b.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);
                for(int y=0;y<Height;y++)Marshal.Copy(result,y*Stride,d.Scan0+y*d.Stride,Width*3);
                b.UnlockBits(d);b.Save(Path.Combine(masterDir,"panorama-frame-"+(frame+1).ToString("00")+".png"),ImageFormat.Png);
            }
        }
        return changed;
    }
    public void SaveAnnotation(string path,int maxWidth) {
        double s=Math.Min(1,maxWidth/(double)Width);int w=(int)Math.Round(Width*s),h=(int)Math.Round(Height*s);
        using(Bitmap preview=new Bitmap(w,h,PixelFormat.Format24bppRgb)) {
            using(Graphics g=Graphics.FromImage(preview)) {
                using(Bitmap sourceBitmap=new Bitmap(Width,Height,PixelFormat.Format24bppRgb)) {
                    BitmapData d=sourceBitmap.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);Marshal.Copy(Source,0,d.Scan0,Source.Length);sourceBitmap.UnlockBits(d);
                    g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(sourceBitmap,new Rectangle(0,0,w,h));
                }
                using(Pen pen=new Pen(Color.Lime,1))using(Font font=new Font("Arial",8))foreach(CrowdRegion r in Regions) {
                    PointF[] p=new PointF[r.Polygon.Length];for(int i=0;i<p.Length;i++)p[i]=new PointF((float)(r.Polygon[i].X*s),(float)(r.Polygon[i].Y*s));
                    g.DrawPolygon(pen,p);g.DrawString(r.Id,font,Brushes.White,p[0]);
                }
            }
            preview.Save(path,ImageFormat.Png);
        }
    }
    public void SavePanelAnnotations(string folder,int tileWidth,int previewWidth) {
        using(Bitmap sourceBitmap=new Bitmap(Width,Height,PixelFormat.Format24bppRgb)) {
            BitmapData data=sourceBitmap.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.WriteOnly,PixelFormat.Format24bppRgb);
            Marshal.Copy(Source,0,data.Scan0,Source.Length);sourceBitmap.UnlockBits(data);
            double scale=previewWidth/(double)tileWidth;
            for(int panel=0;panel<Width/tileWidth;panel++) {
                using(Bitmap preview=new Bitmap(previewWidth,(int)Math.Round(Height*scale),PixelFormat.Format24bppRgb))
                using(Graphics g=Graphics.FromImage(preview))
                using(Font font=new Font("Arial",9,FontStyle.Bold)) {
                    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                    g.DrawImage(sourceBitmap,new Rectangle(0,0,preview.Width,preview.Height),panel*tileWidth,0,tileWidth,Height,GraphicsUnit.Pixel);
                    for(int region=0;region<Regions.Count;region++) {
                        CrowdRegion r=Regions[region];if(r.Bounds.Right<panel*tileWidth||r.Bounds.Left>=(panel+1)*tileWidth)continue;
                        Color color=r.Kind=="head"?Color.Lime:(r.Kind=="hand"?Color.Yellow:Color.Cyan);
                        PointF[] points=new PointF[r.Polygon.Length];for(int i=0;i<points.Length;i++)points[i]=new PointF((float)((r.Polygon[i].X-panel*tileWidth)*scale),(float)(r.Polygon[i].Y*scale));
                        using(Pen pen=new Pen(color,1))g.DrawPolygon(pen,points);
                        using(Brush brush=new SolidBrush(color))g.DrawString((region+1).ToString(),font,brush,(float)((r.Bounds.Left-panel*tileWidth)*scale),(float)(r.Bounds.Top*scale)-12);
                    }
                    preview.Save(Path.Combine(folder,"crowd-mask-panel-"+(panel+1)+".png"),ImageFormat.Png);
                }
            }
        }
    }
    public void Dispose() { Source=null;Mask=null;Owner=null;ActivePixels=null;foreach(CrowdRegion r in Regions)r.Alpha=null; }
}
