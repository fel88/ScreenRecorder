using System;
using System.Drawing;
using System.Windows.Forms;

namespace ScreenRecorder
{
    public class Watermark : TextBox
    {
        public Watermark()
        {
            TextChanged += Watermark_TextChanged;
            this.Invalidate(true);
            this.SetStyle(ControlStyles.UserPaint, true);
            ForeColor = SystemColors.GrayText;
        }


        private void Watermark_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                this.SetStyle(ControlStyles.UserPaint, true);
                ForeColor = SystemColors.GrayText;
            }
            else
            {
                this.SetStyle(ControlStyles.UserPaint, false);
                ForeColor = Color.Black;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(Text))
            {
                e.Graphics.DrawString(Hint, Font, SystemBrushes.GrayText, new Point(0, 0));
            }
            else
            {
                e.Graphics.DrawString(Text, Font, SystemBrushes.GrayText, new Point(0, 0));
            }

            base.OnPaint(e);
        }

        public string Hint { get; set; }
    }    
}
