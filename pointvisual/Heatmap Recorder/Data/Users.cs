using System.IO;
using System.Collections.Generic;
using System.Linq;
using CsvHelper;

namespace Heatmap_Recorder
{
    public class Users
    {
        public int id { get; set; }
        public string name { get; set; }
        public int gender { get; set; }
        public int years { get; set; }
        public string comments { get; set; }

        public Users(int id, string name, int gender, int years, string comments = "")
        {
            this.id = id;
            this.name = name;
            this.gender = gender;
            this.years = years;
            this.comments = comments;
        }

        public static Users Add(Users user, string path)
        {
            if(!File.Exists(Path.Combine(path, "users.csv")))
            {
                List<Users> New = new List<Users>();
                user.id = 1;

                New.Add(user);
                Write(New, path);
            }
            else
            {
                List<Users> Old = Read(Path.Combine(path, "users.csv"));

                foreach(var item in Old)
                {
                    if (item.name == user.name & item.years == user.years & item.gender == user.gender)
                        return item;
                }

                user.id = Old[Old.Count - 1].id + 1;

                Old.Add(user);
                Write(Old, path);
            }

            return user;
        }

        private static void Write(List<Users> input, string path)
        {
            using (var writer = new StreamWriter(Path.Combine(path, "users.csv")))
            using (var csv = new CsvWriter(writer))
            {
                csv.WriteRecords(input);
            }

            Directory.CreateDirectory(Path.Combine(path, input[input.Count - 1].id.ToString()));
        }

        private static List<Users> Read(string path)
        {
            List<Users> output = new List<Users>();

            using (var reader = new StreamReader(path))
            using (var csv = new CsvReader(reader))
            {
                output = csv.GetRecords<Users>().ToList();
            }

            return output;
        }
    }
}
