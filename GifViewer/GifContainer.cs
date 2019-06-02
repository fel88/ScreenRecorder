using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GifViewer
{
    public class GifContainer
    {
        //public Bitmap Bmp;
        public GifHeader Header = new GifHeader();
        public GifLogicalScreen LogicalScreen = new GifLogicalScreen();
        public List<GifDataBlock> Data = new List<GifDataBlock>();

        public int Frames
        {
            get; private set;

        }

        public GraphicControlExtension LastGceBlock;
        public GifRenderBlock LastRenderBlock;
        public Image LastRender;

        public MemoryStream GetRawFrameStream(int index)
        {
            MemoryStream ms = new MemoryStream();
            Header.Write(ms);

            //LogicalScreen.GlobalColorTable.Colors[LogicalScreen.Desc.BackgroundColorIndex] = Color.Black;
            LogicalScreen.Write(ms);
            int frame = 0;
            for (int i = 0; i < Data.Count; i++)
            {
                var t = Data[i];
                if (frame == index && t is GifRenderBlock)
                {

                    int j = 0;
                    for (j = i - 1; j > 0; j--)
                    {
                        if (Data[j] is GifRenderBlock)
                        {
                            j++;
                            break;
                        }
                    }
                    LastGceBlock = null;
                    for (int y = j; y <= i; y++)
                    {
                        if (Data[y] is GraphicControlExtension)
                        {
                            LastGceBlock = Data[y] as GraphicControlExtension;
                            // LogicalScreen.GlobalColorTable.Colors[LastGceBlock.TransparentColorIndex] = Color.Black;
                        }
                        if (Data[y] is GifRenderBlock)
                        {
                            LastRenderBlock = Data[y] as GifRenderBlock;
                        }
                        Data[y].Write(ms);
                    }

                    break;
                }
                if (t is GifRenderBlock)
                {
                    frame++;
                }

            }
            ms.WriteByte(0x3b);
            return ms;
        }




        public GifRenderBlock GetRenderBlock(int index)
        {

            int frame = 0;
            for (int i = 0; i < Data.Count; i++)
            {
                var t = Data[i];
                if (frame == index && t is GifRenderBlock)
                {
                    return t as GifRenderBlock;
                }
                if (t is GifRenderBlock)
                {
                    frame++;
                }
            }
            return null;
        }
        public GraphicControlExtension GetGceBlock(GifRenderBlock gr)
        {
            GraphicControlExtension gce = null;
            foreach (var item in Data)
            {
                if (item is GraphicControlExtension)
                {
                    gce = item as GraphicControlExtension;
                }
                if (gr == item) break;
            }
            return gce;
        }
        public Bitmap GetDecodedBmp(int _index)
        {
            var fr = GetRenderBlock(_index);
            var gce = GetGceBlock(fr);
            LastRenderBlock = fr;
            LastGceBlock = gce;
            Bitmap decbmp = new Bitmap(fr.Width, fr.Height);
            LockBitmap l = new LockBitmap(decbmp);
            l.LockBits();

            var dec = GifParser.GetDecodedData(fr);
            GifColorTable clrt = LogicalScreen.GlobalColorTable;
            if (fr.LocalColorTableFlag)
            {
                clrt = fr.ColorTable;
            }
            for (int i = 0; i < fr.Width; i++)
            {
                for (int j = 0; j < fr.Height; j++)
                {
                    int index = j * fr.Width + i;
                    var clr = clrt.Colors[dec[index]];
                    if (gce.TransparencyFlag && gce.TransparentColorIndex == dec[index])
                    {
                        l.SetPixel(i, j, Color.Transparent);                        
                    }
                    else
                    {
                        l.SetPixel(i, j, clr);                        
                    }
                }
            }
            l.UnlockBits();
            return decbmp;
        }
        public Image GetFrame(int index)
        {
            //Bmp.SelectActiveFrame(System.Drawing.Imaging.FrameDimension.Time, index);
            //return Bmp.Clone() as Bitmap;

            //var ms = GetRawFrameStream(index);
            //   ms.Dispose();
            GifDisposalMethodEnum dm = GifDisposalMethodEnum.Unknown;
            var bmp = GetDecodedBmp(index);
            if (LastGceBlock != null)
            {
                dm = LastGceBlock.DisposalMethod;
            }

            //var bmp = Bitmap.FromStream(ms) as Bitmap;
            ////var r = UpdateBmp(bmp);
            //   bmp.Dispose();
            //  bmp = r;

            //bmp update
            //ms.Dispose();
            if (dm == GifDisposalMethodEnum.RestoreBackground)
            {
                var bmpret = new Bitmap(LogicalScreen.Desc.ScreenWidth, LogicalScreen.Desc.ScreenHeight);
                var gr = Graphics.FromImage(bmpret);
                gr.Clear(LogicalScreen.GlobalColorTable.Colors[LogicalScreen.Desc.BackgroundColorIndex]);
                if (LogicalScreen.Desc.BackgroundColorIndex == LastGceBlock.TransparentColorIndex)
                {
                    gr.Clear(Color.Transparent);
                }

                gr.DrawImage(bmp, LastRenderBlock.LeftPosition, LastRenderBlock.TopPosition);
                bmp.Dispose();
                bmp = bmpret;
            }
            if (dm == GifDisposalMethodEnum.DoNotDispose && LastRender != null && LastGceBlock.TransparencyFlag)
            {
                var bmpret = new Bitmap(LastRender.Width, LastRender.Height);
                var gr = Graphics.FromImage(bmpret);
                gr.DrawImage(LastRender, 0, 0);
                              
                gr.DrawImage(bmp, LastRenderBlock.LeftPosition, LastRenderBlock.TopPosition);
                bmp.Dispose();
                bmp = bmpret;
            }
            LastRender = bmp.Clone() as Bitmap;
            return bmp;

        }

        public void UpdateInfo()
        {
            Frames = Data.Count(z => z is GifRenderBlock);
        }
    }
}
