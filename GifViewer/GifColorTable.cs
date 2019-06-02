using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace GifViewer
{
    public class GifColorTable
    {
        public int ColorsAmount
        {
            get; set;
        }
        public List<Color> Colors = new List<Color>();
        public void Read(Stream s)
        {
            for (int i = 0; i < ColorsAmount; i++)
            {
                Colors.Add(s.ReadColor3());
            }
        }

        public void Write(Stream s)
        {
            for (int i = 0; i < ColorsAmount; i++)
            {
                var clr = Colors[i];
                s.WriteByte(clr.R);
                s.WriteByte(clr.G);
                s.WriteByte(clr.B);
            }
        }
    }

}
