using System.IO;

namespace ScreenRecorder
{
    public static class MjpegAviRecorder
    {
        static void fwrite_DWORD(FileStream file_ptr, uint word)
        {
            for (int i = 0; i < 4; i++)
            {
                var pi = (byte)((word & (0xff << (i * 8))) >> i * 8);
                file_ptr.WriteByte(pi);
            }

        }
        static void fwrite_WORD(FileStream file_ptr, uint word)
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
                fwrite_DWORD(fs, (uint)len);

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
            fwrite_DWORD(fs, (uint)index_length);
                        
            mjpeg.MoveTo(0);

            while (!mjpeg.Eof())
            {                
                var len = mjpeg.CurrentMem.Length;
                if (len % 2 != 0) len++;

                fs.WriteAsByteArray("00db");
                
                fwrite_DWORD(fs, (uint)AVI_KEYFRAME);
                fwrite_DWORD(fs, (uint)offset_count);
                fwrite_DWORD(fs, (uint)len);
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
            fwrite_DWORD(fs, RIFF_LISTdwSize);
            fs.WriteAsByteArray("AVI ");
            fs.WriteAsByteArray("LIST");

            fwrite_DWORD(fs, 208);
            fs.WriteAsByteArray("hdrl");
            fs.WriteAsByteArray("avih");

            fwrite_DWORD(fs, 56);
            var avihdwMicroSecPerFrame = (uint)(1000000 / fps);
            fwrite_DWORD(fs, avihdwMicroSecPerFrame);

            var avihdwMaxBytesPerSec = 7000;
            fwrite_DWORD(fs, (uint)avihdwMaxBytesPerSec);

            var avihdwPaddingGranularity = 0;
            fwrite_DWORD(fs, (uint)avihdwPaddingGranularity);


            fwrite_DWORD(fs, (uint)16);
            fwrite_DWORD(fs, (uint)count);
            fwrite_DWORD(fs, (uint)0);
            fwrite_DWORD(fs, (uint)1);

            fwrite_DWORD(fs, (uint)0);
            fwrite_DWORD(fs, (uint)jpgs_width);
            fwrite_DWORD(fs, (uint)jpgs_height);
            fwrite_DWORD(fs, (uint)0);
            fwrite_DWORD(fs, (uint)0);
            fwrite_DWORD(fs, (uint)0);
            fwrite_DWORD(fs, (uint)0);

            fs.WriteAsByteArray("LIST");

            fwrite_DWORD(fs, (uint)132);

            fs.WriteAsByteArray("strl");
            fs.WriteAsByteArray("strh");

            fwrite_DWORD(fs, 48);            
            fs.WriteAsByteArray("vids");
            fs.WriteAsByteArray("MJPG");

            fwrite_DWORD(fs, 0);
            fwrite_WORD(fs, 0);
            fwrite_WORD(fs, 0);
            fwrite_DWORD(fs, 0);
            fwrite_DWORD(fs, 1);

            fwrite_DWORD(fs, (uint)fps);
            fwrite_DWORD(fs, 0);
            fwrite_DWORD(fs, (uint)count);
            fwrite_DWORD(fs, 0);
            fwrite_DWORD(fs, 0);

            fwrite_DWORD(fs, 0);
            fs.WriteAsByteArray("strf");

            fwrite_DWORD(fs, 40);
            fwrite_DWORD(fs, 40);
            fwrite_DWORD(fs, (uint)jpgs_width);
            fwrite_DWORD(fs, (uint)jpgs_height);
            fwrite_WORD(fs, 1);
            fwrite_WORD(fs, 24);

            fs.WriteAsByteArray("MPNG");

            var strfbiSizeImage = ((jpgs_width * jpgs_height / 8 + 3) & 0xFFFFFFFC) * jpgs_height;
            fwrite_DWORD(fs, (uint)strfbiSizeImage);
            fwrite_DWORD(fs, 0);
            fwrite_DWORD(fs, 0);
            fwrite_DWORD(fs, 0);
            fwrite_DWORD(fs, 0);

            fs.WriteAsByteArray("LIST");

            fwrite_DWORD(fs, 16);

            fs.WriteAsByteArray("odml");
            fs.WriteAsByteArray("dmlh");

            fwrite_DWORD(fs, 4);
            fwrite_DWORD(fs, (uint)count);
            fs.WriteAsByteArray("LIST");

            var movidwSize = len + 4 + 8 * count;
            fwrite_DWORD(fs, (uint)movidwSize);

            fs.WriteAsByteArray("movi");
            appendFrames(fs, mjpeg);
            fs.Close();
            fs.Dispose();
        }
    }
}
