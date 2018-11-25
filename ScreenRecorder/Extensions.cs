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
        public static uint fread_DWORD(this FileStream file_ptr)
        {
            byte[] bb = new byte[4];
            file_ptr.Read(bb, 0, 4);
            uint ret = 0;
            for (int i = 0; i < 4; i++)
            {
                ret |= (uint)(bb[i] << (i * 8));
            }
            return ret;

        }
        public static byte[] freadArray(this FileStream file_ptr,int cnt)
        {
            byte[] bb = new byte[cnt];
            file_ptr.Read(bb, 0, cnt);
            
            return bb;
        }

        public static bool IsEqual(this byte[] b1,byte[] b2)
        {
            if (b1.Length != b2.Length) return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }
        public static void fwrite_DWORD(this FileStream file_ptr, uint word)
        {
            for (int i = 0; i < 4; i++)
            {
                var pi = (byte)((word & (0xff << (i * 8))) >> i * 8);
                file_ptr.WriteByte(pi);
            }

        }
    }
}
