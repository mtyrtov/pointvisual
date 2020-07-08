using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace heatmaptester
{
    public partial class Form1 : Form
    {
        private static List<Heatmap.HeatPoint> HeatPoints = new List<Heatmap.HeatPoint>(); //набор точек для построения тепловой карты
        private static List<Assistant.Eye> Data = new List<Assistant.Eye>(); //Все данные из файла с координами взгляда
        private static List<Assistant.Eye> Segment = new List<Assistant.Eye>(); //Выборка данных по временным отрезкам из файла с координатами взгляда

        private static Bitmap HeatMap; //слой для тепловой карты
        private static Bitmap BackgroundPic; //слой для всего остального

        private static string FileName;
        private static List<Times> timeSettings = new List<Times>();
        
        private static int CurrentRange = 1;
        private static int CurrentDrop = 0;

        //1) Полка мейкер:
	       // * Рендер под конкретное разрешение с черными полосами

        //2) Привязка к центру:
	       // *погрешность регулурется
	       // *максимальная погрешность - ширина продукта

        //3) Файлы в тепловую карту берем исходные картинки(к ним привязаны дата-файлы)
        //4) В стимуляторе отключаем скрин экрана

        public Form1()
        {
            InitializeComponent();
            Directory.CreateDirectory("temp");

            timeSettings.Add(new Times(0, 1));
            timeSettings.Add(new Times(1, 3));
            timeSettings.Add(new Times(3, 6));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (BackgroundPic != null)
            {
                openFileDialog1.Filter = "CSV Files (*.csv)| *.csv";
                openFileDialog1.Multiselect = true;

                if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                    return;

                if(openFileDialog1.FileNames.Length > 1)
                {
                    openFileDialog1.InitialDirectory = openFileDialog1.FileNames[0];
                }
                else
                {
                    openFileDialog1.InitialDirectory = openFileDialog1.FileName;
                }
                
                try
                {
                    CurrentDrop = 0;
                    checkBox2.Checked = false;
                    
                    if(openFileDialog1.FileNames.Length > 1)
                    {
                        comboBox1.Items.Clear();

                        Data = Assistant.DataConcat(openFileDialog1.FileNames, BackgroundPic.Width, BackgroundPic.Height);

                        foreach(var file in openFileDialog1.FileNames)
                        {
                            comboBox1.Items.Add(Path.GetFileName(file));
                        }

                        comboBox1.SelectedIndex = 0;
                    }
                    else
                    {
                        Data = Assistant.ReadFile(openFileDialog1.FileName, BackgroundPic.Width, BackgroundPic.Height);
                    }

                    double start;
                    double end;

                    try
                    {
                        start = Decimal.ToDouble(numericUpDown1.Value);
                        end = Decimal.ToDouble(numericUpDown2.Value);
                    }
                    catch
                    {
                        MessageBox.Show("Указан корректный промежуток!");
                        return;
                    }

                    Segment = Assistant.GetDataSegment(Data, start, end);
                    textBox1.Text = openFileDialog1.FileName;

                    var max = Math.Round(Double.Parse(Data[Data.Count - 1].timeStamp) - Double.Parse(Data[0].timeStamp));
                    numericUpDown1.Maximum = (int) max;
                    numericUpDown2.Maximum = (int) max;

                    GetFileName();
                }
                catch
                {
                    MessageBox.Show("Файл открыт в другой программе! Закройте и повторите попытку.");
                    return;
                }

                if (BackgroundPic != null)
                {
                    CreatePathMap();

                    //HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);
                    if(radioButton2.Checked)
                    {
                        HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);
                    }
                    else
                    {
                        HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);
                    }

                    CreateMap(checkBox1.Checked);
                    pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);
                }
            }
            else
            {
                MessageBox.Show("Сначала залейте фоновое изображение!");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files (*.bmp;*.jpg;*.png)|*.BMP;*.JPG;*.PNG";
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            openFileDialog1.InitialDirectory = openFileDialog1.FileName;
            textBox2.Text = openFileDialog1.FileName;

            string tempPath = @"temp/" + Path.GetFileName(openFileDialog1.FileName);

            if (!File.Exists(tempPath))
            {
                File.Copy(openFileDialog1.FileName, tempPath);
            }
            
            if(BackgroundPic != null)
            {
                pictureBox1.Image = null;
            }
            
            BackgroundPic = new Bitmap(tempPath);

            if (Segment.Count > 0)
                CreatePathMap();
            else
                pictureBox1.BackgroundImage = Assistant.ResizeImage(BackgroundPic, pictureBox1.Width, pictureBox1.Height);
        }

        private void CreatePathMap()
        {
            if(BackgroundPic != null & Segment.Count > 0)
            {
                if (pictureBox1.BackgroundImage != null)
                    pictureBox1.BackgroundImage.Dispose();

                if (fixLayer.Checked)
                    pictureBox1.BackgroundImage = Assistant.ResizeImage(Pathmap.CreateMap(new Bitmap(BackgroundPic), Segment, trackBar3.Value, trackBar4.Value,
                        checkPoints.Checked, checkFix.Checked, checkPath.Checked, BackgroundPic.Height), pictureBox1.Width, pictureBox1.Height);
                else
                    pictureBox1.BackgroundImage = Assistant.ResizeImage(BackgroundPic, pictureBox1.Width, pictureBox1.Height);
            }
        }

        private void CreateMap(bool color)
        {
            if(HeatPoints.Count > 0)
            {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();

                Bitmap bMap = new Bitmap(1280, 1024);
                bMap = Heatmap.CreateIntensityMask(bMap, HeatPoints, trackBar2.Value);

                if (HeatMap != null)
                    HeatMap.Dispose();

                if (color)
                    HeatMap = Heatmap.Colorize(bMap, 255);
                else
                    HeatMap = Assistant.MakeGrayscale(Heatmap.Colorize(bMap, 255));

                exportButton.Enabled = true;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if(HeatMap != null)
            {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();
                pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);
            }
        }

        private void trackBar2_ValueChanged(object sender, EventArgs e)
        {
            if(HeatMap != null)
            {
                CreateMap(checkBox1.Checked);
                pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (BackgroundPic != null & Segment.Count > 0)
            {
                checkBox1.Checked = true;
                trackBar1.Value = 70;
                trackBar2.Value = 25;

                CreateMap(checkBox1.Checked);
                pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = textBox3.Text;
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;

            Bitmap image;

            if(radioButton2.Checked)
            {
                HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);
            }
            else
            {
                HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);
            }

            if (BackgroundPic != null)
            {
                if (!fixLayer.Checked)
                    image = new Bitmap(BackgroundPic);
                else
                    image = Pathmap.CreateMap(new Bitmap(BackgroundPic), Segment, trackBar3.Value, trackBar4.Value, checkPoints.Checked, checkFix.Checked, checkPath.Checked, BackgroundPic.Height);

                if (checkBox5.Checked)
                {
                    using (Graphics g = Graphics.FromImage(image))
                    {
                        g.DrawImage(Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100), 0, 0);
                        g.Dispose();
                    }
                }

                image.Save(saveFileDialog1.FileName);
                Clipboard.SetImage(image);
                image.Dispose();
            }
            else
            {
                DialogResult dialogResult = MessageBox.Show("Экспортировать карту без фона?", "Heatmap Maker", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100).Save(saveFileDialog1.FileName);
                }
                else
                    return;
            }

            panel1.Visible = true;
            timer1.Enabled = true;
        }

        private void checkPoints_CheckedChanged(object sender, EventArgs e)
        {
            label7.Text = trackBar3.Value.ToString() + "%";
            label8.Text = trackBar4.Value.ToString() + "px";

            if (BackgroundPic != null & Segment.Count > 0)
            {
                if (radioButton2.Checked)
                {
                    HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);

                    CreateMap(checkBox1.Checked);
                    pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);
                }

                CreatePathMap();
            }
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            CurrentRange = 1;
            numericUpDown1.Value = timeSettings[CurrentRange].start;
            numericUpDown2.Value = timeSettings[CurrentRange].end;

            trackBar3.Value = 5;
            trackBar4.Value = 20;
            label7.Text = trackBar3.Value.ToString() + "%";
            label8.Text = trackBar4.Value.ToString() + " px";

            checkPoints.Checked = false;
            checkFix.Checked = false;
            checkPath.Checked = false;

            radioButton2.Checked = true;

            GenerateMap();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            trackBar1.Enabled = checkBox5.Checked;
            trackBar2.Enabled = checkBox5.Checked;
            checkBox1.Enabled = checkBox5.Checked;
            button1.Enabled = checkBox5.Checked;

            if (HeatMap != null)
            {
                if (pictureBox1.Image != null)
                    pictureBox1.Image.Dispose();

                if (checkBox5.Checked)
                    pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);
                else
                    pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, 0);
            }
        }

        private void fixLayer_CheckedChanged(object sender, EventArgs e)
        {
            checkPoints.Enabled = fixLayer.Checked;
            checkFix.Enabled = fixLayer.Checked;
            checkPath.Enabled = fixLayer.Checked;
            trackBar3.Enabled = fixLayer.Checked;
            trackBar4.Enabled = fixLayer.Checked;
            button2.Enabled = fixLayer.Checked;

            CreatePathMap();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            GenerateMap();
        }

        private string SetPatternTime(string input)
        {
            if(input.IndexOf(",") > 0)
            {
                return input.Replace(",", "").Substring(0, 2);
            }
            else
            {
                return input + "0";
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            if (BackgroundPic != null & Segment.Count > 0)
            {
                if (radioButton.Checked)
                {
                    if (radioButton.Tag.ToString() == "0")
                    {
                        HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);

                        CreateMap(checkBox1.Checked);
                        pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);

                        CreatePathMap();
                    }
                    else
                    {
                        HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);

                        CreateMap(checkBox1.Checked);
                        pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);

                        CreatePathMap();
                    }
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if(CurrentRange > 0)
            {
                CurrentRange--;

                numericUpDown1.Value = timeSettings[CurrentRange].start;
                numericUpDown2.Value = timeSettings[CurrentRange].end;

                GenerateMap();
            }
        }

        private void GenerateMap(int drop = -1, bool upd = false)
        {
            if (Data.Count > 0 & BackgroundPic != null)
            {
                double start;
                double end;

                try
                {
                    start = Decimal.ToDouble(numericUpDown1.Value);
                    end = Decimal.ToDouble(numericUpDown2.Value);
                }
                catch
                {
                    MessageBox.Show("Указан корректный промежуток!");
                    return;
                }

                if (end <= start)
                {
                    MessageBox.Show("Выбран не корректный промежуток!");
                }
                else
                {
                    if(drop >= 0 || upd)
                    {
                        Data = Assistant.DataConcat(openFileDialog1.FileNames, BackgroundPic.Width, BackgroundPic.Height, drop);
                    }

                    Segment = Assistant.GetDataSegment(Data, start, end);

                    if (Segment.Count > 0)
                    {
                        if (radioButton2.Checked)
                        {
                            HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);
                        }
                        else
                        {
                            HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);
                        }

                        CreateMap(checkBox1.Checked);
                        pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);

                        CreatePathMap();

                        GetFileName();
                    }
                    else
                    {
                        MessageBox.Show("Для этого промежутка нет данных!");
                    }
                }
            }
            else
            {
                MessageBox.Show("Сначала загрузите картинку и данные!");
            }
        }

        private void GetFileName()
        {
            FileName = Path.GetFileNameWithoutExtension(textBox2.Text);
            textBox3.Text = FileName + "-" + SetPatternTime(numericUpDown1.Value.ToString()) + "_" + SetPatternTime(numericUpDown2.Value.ToString());
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (CurrentRange < 2)
            {
                CurrentRange++;

                numericUpDown1.Value = timeSettings[CurrentRange].start;
                numericUpDown2.Value = timeSettings[CurrentRange].end;

                GenerateMap();
            }
        }

        private void domainUpDown1_SelectedItemChanged(object sender, EventArgs e)
        {
            //if (Data.Count > 0 & BackgroundPic != null)
            //{
            //    double start = Double.Parse(domainUpDown1.Text);
            //    double end = Double.Parse(domainUpDown2.Text);

            //    Segment = Assistant.GetDataSegment(Data, start, end);

            //    if (radioButton2.Checked)
            //    {
            //        HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);
            //    }
            //    else
            //    {
            //        HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);
            //    }

            //    CreateMap(checkBox1.Checked);
            //    pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);

            //    CreatePathMap();
            //}
            //else
            //{
            //    MessageBox.Show("Сначала загрузите картинку и данные!");
            //}
        }

        private void domainUpDown2_SelectedItemChanged(object sender, EventArgs e)
        {
            //if (Data.Count > 0 & BackgroundPic != null)
            //{
            //    double start = Double.Parse(domainUpDown1.Text);
            //    double end = Double.Parse(domainUpDown2.Text);

            //    Segment = Assistant.GetDataSegment(Data, start, end);

            //    if (radioButton2.Checked)
            //    {
            //        HeatPoints = Assistant.GetTGFPoints(Segment, trackBar3.Value, BackgroundPic.Width, BackgroundPic.Height);
            //    }
            //    else
            //    {
            //        HeatPoints = Assistant.GetPoints(Segment, BackgroundPic.Width, BackgroundPic.Height);
            //    }

            //    CreateMap(checkBox1.Checked);
            //    pictureBox1.Image = Assistant.SetImageOpacity(HeatMap, (float)trackBar1.Value / 100);

            //    CreatePathMap();
            //}
            //else
            //{
            //    MessageBox.Show("Сначала загрузите картинку и данные!");
            //}
        }

        public struct Times
        {
            public int start;
            public int end;

            public Times(int start, int end)
            {
                this.start = start;
                this.end = end;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            panel1.Visible = false;
            timer1.Enabled = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(Data.Count > 0 & BackgroundPic != null)
            {
                button7.Enabled = checkBox2.Checked;
                button8.Enabled = checkBox2.Checked;
                comboBox1.Enabled = checkBox2.Checked;

                if (checkBox2.Checked)
                {
                    GenerateMap(comboBox1.SelectedIndex);
                }
                else
                {
                    GenerateMap(-1, true);
                }
            }
            else
            {
                checkBox2.Checked = false;
            }
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            if(CurrentRange > 0)
            {
                CurrentRange--;
                comboBox1.SelectedIndex = CurrentRange;
                GenerateMap(comboBox1.SelectedIndex);
            }
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            if(CurrentRange < comboBox1.Items.Count - 1)
            {
                CurrentRange++;
                comboBox1.SelectedIndex = CurrentRange;
                GenerateMap(comboBox1.SelectedIndex);
            }
        }
    }
}