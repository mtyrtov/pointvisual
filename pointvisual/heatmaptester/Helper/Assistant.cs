using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using CsvHelper;

namespace heatmaptester
{
    public class Assistant
    {
        public static List<Eye> DataConcat(string[] files, int width, int height, int drop = -1) //склеивает данные из всех файлов 
        {
            List<Eye> result = new List<Eye>();

            int idx = 0;
            foreach (string file in files)
            {
                if(idx != drop)
                {
                    List<Eye> temp = ReadFile(file, width, height);
                    temp = FixTimeStamp(temp);
                    result.AddRange(temp);
                }
                else
                {
                    Console.WriteLine(file);
                }
                idx++;
            }

            result = result.OrderBy(Eye => Eye.timeStamp).ToList();
            return result;
        }

        public static List<Eye> FixTimeStamp(List<Eye> input)
        {
            List<Eye> output = new List<Eye>();
            double timeStart = Double.Parse(input[0].timeStamp);

            foreach(var point in input)
            {
                output.Add(new Eye(point.gazeX, point.gazeY, (Double.Parse(point.timeStamp) - timeStart).ToString(), point.cluster));
            }

            return output;
        }

        public static List<Eye> ReadFile(string path, int width, int height) //читает данные из файла
        {
            List<Eye> output = new List<Eye>();

            using (var csvReader = new StreamReader(path))
            using (var csv = new CsvReader(csvReader))
            {
                csv.Configuration.HeaderValidated = null; //отключаем валидацию хедеров, чтобы фиксация взгляда (tgf) не вызывала исключения
                csv.Configuration.MissingFieldFound = null; //также отключаем проверку на не найденные поля
                output = FilterData(csv.GetRecords<Eye>().ToList(), width, height);
            }

            return output;
        }

        public static List<Eye> GetDataSegment(List<Eye> data, double start, double end)
        {
            List<Eye> new_data = new List<Eye>();
            data = FixTimeStamp(data);

            foreach (var point in data)
            {
                if(Double.Parse(point.timeStamp) >= start & Double.Parse(point.timeStamp) < end)
                {
                    new_data.Add(point);
                }
            }

            return new_data;
        }

        public static List<Heatmap.HeatPoint> GetPoints(List<Eye> input, int width, int height) //конвертирует данные в формат для построения тепловой карт
        {
            List<Heatmap.HeatPoint> output = new List<Heatmap.HeatPoint>();

            int[,] temp = new int[width, height];

            foreach (var item in input)
            {
                try
                {
                    temp[item.gazeX, item.gazeY] += 1;
                }
                catch
                {

                }
            }

            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    if (temp[i, j] != 0)
                        output.Add(new Heatmap.HeatPoint(i, j, Convert.ToByte(temp[i, j])));

            return output;
        }

        public static List<Heatmap.HeatPoint> GetTGFPoints(List<Eye> input, int difPercent, int width, int height)
        {
            List<Heatmap.HeatPoint> output = new List<Heatmap.HeatPoint>();

            if(input.Count > 0)
            {
                var dataTgf = Pathmap.GetMapTGF(input, difPercent, height);
                double max = GetMaxTGF(dataTgf);

                foreach (var item in dataTgf)
                {
                    output.Add(new Heatmap.HeatPoint(item.gazeX, item.gazeY, Convert.ToByte(NormalizeTGF(max, item.timeStamp))));
                }
            }

            return output;
        }

        private static double GetMaxTGFv2(double[,] input, int width, int height)
        {
            double max = 0;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (input[i, j] > max)
                    {
                        max = input[i, j];
                    }
                }
            }

            return max;
        }

        private static int NormalizeTGF(double max, string timestamp)
        {
            return (int)Math.Round((Double.Parse(timestamp) / max) * 32);
        }

        private static double GetMaxTGF(List<Eye> input)
        {
            double max = Double.Parse(input[0].timeStamp);

            foreach (var item in input)
            {
                if (Double.Parse(item.timeStamp) > max)
                {
                    max = Double.Parse(item.timeStamp);
                }
            }

            return max;
        }

        //private static double GetMinTGF(List<Eye> input)
        //{
        //    double min = Double.Parse(input[0].timeStamp);

        //    foreach (var item in input)
        //    {
        //        if(Double.Parse(item.timeStamp) < min)
        //        {
        //            min = Double.Parse(item.timeStamp);
        //        }
        //    }

        //    return min;
        //}

        public static List<Eye> FilterData(List<Eye> data, int width, int height) //филтрует набор значений, отбрасывая не корректные (координаты за пределами экрана)
        {
            List<Eye> output = new List<Eye>();

            foreach (var item in data)
            {
                if (Enumerable.Range(1, width).Contains(item.gazeX))
                    if (Enumerable.Range(1, height).Contains(item.gazeY))
                        output.Add(item);
            }

            return output;
        }

        public static Image SetImageOpacity(Image image, float opacity) //устанавливает прозрачность изображения (слоя с тепловой картой)
        {
            try
            {
                //создать растровое изображение размером предоставленного изображения
                Bitmap bmp = new Bitmap(image.Width, image.Height);

                //создать графический объект из изображения
                using (Graphics gfx = Graphics.FromImage(bmp))
                {
                    //создать объект цветовой матрицы  
                    ColorMatrix matrix = new ColorMatrix();

                    //установить непрозрачность 
                    matrix.Matrix33 = opacity;

                    //создать атрибуты изображения
                    ImageAttributes attributes = new ImageAttributes();

                    //установить цвет (непрозрачность) изображения 
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    //Теперь нарисуйте изображение 
                    gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }

                return bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public static Image ResizeImage(Image imgToResize, int newWidth, int newHeight) //ресайз изображения
        {
            Bitmap b = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(imgToResize, 0, 0, newWidth, newHeight);
            g.Dispose();

            return (Image)b;
        }

        public static Bitmap MakeGrayscale(Bitmap original) //перевод в чб (хз зачем оно надо)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
                    new float[] {.3f, .3f, .3f, 0, 0},
                    new float[] {.59f, .59f, .59f, 0, 0},
                    new float[] {.11f, .11f, .11f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

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
