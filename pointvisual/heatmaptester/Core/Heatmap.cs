using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace heatmaptester
{
    public class Heatmap
    {
        public static Bitmap CreateIntensityMask(Bitmap bSurface, List<HeatPoint> aHeatPoints, int Radius)
        {
            // Создать новую графическую поверхность из растрового изображения памяти
            Graphics DrawSurface = Graphics.FromImage(bSurface);
            // Установите цвет фона на белый, чтобы пиксели можно было правильно раскрасить
            DrawSurface.Clear(Color.White);
            // Проследите данные точки нагрева и нарисуйте маски для каждой точки нагрева
            foreach (HeatPoint DataPoint in aHeatPoints)
            {
                // Отобразить текущую точку нагрева на поверхности рисования
                DrawHeatPoint(DrawSurface, DataPoint, Radius);
            }
            return bSurface;
        }

        private static void DrawHeatPoint(Graphics Canvas, HeatPoint HeatPoint, int Radius)
        {
            // Создать общий список точек для хранения точек окружности
            List<Point> CircumferencePointsList = new List<Point>();
            // Создайте пустую точку, чтобы заранее определить структуру точек, используемую в цикле окружности.
            Point CircumferencePoint;
            // Создайте пустой массив, который будет заполнен точками из общего списка
            Point[] CircumferencePointsArray;
            // Вычислить отношение к шкале интенсивности байтов в диапазоне от 0-255 до 0-1
            float fRatio = 1F / Byte.MaxValue;
            // Предварительно рассчитать половину байта максимального значения
            byte bHalf = Byte.MaxValue / 2;
            // Отразить интенсивность по центру от низкого-высокого до высокого-низкого
            int iIntensity = (byte)(HeatPoint.Intensity - ((HeatPoint.Intensity - bHalf) * 2));
            // Сохраните масштабированное и отраженное значение интенсивности для использования с расположением центра градиента
            float fIntensity = iIntensity * fRatio;
            // Обведите все углы круга
            // Определите переменную цикла как double, чтобы предотвратить приведение в каждой итерации
            // Повторяйте цикл по дельте в 10 градусов, это может измениться, чтобы улучшить производительность
            for (double i = 0; i <= 360; i += 10)
            {
                // Заменить последнюю точку итерации новой структурой пустой точки
                CircumferencePoint = new Point();
                // Построить новую точку на окружности окружности определенного радиуса
                // Использование координат точки, радиуса и угла
                // Рассчитать положение этой итерации точки на окружности
                CircumferencePoint.X = Convert.ToInt32(HeatPoint.X + Radius * Math.Cos(ConvertDegreesToRadians(i)));
                CircumferencePoint.Y = Convert.ToInt32(HeatPoint.Y + Radius * Math.Sin(ConvertDegreesToRadians(i)));
                // Добавить заново окружную точку в список общих точек
                CircumferencePointsList.Add(CircumferencePoint);
            }
            // Заполните пустой системный массив точек из общего списка точек
            // Сделайте это, чтобы удовлетворить тип данных методов PathGradientBrush и FillPolygon
            CircumferencePointsArray = CircumferencePointsList.ToArray();
            // Создать новый PathGradientBrush для создания радиального градиента, используя точки окружности
            PathGradientBrush GradientShaper = new PathGradientBrush(CircumferencePointsArray);
            // Create new color blend to tell the PathGradientBrush what colors to use and where to put them
            ColorBlend GradientSpecifications = new ColorBlend(3);
            // Определите положения цветов градиента, используйте интенсивность, чтобы настроить средний цвет
            // показать больше маски или меньше маски
            GradientSpecifications.Positions = new float[3] { 0, fIntensity, 1 };
            // Определите цвета градиента и их значения альфа, отрегулируйте альфа цвета градиента, чтобы соответствовать интенсивности
            GradientSpecifications.Colors = new Color[3]
            {
                Color.FromArgb(0, Color.White),
                Color.FromArgb(HeatPoint.Intensity, Color.Black),
                Color.FromArgb(HeatPoint.Intensity, Color.Black)
            };
            // Передайте цветовое смешение в PathGradientBrush, чтобы проинструктировать его, как генерировать градиент
            GradientShaper.InterpolationColors = GradientSpecifications;
            // Нарисуйте многоугольник (круг), используя массив точек и градиентную кисть
            Canvas.FillPolygon(GradientShaper, CircumferencePointsArray);
        }

        private static double ConvertDegreesToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

        public static Bitmap Colorize(Bitmap Mask, byte Alpha)
        {
            // Создать новое растровое изображение, чтобы действовать в качестве рабочей поверхности для процесса окраски
            Bitmap Output = new Bitmap(Mask.Width, Mask.Height, PixelFormat.Format32bppArgb);
            // Создайте графический объект из нашего растрового изображения памяти, чтобы мы могли рисовать на нем и очищать его поверхность для рисования
            Graphics Surface = Graphics.FromImage(Output);
            Surface.Clear(Color.Transparent);
            // Создайте массив сопоставлений цветов, чтобы перенастроить маску в оттенки серого на полный цвет.
            // Примите альфа-байт, чтобы указать прозрачность выходного изображения.
            ColorMap[] Colors = CreatePaletteIndex(Alpha);
            // Создать новый класс атрибутов изображения для обработки переназначений цветов
            // Внедрите наш массив цветовой карты, чтобы проинструктировать класс атрибутов изображения, как сделать раскрашивание
            ImageAttributes Remapper = new ImageAttributes();
            Remapper.SetRemapTable(Colors);
            // Нарисуйте свою маску на рабочей поверхности растрового изображения памяти, используя новую схему цветового отображения
            Surface.DrawImage(Mask, new Rectangle(0, 0, Mask.Width, Mask.Height), 0, 0, Mask.Width, Mask.Height, GraphicsUnit.Pixel, Remapper);
            // Отправить обратно заново раскрашенное растровое изображение памяти
            return Output;
        }

        private static ColorMap[] CreatePaletteIndex(byte Alpha)
        {
            ColorMap[] OutputMap = new ColorMap[256];
            // Измените этот путь туда, где вы сохранили изображение палитры.
            Bitmap Palette = (Bitmap)Bitmap.FromFile("palette.bmp");
            // Прокрутите каждый пиксель и создайте новое цветовое отображение
            for (int X = 0; X <= 255; X++)
            {
                OutputMap[X] = new ColorMap();
                OutputMap[X].OldColor = Color.FromArgb(X, X, X);
                OutputMap[X].NewColor = Color.FromArgb(Alpha, Palette.GetPixel(X, 0));
            }
            return OutputMap;
        }

        public struct HeatPoint
        {
            public int X;
            public int Y;
            public byte Intensity;

            public HeatPoint(int X, int Y, byte Intensity)
            {
                this.X = X;
                this.Y = Y;
                this.Intensity = Intensity;
            }
        }
    }
}
