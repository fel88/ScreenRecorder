using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

            comboBox2.SelectedIndex = 0;

            comboBox1.Items.Add(new ComboBoxItem() { Name = "24bpp", Tag = PixelFormat.Format24bppRgb });
            comboBox1.Items.Add(new ComboBoxItem() { Name = "8 bit Indexed", Tag = PixelFormat.Format8bppIndexed });
            comboBox1.Items.Add(new ComboBoxItem() { Name = "4 bit Indexed", Tag = PixelFormat.Format4bppIndexed });
            comboBox1.Items.Add(new ComboBoxItem() { Name = "1 bit Indexed", Tag = PixelFormat.Format1bppIndexed });
            comboBox1.SelectedIndex = 0;

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

        public static async Task CopyFileAsync(string sourceFile, string destinationFile)
        {
            using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                await sourceStream.CopyToAsync(destinationStream);
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
                RECT _rect;
                User32.GetWindowRect(wnd, out _rect);

                if (!string.IsNullOrEmpty(txt) && txt.ToUpper().Contains(watermark1.Text.ToUpper()))
                {
                    listView1.Items.Add(new ListViewItem(new string[] { txt, wnd.ToString(), $"{_rect.Width}x{_rect.Height}" }) { Tag = wnd });
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

            label1.BackColor = Color.Green;
            label1.ForeColor = Color.White;
            if (wholeScreen)
            {
                var b = Screen.PrimaryScreen.Bounds;
                _rect = new RECT() { Left = b.Left, Top = b.Top, Width = b.Width, Height = b.Height };
            }
            else
            {
                User32.GetWindowRect(hwn, out _rect);
                if (!User32.IsWindow(hwn))
                {
                    label1.Text = "Incorrect window";
                    label1.BackColor = Color.Red;
                    label1.ForeColor = Color.White;
                    return;
                }
            }

            if (_rect.Width <= 0 || _rect.Height <= 0)
                return;

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
                        int yy = 5;
                        if (KeysHintsLocation == KeysHintsLocation.BottomLeft)
                        {
                            yy = rect.Height - hintFont.Height - 5;
                        }
                        if (item.ToString().Length > 0)
                        {
                            var ms = gfxScreenshot.MeasureString(item.ToString(), hintFont);
                            gfxScreenshot.FillRectangle(new SolidBrush(Color.FromArgb(128, Color.Blue)), 5 + xx, yy, ms.Width, hintFont.Height);
                            gfxScreenshot.DrawString(item.ToString(), hintFont, Brushes.White, 5 + xx, yy);
                            xx += (int)ms.Width + 10;
                        }
                    }
                }
            }

            pictureBox1.Image = bmpScreenshot;

            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);

            Encoder myEncoder = Encoder.Quality;
            Encoder myEncoder2 = Encoder.Compression;

            EncoderParameters myEncoderParameters = new EncoderParameters(1);

            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, jpegQuality);
            myEncoderParameters.Param[0] = myEncoderParameter;

            if (!pause)
            {
                using (FileStream fs = new FileStream("temp.avi", FileMode.Append, FileAccess.Write))
                {
                    MemoryStream ms = new MemoryStream();
                    if (radioButton2.Checked)
                    {
                        bmpScreenshot.Clone(new Rectangle(0, 0, bmpScreenshot.Width, bmpScreenshot.Height), CurrentPixelFormat).Save(ms, ImageFormat.Png);
                    }
                    else
                    {
                        bmpScreenshot.Save(ms, jgpEncoder, myEncoderParameters);
                    }
                    fs.Write(ms.ToArray(), 0, (int)ms.Length);
                    toolStripStatusLabel1.Text = "Record on. Size: " + ((fs.Length / (1024f * 1024f)).ToString("0.0") + " MB") + "; Frames captured: " + cntr;
                }
                cntr++;
            }
        }

        PixelFormat CurrentPixelFormat
        {
            get
            {
                return (PixelFormat)((comboBox1.SelectedItem as ComboBoxItem).Tag);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!wholeScreen && !User32.IsWindow(hwn))
            {
                label1.Text = "Incorrect window";
                label1.BackColor = Color.Red;
                label1.ForeColor = Color.White;
                return;
            }


            timer1.Enabled = checkBox1.Checked;
            if (checkBox1.Checked)
            {
                cntr = 0;
                pause = false;
                checkBox1.Image = RecStopIcon;
                checkBox4.Enabled = true;
                if (File.Exists("temp.avi"))
                {
                    File.Delete("temp.avi");
                }
                watermark1.Enabled = false;
                listView1.Enabled = false;
                checkBox6.Enabled = false;
            }
            else
            {
                StopRecord();
            }
        }

        public async void StopRecord()
        {
            watermark1.Enabled = true;
            listView1.Enabled = true;
            checkBox6.Enabled = true;

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
                        await CopyFileAsync("temp.avi", sfd.FileName);
                        //File.Copy("temp.avi", sfd.FileName, true);
                        toolStripStatusLabel1.Text = "File saved: " + sfd.FileName;
                        if (MessageBox.Show("Run file: " + sfd.FileName + "?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            Process.Start(sfd.FileName);
                        }
                    }
                }
            }
        }

        uint fps = 30;

        void MakeAvi()
        {
            MjpegIterator mjpeg = new MjpegIterator("temp.avi");
            MjpegAviRecorder.Write("output.avi", mjpeg, fps);
            mjpeg.Close();
            File.Delete("temp.avi");
            File.Move("output.avi", "temp.avi");
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count <= 0)
                return;

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
                crntFrame = 0;
                avi = new AviContainer(ofd.FileName);
                pictureBox2.Image = avi.GetFrame(0);
                trackBar1.Maximum = avi.Frames;
                textBox1.Text = avi.Fps.ToString();
                textBox4.Text = avi.Frames.ToString();

                timer2.Interval = (int)(1000f / avi.Fps);
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

        void SetPlayFrame(int index)
        {
            if (pictureBox2.Image != null)
            {
                pictureBox2.Image.Dispose();
            }
            pictureBox2.Image = avi.GetFrame(index);


            //play audio
            /*var bytes = avi.GetAudioFrames(index);
            if (bytes.Length > 0)
            {
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                // Call unmanaged code
                w.SendWODevice(unmanagedPointer, (uint)bytes.Length);
                //Marshal.FreeHGlobal(unmanagedPointer);
            }*/



        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (avi != null && !isPlay)
            {
                SetPlayFrame(trackBar1.Value);
                crntFrame = trackBar1.Value;
                textBox4.Text = crntFrame + " / " + avi.Frames;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var i = (int)pictureBox2.SizeMode;
            i++;
            var vals = Enum.GetValues(typeof(PictureBoxSizeMode));
            i %= vals.Length;

            pictureBox2.SizeMode = (PictureBoxSizeMode)i;
        }
        bool isPlay = false;
        int crntFrame = 0;
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!isPlay) return;
            crntFrame++;
            textBox4.Text = crntFrame + " / " + avi.Frames;
            if (crntFrame >= avi.Frames)
            {
                isPlay = false;
                return;
            }

            SetPlayFrame(crntFrame);
            trackBar1.Value = crntFrame;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            isPlay = !isPlay;
            if (isPlay)
            {
                crntFrame = 0;
                w.InitWODevice(48000, 2, 16, false);  // initialise the audio device  
                for (int i = 0; i < avi.AudioFrames; i++)
                {
                    var bytes = avi.GetAudioFrame(i);
                    IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                    Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                    // Call unmanaged code
                    w.SendWODevice(unmanagedPointer, (uint)bytes.Length);
                    //Marshal.FreeHGlobal(unmanagedPointer);

                }
            }
            else
            {
                w.RawResetWODevice();
                w.CloseWODevice();
            }
            trackBar1.Enabled = !isPlay;
        }
        woLib w = new ScreenRecorder.woLib();
        private void button1_Click(object sender, EventArgs e)
        {

            w.InitWODevice(48000, 2, 16, false);  // initialise the audio device  
            for (int i = 0; i < avi.AudioFrames; i++)
            {
                var bytes = avi.GetAudioFrame(i);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                // Call unmanaged code
                w.SendWODevice(unmanagedPointer, (uint)bytes.Length);
                //Marshal.FreeHGlobal(unmanagedPointer);

            }
            // while (w.GetQueued() > 0) ;
            // w.CloseWODevice();
        }

        int jpegQuality = 90;
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            jpegQuality = int.Parse(textBox2.Text);
        }

        bool wholeScreen = false;
        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            wholeScreen = checkBox6.Checked;
            if (wholeScreen)
            {
                var b = Screen.PrimaryScreen.Bounds;
                rect = new RECT() { Left = b.Left, Top = b.Top, Width = b.Width, Height = b.Height };

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
            listView1.Enabled = !wholeScreen;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            fps = uint.Parse(textBox3.Text);
        }

        public KeysHintsLocation KeysHintsLocation;

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            hintFont = new Font("Consolas", (int)numericUpDown1.Value);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            KeysHintsLocation = comboBox2.SelectedIndex == 1 ? KeysHintsLocation.BottomLeft : KeysHintsLocation.TopLeft;
        }
    }
}
