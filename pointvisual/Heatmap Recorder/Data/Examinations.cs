using System.Collections.Generic;
using System.Linq;
using System.IO;
using CsvHelper;

namespace Heatmap_Recorder
{
    class Examinations
    {
        public int id { get; set; }
        public int user { get; set; }
        public string image { get; set; }
        public int time { get; set; }
        public int pause { get; set; }
        public int mode { get; set; }

        public Examinations(int id, int user, string image, int time, int pause, int mode)
        {
            this.id = id;
            this.user = user;
            this.image = image;
            this.time = time;
            this.pause = pause;
            this.mode = mode;
        }

        public static int Add(Examinations exam, string path)
        {
            if (!File.Exists(Path.Combine(path, "exam.csv")))
            {
                List<Examinations> New = new List<Examinations>();
                exam.id = 1;

                New.Add(exam);
                Write(New, path);
            }
            else
            {
                List<Examinations> Old = Read(Path.Combine(path, "exam.csv"));
                exam.id = Old[Old.Count - 1].id + 1;

                Old.Add(exam);
                Write(Old, path);
            }

            return exam.id;
        }

        private static void Write(List<Examinations> input, string path)
        {
            using (var writer = new StreamWriter(Path.Combine(path, "exam.csv")))
            using (var csv = new CsvWriter(writer))
            {
                csv.WriteRecords(input);
            }

            Directory.CreateDirectory(Path.Combine(path, input[input.Count - 1].id.ToString()));
        }

        private static List<Examinations> Read(string path)
        {
            List<Examinations> output = new List<Examinations>();

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader))
            {
                output = csv.GetRecords<Examinations>().ToList();
            }

            return output;
        }
    }
}
