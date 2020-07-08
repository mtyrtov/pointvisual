using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using EyeXFramework;
using Tobii.EyeX.Framework;
using CsvHelper;

namespace Heatmap_Recorder
{
    public partial class Viewer : Form
    {
        //для чтения данных с eye-трекера и обеспечения минимальной дискретности сигнала
        private EyeXHost Host;
        private GazePointDataStream Stream;

        //для контроля записи/отображения картинок
        private static List<string> ImageList = new List<string>(); //набор картинок к показу (путей до них)
        private static int idx = -1; //индекс показываемой картинки
        private static bool record = false; //статус записи
        private static string path; //путь до папки с проектом

        private static List<Eye> data = new List<Eye>(); //лист для данных

        public Viewer(string dir, string project, int time, int pause, int mode) //путь к папке, время показа, время паузы, порядок показа
        {
            path = project;
            ImageList = InitDir(dir, mode); //инициализируем файлы из переданной директории
            InitializeComponent();

            button1.Location = new Point((Screen.PrimaryScreen.Bounds.Size.Width - button1.Width) / 2, //располагаем кнопку по центру экрана
                (Screen.PrimaryScreen.Bounds.Size.Height - button1.Height) / 2);
            timer1.Interval = time * 1000; //устанавливаем время показа картинки
            timer2.Interval = pause * 1000; //устанавливаем время между картинками
        }

        private List<string> InitDir(string path, int mode)
        {
            List<string> output = new List<string>();
            DirectoryInfo dir = new DirectoryInfo(path); //инциализурует директорию
            FileInfo[] files = dir.GetFiles("*.png"); //TODO: сделать чтобы можно было указать больше одного расширения

            foreach (var file in files) //заполняет лист путями к картинкам
                output.Add(file.FullName);

            if(mode > 0) //если указан случайный порядок показа - перемешает лист
            {
                Random rnd = new Random();

                for (int i = 0; i < output.Count; i++)
                {
                    var temp = output[0];
                    output.RemoveAt(0);
                    output.Insert(rnd.Next(output.Count), temp);
                }
            }

            return output;
        }

        private void EyeStart() //стартует поток записи данных с eye-трекера
        {
            Host = new EyeXHost();
            {
                Stream = Host.CreateGazePointDataStream(GazePointDataMode.LightlyFiltered);
                {
                    Host.Start();

                    Stream.Next += (s, e) => {
                        if (record)
                            data.Add(new Eye((int)e.X, (int)e.Y, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                    };
                }
            }
        }

        private void SaveData(string path = "") //сохраняет полученные данные в .csv
        {
            if (path.Length == 0)
                path = "eye-" + data[data.Count - 1].timestamp.ToString().Replace(",", String.Empty) + ".csv";

            if (data.Count > 0)
            {
                using (var writer = new StreamWriter(path))
                using (var csv = new CsvWriter(writer))
                {
                    csv.WriteRecords(data);
                }

                data.Clear();
            }
        }

        private void SetPicture() //устанавливает изображения для показа, устанавливает флаг для начала записи данных в память
        {
            idx++;

            if (idx < ImageList.Count)
            {
                pictureBox1.Image = new Bitmap(ImageList[idx]);
                timer1.Enabled = true;
                record = true;
            }
            else
                this.Close(); //<-- временно!
        }

        private void SetPause() //останавливает запись, скрывает картинку, записывает данные
        {
            record = false;
            SaveData(Path.Combine(path, Data.user.id.ToString(), Examinations.Add(new Examinations(0, Data.user.id, ImageList[idx], 0, 0, 0), path) + ".csv"));
            
            pictureBox1.Image = null;
            timer2.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Visible = false;
            timer2.Start();
        }

        private void timer1_Tick(object sender, EventArgs e) //меняет картинки (время показа)
        {
            SetPause();
            timer1.Enabled = false;
        }

        private void timer2_Tick(object sender, EventArgs e) //пауза между картинками
        {
            SetPicture();
            timer2.Enabled = false;
        }

        private void Viewer_Load(object sender, EventArgs e)
        {
            EyeStart();
        }

        private void Viewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            Host.Dispose();
        }
    }

    public struct Eye
    {
        public int gazeX { get; set; }
        public int gazeY { get; set; }
        public long timestamp { get; set; }

        public Eye(int gazeX, int gazeY, long timestamp)
        {
            this.gazeX = gazeX;
            this.gazeY = gazeY;
            this.timestamp = timestamp;
        }
    }
}