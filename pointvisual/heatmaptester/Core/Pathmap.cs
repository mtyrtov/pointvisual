using System;
using System.Collections.Generic;
using System.Drawing;

namespace heatmaptester
{
    public class Pathmap
    {
        private static readonly Brush brush_DarkBlue = new SolidBrush(Color.DarkBlue);
        private static readonly Brush brush_Black = new SolidBrush(Color.Red);
        private static readonly Brush brush_Green = new SolidBrush(Color.Green);

        public static Bitmap CreateMap(Bitmap image, List<Assistant.Eye> data, int difPercent, int radius, bool points, bool fix, bool path, int height)
        {
            List<Assistant.Eye> fixList = GetLifeTGF(data, difPercent, height); //набор точек, по которым нужно построить карту
            
            int idx = 1;
            using (Graphics g = Graphics.FromImage(image))
            {
                if(points)
                {
                    foreach (var item in data)
                    {
                        g.DrawRectangle(new Pen(brush_Black, 1), item.gazeX, item.gazeY, 1, 1);
                    }
                }
                
                foreach (var item in fixList)
                {
                    if(fix)
                    {
                        if(idx != 1)
                        {
                            g.DrawEllipse(new Pen(brush_DarkBlue, 2), item.gazeX - (radius / 2), item.gazeY - (radius / 2), radius, radius);
                        }
                        else
                        {
                            g.DrawEllipse(new Pen(brush_Green, 2), item.gazeX - (radius / 2), item.gazeY - (radius / 2), radius, radius);
                        } 
                        g.DrawString(idx.ToString(), new Font("Tahoma", 10, FontStyle.Bold), Brushes.Black, new RectangleF(item.gazeX - 8, item.gazeY - 8, 50, 50));
                    }

                    if(path)
                    {
                        if (idx < fixList.Count)
                            g.DrawLine(new Pen(brush_DarkBlue, 1), new Point(item.gazeX, item.gazeY), new Point(fixList[idx].gazeX, fixList[idx].gazeY));
                    }

                    idx++;
                }
            }
            
            return image;
        }

        //определяет время фиксации взгляда
        public static List<Assistant.Eye> GetLifeTGF(List<Assistant.Eye> eyeList, int difPercent, int height)
        {
            int dif = (height / 100) * difPercent;

            List<RowCluster> points = new List<RowCluster>();
            List<Assistant.Eye> output = new List<Assistant.Eye>();
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

            //соотносим каждую строку(точку) кластеру из списка кластеров и апдейтим время фиксации в каждой точке на время фиксации кластера
            foreach (var i in points)
            {
                foreach (var j in clusters)
                {
                    if (i.cluster == j.cluster)
                    {
                        output.Add(new Assistant.Eye(eyeList[i.id].gazeX, eyeList[i.id].gazeY, j.timeFixation.ToString(), j.cluster));
                    }
                }
            }

            int sumX = 0;
            int sumY = 0;
            int counter = 0;
            List<Assistant.Eye> rangeList = new List<Assistant.Eye>();
            for (var i = 0; i < output.Count - 1; i++)
            {
                sumX = sumX + output[i].gazeX;
                sumY = sumY + output[i].gazeY;
                counter++;

                if (output[i].cluster != output[i + 1].cluster)
                {
                    rangeList.Add(new Assistant.Eye(sumX / counter, sumY / counter, output[i].timeStamp, output[i].cluster));

                    sumX = 0;
                    sumY = 0;
                    counter = 0;
                }
            }

            return rangeList;
        }

        public static List<Assistant.Eye> GetMapTGF(List<Assistant.Eye> eyeList, int difPercent, int height)
        {
            List<Assistant.Eye> output = new List<Assistant.Eye>();

            if (eyeList.Count > 0)
            {
                int dif = (height / 100) * difPercent;

                List<RowCluster> points = new List<RowCluster>();
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

                //соотносим каждую строку(точку) кластеру из списка кластеров и апдейтим время фиксации в каждой точке на время фиксации кластера
                foreach (var i in points)
                {
                    foreach (var j in clusters)
                    {
                        if (i.cluster == j.cluster)
                        {
                            output.Add(new Assistant.Eye(eyeList[i.id].gazeX, eyeList[i.id].gazeY, j.timeFixation.ToString(), j.cluster));
                        }
                    }
                }

                return output;
            }
            else
            {
                return output;
            }
            
            //    int sumX = 0;
            //    int sumY = 0;
            //    int counter = 0;
            //    List<Assistant.Eye> rangeList = new List<Assistant.Eye>();
            //    for (var i = 0; i < output.Count - 1; i++)
            //    {
            //        sumX = sumX + output[i].gazeX;
            //        sumY = sumY + output[i].gazeY;
            //        counter++;

            //        if (output[i].cluster != output[i + 1].cluster)
            //        {
            //            rangeList.Add(new Assistant.Eye(sumX / counter, sumY / counter, output[i].timeStamp, output[i].cluster));

            //            sumX = 0;
            //            sumY = 0;
            //            counter = 0;
            //        }
            //    }

            //    return rangeList;
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
    }
}
