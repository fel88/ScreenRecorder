using System.IO;

namespace GifViewer
{
    public class GifHeader
    {
        public byte[] Signature = new byte[3];
        public byte[] Version = new byte[3];
        public void Read(Stream s)
        {
            s.Read(Signature, 0, 3);
            s.Read(Version, 0, 3);
        }

        public void Write(Stream s)
        {
            s.Write(Signature, 0, Signature.Length);
            s.Write(Version, 0, Signature.Length);
        }
    }
}
