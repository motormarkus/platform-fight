using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.IO;

public static class WorksiteDetail {
 public static byte[] Read(Bitmap im) {
  var bd=im.LockBits(new Rectangle(0,0,im.Width,im.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  byte[] d=new byte[im.Width*im.Height*4];
  for(int y=0;y<im.Height;y++) Marshal.Copy(IntPtr.Add(bd.Scan0,y*bd.Stride),d,y*im.Width*4,im.Width*4);
  im.UnlockBits(bd);return d;
 }
 public static Bitmap Image(byte[] d,int w,int h) {
  Bitmap im=new Bitmap(w,h,PixelFormat.Format32bppArgb);
  var bd=im.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
  for(int y=0;y<h;y++)Marshal.Copy(d,y*w*4,IntPtr.Add(bd.Scan0,y*bd.Stride),w*4);
  im.UnlockBits(bd);return im;
 }
 public static byte[] Load(string path,out int w,out int h){
  using(Bitmap im=new Bitmap(path)){w=im.Width;h=im.Height;return Read(im);}
 }
 public static void Save(byte[] d,int w,int h,string path){using(Bitmap im=Image(d,w,h))im.Save(path,ImageFormat.Png);}
 public static void Clean(byte[] d){
  for(int p=0;p<d.Length;p+=4){
   if(d[p+3]<32){d[p]=d[p+1]=d[p+2]=d[p+3]=0;}
   else if(d[p+3]>220)d[p+3]=255;
  }
 }
 public static byte[] Resize(byte[] d,int iw,int ih,int w,int h){
  using(Bitmap src=Image(d,iw,ih))using(Bitmap dst=new Bitmap(w,h,PixelFormat.Format32bppArgb)){
   using(Graphics g=Graphics.FromImage(dst))using(ImageAttributes attr=new ImageAttributes()){
    g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;attr.SetWrapMode(WrapMode.TileFlipXY);
    g.DrawImage(src,new Rectangle(0,0,w,h),0,0,iw,ih,GraphicsUnit.Pixel,attr);
   }return Read(dst);
  }
 }
 static int Clamp(int v,int a,int b){return Math.Max(a,Math.Min(b,v));}
 static double ClampD(double v,double a,double b){return Math.Max(a,Math.Min(b,v));}
 static double PatchCost(byte[] a,byte[] b,int w,int h,int cx,int cy,int dx,int dy){
  double cost=0;int count=0;
  for(int yy=-80;yy<=80;yy+=10) for(int xx=-80;xx<=80;xx+=10){
   int ax=cx+xx,ay=cy+yy,bx=ax+dx,by=ay+dy;
   if(ax<0||ay<0||bx<0||by<0||ax>=w||bx>=w||ay>=h||by>=h)continue;
   int p=(ay*w+ax)*4,q=(by*w+bx)*4;
   double aa=a[p+3]/255.0,bb=b[q+3]/255.0;
   cost+=Math.Abs(a[p+3]-b[q+3])*2.5;
   for(int c=0;c<3;c++)cost+=Math.Abs(a[p+c]*aa-b[q+c]*bb)/3.0;
   count++;
  }
  return cost/Math.Max(1,count)+0.012*(dx*dx+dy*dy);
 }
 // Small smoothly varying registration aligns the newly drawn geometry to the original layout.
 public static byte[] Register(byte[] reference,byte[] input,int w,int h){
  int step=128,nx=(w-1)/step+2,ny=(h-1)/step+2;
  double[] fx=new double[nx*ny],fy=new double[nx*ny];
  for(int gy=0;gy<ny;gy++)for(int gx=0;gx<nx;gx++){
   int cx=Math.Min(w-1,gx*step),cy=Math.Min(h-1,gy*step),idx=gy*nx+gx;
   double best=PatchCost(reference,input,w,h,cx,cy,0,0);int bx=0,by=0;
   for(int dy=-32;dy<=32;dy+=4)for(int dx=-32;dx<=32;dx+=4){
    double score=PatchCost(reference,input,w,h,cx,cy,dx,dy);
    if(score<best){best=score;bx=dx;by=dy;}
   }
   int sx=bx,sy=by;
   for(int dy=sy-3;dy<=sy+3;dy++)for(int dx=sx-3;dx<=sx+3;dx++){
    double score=PatchCost(reference,input,w,h,cx,cy,dx,dy);
    if(score<best){best=score;bx=dx;by=dy;}
   }
   fx[idx]=bx;fy[idx]=by;
  }
  // Mild regularization avoids ripples in straight columns.
  for(int pass=0;pass<2;pass++){
   double[] tx=(double[])fx.Clone(),ty=(double[])fy.Clone();
   for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){
    int i=y*nx+x;double sx=fx[i]*6,sy=fy[i]*6,n=6;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
     if(Math.Abs(dx)+Math.Abs(dy)!=1)continue;
     int xx=x+dx,yy=y+dy;if(xx<0||yy<0||xx>=nx||yy>=ny)continue;
     sx+=fx[yy*nx+xx];sy+=fy[yy*nx+xx];n++;
    }tx[i]=sx/n;ty[i]=sy/n;
   }fx=tx;fy=ty;
  }
  byte[] result=new byte[input.Length];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int gx=x/step,gy=y/step;double ux=(x%step)/(double)step,uy=(y%step)/(double)step;
   int i=gy*nx+gx;
   double dx=fx[i]*(1-ux)*(1-uy)+fx[i+1]*ux*(1-uy)+fx[i+nx]*(1-ux)*uy+fx[i+nx+1]*ux*uy;
   double dy=fy[i]*(1-ux)*(1-uy)+fy[i+1]*ux*(1-uy)+fy[i+nx]*(1-ux)*uy+fy[i+nx+1]*ux*uy;
   double sx=ClampD(x+dx,0,w-1.001),sy=ClampD(y+dy,0,h-1.001);
   int ix=(int)sx,iy=(int)sy;double u=sx-ix,v=sy-iy;
   int p=(y*w+x)*4,q=(iy*w+ix)*4;
   double wa=(1-u)*(1-v),wb=u*(1-v),wc=(1-u)*v,wd=u*v;
   double aa=input[q+3]*wa,ab=input[q+7]*wb,ac=input[q+w*4+3]*wc,ad=input[q+w*4+7]*wd,alpha=aa+ab+ac+ad;
   if(alpha<1)continue;
   for(int c=0;c<3;c++)result[p+c]=(byte)Clamp((int)Math.Round((input[q+c]*aa+input[q+4+c]*ab+input[q+w*4+c]*ac+input[q+w*4+4+c]*ad)/alpha),0,255);
   result[p+3]=(byte)Clamp((int)Math.Round(alpha),0,255);
  }return result;
 }
 // Correct broad color differences; do not blur any newly rendered fine detail.
 public static void MatchTone(byte[] reference,byte[] d,int w,int h){
  int step=192,nx=(w-1)/step+2,ny=(h-1)/step+2;
  double[] offsets=new double[nx*ny*3];
  for(int gy=0;gy<ny;gy++)for(int gx=0;gx<nx;gx++){
   double[] sum=new double[3];int count=0;
   int cx=Math.Min(w-1,gx*step),cy=Math.Min(h-1,gy*step);
   for(int y=Math.Max(0,cy-160);y<Math.Min(h,cy+161);y+=6)for(int x=Math.Max(0,cx-160);x<Math.Min(w,cx+161);x+=6){
    int p=(y*w+x)*4;if(reference[p+3]<245||d[p+3]<245)continue;
    for(int c=0;c<3;c++)sum[c]+=reference[p+c]-d[p+c];count++;
   }
   for(int c=0;c<3;c++)offsets[(gy*nx+gx)*3+c]=count>30?ClampD(sum[c]/count,-32,32)*0.85:0;
  }
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){
   int p=(y*w+x)*4;if(d[p+3]==0)continue;
   int gx=x/step,gy=y/step;double u=(x%step)/(double)step,v=(y%step)/(double)step;
   for(int c=0;c<3;c++){
    int i=(gy*nx+gx)*3+c;
    double delta=offsets[i]*(1-u)*(1-v)+offsets[i+3]*u*(1-v)+offsets[i+nx*3]*(1-u)*v+offsets[i+nx*3+3]*u*v;
    d[p+c]=(byte)Clamp((int)Math.Round(d[p+c]+delta),0,255);
   }
  }
 }
 public static void Process(string raw,string reference,string target){
  int rw,rh,w,h;byte[] d=Load(raw,out rw,out rh);Clean(d);
  byte[] r=Load(reference,out w,out h);d=Resize(d,rw,rh,w,h);Clean(d);
  d=Register(r,d,w,h);MatchTone(r,d,w,h);Clean(d);Save(d,w,h,target);
 }
 static double Diff(byte[] a,int p,byte[] b,int q){
  double aa=a[p+3]/255.0,bb=b[q+3]/255.0,cost=Math.Abs(a[p+3]-b[q+3])*3.0;
  for(int c=0;c<3;c++)cost+=Math.Abs(a[p+c]*aa-b[q+c]*bb);
  return cost;
 }
 static void BlendPixel(byte[] a,int p,byte[] b,int q,double t){
  if(t<=0)return;if(t>=1){Buffer.BlockCopy(b,q,a,p,4);return;}
  double aa=a[p+3]*(1-t),bb=b[q+3]*t,alpha=aa+bb;
  if(alpha<1){a[p]=a[p+1]=a[p+2]=a[p+3]=0;return;}
  for(int c=0;c<3;c++)a[p+c]=(byte)Clamp((int)Math.Round((a[p+c]*aa+b[q+c]*bb)/alpha),0,255);
  a[p+3]=(byte)Clamp((int)Math.Round(alpha),0,255);
 }
 static double PairCost(byte[] a,int aw,int ah,byte[] b,int bw,int bh,int ox,int oy,int cx,int cy,int dx,int dy,bool vertical,int overlap){
  double score=0;int n=0;
  for(int yy=-40;yy<=40;yy+=5)for(int xx=-40;xx<=40;xx+=5){
   int x=cx+xx,y=cy+yy,tx=x+dx,ty=y+dy;
   if(x<0||y<0||x>=bw||y>=bh||tx<0||ty<0||tx>=bw||ty>=bh)continue;
   if((vertical?x:y)>=overlap||x+ox>=aw||y+oy>=ah)continue;
   score+=Diff(a,((y+oy)*aw+x+ox)*4,b,(ty*bw+tx)*4);n++;
  }return score/Math.Max(1,n)+0.025*(dx*dx+dy*dy);
 }
 // Match adjacent independently drawn edges directly; taper the warp back into the new tile.
 static byte[] AlignOverlap(byte[] a,int aw,int ah,byte[] b,int bw,int bh,int ox,int oy,int overlap,bool vertical){
  int sx=vertical?64:96,sy=vertical?96:64;
  int mx=vertical?overlap:bw,my=vertical?bh:overlap;
  int nx=(mx-1)/sx+2,ny=(my-1)/sy+2;double[] fx=new double[nx*ny],fy=new double[nx*ny];
  for(int gy=0;gy<ny;gy++)for(int gx=0;gx<nx;gx++){
   int cx=Math.Min(mx-1,gx*sx),cy=Math.Min(my-1,gy*sy),i=gy*nx+gx,bx=0,by=0;
   double best=PairCost(a,aw,ah,b,bw,bh,ox,oy,cx,cy,0,0,vertical,overlap);
   for(int dy=-24;dy<=24;dy+=3)for(int dx=-24;dx<=24;dx+=3){
    double sc=PairCost(a,aw,ah,b,bw,bh,ox,oy,cx,cy,dx,dy,vertical,overlap);
    if(sc<best){best=sc;bx=dx;by=dy;}
   }
   int ex=bx,ey=by;
   for(int dy=ey-2;dy<=ey+2;dy++)for(int dx=ex-2;dx<=ex+2;dx++){
    double sc=PairCost(a,aw,ah,b,bw,bh,ox,oy,cx,cy,dx,dy,vertical,overlap);
    if(sc<best){best=sc;bx=dx;by=dy;}
   }fx[i]=bx;fy[i]=by;
  }
  double[] fxx=(double[])fx.Clone(),fyy=(double[])fy.Clone();
  for(int gy=0;gy<ny;gy++)for(int gx=0;gx<nx;gx++){
   int i=gy*nx+gx;double tx=fx[i]*8,ty=fy[i]*8,n=8;
   for(int yy=-1;yy<=1;yy++)for(int xx=-1;xx<=1;xx++){
    if(Math.Abs(xx)+Math.Abs(yy)!=1)continue;int x=gx+xx,y=gy+yy;if(x<0||y<0||x>=nx||y>=ny)continue;
    tx+=fx[y*nx+x];ty+=fy[y*nx+x];n++;
   }fxx[i]=tx/n;fyy[i]=ty/n;
  }fx=fxx;fy=fyy;
  byte[] result=(byte[])b.Clone();int wx=vertical?Math.Min(bw,overlap+192):bw,wy=vertical?bh:Math.Min(bh,overlap+192);
  for(int y=0;y<wy;y++)for(int x=0;x<wx;x++){
   double px=Math.Min(x,mx-1),py=Math.Min(y,my-1);int gx=(int)px/sx,gy=(int)py/sy;
   double u=(px-gx*sx)/sx,v=(py-gy*sy)/sy;int i=gy*nx+gx;
   double dx=fx[i]*(1-u)*(1-v)+fx[i+1]*u*(1-v)+fx[i+nx]*(1-u)*v+fx[i+nx+1]*u*v;
   double dy=fy[i]*(1-u)*(1-v)+fy[i+1]*u*(1-v)+fy[i+nx]*(1-u)*v+fy[i+nx+1]*u*v;
   double fade=ClampD((overlap+192.0-(vertical?x:y))/192,0,1);fade=fade*fade*(3-2*fade);
   double xx=ClampD(x+dx*fade,0,bw-1.001),yy=ClampD(y+dy*fade,0,bh-1.001);int ix=(int)xx,iy=(int)yy;
   u=xx-ix;v=yy-iy;int p=(y*bw+x)*4,q=(iy*bw+ix)*4;
   double aa=b[q+3]*(1-u)*(1-v),ab=b[q+7]*u*(1-v),ac=b[q+bw*4+3]*(1-u)*v,ad=b[q+bw*4+7]*u*v,alpha=aa+ab+ac+ad;
   if(alpha<1){result[p]=result[p+1]=result[p+2]=result[p+3]=0;continue;}
   for(int c=0;c<3;c++)result[p+c]=(byte)Clamp((int)Math.Round((b[q+c]*aa+b[q+4+c]*ab+b[q+bw*4+c]*ac+b[q+bw*4+4+c]*ad)/alpha),0,255);
   result[p+3]=(byte)Clamp((int)Math.Round(alpha),0,255);
  }return result;
 }
 // Dynamic minimum-error cut across the overlap, with a narrow 12px transition.
 public static void JoinX(byte[] canvas,int cw,int ch,byte[] b,int bw,int bh,int x0,int overlap){
  if(overlap<=0){for(int y=0;y<bh;y++)Buffer.BlockCopy(b,y*bw*4,canvas,(y*cw+x0)*4,bw*4);return;}
  b=AlignOverlap(canvas,cw,ch,b,bw,bh,x0,0,overlap,true);
  int margin=32,n=overlap-margin*2;
  double[] prev=new double[n],curr=new double[n];sbyte[] parent=new sbyte[n*bh];
  for(int y=0;y<bh;y++){
   for(int k=0;k<n;k++){
    int x=k+margin;double cost=Diff(canvas,(y*cw+x0+x)*4,b,(y*bw+x)*4)+0.001*(k-n/2)*(k-n/2);
    if(y==0){curr[k]=cost;continue;}
    double best=prev[k];sbyte move=0;
    if(k>0&&prev[k-1]+0.5<best){best=prev[k-1]+0.5;move=-1;}
    if(k+1<n&&prev[k+1]+0.5<best){best=prev[k+1]+0.5;move=1;}
    curr[k]=cost+best;parent[y*n+k]=move;
   }var temp=prev;prev=curr;curr=temp;
  }
  int end=0;for(int k=1;k<n;k++)if(prev[k]<prev[end])end=k;
  int[] seam=new int[bh];
  for(int y=bh-1;y>=0;y--){seam[y]=end+margin;if(y>0)end+=parent[y*n+end];}
  for(int y=0;y<bh;y++)for(int x=0;x<bw;x++){
   double t=ClampD((x-seam[y]+6)/12.0,0,1);t=t*t*(3-2*t);
   BlendPixel(canvas,(y*cw+x0+x)*4,b,(y*bw+x)*4,t);
  }
 }
 public static void JoinY(byte[] canvas,int cw,int ch,byte[] b,int bw,int bh,int y0,int overlap){
  if(overlap<=0){Buffer.BlockCopy(b,0,canvas,y0*cw*4,b.Length);return;}
  b=AlignOverlap(canvas,cw,ch,b,bw,bh,0,y0,overlap,false);
  int margin=32,n=overlap-margin*2;
  double[] prev=new double[n],curr=new double[n];sbyte[] parent=new sbyte[n*bw];
  for(int x=0;x<bw;x++){
   for(int k=0;k<n;k++){
    int y=k+margin;double cost=Diff(canvas,((y0+y)*cw+x)*4,b,(y*bw+x)*4)+0.001*(k-n/2)*(k-n/2);
    if(x==0){curr[k]=cost;continue;}
    double best=prev[k];sbyte move=0;
    if(k>0&&prev[k-1]+0.5<best){best=prev[k-1]+0.5;move=-1;}
    if(k+1<n&&prev[k+1]+0.5<best){best=prev[k+1]+0.5;move=1;}
    curr[k]=cost+best;parent[x*n+k]=move;
   }var temp=prev;prev=curr;curr=temp;
  }
  int end=0;for(int k=1;k<n;k++)if(prev[k]<prev[end])end=k;
  int[] seam=new int[bw];
  for(int x=bw-1;x>=0;x--){seam[x]=end+margin;if(x>0)end+=parent[x*n+end];}
  for(int y=0;y<bh;y++)for(int x=0;x<bw;x++){
   double t=ClampD((y-seam[x]+6)/12.0,0,1);t=t*t*(3-2*t);
   BlendPixel(canvas,((y0+y)*cw+x)*4,b,(y*bw+x)*4,t);
  }
 }
 public static void EdgeCollar(byte[] d,byte[] original,int w,int h){
  for(int y=0;y<h;y++)for(int x=0;x<384;x++){
   double t=ClampD((x-24)/360.0,0,1);t=t*t*(3-2*t);
   BlendPixel(d,(y*w+x)*4,original,(y*w+x)*4,1-t);
  }
 }
 public static void Preview(string input,string output,int w,int h){
  int iw,ih;byte[] d=Load(input,out iw,out ih);d=Resize(d,iw,ih,w,h);
  // Composite over navy solely for the preview; the main asset keeps alpha.
  for(int p=0;p<d.Length;p+=4){
   double a=d[p+3]/255.0;d[p]=(byte)(d[p]*a+52*(1-a));d[p+1]=(byte)(d[p+1]*a+40*(1-a));d[p+2]=(byte)(d[p+2]*a+33*(1-a));d[p+3]=255;
  }Save(d,w,h,output);
 }
 public static void Crop(string input,string output,int x,int y,int w,int h){
  using(Bitmap src=new Bitmap(input))using(Bitmap crop=src.Clone(new Rectangle(x,y,w,h),PixelFormat.Format32bppArgb))crop.Save(output,ImageFormat.Png);
 }
}

