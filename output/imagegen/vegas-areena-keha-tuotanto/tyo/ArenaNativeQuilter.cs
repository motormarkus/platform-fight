using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public sealed class ArenaNativePatch
{
    public string File;
    public int X, Y, Width = 1536, Height = 1024, CropX, CropY;
    public Rectangle[] AvoidRectangles = new Rectangle[0];
}

public sealed class ArenaNativeSourceReport
{
    public string file;
    public int sourceWidth, sourceHeight, cropX, cropY, targetWidth, targetHeight, x, y;
    public double scaleX = 1, scaleY = 1;
}

public sealed class ArenaNativeSeamReport
{
    public string file;
    public int x, y, verticalOverlap, horizontalOverlap;
    public double verticalMeanCost, horizontalMeanCost;
    public int verticalProtectedSamples, horizontalProtectedSamples;
}

public sealed class ArenaNativeComposeReport
{
    public int width, height, frameWidth, panelCount;
    public bool allPixelsCovered;
    public long uncoveredPixels;
    public long sourcePixelsResampled = 0;
    public long colorAdjustedPixels = 0;
    public long nativeGridBlendedPixels = 0;
    public string method = "Native one-to-one crops; hard minimum-error overlap seams; no crossfading of people";
    public List<ArenaNativeSourceReport> nativePatches = new List<ArenaNativeSourceReport>();
    public List<ArenaNativeSeamReport> seams = new List<ArenaNativeSeamReport>();
    public List<ArenaNativeColorReport> colorLeveling = new List<ArenaNativeColorReport>();
}

public sealed class ArenaNativeColorReport
{
    public string file, region;
    public double offsetR, offsetG, offsetB;
    public int sampleCount, x, y, width, height;
    public long adjustedPixels;
    public string method = "additive low-frequency color offset";
}

public static class ArenaNativeQuilter
{
    private sealed class Raster
    {
        public int Width, Height, Stride;
        public byte[] Pixels;
    }

    private static Raster ReadCrop(ArenaNativePatch spec, out int nativeWidth, out int nativeHeight)
    {
        using (Bitmap source = new Bitmap(spec.File))
        {
            nativeWidth = source.Width;
            nativeHeight = source.Height;
            if (spec.Width <= 0 || spec.Height <= 0 || spec.CropX < 0 || spec.CropY < 0 ||
                spec.CropX + spec.Width > nativeWidth || spec.CropY + spec.Height > nativeHeight)
                throw new InvalidOperationException("Native source is too small for the requested one-to-one crop; upscaling is forbidden: " + spec.File + " (" + nativeWidth + "x" + nativeHeight + ")");
            using (Bitmap crop = source.Clone(new Rectangle(spec.CropX, spec.CropY, spec.Width, spec.Height), PixelFormat.Format24bppRgb))
            {
                BitmapData data = crop.LockBits(new Rectangle(0, 0, crop.Width, crop.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                Raster result = new Raster { Width = crop.Width, Height = crop.Height, Stride = (crop.Width * 3 + 3) & ~3 };
                result.Pixels = new byte[result.Stride * result.Height];
                for (int y = 0; y < result.Height; y++)
                    Marshal.Copy(IntPtr.Add(data.Scan0, data.Stride * y), result.Pixels, result.Stride * y, result.Width * 3);
                crop.UnlockBits(data);
                return result;
            }
        }
    }

    private static void SaveRaster(string file, Raster pixels)
    {
        using (Bitmap result = new Bitmap(pixels.Width, pixels.Height, PixelFormat.Format24bppRgb))
        {
            BitmapData data = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            for (int y = 0; y < pixels.Height; y++)
                Marshal.Copy(pixels.Pixels, pixels.Stride * y, IntPtr.Add(data.Scan0, data.Stride * y), pixels.Width * 3);
            result.UnlockBits(data);
            result.Save(file, ImageFormat.Png);
        }
    }

    private static double Luma(Raster r, int x, int y)
    {
        x = Math.Max(0, Math.Min(r.Width - 1, x));
        y = Math.Max(0, Math.Min(r.Height - 1, y));
        int i = y * r.Stride + x * 3;
        return .114 * r.Pixels[i] + .587 * r.Pixels[i + 1] + .299 * r.Pixels[i + 2];
    }

    private static double Cost(Raster master, Raster patch, ArenaNativePatch spec, int px, int py)
    {
        // A small local window discourages seams that match a single accidental texture pixel.
        double diff = 0;
        for (int dy = -1; dy <= 1; dy++)
        {
            int sy = Math.Max(0, Math.Min(patch.Height - 1, py + dy));
            int my = spec.Y + sy;
            for (int dx = -1; dx <= 1; dx++)
            {
                int sx = Math.Max(0, Math.Min(patch.Width - 1, px + dx));
                int mx = spec.X + sx;
                int a = my * master.Stride + mx * 3;
                int b = sy * patch.Stride + sx * 3;
                for (int c = 0; c < 3; c++)
                {
                    double d = master.Pixels[a + c] - patch.Pixels[b + c];
                    diff += d * d;
                }
            }
        }
        diff /= 27;
        double gOld = Math.Abs(Luma(master, spec.X + px + 1, spec.Y + py) - Luma(master, spec.X + px - 1, spec.Y + py))
                    + Math.Abs(Luma(master, spec.X + px, spec.Y + py + 1) - Luma(master, spec.X + px, spec.Y + py - 1));
        double gNew = Math.Abs(Luma(patch, px + 1, py) - Luma(patch, px - 1, py))
                    + Math.Abs(Luma(patch, px, py + 1) - Luma(patch, px, py - 1));
        // Prefer quiet material between silhouettes and trim rather than cutting a face or outline.
        diff += 5 * Math.Abs(gOld - gNew) + 1.5 * (gOld + gNew);
        foreach (Rectangle protectedArea in spec.AvoidRectangles)
            if (protectedArea.Contains(px, py)) diff += 1000000;
        return diff;
    }

    private static int[] VerticalSeam(Raster master, Raster patch, ArenaNativePatch spec, int overlap, out double meanCost)
    {
        int rows = patch.Height;
        double[] previous = new double[overlap], current = new double[overlap];
        sbyte[] direction = new sbyte[overlap * rows];
        int margin = Math.Min(12, Math.Max(0, (overlap - 1) / 4));
        for (int x = 0; x < overlap; x++) previous[x] = Cost(master, patch, spec, x, 0) + ((x < margin || x >= overlap - margin) ? 10000000 : 0);
        for (int y = 1; y < rows; y++)
        {
            for (int x = 0; x < overlap; x++)
            {
                int from = x;
                double best = previous[x];
                if (x > 0 && previous[x - 1] + 3 < best) { best = previous[x - 1] + 3; from = x - 1; }
                if (x + 1 < overlap && previous[x + 1] + 3 < best) { best = previous[x + 1] + 3; from = x + 1; }
                direction[y * overlap + x] = (sbyte)(from - x);
                current[x] = best + Cost(master, patch, spec, x, y) + ((x < margin || x >= overlap - margin) ? 10000000 : 0);
            }
            double[] swap = previous; previous = current; current = swap;
        }
        int end = 0;
        for (int x = 1; x < overlap; x++) if (previous[x] < previous[end]) end = x;
        meanCost = previous[end] / rows;
        int[] seam = new int[rows];
        seam[rows - 1] = end;
        for (int y = rows - 1; y > 0; y--) seam[y - 1] = seam[y] + direction[y * overlap + seam[y]];
        return seam;
    }

    private static int[] HorizontalSeam(Raster master, Raster patch, ArenaNativePatch spec, int overlap, out double meanCost)
    {
        int cols = patch.Width;
        double[] previous = new double[overlap], current = new double[overlap];
        sbyte[] direction = new sbyte[overlap * cols];
        int margin = Math.Min(10, Math.Max(0, (overlap - 1) / 4));
        for (int y = 0; y < overlap; y++) previous[y] = Cost(master, patch, spec, 0, y) + ((y < margin || y >= overlap - margin) ? 10000000 : 0);
        for (int x = 1; x < cols; x++)
        {
            for (int y = 0; y < overlap; y++)
            {
                int from = y;
                double best = previous[y];
                if (y > 0 && previous[y - 1] + 3 < best) { best = previous[y - 1] + 3; from = y - 1; }
                if (y + 1 < overlap && previous[y + 1] + 3 < best) { best = previous[y + 1] + 3; from = y + 1; }
                direction[x * overlap + y] = (sbyte)(from - y);
                current[y] = best + Cost(master, patch, spec, x, y) + ((y < margin || y >= overlap - margin) ? 10000000 : 0);
            }
            double[] swap = previous; previous = current; current = swap;
        }
        int end = 0;
        for (int y = 1; y < overlap; y++) if (previous[y] < previous[end]) end = y;
        meanCost = previous[end] / cols;
        int[] seam = new int[cols]; seam[cols - 1] = end;
        for (int x = cols - 1; x > 0; x--) seam[x - 1] = seam[x] + direction[x * overlap + seam[x]];
        return seam;
    }

    private static int FullLeftOverlap(byte[] owners, int worldWidth, ArenaNativePatch spec)
    {
        int width = 0;
        for (int x = 0; x < spec.Width; x++)
        {
            bool full = true;
            for (int y = 0; y < spec.Height; y++) if (owners[(spec.Y + y) * worldWidth + spec.X + x] == 0) { full = false; break; }
            if (!full) break;
            width++;
        }
        return width;
    }

    private static int FullTopOverlap(byte[] owners, int worldWidth, ArenaNativePatch spec)
    {
        int height = 0;
        for (int y = 0; y < spec.Height; y++)
        {
            bool full = true;
            for (int x = 0; x < spec.Width; x++) if (owners[(spec.Y + y) * worldWidth + spec.X + x] == 0) { full = false; break; }
            if (!full) break;
            height++;
        }
        return height;
    }

    private static void SavePreview(string path, Raster r, int width)
    {
        int height = Math.Max(1, (int)Math.Round((double)r.Height * width / r.Width));
        Raster p = new Raster { Width = width, Height = height, Stride = (width * 3 + 3) & ~3 };
        p.Pixels = new byte[p.Stride * height];
        // Preview-only downsampling; full master and panels never use this path.
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            int x0 = x * r.Width / width, x1 = Math.Max(x0 + 1, (x + 1) * r.Width / width);
            int y0 = y * r.Height / height, y1 = Math.Max(y0 + 1, (y + 1) * r.Height / height);
            int count = (x1 - x0) * (y1 - y0);
            for (int c = 0; c < 3; c++)
            {
                long sum = 0;
                for (int sy = y0; sy < y1; sy++) for (int sx = x0; sx < x1; sx++) sum += r.Pixels[sy * r.Stride + sx * 3 + c];
                p.Pixels[y * p.Stride + x * 3 + c] = (byte)(sum / count);
            }
        }
        SaveRaster(path, p);
    }

    private static double Smooth(double value)
    {
        value=Math.Max(0,Math.Min(1,value));return value*value*(3-2*value);
    }

    private static double TrimmedMean(int[] histogram, int count)
    {
        int discard=count/10, remaining=count-2*discard, low=discard, high=discard;
        int[] bins=(int[])histogram.Clone();
        for(int i=0;i<511&&low>0;i++){int take=Math.Min(low,bins[i]);bins[i]-=take;low-=take;}
        for(int i=510;i>=0&&high>0;i--){int take=Math.Min(high,bins[i]);bins[i]-=take;high-=take;}
        double sum=0;for(int i=0;i<511;i++)sum+=(i-255)*bins[i];
        return remaining>0?sum/remaining:0;
    }

    private static void LevelRegion(Raster master, byte[] owners, ArenaNativePatch currentSpec, Raster current, ArenaNativePatch previousSpec, Raster previous, int sourceId, Rectangle safe, string name, int overlap, ArenaNativeComposeReport report)
    {
        Rectangle patchBounds=new Rectangle(currentSpec.X,currentSpec.Y,current.Width,current.Height);
        Rectangle incoming=new Rectangle(currentSpec.X,currentSpec.Y,overlap,current.Height);
        Rectangle samples=Rectangle.Intersect(safe,incoming);
        samples=Rectangle.Intersect(samples,new Rectangle(previousSpec.X,previousSpec.Y,previous.Width,previous.Height));
        if(samples.Width<16||samples.Height<16)return;
        int[][] hist={new int[511],new int[511],new int[511]};int count=0;
        for(int wy=samples.Top+2;wy<samples.Bottom-2;wy+=2)for(int wx=samples.Left+2;wx<samples.Right-2;wx+=2)
        {
            int a=(wy-previousSpec.Y)*previous.Stride+(wx-previousSpec.X)*3;
            int b=(wy-currentSpec.Y)*current.Stride+(wx-currentSpec.X)*3;
            for(int c=0;c<3;c++)hist[c][previous.Pixels[a+c]-current.Pixels[b+c]+255]++;
            count++;
        }
        if(count<100)return;
        double[] offset={TrimmedMean(hist[0],count),TrimmedMean(hist[1],count),TrimmedMean(hist[2],count)};
        double largest=Math.Max(Math.Abs(offset[0]),Math.Max(Math.Abs(offset[1]),Math.Abs(offset[2])));
        if(largest<5)return;
        for(int c=0;c<3;c++)offset[c]=Math.Max(-24,Math.Min(24,offset[c]));
        Rectangle apply=Rectangle.Intersect(safe,patchBounds);
        apply=Rectangle.Intersect(apply,new Rectangle(currentSpec.X,currentSpec.Y,Math.Min(current.Width,overlap+384),current.Height));
        long changed=0;
        for(int wy=apply.Top;wy<apply.Bottom;wy++)for(int wx=apply.Left;wx<apply.Right;wx++)
        {
            if(owners[wy*master.Width+wx]!=sourceId)continue;
            double weight=1-Smooth((wx-currentSpec.X-overlap)/384.0);
            weight*=Smooth((wy-safe.Top)/24.0);
            if(safe.Bottom<master.Height)weight*=Smooth((safe.Bottom-1-wy)/24.0);
            if(safe.Left>0)weight*=Smooth((wx-safe.Left)/24.0);
            if(safe.Right<master.Width)weight*=Smooth((safe.Right-1-wx)/24.0);
            if(weight<=0)continue;
            int p=wy*master.Stride+wx*3;bool differs=false;
            for(int c=0;c<3;c++)
            {
                byte adjusted=(byte)Math.Max(0,Math.Min(255,Math.Round(master.Pixels[p+c]+offset[c]*weight)));
                if(adjusted!=master.Pixels[p+c])differs=true;
                master.Pixels[p+c]=adjusted;
            }
            if(differs)changed++;
        }
        report.colorAdjustedPixels+=changed;
        report.colorLeveling.Add(new ArenaNativeColorReport{file=currentSpec.File,region=name,offsetR=offset[2],offsetG=offset[1],offsetB=offset[0],sampleCount=count,x=apply.X,y=apply.Y,width=apply.Width,height=apply.Height,adjustedPixels=changed});
    }

    private static void LevelEmptySurfaces(Raster master, byte[] owners, ArenaNativePatch[] patches, ArenaNativeComposeReport report)
    {
        // These rectangles are independently checked empty-surface regions of the approved arena layout.
        // Level only a smooth low-frequency channel offset; retain every native pixel position and texture.
        Rectangle approachFloor=new Rectangle(0,1460,Math.Min(5600,master.Width),Math.Max(0,master.Height-1460));
        Rectangle ringsideFloor=new Rectangle(Math.Min(5600,master.Width),1790,Math.Max(0,master.Width-5600),Math.Max(0,master.Height-1790));
        Rectangle mat=new Rectangle(6650,1210,6450,140);
        for(int i=1;i<patches.Length;i++)
        {
            ArenaNativePatch previousSpec=patches[i-1],currentSpec=patches[i];
            if(currentSpec.Y!=previousSpec.Y||currentSpec.Y==0)continue;
            int overlap=previousSpec.X+previousSpec.Width-currentSpec.X;
            if(overlap<=0)continue;
            int ignoredWidth,ignoredHeight;
            Raster previous=ReadCrop(previousSpec,out ignoredWidth,out ignoredHeight),current=ReadCrop(currentSpec,out ignoredWidth,out ignoredHeight);
            LevelRegion(master,owners,currentSpec,current,previousSpec,previous,i+1,approachFloor,"approach-floor",overlap,report);
            LevelRegion(master,owners,currentSpec,current,previousSpec,previous,i+1,ringsideFloor,"ringside-floor",overlap,report);
            LevelRegion(master,owners,currentSpec,current,previousSpec,previous,i+1,mat,"empty-ring-canvas",overlap,report);
        }
        if(report.colorAdjustedPixels>0)report.method+="; recorded additive low-frequency leveling restricted to independently checked empty floor/canvas regions";
    }

    private static void FeatherNativeRegion(Raster master,ArenaNativePatch currentSpec,Raster current,ArenaNativePatch previousSpec,Raster previous,Rectangle safe,string name,int overlap,ArenaNativeComposeReport report)
    {
        Rectangle apply=Rectangle.Intersect(safe,new Rectangle(currentSpec.X,currentSpec.Y,overlap,current.Height));
        apply=Rectangle.Intersect(apply,new Rectangle(previousSpec.X,previousSpec.Y,previous.Width,previous.Height));
        if(apply.Width<2||apply.Height<2)return;
        long changed=0;
        for(int wy=apply.Top;wy<apply.Bottom;wy++)for(int wx=apply.Left;wx<apply.Right;wx++)
        {
            double strength=Smooth((wy-safe.Top)/24.0);
            if(safe.Bottom<master.Height)strength*=Smooth((safe.Bottom-1-wy)/24.0);
            if(safe.Left>0)strength*=Smooth((wx-safe.Left)/24.0);
            if(safe.Right<master.Width)strength*=Smooth((safe.Right-1-wx)/24.0);
            if(strength<=0)continue;
            double alpha=Smooth((wx-currentSpec.X)/(double)(overlap-1));
            int a=(wy-previousSpec.Y)*previous.Stride+(wx-previousSpec.X)*3;
            int b=(wy-currentSpec.Y)*current.Stride+(wx-currentSpec.X)*3;
            int m=wy*master.Stride+wx*3;bool differs=false;
            for(int c=0;c<3;c++)
            {
                // Both inputs are sampled at the identical integer world coordinate: no spatial resampling.
                double mixed=previous.Pixels[a+c]*(1-alpha)+current.Pixels[b+c]*alpha;
                byte value=(byte)Math.Round(master.Pixels[m+c]*(1-strength)+mixed*strength);
                if(value!=master.Pixels[m+c])differs=true;
                master.Pixels[m+c]=value;
            }
            if(differs)changed++;
        }
        report.nativeGridBlendedPixels+=changed;
        report.colorLeveling.Add(new ArenaNativeColorReport{file=currentSpec.File,region=name,method="smooth overlap mix of two native-grid samples at identical integer coordinates; no enlargement or spatial resampling",x=apply.X,y=apply.Y,width=apply.Width,height=apply.Height,adjustedPixels=changed});
    }

    private static void FeatherEmptySurfaces(Raster master,ArenaNativePatch[] patches,ArenaNativeComposeReport report)
    {
        Rectangle approachFloor=new Rectangle(0,1446,Math.Min(5600,master.Width),Math.Max(0,master.Height-1446));
        Rectangle ringsideFloor=new Rectangle(Math.Min(5600,master.Width),1790,Math.Max(0,master.Width-5600),Math.Max(0,master.Height-1790));
        Rectangle mat=new Rectangle(6650,1210,6450,140);
        for(int i=1;i<patches.Length;i++)
        {
            ArenaNativePatch previousSpec=patches[i-1],currentSpec=patches[i];
            if(currentSpec.Y!=previousSpec.Y||currentSpec.Y==0)continue;
            int overlap=previousSpec.X+previousSpec.Width-currentSpec.X;
            if(overlap<=0)continue;
            int ignoredWidth,ignoredHeight;
            Raster previous=ReadCrop(previousSpec,out ignoredWidth,out ignoredHeight),current=ReadCrop(currentSpec,out ignoredWidth,out ignoredHeight);
            FeatherNativeRegion(master,currentSpec,current,previousSpec,previous,approachFloor,"approach-floor",overlap,report);
            FeatherNativeRegion(master,currentSpec,current,previousSpec,previous,ringsideFloor,"ringside-floor",overlap,report);
            FeatherNativeRegion(master,currentSpec,current,previousSpec,previous,mat,"empty-ring-canvas",overlap,report);
        }
        report.method+="; recorded native-grid overlap feathering restricted to independently checked empty floor/canvas regions, with no spatial resampling; heads and structure retain hard seams";
    }

    public static ArenaNativeComposeReport Compose(ArenaNativePatch[] patches, int width, int height, int frameWidth, string output, bool allowUncovered, bool levelEmptySurfaces, bool featherEmptySurfaces)
    {
        if (width <= 0 || height <= 0 || frameWidth <= 0 || width % frameWidth != 0) throw new ArgumentException("Invalid canvas/panel dimensions");
        if (patches.Length > 254) throw new ArgumentException("At most254 patches supported by ownership map");
        Array.Sort(patches, delegate(ArenaNativePatch a, ArenaNativePatch b) { int d = a.Y.CompareTo(b.Y); return d != 0 ? d : a.X.CompareTo(b.X); });
        Directory.CreateDirectory(output);
        Raster master = new Raster { Width = width, Height = height, Stride = (width * 3 + 3) & ~3 };
        master.Pixels = new byte[master.Stride * height];
        byte[] owners = new byte[width * height];
        ArenaNativeComposeReport report = new ArenaNativeComposeReport { width = width, height = height, frameWidth = frameWidth, panelCount = width / frameWidth };
        for (int index = 0; index < patches.Length; index++)
        {
            ArenaNativePatch spec = patches[index];
            if (spec.X < 0 || spec.Y < 0 || spec.X + spec.Width > width || spec.Y + spec.Height > height) throw new ArgumentException("Patch extends outside canvas: " + spec.File);
            int sw, sh;
            Raster patch = ReadCrop(spec, out sw, out sh);
            int left = FullLeftOverlap(owners, width, spec), top = FullTopOverlap(owners, width, spec);
            if (left == patch.Width || top == patch.Height) throw new InvalidOperationException("Patch is wholly redundant; check ordering/positions: " + spec.File);
            double vc = 0, hc = 0;
            int[] vs = left > 0 ? VerticalSeam(master, patch, spec, left, out vc) : null;
            int[] hs = top > 0 ? HorizontalSeam(master, patch, spec, top, out hc) : null;
            for (int y = 0; y < patch.Height; y++) for (int x = 0; x < patch.Width; x++)
            {
                int ownerIndex = (spec.Y + y) * width + spec.X + x;
                bool use = owners[ownerIndex] == 0 || ((vs == null || x >= vs[y]) && (hs == null || y >= hs[x]));
                if (!use) continue;
                int m = (spec.Y + y) * master.Stride + (spec.X + x) * 3, p = y * patch.Stride + x * 3;
                master.Pixels[m] = patch.Pixels[p]; master.Pixels[m + 1] = patch.Pixels[p + 1]; master.Pixels[m + 2] = patch.Pixels[p + 2];
                owners[ownerIndex] = (byte)(index + 1);
            }
            report.nativePatches.Add(new ArenaNativeSourceReport { file = spec.File, sourceWidth = sw, sourceHeight = sh, cropX = spec.CropX, cropY = spec.CropY, targetWidth = spec.Width, targetHeight = spec.Height, x = spec.X, y = spec.Y });
            int vProtected=0,hProtected=0;
            if(vs!=null)for(int sy=0;sy<vs.Length;sy++)foreach(Rectangle box in spec.AvoidRectangles)if(box.Contains(vs[sy],sy)){vProtected++;break;}
            if(hs!=null)for(int sx=0;sx<hs.Length;sx++)foreach(Rectangle box in spec.AvoidRectangles)if(box.Contains(sx,hs[sx])){hProtected++;break;}
            report.seams.Add(new ArenaNativeSeamReport { file = spec.File, x = spec.X, y = spec.Y, verticalOverlap = left, horizontalOverlap = top, verticalMeanCost = vc, horizontalMeanCost = hc, verticalProtectedSamples=vProtected,horizontalProtectedSamples=hProtected });
        }
        foreach (byte owner in owners) if (owner == 0) report.uncoveredPixels++;
        report.allPixelsCovered = report.uncoveredPixels == 0;
        if (!report.allPixelsCovered && !allowUncovered) throw new InvalidOperationException("Incomplete native canvas: " + report.uncoveredPixels + " pixels are not supplied by any native patch");
        if(levelEmptySurfaces&&featherEmptySurfaces)throw new ArgumentException("Choose either additive leveling or native-grid feathering, not both.");
        if(featherEmptySurfaces)FeatherEmptySurfaces(master,patches,report);
        else if(levelEmptySurfaces)LevelEmptySurfaces(master,owners,patches,report);
        SaveRaster(Path.Combine(output, "static-master-" + width + "x" + height + ".png"), master);
        for (int panel = 0; panel < report.panelCount; panel++)
        {
            Raster p = new Raster { Width = frameWidth, Height = height, Stride = (frameWidth * 3 + 3) & ~3 };
            p.Pixels = new byte[p.Stride * p.Height];
            for (int y = 0; y < height; y++) Buffer.BlockCopy(master.Pixels, y * master.Stride + panel * frameWidth * 3, p.Pixels, y * p.Stride, frameWidth * 3);
            SaveRaster(Path.Combine(output, "static-" + (panel + 1).ToString("00") + "-" + frameWidth + "x" + height + ".png"), p);
        }
        File.WriteAllBytes(Path.Combine(output, "source-ownership.bin"), owners);
        SavePreview(Path.Combine(output, "preview-static.png"), master, Math.Min(3000, width));
        Raster ownerMap = new Raster { Width = width, Height = height, Stride = master.Stride, Pixels = new byte[master.Pixels.Length] };
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            byte o = owners[y * width + x]; int q = y * ownerMap.Stride + x * 3;
            ownerMap.Pixels[q] = (byte)((o * 97) % 251); ownerMap.Pixels[q + 1] = (byte)((o * 53) % 251); ownerMap.Pixels[q + 2] = (byte)((o * 149) % 251);
        }
        SavePreview(Path.Combine(output, "preview-source-ownership.png"), ownerMap, Math.Min(3000, width));
        // Reviewer-only seam overlay; never used as a production source.
        Raster overlay = new Raster { Width = width, Height = height, Stride = master.Stride, Pixels = (byte[])master.Pixels.Clone() };
        for (int y = 1; y < height; y++) for (int x = 1; x < width; x++)
        {
            int o = y * width + x;
            if (owners[o] == owners[o-1] && owners[o] == owners[o-width]) continue;
            for (int dy=-2;dy<=2;dy++) for(int dx=-2;dx<=2;dx++)
            {
                int tx=x+dx,ty=y+dy;
                if(tx<0||ty<0||tx>=width||ty>=height)continue;
                int z=ty*overlay.Stride+tx*3;
                overlay.Pixels[z]=20;overlay.Pixels[z+1]=20;overlay.Pixels[z+2]=255;
            }
        }
        SavePreview(Path.Combine(output,"preview-seams.png"),overlay,Math.Min(3000,width));
        return report;
    }
}
