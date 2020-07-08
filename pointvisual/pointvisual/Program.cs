using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using CsvHelper;

namespace pointvisual
{
    class Program
    {
        //пути ко входным файлам: csv с координами взгляда + картинка на которую смотрели
        private static readonly string inputCsv = @"D:\projects\screencapture\screencapture\bin\Debug\project\targetTest\test.csv";
        private static readonly string inputPic = @"D:\projects\screencapture\screencapture\bin\Debug\project\targetTest\test.png";

        //директория (путь) куда будет сохранено изображение с наложением
        private static readonly string output = @"D:\projects\screencapture\screencapture\bin\Debug\project\targetTest\";

        //лист с координатами взгляда
        private static List<Eye> eyeList = new List<Eye>();
        //лист с координатами где зафиксирован взгляд
        private static List<Eye> fixList = new List<Eye>();
      
        private const int difPercent = 5; //процент погрешности фиксации взгляда
        private static int dif; //заданный процент от высоты экрана в пикселях

        private static readonly Brush brush_DarkBlue = new SolidBrush(Color.DarkBlue);
        private static readonly Brush brush_Black = new SolidBrush(Color.Red);

        static void Main(string[] args)
        {
            //находим заданый процент от высоты экрана в пикселях
            dif = (1080 / 100) * difPercent; 

            //загружаем данные с шапки (единый .csv на все исследование)
            using (var csvReader = new StreamReader(inputCsv))
            using (var csv = new CsvReader(csvReader))
            {
                csv.Configuration.HeaderValidated = null; //отключаем валидацию хедеров, чтобы фиксация взгляда (tgf) не вызывала исключения
                csv.Configuration.MissingFieldFound = null; //также отключаем проверку на не найденные поля
                eyeList = FilterData(csv.GetRecords<Eye>().ToList());
            }

            fixList = GetLifeTGF(); //набор точек, по которым нужно построить карту

            //подгружаем скриншот
            Bitmap image = new Bitmap(inputPic);
            //счетчик
            int idx = 1;
            using (Graphics g = Graphics.FromImage(image))
            {
                foreach (var item in fixList)
                {
                    g.DrawEllipse(new Pen(brush_DarkBlue, 2),
                        item.gazeX - (20 / 2), item.gazeY - (20 / 2), 20, 20);
                    g.DrawString(idx.ToString(), new Font("Tahoma", 10, FontStyle.Bold), Brushes.Black, new RectangleF(item.gazeX - 8, item.gazeY - 8, 50, 50));

                    if (idx < fixList.Count)
                        g.DrawLine(new Pen(brush_DarkBlue, 1), new Point(item.gazeX, item.gazeY), new Point(fixList[idx].gazeX, fixList[idx].gazeY));

                    idx++;
                }

                foreach (var item in eyeList)
                {
                    g.DrawRectangle(new Pen(brush_Black, 1), item.gazeX, item.gazeY, 1, 1);
                }
            }

            image.Save(Path.Combine(output, "output.png"), ImageFormat.Png);

            using (var writer = new StreamWriter(Path.Combine(output, "output.csv")))
            using (var csv = new CsvWriter(writer))
            {
                csv.WriteRecords(eyeList);
            }

            Console.WriteLine("Successfully!");
            Console.ReadLine();
        }

        public static List<Eye> FilterData(List<Eye> data) //филтрует набор значений, отбрасывая не корректные (координаты за пределами экрана)
        {
            List<Eye> outData = new List<Eye>();

            foreach(var item in data)
            {
                if (Enumerable.Range(1, 1920).Contains(item.gazeX))
                    if (Enumerable.Range(1, 1080).Contains(item.gazeY))
                        outData.Add(item);
            }

            return outData;
        }

        //определяет время фиксации взгляда
        public static List<Eye> GetLifeTGF() //TODO: пофиксить, чтобы было время фиксации взгляда (раскомментировать + доработать входные/выходные данные)!
        {
            List<RowCluster> points = new List<RowCluster>();
            List<Eye> output = new List<Eye>();
            List<ClusterTimeFixation> clusters = new List<ClusterTimeFixation>();

            int StartX = eyeList[0].gazeX; //Координаты первой точки в кластере TODO: исправить после внесений фиксов в стимулятор!
            int StartY = eyeList[0].gazeY; //TODO: исправить после внесений фиксов в стимулятор! 

            int ClusterCounter = 1; //Счетчик кластеров, когда по условию создается новый кластер, увеличивается
            double startTimeFixed = Double.Parse(eyeList[0].timeStamp.Replace(".", ",")); //Меняется, когда объявляется новый кластер и указывает на время начала фиксации в номо кластере
            double timeFixed = 0; //Вычисляется на каждой строке, время фиксирования от начала фиксации на кластере до данной точки
            double tempTimeStamp = 0; //для хранения значения timeStamp предыдущей точки, при последовательном проходе по ним

            for (var i = 0; i < eyeList.Count; i++)
            {
                //Первую строку определяем как стартовую для кластера 1, по идее это условие срабатывает только 1 раз вначале
                if (StartX == 0 && StartY == 0)
                {
                    StartX = eyeList[i].gazeX;
                    StartY = eyeList[i].gazeY;
                }

                //Вычитаем из XY текущей строки значения XY стартовой точки последнего кластера, 
                //если разница не больше, чем заданый пользователем предел, то добавляем в List<UpdateRow> id этой строки с номером текущего кластера...                
                if ((Math.Abs((Int32.Parse(eyeList[i].gazeX.ToString()) - StartX)) <= dif) && (Math.Abs((Int32.Parse(eyeList[i].gazeY.ToString()) - StartY)) <= dif))
                {
                    points.Add(new RowCluster(Int32.Parse(i.ToString()), ClusterCounter)); //добавляется новая точка в list, с номером текущего кластера
                    timeFixed = Double.Parse(eyeList[i].timeStamp.Replace(".", ",")) - startTimeFixed;
                    tempTimeStamp = Double.Parse(eyeList[i].timeStamp.Replace(".", ",")); //временная переменная хранит таймстамп предыдущего шага
                }
                else //... иначе номер кластера увеличиваем на единицу и данная точка становится стартовой для нового кластера
                {
                    clusters.Add(new ClusterTimeFixation(ClusterCounter, timeFixed)); //кластер записывается в list
                    ClusterCounter++; //начинается новый кластер, абстрактно
                    points.Add(new RowCluster(Int32.Parse(i.ToString()), ClusterCounter)); //добавляется новая точка в list, с номером  нового кластера
                    StartX = Int32.Parse(eyeList[i].gazeX.ToString()); //определяются стартовые точки, как начальная точка нового кластера
                    StartY = Int32.Parse(eyeList[i].gazeY.ToString());

                    startTimeFixed = tempTimeStamp; //Время начала фиксации нового кластера
                    tempTimeStamp = Double.Parse(eyeList[i].timeStamp.Replace(".", ",")); //временная переменная для сохранения таймстампа этого шага для следующей строки
                }
            }

            clusters.Add(new ClusterTimeFixation(ClusterCounter, timeFixed)); //добавляется последний кластер

            Console.WriteLine("clusters: " + clusters.Count);

            //соотносим каждую строку(точку) кластеру из списка кластеров и апдейтим время фиксации в каждой точке на время фиксации кластера
            for(int i = 1; i < points.Count - 1; i++)
            {
                if (points[i].cluster != points[i + 1].cluster)
                {
                    foreach (var item in clusters)
                    {
                        if (item.cluster == points[i].cluster)
                        {
                            output.Add(new Eye(eyeList[i].gazeX, eyeList[i].gazeY, item.timeFixation.ToString(), points[i].cluster));
                            break;
                        }
                    }
                }
            }

            Console.WriteLine("points: " + output.Count);
            return output;
        }

        //структуры необходимые для вычисления фиксации взгляда
        //для контроля кластера
        public struct RowCluster
        {
            public int id { get; set; }
            public int cluster { get; set; }

            public RowCluster(int id, int cluster)
            {
                this.id = id;
                this.cluster = cluster;
            }
        }

        //для контроля времени фиксации взгляда
        public struct ClusterTimeFixation
        {
            public int cluster { get; set; }
            public double timeFixation { get; set; }

            public ClusterTimeFixation(int cluster, double timeFixation)
            {
                this.cluster = cluster;
                this.timeFixation = timeFixation;
            }
        }

        //структура для результирующий данных (необходимых для построения)
        public struct Eye
        {
            public int gazeX { get; set; }
            public int gazeY { get; set; }
            public string timeStamp { get; set; }
            public int cluster { get; set; }

            public Eye(int gazeX, int gazeY, string timeStamp, int cluster)
            {
                this.gazeX = gazeX;
                this.gazeY = gazeY;
                this.timeStamp = timeStamp;
                this.cluster = cluster;
            }
        }
    }
}