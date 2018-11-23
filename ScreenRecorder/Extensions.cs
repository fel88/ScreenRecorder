using System.IO;
using System.Linq;

namespace ScreenRecorder
{
    public static class Extensions
    {
        public static void WriteAsByteArray(this FileStream stream, string str)
        {
            stream.Write(str.Select(z => (byte)z).ToArray(), 0, str.Length);
        }
    }
}
