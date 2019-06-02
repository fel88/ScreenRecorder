using System.IO;

namespace GifViewer
{
    public class GifCommentBlock : GifDataBlock
    {
        public static GifCommentBlock Read(Stream s)
        {
            GifCommentBlock ret = new GifCommentBlock();
            ret.Shift = s.Position;
            s.ReadByte();
            s.ReadByte();
            var b = s.ReadByte();
            while (b != 0)
            {
                var bb = new byte[b];
                s.Read(bb, 0, b);
                b = s.ReadByte();
            }
            return ret;
        }
    }
}
