using System.IO;

namespace GifViewer
{
    public class GifDataBlock
    {
        public long Shift { get; set; }
        public virtual void Write(Stream ms)
        {

        }
    }
}
