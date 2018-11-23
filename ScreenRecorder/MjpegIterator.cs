using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenRecorder
{
    public class MjpegIterator
    {
        FileStream fs;
        ImageFormat Format;
        List<long> Shifts = new List<long>();
        byte[] marker;
        public MjpegIterator(string path)
        {
            Format = ImageFormat.Png;
            fs = new FileStream(path, FileMode.Open, FileAccess.Read);

            var b = (byte)(fs.ReadByte());
            if (b == 0x89)
            {
                Format = ImageFormat.Png;
                //png: 89  50  4e  47  0d  0a  1a  0a
                marker = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
            }
            if (b == 0xFF)
            {
                Format = ImageFormat.Jpeg;
                marker = new byte[] { 0xff, 0xD8 };
            }

            fs.Seek(0, SeekOrigin.Begin);
            //read all frames
            while (fs.Position < fs.Length)
            {
                seekNext();                
                if (fs.Position == fs.Length) break;
                Shifts.Add(position - marker.Length);
            }
            mem = new MemoryStream();
            byte[] bb = new byte[Shifts[1]];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(bb, 0, bb.Length);
            mem.Write(bb, 0, bb.Length);
        }

        int position;
        void seekNext()
        {
            List<byte> buffer = new List<byte>();
            bool exit = false;
            position = (int)fs.Position;
            while (fs.Position < fs.Length)
            {
                long last = fs.Length - fs.Position;
                var sz = Math.Min(last, 1024);
                byte[] bbs = new byte[sz];
                fs.Read(bbs, 0, (int)sz);
                for (int u = 0; u < sz; u++)
                {
                    var bb = bbs[u];
                    position++;
                    buffer.Add(bb);
                    if (buffer.Count > marker.Length)
                    {
                        buffer.RemoveAt(0);
                    }

                    if (buffer.Count == marker.Length)
                    {
                        bool match = true;
                        for (int i = 0; i < buffer.Count; i++)
                        {
                            if (buffer[i] != marker[i])
                            {
                                match = false;
                            }
                        }
                        if (match)
                        {
                            exit = true;
                            break;
                        }
                    }
                }
                if (exit) break;
            }            
        }

        int currentIndex = 0;
        MemoryStream mem;
        public Bitmap Image()
        {
            var b = (Bitmap)Bitmap.FromStream(mem);
            return b;
        }
        public void Close()
        {
            fs.Close();
        }

        public bool Next()
        {
            currentIndex++;
            if (currentIndex == Shifts.Count)
            {
                return false;
            }
            MoveTo(currentIndex);
            return true;
        }

        public void MoveTo(int v)
        {
            currentIndex = v;
            fs.Seek(Shifts[v], SeekOrigin.Begin);
            mem = new MemoryStream();
            long pos1 = 0;
            long pos2 = 0;
            pos1 = Shifts[v];
            if (Shifts.Count == (v + 1))
            {
                pos2 = fs.Length;
            }
            else
            {
                pos2 = Shifts[v + 1];
            }

            fs.Seek(pos1, SeekOrigin.Begin);
            byte[] bb = new byte[pos2 - pos1];
            fs.Read(bb, 0, bb.Length);
            mem.Write(bb, 0, bb.Length);
        }

        public bool Eof()
        {
            return currentIndex == Shifts.Count;
        }

        public int Count { get { return Shifts.Count; } }

        public MemoryStream CurrentMem { get { return mem; } }
    }


}
