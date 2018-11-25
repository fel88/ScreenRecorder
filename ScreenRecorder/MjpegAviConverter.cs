using System.IO;

namespace ScreenRecorder
{
    public static class MjpegAviRecorder
    {

        public static void fwrite_WORD(FileStream file_ptr, uint word)
        {
            for (int i = 0; i < 2; i++)
            {
                var pi = (byte)((word & (0xff << (i * 8))) >> i * 8);
                file_ptr.WriteByte(pi);
            }
        }

        static ulong get_all_sizes(MjpegIterator mjpeg)
        {
            ulong sizes = 0;

            mjpeg.MoveTo(0);
            while (!mjpeg.Eof())
            {
                var len = mjpeg.CurrentMem.Length;
                if (len % 2 != 0) len += 1;
                sizes += (ulong)len;
                if (!mjpeg.Next()) break;
            }
            return sizes;
        }

        static void appendFrames(FileStream fs, MjpegIterator mjpeg)
        {
            mjpeg.MoveTo(0);

            ulong nbr_of_jpgs = (ulong)mjpeg.Count;
            while (!mjpeg.Eof())
            {
                var len = mjpeg.CurrentMem.Length;

                fs.WriteAsByteArray("00db");
                fs.fwrite_DWORD((uint)len);

                var bb = mjpeg.CurrentMem.ToArray();
                fs.Write(bb, 0, bb.Length);
                if (bb.Length % 2 != 0)
                {
                    fs.WriteByte(0);
                }
                if (!mjpeg.Next()) break;
            }

            ulong AVI_KEYFRAME = 16;
            ulong offset_count = 4;
            ulong index_length = 4 * 4 * nbr_of_jpgs;

            fs.WriteAsByteArray("idx1");
            fs.fwrite_DWORD((uint)index_length);

            mjpeg.MoveTo(0);

            while (!mjpeg.Eof())
            {
                var len = mjpeg.CurrentMem.Length;
                if (len % 2 != 0) len++;

                fs.WriteAsByteArray("00db");

                fs.fwrite_DWORD((uint)AVI_KEYFRAME);
                fs.fwrite_DWORD((uint)offset_count);
                fs.fwrite_DWORD((uint)len);
                offset_count += (uint)(len + 8);
                if (!mjpeg.Next()) break;
            }
        }


        public static void Write(string path, MjpegIterator mjpeg, ulong fps)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            var bmp = mjpeg.Image();
            ulong count = (ulong)mjpeg.Count;
            var jpgs_width = bmp.Width;
            var jpgs_height = bmp.Height;
            bmp.Dispose();


            fs.WriteAsByteArray("RIFF");

            var len = get_all_sizes(mjpeg);
            var RIFF_LISTdwSize = (uint)(150 + 12 + len + 8 * count + 8 + 4 * 4 * count);
            fs.fwrite_DWORD(RIFF_LISTdwSize);
            fs.WriteAsByteArray("AVI ");
            fs.WriteAsByteArray("LIST");

            fs.fwrite_DWORD(208);
            fs.WriteAsByteArray("hdrl");
            fs.WriteAsByteArray("avih");

            fs.fwrite_DWORD(56);
            var avihdwMicroSecPerFrame = (uint)(1000000 / fps);
            fs.fwrite_DWORD(avihdwMicroSecPerFrame);

            var avihdwMaxBytesPerSec = 7000;
            fs.fwrite_DWORD((uint)avihdwMaxBytesPerSec);

            var avihdwPaddingGranularity = 0;
            fs.fwrite_DWORD((uint)avihdwPaddingGranularity);


            fs.fwrite_DWORD((uint)16);
            fs.fwrite_DWORD((uint)count);
            fs.fwrite_DWORD((uint)0);
            fs.fwrite_DWORD((uint)1);

            fs.fwrite_DWORD((uint)0);
            fs.fwrite_DWORD((uint)jpgs_width);
            fs.fwrite_DWORD((uint)jpgs_height);
            fs.fwrite_DWORD((uint)0);
            fs.fwrite_DWORD((uint)0);
            fs.fwrite_DWORD((uint)0);
            fs.fwrite_DWORD((uint)0);

            fs.WriteAsByteArray("LIST");

            fs.fwrite_DWORD((uint)132);

            fs.WriteAsByteArray("strl");
            fs.WriteAsByteArray("strh");

            fs.fwrite_DWORD(48);
            fs.WriteAsByteArray("vids");
            fs.WriteAsByteArray("MJPG");

            fs.fwrite_DWORD(0);
            fwrite_WORD(fs, 0);
            fwrite_WORD(fs, 0);
            fs.fwrite_DWORD(0);
            fs.fwrite_DWORD(1);

            fs.fwrite_DWORD((uint)fps);
            fs.fwrite_DWORD(0);
            fs.fwrite_DWORD((uint)count);
            fs.fwrite_DWORD(0);
            fs.fwrite_DWORD(0);

            fs.fwrite_DWORD(0);
            fs.WriteAsByteArray("strf");

            fs.fwrite_DWORD(40);
            fs.fwrite_DWORD(40);
            fs.fwrite_DWORD((uint)jpgs_width);
            fs.fwrite_DWORD((uint)jpgs_height);
            fwrite_WORD(fs, 1);
            fwrite_WORD(fs, 24);

            fs.WriteAsByteArray("MPNG");

            var strfbiSizeImage = ((jpgs_width * jpgs_height / 8 + 3) & 0xFFFFFFFC) * jpgs_height;
            fs.fwrite_DWORD((uint)strfbiSizeImage);
            fs.fwrite_DWORD(0);
            fs.fwrite_DWORD(0);
            fs.fwrite_DWORD(0);
            fs.fwrite_DWORD(0);

            fs.WriteAsByteArray("LIST");

            fs.fwrite_DWORD(16);

            fs.WriteAsByteArray("odml");
            fs.WriteAsByteArray("dmlh");

            fs.fwrite_DWORD(4);
            fs.fwrite_DWORD((uint)count);
            fs.WriteAsByteArray("LIST");

            var movidwSize = len + 4 + 8 * count;
            fs.fwrite_DWORD((uint)movidwSize);

            fs.WriteAsByteArray("movi");
            appendFrames(fs, mjpeg);
            fs.Close();
            fs.Dispose();
        }
    }


}
