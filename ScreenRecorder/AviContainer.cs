using System.IO;

namespace ScreenRecorder
{
    public class AviContainer
    {
        public AviContainer(string path)
        {
            Path = path;
            fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
            UpdateInfos();
        }
        FileStream fs;

        void UpdateInfos()
        {
            fs.Seek(fpsPosition, SeekOrigin.Begin);
            uint fps = MjpegAviRecorder.fread_DWORD(fs);
            //Fps = 1000000 / avihdwMicroSecPerFrame;
            Fps = fps;
        }

        public uint Fps;

        public string Path { get; internal set; }
        long fpsPosition = 132;
        public void SaveFps()
        {

            fs.Seek(fpsPosition, SeekOrigin.Begin);
            MjpegAviRecorder.fwrite_DWORD(fs, (uint)Fps);
        }

        public void Close()
        {            
            fs.Close();
        }
    }
}
