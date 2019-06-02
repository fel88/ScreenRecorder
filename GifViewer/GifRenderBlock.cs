using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace GifViewer
{
    public class GifRenderBlock : GifDataBlock
    {
        public int FrameIndex { get; set; }
        public ushort LeftPosition { get; set; }
        public ushort TopPosition { get; set; }
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public byte PackedByte { get; set; }
        public bool LocalColorTableFlag
        {
            get
            {
                return (PackedByte & (1 << 7)) > 0;
            }
        }
        public bool InterlaceFlag
        {
            get
            {
                return (PackedByte & (1 << 6)) > 0;
            }
        }
        public byte SizeOfColorTable
        {
            get
            {
                return (byte)(PackedByte & (0x7));
            }
        }

        public Rectangle Region
        {
            get
            {
                return new Rectangle(LeftPosition, TopPosition, Width, Height);
            }
        }

        public byte[][] DataList { get; internal set; }

        public GifColorTable ColorTable;
        public List<byte> Data = new List<byte>();
        public byte LZWMinCodeSize;
        public static GifRenderBlock Read(Stream s)
        {
            GifRenderBlock ret = new GifRenderBlock();
            ret.Shift = s.Position;
            s.ReadByte();

            ret.LeftPosition = s.ReadUshort();
            ret.TopPosition = s.ReadUshort();
            ret.Width = s.ReadUshort();
            ret.Height = s.ReadUshort();
            ret.PackedByte = (byte)s.ReadByte();
            if (ret.LocalColorTableFlag)
            {
                ret.ColorTable = new GifColorTable();
                ret.ColorTable.ColorsAmount = (int)Math.Pow(2, ret.SizeOfColorTable + 1);
                ret.ColorTable.Read(s);
            }
            ret.LZWMinCodeSize = (byte)s.ReadByte();

            var b = s.ReadByte();
            List<byte[]> dl = new List<byte[]>();
            while (b != 0)
            {
                ret.Data.Add((byte)b);
                var dd = new byte[b];
                s.Read(dd, 0, b);
                ret.Data.AddRange(dd);
                dl.Add(dd);
                b = s.ReadByte();
            }
            ret.DataList = dl.ToArray();
            return ret;
        }

        public override void Write(Stream ms)
        {

            ms.WriteByte(0x2c);
            ms.WriteUshort(LeftPosition);
            ms.WriteUshort(TopPosition);
            ms.WriteUshort(Width);
            ms.WriteUshort(Height);
            ms.WriteByte(PackedByte);

            if (LocalColorTableFlag)
            {
                ColorTable.Write(ms);
            }

            ms.WriteByte(LZWMinCodeSize);
            ms.Write(Data.ToArray(), 0, Data.Count);
            ms.WriteByte(0);
        }
    }
}
