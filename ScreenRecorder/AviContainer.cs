using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ScreenRecorder
{
    public class AviContainer
    {
        public AviContainer(string path)
        {
            Path = path;
            fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
            GetShifts();
            UpdateInfos();

        }
        FileStream fs;

        void UpdateInfos()
        {
            fs.Seek(fpsPosition, SeekOrigin.Begin);
            uint fps = fs.fread_DWORD();
            //Fps = 1000000 / avihdwMicroSecPerFrame;
            Fps = fps;
        }
        List<long> shifts = new List<long>();

        void GetShifts()
        {
            uint start = 240;
            shifts.Clear();
            fs.Seek(start, SeekOrigin.Begin);
            while (true)
            {
                var marker = fs.fread_DWORD();
                //30 30 64 62
                if (marker == 0x62643030)
                {
                    shifts.Add(fs.Position - 4);
                    var len = fs.fread_DWORD();
                    fs.Seek(len, SeekOrigin.Current);
                    if (len % 2 != 0)
                    {
                        fs.ReadByte();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public Bitmap GetFrame(int index)
        {
            if (index > (shifts.Count - 1))
            {
                return null;
            }
            //240
            long shift = shifts[index];
            //4:00db
            //4:len        
            fs.Seek(shift, SeekOrigin.Begin);
            var marker = fs.fread_DWORD();
            var len = fs.fread_DWORD();
            var ms = new MemoryStream();
            byte[] bb = new byte[len];
            fs.Read(bb, 0, bb.Length);
            ms.Write(bb, 0, bb.Length);
            var ret = Bitmap.FromStream(ms);
            return ret as Bitmap;
        }
        public uint Fps;

        public string Path { get; internal set; }
        public int Frames
        {
            get
            {
                return shifts.Count;
            }
        }

        long fpsPosition = 132;
        public void SaveFps()
        {

            fs.Seek(fpsPosition, SeekOrigin.Begin);
            fs.fwrite_DWORD((uint)Fps);
        }

        public void Close()
        {
            fs.Close();
        }
    }
}
