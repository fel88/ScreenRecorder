using GifLib;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace GifViewer
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            Text = Caption;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            typeof(PictureBox).InvokeMember("DoubleBuffered",
 BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
 null, pictureBox1, new object[] { true });

        }


        public void UpdateList()
        {
            listView1.Items.Clear();
            listView1.Items.Add(new ListViewItem(new string[] { "LogicalScreen" }) { Tag = g.LogicalScreen.Desc });
            if (g.LogicalScreen.Desc.GlobalColorTableFlag)
            {
                listView1.Items.Add(new ListViewItem(new string[] { "glob clr table" }) { Tag = g.LogicalScreen.GlobalColorTable });
            }
            foreach (var item in g.Data)
            {
                if (item is GifRenderBlock)
                {
                    var r = item as GifRenderBlock;
                    if (r.ColorTable != null)
                    {
                        listView1.Items.Add(new ListViewItem(new string[] { "clr table" }) { Tag = r.ColorTable });
                    }

                }
                listView1.Items.Add(new ListViewItem(new string[] { item.GetType().Name }) { Tag = item });
            }
        }

        GifContainer g;


        public const string Caption = "GifViewer";
        public void LoadGif(string path)
        {
            Text = Caption + ": " + path;

            g = GifParser.Parse(path);

            UpdateList();
        }


        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                var tag = listView1.SelectedItems[0].Tag;
                propertyGrid1.SelectedObject = listView1.SelectedItems[0].Tag;
                if (tag is GifColorTable)
                {
                    listView2.Items.Clear();
                    for (int i = 0; i < (tag as GifColorTable).Colors.Count; i++)
                    {
                        listView2.Items.Add(new ListViewItem(new string[] { "clr" + i }) { Tag = (tag as GifColorTable).Colors[i] });
                    }
                }

            }
        }



        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                var clr = (Color)listView2.SelectedItems[0].Tag;
                pictureBox2.BackColor = clr;
                label2.Text = clr.ToString();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "GIF (*.gif)|*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                frm = 0;
                LoadGif(ofd.FileName);
            }
        }


        int frm = 0;
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = g.GetFrame(++frm);
            UpdateLabels();
        }

        void UpdateLabels()
        {
            label1.Text = "frame: " + frm + " / " + g.Frames;
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                pictureBox1.Image = g.GetFrame(frm++);
                if (g.LastGceBlock != null)
                {

                    if (checkBox2.Checked)
                    {
                        timer1.Interval = g.LastGceBlock.Delay;
                    }
                    else
                    {
                        timer1.Interval = 1;
                    }
                }
                frm %= g.Frames;
                UpdateLabels();
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pictureBox1.SizeMode = (PictureBoxSizeMode)(((int)pictureBox1.SizeMode + 1) % (Enum.GetValues(typeof(PictureBoxSizeMode)).Length));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.SetStyle(
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.UserPaint |
    ControlStyles.DoubleBuffer,
    true);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;


            var ff = g.GetRawFrameStream(frm);

            //var fr = g.Data.First(z => z is GifRenderBlock) as GifRenderBlock;

            //Clipboard.SetImage(decbmp);

            var bb = ff.ToArray();
            File.WriteAllBytes(sfd.FileName, bb);

            var s = Bitmap.FromStream(ff) as Bitmap;

            int alp = 0;
            for (int i = 0; i < s.Width; i++)
            {
                for (int j = 0; j < s.Height; j++)
                {
                    var px = s.GetPixel(i, j);
                    if (px.A < 0xff)
                    {
                        alp++;
                    }
                }
            }
            if (alp > 0)
            {

            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                GifContainer gifc = new GifContainer();
                gifc.Header = new GifHeader();
                //1. create pallete from tru color bitmaps using 

                //File.WriteAllBytes("output.gif", ms.ToArray());
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfd.ShowDialog() != DialogResult.OK) return;

            var fin = new FileInfo(sfd.FileName);
            for (int i = 0; i < g.Frames; i++)
            {
                var nm = Path.GetFileNameWithoutExtension(fin.FullName);
                var f = g.GetFrame(i);
                f.Save(Path.Combine(fin.Directory.FullName, $"{nm}{i}.png"));
            }
            MessageBox.Show("ready! files was saved into " + fin.Directory.FullName);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            frm = int.Parse(textBox1.Text);
            if (frm < 0 || frm >= g.Frames)
            {
                MessageBox.Show("error: [0;" + (g.Frames - 1) + "] only");
            }
            else
            {
                pictureBox1.Image = g.GetFrame(frm);
                UpdateLabels();
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            var fr = g.GetFrame(frm);
            Clipboard.SetImage(fr);
        }
    }
}
