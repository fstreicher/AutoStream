using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace AutoStream
{
    public class Match
    {
        #region get/set
        public Match(int ID, int timestamp, string map, string queue, string champ, string summoner, string team, string tier)
        {
            this.ID = ID;
            this.Timestamp = timestamp;
            this.Map = map;
            this.Queue = queue;
            this.Champion = champ;
            this.Summoner = summoner;
            this.Team = team;
            this.Tier = tier;
        }

        public string Summoner { get; set; }

        public string Queue { get; set; }

        public int Timestamp { get; set; }

        public string Champion { get; set; }

        public string Map { get; set; }

        public string Team { get; set; }

        public int ID { get; set; }

        public string Tier { get; set; }

        public int color { get; set; }
        #endregion

        public string build_link()
        {
            return "http://op.gg/match/observer/id=" + Convert.ToString(this.ID);
        }
        
        public void load_bat(string path)
        {
            Uri Test = new Uri(build_link());
            try
            {
                path += "\\game.bat";
                WebClient Client = new WebClient();
                Client.DownloadFile(Test, path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void call_bat(string path)
        {
            System.Diagnostics.Process proc = new System.Diagnostics.Process(); // Declare New Process
            proc.StartInfo.FileName = path + "\\game.bat"; ;
            proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
        }

        public string calc_time()
        {
            string time;
            int timestamp_now = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            int diff = timestamp_now - Timestamp - 3600;
            double tmp = diff / 60;
            int minutes = (int)Math.Truncate(tmp);
            //minutes -= 60;      ///WHY IS THAT SO???
            int seconds = Math.Abs(diff % 60);
            time = "{0:D2}:{1:D2}";
            string time_out = string.Format(time, minutes, seconds);
            if (tmp < 0)
                return "#" + Convert.ToString(diff); //"#"+time_out;
            //return "00:00 - loading";
            else
                return time_out;
        }
    }
}
