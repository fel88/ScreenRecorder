using System.IO;

namespace GifViewer
{
    public class GifApplicationExtension : GifDataBlock
    {
        public static GifApplicationExtension Read(Stream s)
        {
            GifApplicationExtension ret = new GifApplicationExtension();
            byte[] bb = new byte[20];
            s.Read(bb, 0, 3 + 8 + 3);

            byte b = (byte)s.ReadByte();
            while (b != 0)
            {
                byte[] bb2 = new byte[b];
                s.Read(bb2, 0, b);
                b = (byte)s.ReadByte();
            }
            return ret;
        }
    }

}
