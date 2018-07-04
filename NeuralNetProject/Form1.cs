using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuralNetProject
{
    public partial class Form1 : Form
    {
        public struct NN_Digit
        {
            public byte Label;
            public byte[] Image;
        }

        NN_Digit[] DisplayDigits;

        #region Sam's Garbage
        //Consants
        bool bDataLoaded = false;

        static int ImgWidth;
        static int ImgHeight;

        static int DigitIndex = 0;

        static int SCALE_RATIO = 3;

        //Drawing
        Panel DrawPanel;
        bool bMouseDown = false;
        Bitmap DigitMap;
        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private static NN_Digit[] LoadData()
        {
            NN_Digit[] Digits = null;

            OpenFileDialog OpenFile = new OpenFileDialog();

            if (OpenFile.ShowDialog() == DialogResult.OK)
            {
                FileStream Labels = (FileStream)OpenFile.OpenFile();

                //Reads in the header information for labels file
                byte[] bMagic = new byte[4];
                byte[] bLength = new byte[4];

                Labels.Read(bMagic, 0, bMagic.Length);
                Labels.Read(bLength, 0, bLength.Length);

                Int32[] MagicNum = new Int32[1];
                Int32[] NumDigits = new Int32[1];

                Buffer.BlockCopy(bMagic.Reverse().ToArray(), 0, MagicNum, 0, 4);
                Buffer.BlockCopy(bLength.Reverse().ToArray(), 0, NumDigits, 0, 4);

                //Initializes Digit Array
                Digits = new NN_Digit[NumDigits[0]];

                //Assigns the labels to each Digit
                for (int i = 0; i < NumDigits[0]; i++)
                {
                    Digits[i].Label = (byte)Labels.ReadByte();
                }

                Labels.Close();
            }

            //Maybe add null check for digits before reading image data

            if (OpenFile.ShowDialog() == DialogResult.OK)
            {
                FileStream Images = (FileStream)OpenFile.OpenFile();

                //Reads in header info for Images
                byte[] bMagic = new byte[4];
                byte[] bLength = new byte[4];
                byte[] bRows = new byte[4];
                byte[] bColumns = new byte[4];

                Images.Read(bMagic, 0, bMagic.Length);
                Images.Read(bLength, 0, bLength.Length);
                Images.Read(bRows, 0, bRows.Length);
                Images.Read(bColumns, 0, bColumns.Length);

                Int32[] MagicNum = new Int32[1];
                Int32[] NumDigits = new Int32[1];
                Int32[] Rows = new Int32[1];
                Int32[] Columns = new Int32[1];

                Buffer.BlockCopy(bMagic.Reverse().ToArray(), 0, MagicNum, 0, 4);
                Buffer.BlockCopy(bLength.Reverse().ToArray(), 0, NumDigits, 0, 4);
                Buffer.BlockCopy(bRows.Reverse().ToArray(), 0, Rows, 0, 4);
                Buffer.BlockCopy(bColumns.Reverse().ToArray(), 0, Columns, 0, 4);

                //Initializes dimensions of the images
                ImgWidth = Columns[0];
                ImgHeight = Rows[0];

                int ImageSize = Rows[0] * Columns[0];

                //Assigns image to each Digit
                for (int i = 0; i < NumDigits[0]; i++)
                {
                    Digits[i].Image = new byte[ImageSize];
                    Images.Read(Digits[i].Image, 0, ImageSize);
                }

                Images.Close();
            }

            return Digits;
        }

        public static void ShuffleArray(NN_Digit[] Digits)
        {
            Random Rand = new Random();

            for (int i = Digits.Length - 1; i > 0; i--)
            {
                int j = Rand.Next(i);

                NN_Digit temp = Digits[i];
                Digits[i] = Digits[j];
                Digits[j] = temp;
            }
        }

        #region Drawing

        private void DrawDigit(PaintEventArgs e)
        {
            if (bDataLoaded)
            {
                Bitmap Image = new Bitmap(ImgWidth, ImgHeight);

                int i = 0;

                for (int x = 0; x < ImgWidth; x++)
                {
                    for (int y = 0; y < ImgHeight; y++)
                    {
                        //Because the data values are backwards
                        int Value = 255 - DisplayDigits[DigitIndex].Image[i];

                        Color Color = Color.FromArgb(Value, Value, Value);
                        Image.SetPixel(y, x, Color);
                        i++;
                    }
                }

                //Scale bitmap so that its easier to see
                Bitmap Scale = new Bitmap(Image, new Size(ImgWidth * SCALE_RATIO, ImgHeight * SCALE_RATIO));

                // Draw bitmap to the screen.
                e.Graphics.DrawImage(Scale, (((Width / 4) * 3) - (Scale.Width / 2)), ((Height / 2) - Scale.Height),
                    Scale.Width, Scale.Height);
            }
        }

        private void DrawLabel(PaintEventArgs e)
        {
            if (bDataLoaded)
            {
                string Label = "Label: " + DisplayDigits[DigitIndex].Label;
                Font Font = new Font("Arial", 16);
                SolidBrush Brush = new SolidBrush(Color.Black);
                Point Location = new Point(((Width / 4) * 3) - ((ImgWidth * SCALE_RATIO) / 2), (Height / 2));

                e.Graphics.DrawString(Label, Font, Brush, Location);
            }

        }

        private void CreatePanel()
        {
            DrawPanel = new Panel();
            DrawPanel.Width = ImgWidth * SCALE_RATIO;
            DrawPanel.Height = ImgHeight * SCALE_RATIO;
            DrawPanel.Location = new Point((Width / 4) - (DrawPanel.Width / 2), (Height / 2) - (DrawPanel.Height));
            DrawPanel.Cursor = Cursors.Cross;
            DrawPanel.BackColor = Color.White;

            DrawPanel.MouseDown += new MouseEventHandler(this.drawPanel_MouseDown);
            DrawPanel.MouseMove += new MouseEventHandler(this.drawPanel_MouseMove);
            DrawPanel.MouseUp += new MouseEventHandler(this.drawPanel_MouseUp);
            DrawPanel.Paint += drawPanel_Paint;

            DigitMap = new Bitmap(DrawPanel.Width, DrawPanel.Height);
            using (Graphics g = Graphics.FromImage(DigitMap))
            {
                g.Clear(Color.White);
            }
            Controls.Add(DrawPanel);
        }

        private void drawPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                bMouseDown = true;
        }

        private void drawPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (bMouseDown)
            {
                Point point = DrawPanel.PointToClient(Cursor.Position);
                DrawPoint((point.X), (point.Y));
            }
        }

        private void drawPanel_MouseUp(object sender, MouseEventArgs e)
        {
            bMouseDown = false;
        }

        private void drawPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(DigitMap, Point.Empty);
        }

        public void DrawPoint(int x, int y)
        {
            Color Color = Color.Black;
            SolidBrush Brush = new SolidBrush(Color);
            //Pen Pen = new Pen(Color, 5);

            using (Graphics g = Graphics.FromImage(DigitMap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillRectangle(Brush, x, y, 6, 6);
            }
            DrawPanel.Invalidate();
        }

        private void CreateDrawButtons()
        {
            Button ClearBtn = new Button();
            ClearBtn.Text = "Clear";
            ClearBtn.Location = new Point((Width / 4) - (ImgWidth * SCALE_RATIO), (Height / 2) + ((ImgHeight * SCALE_RATIO) / 2));
            ClearBtn.Click += new EventHandler(this.ClearBtn_Click);

            Button SendBtn = new Button();
            SendBtn.Text = "Send";
            SendBtn.Location = new Point((Width / 4), (Height / 2) + ((ImgHeight * SCALE_RATIO) / 2));
            SendBtn.Click += new EventHandler(this.SendBtn_Click);

            Controls.Add(ClearBtn);
            Controls.Add(SendBtn);
        }

        private void ClearBtn_Click(object sender, EventArgs e)
        {
            using (Graphics g = Graphics.FromImage(DigitMap))
            {
                g.Clear(Color.White);
            }
            DrawPanel.Invalidate();
        }

        private void SendBtn_Click(object sender, EventArgs e)
        {
            //DrawPanel.Invalidate();
            Bitmap Scale = new Bitmap(DigitMap, new Size(DigitMap.Width / SCALE_RATIO, DigitMap.Height / SCALE_RATIO));
            byte[] Input = new byte[ImgWidth * ImgHeight];

            int i = 0;
            for (int x = 0; x < ImgWidth; x++)
            {
                for (int y = 0; y < ImgHeight; y++)
                {
                    byte Value = Scale.GetPixel(y, x).R;
                    Input[i] = (byte)(255 - Value);

                    i++;
                }
            }

            NN_Digit InputDigit = new NN_Digit();
            InputDigit.Image = Input;
            InputDigit.Label = 10;
            DisplayDigits = new NN_Digit[]{ InputDigit };

            DigitIndex = 0;

            Refresh();
        }

        #endregion 

        #region Scroll Buttons
        private void CreateScrollButtons()
        {
            //Fix Locations of buttons
            Button PrevBtn = new Button();
            PrevBtn.Text = "Previous";
            PrevBtn.Location = new Point(((Width / 4) * 3) - (ImgWidth * SCALE_RATIO), (Height / 2) + ((ImgHeight * SCALE_RATIO) / 2));
            PrevBtn.Click += new EventHandler(this.PrevBtn_Click);

            Button NextBtn = new Button();
            NextBtn.Text = "Next";
            NextBtn.Location = new Point(((Width / 4) * 3), (Height / 2) + ((ImgHeight * SCALE_RATIO) / 2));
            NextBtn.Click += new EventHandler(this.NextBtn_Click);

            Controls.Add(PrevBtn);
            Controls.Add(NextBtn);

        }

        private void PrevBtn_Click(object sender, EventArgs e)
        {
            DigitIndex--;

            if (DigitIndex < 0)
            {
                DigitIndex = 0;
            }

            Refresh();
        }

        private void NextBtn_Click(object sender, EventArgs e)
        {
            DigitIndex++;

            if (DigitIndex >= DisplayDigits.Length)
            {
                DigitIndex = DisplayDigits.Length - 1;
            }

            Refresh();
        }

        #endregion

        private void CreateUI()
        {
            CreateScrollButtons();

            CreatePanel();
            CreateDrawButtons();

            Refresh();
        }

        private void openFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisplayDigits = LoadData();
            //ShuffleArray(DisplayDigits);

            bDataLoaded = true;
            CreateUI();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            DrawDigit(e);
            DrawLabel(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Paint += new PaintEventHandler(Form1_Paint); //Link the Paint event to Form1_Paint
        }
    }
}
