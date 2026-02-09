using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static bustest.Program;

namespace bustest {
    public partial class Form1 : Form {
        private readonly HttpClient client = new HttpClient();
        private bool hasload = false;
        private bool issel = false;
        private int time=0;
        public Form1() {
            InitializeComponent();
            panel1.Hide();
            getdata();
            timevalue();
        }

        private Dictionary<int, string> goback = new Dictionary<int, string> {
            {0,"去程" },
            {1,"返程" },
            {2,"尚未發車" },
            {3,"末班已駛離" },
        };
        private Dictionary<int, string> times = new Dictionary<int, string>
        {
            {-1,"尚未發車" },
            {-2,"交管不停靠" },
            {-3,"末班車已過" },
            {-4,"今日未營運" }
        };
        private Dictionary<int,string> route = new Dictionary<int, string>();
        private Dictionary<int, string> stop = new Dictionary<int, string>();

        private void load()
        {
            if (!hasload)
            {
                var data = Program.routes.GroupBy(x => x.Id).Select(x => new
                {
                    id = x.Key,
                    name = x.First().nameZh
                }).OrderBy(x=>x.name).ToList();
                data.Insert(0, new { id = 0, name = "全部" });
                comboBox2.DataSource = data;
                comboBox2.DisplayMember = "name";
                comboBox2.ValueMember = "id";
                comboBox2.SelectedIndex = 0;
                hasload = true;
            }
            todatagridview();
            panel1.Show();
            time = 0;
        }

        private int first = 0;

        private void todatagridview()
        {
            if (dataGridView1.FirstDisplayedScrollingRowIndex >= 0)
            {
                first = dataGridView1.FirstDisplayedScrollingRowIndex;
            }
            DataTable dt = new DataTable();
            dt.Columns.Add("路線");
            dt.Columns.Add("站牌");
            dt.Columns.Add("預估時間");
            dt.Columns.Add("往返");
            dt.BeginLoadData();
            var data = Program.busdata.Where(x => (comboBox2.SelectedIndex == 0 || x.RouteId == (int)comboBox2.SelectedValue)).OrderBy(x=>x.GoBack).ToList();
            Console.WriteLine(data.First().GoBack);
            foreach (var o in data)
            {
                string time="";
                int timedata = int.Parse(o.EstimateTime);
                if (timedata >= 0)
                {
                    if (timedata == 0)
                    {
                        time = "抵達";
                    }
                    else
                    {
                        var mm = (timedata / 60).ToString("D2");
                        var ss = (timedata % 60).ToString("D2");
                        time = $"{mm}:{ss}";
                    }
                }
                var x = int.Parse(o.GoBack);
                times.TryGetValue(timedata, out string timevalue);
                route.TryGetValue(o.RouteId, out string routes);
                stop.TryGetValue(o.StopId, out string stops);
                goback.TryGetValue(x, out string gobacks);
                if (x == 0) gobacks = $"往{Program.routes.Where(y => y.Id == o.RouteId).First().destinationZh}";
                else if (x == 1) gobacks = $"往{Program.routes.Where(y => y.Id == o.RouteId).First().departureZh}"
                ;
                dt.Rows.Add(routes ?? "N/A", stops ?? "N/A", timevalue ?? time, gobacks ?? "N/A");
            }
            dt.EndLoadData();
            dataGridView1.DataSource = dt;
            if (first < dt.Rows.Count)
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = first;
            }
            else
            {
                dataGridView1.FirstDisplayedScrollingRowIndex = 0;
            }
            dataGridView1.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.Show();
        }

        private async void timevalue()
        {
            while (true)
            {
                time++;
                label2.Text = $"{time}秒前更新";
                await Task.Delay(1000);
            }
        }

        private async void getdata() {
            while (true) {
                try {
                    var data = await client.GetAsync($"{Program.url}/GetEstimateTime.gz");
                    var route = await client.GetAsync($"{Program.url}/GetRoute.gz");
                    var stop = await client.GetAsync($"{Program.url}/GetStop.gz");
                    if (data.IsSuccessStatusCode && route.IsSuccessStatusCode) {
                        using (var stream = await data.Content.ReadAsStreamAsync())
                        using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                        using (var reader = new System.IO.StreamReader(gzip)) {
                            var json = await reader.ReadToEndAsync();
                            var jo = JObject.Parse(json);
                            Program.busdata = jo["BusInfo"].ToObject<List<Program.bus>>();
                        }
                        using (var stream = await route.Content.ReadAsStreamAsync())
                        using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                        using (var reader = new System.IO.StreamReader(gzip)) {
                            var json = await reader.ReadToEndAsync();
                            var jo = JObject.Parse(json);
                            Program.routes = jo["BusInfo"].ToObject<List<Program.route>>();
                        }
                        using (var stream = await stop.Content.ReadAsStreamAsync())
                        using (var gzip = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress))
                        using (var reader = new System.IO.StreamReader(gzip)) {
                            var json = await reader.ReadToEndAsync();
                            var jo = JObject.Parse(json);
                            Program.stops = jo["BusInfo"].ToObject<List<Program.Stop>>();
                        }
                    }
                    else {
                        MessageBox.Show($"Error Code: {data.StatusCode} {route.StatusCode}");
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show("Error Reason : " + ex.Message);
                }
                route = Program.routes.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First().nameZh);
                stop = Program.stops.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First().nameZh);
                load();
                await Task.Delay(10000);
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            todatagridview();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            todatagridview();
        }
    }
}
