using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

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
        List<ShiftInfo> shifts = new List<ShiftInfo>();

        public class ShiftInfo
        {
            public ShiftInfo(int index, long pos, string marker, long len)
            {
                Index = index;
                Position = pos;
                Marker = marker;
                Length = len;
            }
            public int Index;
            public long Length;
            public override string ToString()
            {
                return "ShiftInfo: " + Marker + ":" + Position.ToString("X2") + ", len: " + Length.ToString("X2");
            }
            public long Position;
            public bool IsAudio
            {
                get
                {
                    return Marker == "01wb";
                }
            }
            public string Marker;
        }

        void GetShifts()
        {
            uint start = 240;
            shifts.Clear();
            fs.Seek(0, SeekOrigin.Begin);

            byte[] buf = new byte[4] { 0, 0, 0, 0 };
            for (int i = 0; i < fs.Length; i++)
            {
                for (int l = 0; l < 3; l++)
                {
                    buf[l] = buf[l + 1];
                }
                buf[3] = (byte)fs.ReadByte();
                uint ret = 0;
                for (int l = 0; l < 4; l++)
                {
                    ret |= (uint)(buf[l] << (l * 8));
                }
                if (ret == 0x62643030 || ret == 0x63643030)
                {
                    start = (uint)(i - 3);
                    var len = fs.fread_DWORD();
                    fs.Seek(-4, SeekOrigin.Current);
                    if (len > 0)
                    {
                        break;
                    }
                }
            }
            fs.Seek(start, SeekOrigin.Begin);

            //00db,00dc - compressed and uncompressed
            //01wb - audio
            string[] markers = new[] { "00db", "00dc", "01wb" };
            //string[] skipmarkers = new[] { "01wb" };
            var bts = markers.Select(z => BitConverter.ToUInt32(Encoding.UTF8.GetBytes(z), 0)).ToArray();
            //var skipbts = skipmarkers.Select(z => BitConverter.ToUInt32(Encoding.UTF8.GetBytes(z), 0)).ToArray();

            while (true)
            {
                var marker = fs.fread_DWORD();

                if (bts.Contains(marker))
                {
                    var pos = fs.Position - 4;
                    var len = fs.fread_DWORD();
                    shifts.Add(new ShiftInfo(shifts.Count, pos, Encoding.UTF8.GetString(BitConverter.GetBytes(marker)), len));

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

            long shift = shifts.Where(z => !z.IsAudio).ToArray()[index].Position;
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

        public byte[] GetRawFrame(int index)
        {
            long shift = shifts[index].Position;
            fs.Seek(shift, SeekOrigin.Begin);
            var marker = fs.fread_DWORD();
            var len = fs.fread_DWORD();

            byte[] bb = new byte[len];
            fs.Read(bb, 0, bb.Length);
            return bb;
        }
        public byte[] GetAudioFrame(int index)
        {
            if (index > (shifts.Count - 1))
            {
                return null;
            }

            long shift = shifts.Where(z => z.IsAudio).ToArray()[index].Position;

            fs.Seek(shift, SeekOrigin.Begin);
            var marker = fs.fread_DWORD();
            var len = fs.fread_DWORD();

            byte[] bb = new byte[len];
            fs.Read(bb, 0, bb.Length);

            return bb;
        }
        public uint Fps;

        public string Path { get; internal set; }
        public int Frames
        {
            get
            {
                return shifts.Count(z => !z.IsAudio);
            }
        }
        public int AudioFrames
        {
            get
            {
                return shifts.Count(z => z.IsAudio);
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

        public byte[] GetAudioFrames(int index)
        {
            var v = shifts.Where(z => !z.IsAudio).ToArray()[index];
            
            List<byte> data = new List<byte>();
            for (int i = v.Index + 1; i < shifts.Count; i++)
            {
                if (!shifts[i].IsAudio) break;
            
                data.AddRange(GetRawFrame(i));
            }
            return data.ToArray();           

        }
    }
}
