using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ScreenRecorder
{
    /*
     * TODO:
     * 1. append frames to avi          
     * 2. editor/subs
     */
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            UpdateList();

            checkBox4.Enabled = false;
            DoubleBuffered = true;
            listView1.DoubleBuffered(true);

            CreateIcons();
            checkBox1.Image = RecStartIcon;
            checkBox1.ImageAlign = ContentAlignment.MiddleLeft;
            checkBox1.TextImageRelation = TextImageRelation.ImageBeforeText;

            checkBox4.Image = RecPauseIcon;
            checkBox4.ImageAlign = ContentAlignment.MiddleLeft;
            checkBox4.TextImageRelation = TextImageRelation.ImageBeforeText;
        }


        Bitmap RecStartIcon;
        Bitmap RecStopIcon;
        Bitmap RecPauseIcon;
        Bitmap RecResumeIcon;
        public void CreateIcons()
        {
            RecStartIcon = new Bitmap(16, 16);
            //todo: simple vector drawer. 
            var gr = Graphics.FromImage(RecStartIcon);
            gr.Clear(Color.Transparent);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.FillEllipse(Brushes.Green, 0, 0, RecStartIcon.Width - 1, RecStartIcon.Height - 1);

            RecStopIcon = new Bitmap(16, 16);

            gr = Graphics.FromImage(RecStopIcon);
            gr.Clear(Color.Transparent);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.FillRectangle(Brushes.Red, 0, 0, RecStopIcon.Width - 1, RecStopIcon.Height - 1);


            RecPauseIcon = new Bitmap(16, 16);

            gr = Graphics.FromImage(RecPauseIcon);
            gr.Clear(Color.Transparent);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.FillRectangle(Brushes.Yellow, 0, 0, RecPauseIcon.Width / 2 - 2, RecPauseIcon.Height - 1);
            gr.FillRectangle(Brushes.Yellow, RecPauseIcon.Width / 2 + 2, 0, RecPauseIcon.Width / 2 - 1, RecPauseIcon.Height - 1);

            RecResumeIcon = new Bitmap(16, 16);

            gr = Graphics.FromImage(RecResumeIcon);
            gr.Clear(Color.Transparent);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.FillPolygon(Brushes.Green, new PointF[] { new PointF(0, 0), new PointF(0, 15), new PointF(15, 8) });

        }



        public void UpdateList()
        {

            listView1.Items.Clear();
            var wnds = User32.FindWindows(delegate (IntPtr wnd, IntPtr param)
            {
                return User32.GetWindowText(wnd).Contains(watermark1.Text);
                return true;
            });
            User32.EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                var txt = User32.GetWindowText(wnd);

                if (!string.IsNullOrEmpty(txt) && txt.ToUpper().Contains(watermark1.Text.ToUpper()))
                {
                    listView1.Items.Add(new ListViewItem(new string[] { txt, wnd.ToString() }) { Tag = wnd });
                }
                return true;
            }, IntPtr.Zero);
        }



        /*
        static Bitmap CaptureCursor(ref int x, ref int y)
        {
            Bitmap bmp;
            IntPtr hicon;
            User32.CURSORINFO ci = new User32.CURSORINFO();
             User32.ICONINFO icInfo;
            ci.cbSize = Marshal.SizeOf(ci);
            if (User32.GetCursorInfo(out ci))
            {
                if (ci.flags == User32.CURSOR_SHOWING)
                {
                    hicon = User32.CopyIcon(ci.hCursor);
                    if (User32.GetIconInfo(hicon, out icInfo))
                    {
                        x = ci.ptScreenPos.x - ((int)icInfo.xHotspot);
                        y = ci.ptScreenPos.y - ((int)icInfo.yHotspot);
                        Icon ic = Icon.FromHandle(hicon);
                        bmp = ic.ToBitmap();

                        return bmp;
                    }
                }
            }
            return null;
        }*/

        Bitmap bmpScreenshot;
        Graphics gfxScreenshot;
        RECT rect;
        int cntr = 0;

        Font hintFont = new Font("Consolas", 12);
        private void timer1_Tick(object sender, EventArgs e)
        {

            RECT _rect;
            User32.GetWindowRect(hwn, out _rect);
            label1.BackColor = Color.Green;
            label1.ForeColor = Color.White;
            if (!User32.IsWindow(hwn))
            {
                label1.Text = "Incorrect window";
                label1.BackColor = Color.Red;
                label1.ForeColor = Color.White;
                return;
            }
            if (_rect.Width <= 0 || _rect.Height <= 0)
            {
                return;
            }
            if (_rect.Width != rect.Width || _rect.Height != rect.Height)
            {
                //size changed
                bmpScreenshot.Dispose();
                gfxScreenshot.Dispose();

                rect = _rect;

                bmpScreenshot = new Bitmap((rect.Width / 2 + 1) * 2, (rect.Height / 2 + 1) * 2, PixelFormat.Format24bppRgb);
                gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            }

            rect = _rect;


            gfxScreenshot.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            //User32.PrintWindow(hwn,)


            var pos = (Cursor.Position);
            int w = 6;
            pos.X -= rect.Left;
            pos.Y -= rect.Top;
            if (checkBox3.Checked)
            {
                if (radioButton4.Checked)
                {
                    gfxScreenshot.DrawEllipse(new Pen(Color.Red, 2), pos.X - w / 2, pos.Y - w / 2, w, w);
                }
            }

            List<Keys> lk = new List<Keys>();
            lk.Add(Keys.LButton);
            lk.Add(Keys.RButton);
            lk.Add(Keys.MButton);
            lk.Add(Keys.LShiftKey);
            lk.Add(Keys.RShiftKey);
            lk.Add(Keys.RControlKey);
            lk.Add(Keys.LControlKey);
            lk.Add(Keys.Alt);

            int xx = 0;
            if (checkBox2.Checked)
            {
                foreach (var item in Enum.GetValues(typeof(Keys)))
                {
                    if (User32.GetAsyncKeyState((Keys)item) != 0)
                    {
                        var ms = gfxScreenshot.MeasureString(item.ToString(), hintFont);
                        gfxScreenshot.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Blue)), 5 + xx, 5, ms.Width, hintFont.Height);
                        gfxScreenshot.DrawString(item.ToString(), hintFont, Brushes.White, 5 + xx, 5);

                        xx += (int)ms.Width + 10;
                    }
                }
            }

            pictureBox1.Image = bmpScreenshot;

            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            Encoder myEncoder = Encoder.Quality;
            Encoder myEncoder2 = Encoder.Compression;

            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, int.Parse(textBox2.Text));
            myEncoderParameters.Param[0] = myEncoderParameter;

            if (!pause)
            {
                using (FileStream fs = new FileStream("temp.avi", FileMode.Append, FileAccess.Write))
                {
                    MemoryStream ms = new MemoryStream();
                    if (radioButton2.Checked)
                    {
                        bmpScreenshot.Save(ms, ImageFormat.Png);
                    }
                    else
                    {
                        bmpScreenshot.Save(ms, jgpEncoder, myEncoderParameters);
                    }
                    fs.Write(ms.ToArray(), 0, (int)ms.Length);
                    toolStripStatusLabel1.Text = "Record on. Size: " + (fs.Length / (1024f * 1024f) + " MB");
                }
            }
            cntr++;
        }



        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!User32.IsWindow(hwn))
            {
                label1.Text = "Incorrect window";
                label1.BackColor = Color.Red;
                label1.ForeColor = Color.White;
                return;
            }


            timer1.Enabled = checkBox1.Checked;
            if (checkBox1.Checked)
            {
                checkBox1.Image = RecStopIcon;
                checkBox4.Enabled = true;
                if (File.Exists("temp.avi"))
                {
                    File.Delete("temp.avi");
                }
                watermark1.Enabled = false;
                listView1.Enabled = false;
            }
            else
            {
                watermark1.Enabled = true;
                listView1.Enabled = true;
                pause = false;
                checkBox4.Text = "Pause";
                checkBox4.Image = RecPauseIcon;
                checkBox4.Enabled = false;
                checkBox1.Image = RecStartIcon;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Avi files (*.avi)|*.avi";
                if (MessageBox.Show("Save avi file?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        bool rewr = true;
                        if (File.Exists(sfd.FileName))
                        {
                            if (MessageBox.Show("File exist: " + sfd.FileName + ". Overwrite?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                            {
                                rewr = false;
                            }
                        }

                        if (rewr)
                        {
                            MakeAvi();
                            File.Copy("temp.avi", sfd.FileName, true);
                            toolStripStatusLabel1.Text = "File saved: " + sfd.FileName;
                        }
                    }
                }
            }
        }

        void MakeAvi()
        {
            MjpegIterator mjpeg = new MjpegIterator("temp.avi");
            MjpegAviRecorder.Write("output.avi", mjpeg, uint.Parse(textBox3.Text));
            mjpeg.Close();
            File.Delete("temp.avi");
            File.Move("output.avi", "temp.avi");
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                IntPtr wnd = (IntPtr)listView1.SelectedItems[0].Tag;
                if (pictureBox1.Image != null)
                {
                    pictureBox1.Image.Dispose();
                }
                pictureBox1.Image = User32.CaptureImage(wnd);

                hwn = wnd;
                label1.Text = "Handle: " + wnd.ToString();

                User32.GetWindowRect(hwn, out rect);

                if (bmpScreenshot != null)
                {
                    bmpScreenshot.Dispose();
                }
                if (gfxScreenshot != null)
                {
                    gfxScreenshot.Dispose();
                }
                bmpScreenshot = new Bitmap((rect.Width / 2 + 1) * 2, (rect.Height / 2 + 1) * 2, PixelFormat.Format24bppRgb);
                gfxScreenshot = Graphics.FromImage(bmpScreenshot);
            }
        }

        IntPtr hwn;

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var i = (int)pictureBox1.SizeMode;
            i++;
            var vals = Enum.GetValues(typeof(PictureBoxSizeMode));
            i %= vals.Length;

            pictureBox1.SizeMode = (PictureBoxSizeMode)i;
        }



        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        private void watermark1_TextChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
            {
                label4.Visible = true;
                textBox2.Visible = true;
            }
            else
            {
                label4.Visible = false;
                textBox2.Visible = false;
            }
        }

        bool pause = false;
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                pause = true;
                checkBox4.Image = RecResumeIcon;
                checkBox4.Text = "Resume";
            }
            else
            {
                pause = false;
                checkBox4.Text = "Pause";
                checkBox4.Image = RecPauseIcon;
            }
        }

        private void checkBox3_CheckedChanged_1(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        AviContainer avi;
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Avi files (*.avi)|*.avi";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                if (avi != null)
                {
                    avi.Close();
                }
                avi = new AviContainer(ofd.FileName);
                textBox1.Text = avi.Fps.ToString();                
            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (avi != null)
            {
                if (MessageBox.Show("Update avi file: " + avi.Path + "?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    avi.SaveFps();                    
                    avi.Close();                    
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (avi != null)
            {
                if (!uint.TryParse(textBox1.Text, out avi.Fps))
                {
                    textBox1.BackColor = Color.Red;
                    textBox1.ForeColor = Color.White;
                }
                else
                {
                    textBox1.BackColor = Color.White;
                    textBox1.ForeColor = Color.Black;
                }
            }
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            
        }
    }
}
