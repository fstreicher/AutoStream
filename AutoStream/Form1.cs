using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Xml.XPath;
using HtmlAgilityPack;
using System.Threading;
using Microsoft.Win32;


namespace AutoStream
{
    public partial class Form1 : Form
    {
        #region declarations
        private const int PIC_WIDTH = 200;
        private const int PIC_HEIGHT = 60;
        string path;
        //PerformanceCounter nw_perfCounter;
        //int[] network = new int[20];
        //int nw_counter = 0;
        //int nw_avg;
        int anz = 0;
        int prev = 0;
        bool newentry;
        int PID = 0;
        bool auto = false;
        string[] lines;
        List<Match> matchlist = new List<Match>();
        List<Match> order = new List<Match>();
        string[] dict = new string[] { "teams.csv", "queue.csv", "champions.csv", "summoner.csv", "map.csv", "todo.csv" };
        string[] d_teams;
        string[] d_summoner;
        string[] d_map;
        string[] d_queue;
        string[] d_champion;
        List<string> d_todo = new List<string>();
        Color[] paint = { Color.White, Color.PaleGreen, Color.LightSkyBlue, Color.Coral, Color.PaleGoldenrod, Color.DarkKhaki, Color.IndianRed, Color.NavajoWhite, Color.Navy };
        Bitmap pic_ref;
        Bitmap bitmap = new Bitmap(PIC_WIDTH, PIC_HEIGHT);
        #endregion


        #region methods
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bw2.RunWorkerAsync();
            //nw_perfCounter = new PerformanceCounter("Process", "IO Data Bytes/sec", "League of Legends");
            toolStripStatusLabel1.Text = "loading";
        }


        private void button1_Click(object sender, EventArgs e) ///get data to fill table
        {
            newentry = false;
            System.IO.File.WriteAllText(@path + "\\input.html", dload_source(), Encoding.UTF8); ///saves the source code to a file
            truncate(path);
            crawl();
            if (lines == null)
            {
                MessageBox.Show("no games available right now");
            }
            else
            {
                if (newentry)
                    return_data();
                draw_table();
                resize();
                button2.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e) ///spectate game
        {
            Point cell = dataGridView1.CurrentCellAddress;
            int irow = cell.Y;
            order[irow].load_bat(path); ///downloads the *.bat
            //   fixbat();
            order[irow].call_bat(path); ///runs the *.bat
        }

        private void button3_Click(object sender, EventArgs e) ///start automatic mode
        {
            if (auto)
            {
                /// LoL running? terminate
                bw1.CancelAsync();
                timer1.Stop();
                button3.Text = "start autorun";
                auto = false;
            }
            else
            {
                bw1.RunWorkerAsync();
                timer1.Start();
                button3.Text = "stop autorun";
                auto = true;
            }
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void infoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 box = new AboutBox1();
            box.ShowDialog();
        }

        private void startup() ///things to do upon application launch
        {
            path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "\\AutoStream";
            string temp;

            if (!Directory.Exists(@path))     ///sets the path to put everything
            {
                Directory.CreateDirectory(@path);
            }

            if (!Directory.Exists(@path + "\\dict"))
            {
                Directory.CreateDirectory(@path + "\\dict");
            }
            foreach (string filename in dict)
            {
                temp = "http://url/AutoStream/dict/" + filename;
                Uri url = new Uri(temp);
                try
                {
                    WebClient Client = new WebClient();
                    Client.DownloadFile(url, path + "\\dict\\" + filename);
                }
                catch //(Exception ex)
                {
                    // Console.WriteLine(ex.StackTrace);
                }
            }
            temp = "http://url/AutoStream/resources/reference.bmp";
            Uri url2 = new Uri(temp);
            try
            {
                WebClient Client = new WebClient();
                Client.DownloadFile(url2, path + "\\reference.bmp");
            }
            catch //(Exception ex)
            {
                // Console.WriteLine(ex.StackTrace);
            }

        }

        private void load_dict() ///loads csv into arrays -> TODO: lists
        {
            string d_path = path + "\\dict\\";
            if (!Directory.Exists(d_path))
            {
                Directory.CreateDirectory(d_path);
            }
            d_champion = new string[File.ReadLines(@d_path + "champions.csv").Count()];
            d_champion = File.ReadAllLines(@d_path + "champions.csv");
            d_map = new string[File.ReadLines(@d_path + "map.csv").Count()];
            d_map = File.ReadAllLines(@d_path + "map.csv");
            d_queue = new string[File.ReadLines(@d_path + "queue.csv").Count()];
            d_queue = File.ReadAllLines(@d_path + "queue.csv");
            d_summoner = new string[File.ReadLines(@d_path + "summoner.csv").Count()];
            d_summoner = File.ReadAllLines(@d_path + "summoner.csv");
            d_teams = new string[File.ReadLines(@d_path + "teams.csv").Count()];
            d_teams = File.ReadAllLines(@d_path + "teams.csv");
            //d_todo = new string[File.ReadLines(@d_path + "todo.csv").Count()];
            //d_todo = File.ReadAllLines(@d_path + "todo.csv");
            d_todo.AddRange(File.ReadAllLines(@d_path + "todo.csv"));
        }

        private string dload_source() ///Downloads the source code of a given webpage
        {
            String source;
            WebClient Client = new WebClient();
            Client.Encoding = Encoding.UTF8;        ///sets proper encoding for korean characters

            source = Client.DownloadString("http://op.gg/spectate/pro/");
            return source;
        }

        private void truncate(string path) ///truncates the source file so only the part needed remains
        {
            var lines = File.ReadLines(@path + "\\input.html")
                .SkipWhile(line => !line.Contains("<div class=\"nBox\">")) //keeps content from where table starts
                .TakeWhile(line => !line.Contains("아마추어")); //leaves out amateurs
            File.WriteAllLines(@path + "\\output.html", lines);

            var lines2 = File.ReadAllLines(@path + "\\output.html");
            File.WriteAllLines(@path + "\\output.html", lines2.Take((lines2.Length) - 4), Encoding.UTF8);


        }

        private void crawl() ///checks source code for relevant strings
        {
            String newpath = path + "\\output.html";
            anz = 0;
            lines = File.ReadAllLines(@newpath);

            if (lines.Contains("현재 게임중인 사람이 없습니다"))
            {
                lines = null;
            }
            else
            {
                matchlist.Clear();

                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(@newpath);
                HtmlNodeCollection tr = doc.DocumentNode.SelectNodes("//div[@class='SpectatorSummoner']");

                foreach (HtmlNode row in tr)
                {
                    HtmlNode summoner = row.SelectSingleNode(".//div[@class='summonerName']");
                    string Vsummoner = summoner.InnerText.Trim();
                    HtmlNode team = row.SelectSingleNode(".//div[@class='summonerTeam']");
                    string Vteam = team.InnerText.Trim();
                    ;
                    HtmlNode tier = row.SelectSingleNode(".//span[@class='tierRank tip']");
                    if (tier == null)
                    {
                        tier = row.SelectSingleNode(".//span[@class='tierRank']");
                        if (tier == null)
                        {
                            tier = row.SelectSingleNode(".//div[@class='summonerLevel']");
                        }
                    }

                    string Vtier = tier.InnerText;
                    HtmlNode queue = row.SelectSingleNode(".//div[@class='QueueType']");
                    string Vqueue = queue.InnerText.TrimStart();
                    HtmlNode map = row.SelectSingleNode(".//div[@class='MapName']");
                    string Vmap = map.InnerText.TrimStart();
                    HtmlNode champ = row.SelectSingleNode(".//div[@class='championName']");
                    string Vchamp = champ.InnerText;

                    int Vcntdn = 0;
                    HtmlNode cntdn = row.SelectSingleNode(".//span[@class='_countdown']");
                    if (cntdn == null)
                    {
                        int timestamp_now = (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                        Vcntdn = timestamp_now;// + 60;
                    }
                    else
                    {
                        Vcntdn = Convert.ToInt32(cntdn.Attributes["data-timestamp"].Value);
                    }

                    HtmlNode id = row.SelectSingleNode(".//a[@class='opButton green small SpectateButton']");
                    string Vid = id.Attributes["href"].Value;
                    Vid = Vid.Substring(19, Vid.Length - 19);
                    Int32 Iid = Convert.ToInt32(Vid);

                    matchlist.Add(new Match(Iid,
                                            Vcntdn,
                                            translate(Vmap, 'm'),
                                            translate(Vqueue, 'q'),
                                            translate(Vchamp, 'c'),
                                            translate(Vsummoner, 's'),
                                            translate(Vteam, 't'),
                                            Vtier));
                    anz++;
                }

                //order = matchlist.OrderBy(o => o.Timestamp).ToList();
                order = matchlist.OrderByDescending(o => o.Timestamp).ToList();
            }
        }

        private string translate(string input, char file) ///translates korean strings w/ the help of csv-dictionaries
        {
            int i = 0;
            switch (file)
            {
                case 'c':
                    while (!d_champion[i].Contains(input))
                    {
                        i++;
                        if (i == d_champion.Length)
                        {
                            if (!d_todo.Contains(input))
                            {
                                d_todo.Add(input);
                                File.AppendAllText(@path + @"\dict\todo.csv", "C: " + input + Environment.NewLine, Encoding.UTF8);
                                newentry = true;
                                return input;
                            }
                            else
                            {
                                return input;
                            }
                        }
                    }

                    return d_champion[i].Split(',')[1];

                case 'm':
                    while (!d_map[i].Contains(input))
                    {
                        i++;
                        if (i == d_map.Length)
                        {
                            if (!d_todo.Contains(input))
                            {
                                d_todo.Add(input);
                                File.AppendAllText(@path + @"\dict\todo.csv", "M: " + input + Environment.NewLine, Encoding.UTF8);
                                newentry = true;
                                return input;
                            }
                            else
                            {
                                return input;
                            }
                        }
                    }

                    return d_map[i].Split(',')[1];

                case 'q':
                    while (!d_queue[i].Contains(input))
                    {
                        i++;
                        if (i == d_queue.Length)
                        {
                            if (!d_todo.Contains(input))
                            {
                                d_todo.Add(input);
                                File.AppendAllText(@path + @"\dict\todo.csv", "Q: " + input + Environment.NewLine, Encoding.UTF8);
                                newentry = true;
                                return input;
                            }
                            else
                            {
                                return input;
                            }
                        }
                    }

                    return d_queue[i].Split(',')[1];

                case 's':
                    while (!d_summoner[i].Contains(input))
                    {
                        i++;
                        if (i == d_summoner.Length)
                        {
                            if (!d_todo.Contains(input))
                            {
                                d_todo.Add(input);
                                File.AppendAllText(@path + @"\dict\todo.csv", "S: " + input + Environment.NewLine, Encoding.UTF8);
                                newentry = true;
                                return input;
                            }
                            else
                            {
                                return input;
                            }
                        }
                    }
                    try
                    {
                        return d_summoner[i].Split(',')[1];
                    }
                    catch (Exception)
                    {
                        return input;
                        //throw;
                    }


                case 't':
                    while (!d_teams[i].Contains(input))
                    {
                        i++;
                        if (i == d_teams.Length)
                        {
                            if (!d_todo.Contains(input))
                            {
                                d_todo.Add(input);
                                File.AppendAllText(@path + @"\dict\todo.csv", "T: " + input + Environment.NewLine, Encoding.UTF8);
                                newentry = true;
                                return input;
                            }
                            else
                            {
                                return input;
                            }
                        }
                    }

                    return d_teams[i].Split(',')[1];

                default:
                    return "Error 404";
            }
        }

        private void return_data() ///uploads info via gtp
        {
            try
            {
                FtpWebRequest ftpClient = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://url/web/AutoStream/dict/todo.csv"));
                ftpClient.Credentials = new System.Net.NetworkCredential("user", "password");
                ftpClient.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
                ftpClient.UseBinary = true;
                ftpClient.KeepAlive = true;
                System.IO.FileInfo fi = new System.IO.FileInfo(@path + @"\dict\todo.csv");
                ftpClient.ContentLength = fi.Length;
                byte[] buffer = new byte[4097];
                int bytes = 0;
                int total_bytes = (int)fi.Length;
                System.IO.FileStream fs = fi.OpenRead();
                System.IO.Stream rs = ftpClient.GetRequestStream();
                while (total_bytes > 0)
                {
                    bytes = fs.Read(buffer, 0, buffer.Length);
                    rs.Write(buffer, 0, bytes);
                    total_bytes = total_bytes - bytes;
                }
                //fs.Flush();
                fs.Close();
                rs.Close();
                //FtpWebResponse uploadResponse = (FtpWebResponse)ftpClient.GetResponse();
                //var value = uploadResponse.StatusDescription;
                //uploadResponse.Close();
                //MessageBox.Show(Convert.ToString(value));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                //throw;
            }
        }

        private void resize() ///resizes the main window
        {
            if (anz > 13)
            {
                int temp = anz - 13;
                temp *= 22;
                int h = this.MinimumSize.Height;
                int newheight = h + temp - 6;
                if (newheight > this.MaximumSize.Height)
                    this.Height = this.MaximumSize.Height;
                else
                    this.Height = newheight;
            }
        }

        private void fixbat() ///fixes bat file, not necessary anymore
        {
            var lines2 = File.ReadAllLines(@path + @"\game.bat");
            if (lines2[76].Contains("%RADS_PATH%\\solutions\\lol_game_client_sln\\releases"))
            {
                lines2[76] = "@cd /d \"%RADS_PATH%\\solutions\\lol_game_client_sln\\releases\\0.0.*\\deploy\"";
            }
            else
            {
                MessageBox.Show("error treating batch file, contact your admin ;)");
            }
            File.WriteAllLines(@path + @"\game.bat", lines2);
        }

        private void colorize() ///assigns colors to matches
        {
            List<int> duplicates = new List<int>();

            duplicates = order
                .GroupBy(x => x.ID)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (Match entry in order)
            {
                if (duplicates.Contains(entry.ID))
                {
                    for (int i = 0; i < duplicates.Count; i++)
                    {
                        if (duplicates[i] == entry.ID)
                            if (i + 1 <= paint.Length)
                            {
                                entry.color = i + 1;
                                break;
                            }
                            else
                            {
                                entry.color = 0;
                                break;
                            }
                    }
                }
            }
        }

        private void format_grid(object sender, EventArgs e) ///formats selected row
        {
            try
            {
                dataGridView1.Rows[prev].DefaultCellStyle.Font = new Font(DefaultFont, FontStyle.Regular);
                prev = dataGridView1.CurrentCellAddress.Y;
                dataGridView1.Rows[prev].DefaultCellStyle.Font = new Font(DefaultFont, FontStyle.Underline);
                dataGridView1.ClearSelection();
            }
            catch (Exception)
            {
                //throw;
            }
        }

        private int select_game() ///select game to watch through criteria
        {
            ///TODO:
            /// intelligent criteria
            int i = 0;
            while (!((order[i].Queue == "solo ranked") || (order[i].Queue == "ranked 5")))
            {
                i++;
                if (i > order.Count - 1)
                {
                    ///TODO: was tun wenn keine rankeds????
                    if (order[0].calc_time().Contains("#"))
                        bw1.ReportProgress(70);
                    else
                        bw1.ReportProgress(80);
                    return 0;
                }
            }
            if (order[i].calc_time().Contains("#"))
                bw1.ReportProgress(70);
            else
                bw1.ReportProgress(80);
            //while (order[i].calc_time().Contains("#"))
            //{
            //    bw1.ReportProgress(70);
            //    Thread.Sleep(5000);
            //}
            return i;
        }

        private void timer1_Tick(object sender, EventArgs e) ///debug only
        {
            //listBox1.Items.Clear();
            //int i = 0;
            //listBox1.Items.Add("network stats");
            //foreach (int value in network)
            //{
            //    listBox1.Items.Add(network[i]);
            //    i++;
            //}
            //listBox1.Items.Add("-----------------");
            //listBox1.Items.Add(nw_avg.ToString());
        }

        private void mode_selection(object sender, EventArgs e) ///auto/manual
        {
            if (radioButton1.Checked)
            {
                button1.Enabled = true;
                button3.Enabled = false;
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                button3.Enabled = true;
            }
        }

        private void bw1_DoWork(object sender, DoWorkEventArgs e) ///autorun main routine
        {
            /// load data
            // fill_nw();
            newentry = false;
            bw1.ReportProgress(10); // prepare progress bar, label
            System.IO.File.WriteAllText(@path + "\\input.html", dload_source(), Encoding.UTF8);
            truncate(path);
            crawl();
            bw1.ReportProgress(20); // update progress bar
            if (newentry)
                return_data();

            /// select game
            int game = select_game();

            /// spectate
            bw1.ReportProgress(30); // update label
            export_info(game);
            order[game].load_bat(path); ///downloads the *.bat
            //fixbat();
            order[game].call_bat(path); ///runs the *.bat
            bw1.ReportProgress(40); // last progress bar update

            Process[] p = Process.GetProcessesByName("League of Legends");
            Process proc = p[0];
            PID = proc.Id;
            pic_ref = new Bitmap(@path + "\\reference.bmp");

            Thread.Sleep(10000);

            /// kill process once game is over
            while (!compare_img())// (nw_avg > 18 && !proc.HasExited)
            {
                #region nw_
                //try
                //{
                //    int traffic = (int)Math.Round(nw_perfCounter.NextValue());
                //    network[nw_counter] = traffic;

                //    if (nw_counter < network.Length - 1)
                //        nw_counter++;
                //    else
                //        nw_counter = 0;
                //    nw_avg = (int)Math.Round(network.Average());
                //}
                #endregion
                try
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(new Point(850, 700), Point.Empty, bitmap.Size);
                    }
                    //bitmap.Save(@path + "\\screencap.bmp", ImageFormat.Bmp);

                }
                catch (InvalidOperationException)
                {
                    auto = false;
                }
                bw1.ReportProgress(50);
                Thread.Sleep(2500);
            }

            try
            {
                proc.Kill();
            }
            catch (InvalidOperationException)
            {
                auto = false;
                //button text ändern nicht nötig -> RunWorkerCompleted
                bw1.ReportProgress(60);
                MessageBox.Show("process already terminated");
            }
        }

        private void bw1_ProgressChanged(object sender, ProgressChangedEventArgs e) ///autorun progess reporter
        {
            switch (e.ProgressPercentage)
            {
                case 10:
                    toolStripProgressBar1.Value = 60;
                    toolStripStatusLabel1.Text = "gathering data";
                    break;
                case 20:
                    toolStripProgressBar1.Value = 80;
                    toolStripStatusLabel1.Text = "processing data";
                    draw_table();
                    break;
                case 30:
                    toolStripProgressBar1.Value = 90;
                    toolStripStatusLabel1.Text = "getting gamefiles";
                    //draw_table();
                    break;
                case 40:
                    toolStripProgressBar1.Value = 100;
                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    toolStripStatusLabel1.Text = "game launching";
                    Thread.Sleep(1500);
                    break;
                case 50:
                    toolStripStatusLabel1.Text = "game is running";
                    break;
                case 60:
                    button3.Text = "start autorun";
                    toolStripStatusLabel2.Text = "game closed";
                    break;
                case 70:
                    toolStripStatusLabel2.Text = "waiting for game to start";
                    break;
                case 80:
                    toolStripStatusLabel2.Text = "";
                    break;
                default:
                    break;
            }
        }

        private void bw1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) ///wrap up, restart
        {
            toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            toolStripProgressBar1.Value = 0;
            if ((e.Cancelled == true)) //cancelled
            {
                toolStripStatusLabel1.Text = "program interrupted";
            }

            else if (!(e.Error == null))
            {
                MessageBox.Show("Error: " + e.Error.Message);
                if (auto)
                {
                    toolStripStatusLabel2.Text = "game over... preparing next game";
                    bw1.RunWorkerAsync();
                }
                else
                {
                    toolStripStatusLabel2.Text = "Error: " + e.Error.Message;
                    toolStripStatusLabel1.Text = "idle";
                }
            }

            else //finished as planned
            {
                if (auto)
                {
                    toolStripStatusLabel2.Text = "game over... preparing next game";
                    bw1.RunWorkerAsync();
                }
                else
                {
                    toolStripStatusLabel2.Text = "game closed or game over...";
                    toolStripStatusLabel1.Text = "idle";
                }

            }
            //timer1.Stop();
        }

        private void bw2_DoWork(object sender, DoWorkEventArgs e) ///startup routine
        {
            startup();
            load_dict();
            fill_nw();
        }

        private void bw2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) ///wrap up
        {
            toolStripStatusLabel1.Text = "idle";
            toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            radioButton1.Enabled = true;
            radioButton2.Enabled = true;
        }

        private void fill_nw() ///put dummy data into nw array
        {
            //for (int i = 0; i < network.Length; i++)
            //    network[i] = 100000;
            //nw_avg = (int)Math.Round(network.Average());
        }

        private void export_info(int number) ///puts playerdata into txt
        {
            ///TODO
            /// get all data from all players available in that match
            string player = translate(order[number].Summoner, 's');
            string champ = translate(order[number].Champion, 'c');
            File.WriteAllText(@path + "\\info.txt", player + " on" + Environment.NewLine + champ, Encoding.UTF8);
        }

        private void draw_table() ///draws table
        {
            int i = 0;

            dataGridView1.Rows.Clear();
            colorize();
            foreach (Match derp in order)
            {
                dataGridView1.Rows.Add(derp.Team, derp.Summoner, derp.Tier, derp.Champion, derp.Queue, derp.Map, derp.calc_time(), derp.ID);
                try
                {
                    dataGridView1.Rows[i].DefaultCellStyle.BackColor = paint[derp.color];
                    i++;
                }
                catch (IndexOutOfRangeException)
                {
                    dataGridView1.Rows[i].DefaultCellStyle.BackColor = paint[0];
                    i++;
                }
            }
            dataGridView1.ClearSelection();
            ///TODO: paint selected game, all instances
        }

        private bool compare_img()
        {
            //  Bitmap pic_cap = new Bitmap(@path + "\\screencap.bmp");
            for (int i = 0; i < PIC_WIDTH; i++)
            {
                for (int j = 0; j < PIC_HEIGHT; j++)
                {
                    if (pic_ref.GetPixel(i, j) != bitmap.GetPixel(i, j))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        
        private void suchenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://url/AutoStream/documentation.html");
        }
        
        #endregion
    }
}