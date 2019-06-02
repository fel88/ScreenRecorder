using System.Drawing;
using System.IO;

namespace GifViewer
{
    public static class Extensions
    {
        public static Color ReadColor3(this Stream s)
        {

            var b1 = s.ReadByte();
            var b2 = s.ReadByte();
            var b3 = s.ReadByte();

            return Color.FromArgb(b1, b2, b3);
        }
        public static ushort ReadUshort(this Stream s)
        {
            ushort ret = 0;
            var b1 = s.ReadByte();
            var b2 = s.ReadByte();
            ret = (ushort)(b1 | (b2 << 8));
            return ret;
        }
        public static void WriteUshort(this Stream s, ushort v)
        {
            s.WriteByte((byte)(v & 0xff));
            s.WriteByte((byte)((v >> 8) & 0xff));
        }
    }


}
