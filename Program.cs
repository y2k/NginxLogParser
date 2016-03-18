using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SQLite;
using static System.Environment;

namespace Y2k.NginxLogParser
{
    public class Program
    {
        public static void Main()
        {
            var stats = File
                .ReadAllLines(GetFolderPath(SpecialFolder.MyDocuments) + "/Dropbox/Documents/access.log")
                .Select(s => new Parser().Parse(s));

            File.Delete("stats.db");
            using (var db = new SQLiteConnection("stats.db"))
            {
                db.CreateTable<LogEntry>();
                db.InsertAll(stats, true);
            }
        }
    }

    class Parser
    {
        // 92.113.149.181 - - [11/Mar/2016:08:02:47 +0000] "GET /cache/fit?quality=30&bgColor=ffffff&width=336&height=596&url=http://img0.joyreactor.cc/pics/post/-2929746.gif 
        // HTTP/1.1" 404 0 "-" "Dalvik/2.1.0 (Linux; U; Android 5.1; m2 Build/LMY47D)" "-"
        static readonly Regex Pattern = new Regex("(\\d+\\.\\d+\\.\\d+\\.\\d+) - - \\[(.+?)\\] \"(\\w+) ([^ ]+) HTTP/([\\d\\.]+)\" (\\d+) (\\d+) \"(.+?)\" \"(.+?)\"");

        public LogEntry Parse(string text)
        {
            var parts = Pattern.Match(text).Groups.Cast<Group>().Select(s => s.Value).Skip(1).ToList();
            var queries = Regex
                .Matches(parts[3], "[\\?\\&](.+?)=([^\\&]+)")
                .Cast<Match>()
                .ToDictionary(s => s.Groups[1].Value, s => s.Groups[2].Value);
            return new LogEntry
            {
                Ip = parts[0],
                Date = DateTime.ParseExact(parts[1], "dd/MMM/yyyy:HH:mm:ss zzzz", CultureInfo.InvariantCulture),
                Method = parts[2],
                Url = parts[3],
                HttpVersion = parts[4],
                HttpStatus = int.Parse(parts[5]),
                Length = long.Parse(parts[6]),
                Referer = parts[7] == "-" ? "" : parts[7],
                UserAgent = parts[8],

                Width = queries.Where(s => s.Key == "width").Select(s => int.Parse(s.Value)).FirstOrDefault(),
                Height = queries.Where(s => s.Key == "height").Select(s => int.Parse(s.Value)).FirstOrDefault(),
                Image = queries.Where(s => s.Key == "url").Select(s => Uri.UnescapeDataString(s.Value)).FirstOrDefault(),
            };
        }
    }

    [Table("stats")]
    public class LogEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Method { get; set; }
        public string HttpVersion { get; set; }
        public int HttpStatus { get; set; }
        public long Length { get; set; }
        public string Ip { get; set; }
        public string UserAgent { get; set; }
        public string Url { get; set; }
        public string Referer { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public string Image { get; set; }
        public bool IsNorm { get; set; }

        public override string ToString()
        {
            return string.Format("{0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8}", Date, Method, HttpVersion, HttpStatus, Ip, UserAgent, Length, Url, Referer);
        }
    }
}