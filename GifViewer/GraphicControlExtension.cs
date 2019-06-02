using System.IO;

namespace GifViewer
{
    public class GraphicControlExtension : GifDataBlock
    {
        public bool TransparencyFlag
        {
            get
            {
                return (PackedByte & 0x1) > 0;
            }
        }
        public GifDisposalMethodEnum DisposalMethod
        {
            get
            {
                var rr = (GifDisposalMethodEnum)5;
                return (GifDisposalMethodEnum)((PackedByte >> 2) & (0x7));
            }
        }
        public byte PackedByte { get; set; }
        public byte TransparentColorIndex { get; set; }
        public ushort DelayTime { get; set; }
        public int Delay
        {
            get
            {
                return DelayTime * 10;
            }
        }

        public static GraphicControlExtension Read(Stream s)
        {
            GraphicControlExtension ret = new GraphicControlExtension();
            ret.Shift = s.Position;
            s.ReadByte();
            s.ReadByte();
            s.ReadByte();
            ret.PackedByte = (byte)s.ReadByte();
            ret.DelayTime = s.ReadUshort();
            ret.TransparentColorIndex = (byte)s.ReadByte();
            s.ReadByte();
            return ret;
        }

        public override void Write(Stream s)
        {

            s.WriteByte(0x21);
            s.WriteByte(0xf9);
            s.WriteByte(0x4);
            s.WriteByte(PackedByte);
            s.WriteUshort(DelayTime);
            s.WriteByte(TransparentColorIndex);
            s.WriteByte(0);
        }
    }
    public enum GifDisposalMethodEnum
    {
        NoDisposal = 0,
        DoNotDispose = 1,
        RestoreBackground = 2,
        RestorePervious = 3,
        Unknown
    }
}
