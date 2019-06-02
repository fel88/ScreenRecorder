using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GifViewer
{
    public class GifLogicalScreen
    {
        public GifLogicalScreenDesc Desc = new GifLogicalScreenDesc();
        public GifColorTable GlobalColorTable;

        public void Read(Stream s)
        {
            Desc.Read(s);
            if (Desc.GlobalColorTableFlag)
            {
                GlobalColorTable = new GifColorTable();
                GlobalColorTable.ColorsAmount = (int)Math.Pow(2, Desc.SizeOfGlobalColorTable + 1);
                GlobalColorTable.Read(s);
            }
        }

        public void Write(Stream s)
        {
            Desc.Write(s);
            if (Desc.GlobalColorTableFlag)
            {
                GlobalColorTable.Write(s);
            }
        }
    }
    public class GifLogicalScreenDesc
    {
        public bool GlobalColorTableFlag
        {
            get
            {
                return (PackedField & (1 << 7)) > 0;
            }
        }
        public byte SizeOfGlobalColorTable
        {
            get
            {
                return (byte)(PackedField & (0x7));
            }
        }
        public ushort ScreenWidth { get; set; }
        public ushort ScreenHeight { get; set; }
        public byte BackgroundColorIndex { get; set; }
        public byte PixelAspectRato { get; set; }
        public byte PackedField { get; set; }
        public void Read(Stream s)
        {
            ScreenWidth = s.ReadUshort();
            ScreenHeight = s.ReadUshort();
            PackedField = (byte)s.ReadByte();
            BackgroundColorIndex = (byte)s.ReadByte();
            PixelAspectRato = (byte)s.ReadByte();
        }

        public void Write(Stream s)
        {
            s.WriteUshort(ScreenWidth);
            s.WriteUshort(ScreenHeight);
            s.WriteByte(PackedField);
            s.WriteByte(BackgroundColorIndex);
            s.WriteByte(PixelAspectRato);
        }
    }

}
