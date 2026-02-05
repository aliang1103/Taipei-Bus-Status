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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace bustest {
    public partial class Form1 : Form {
        private readonly HttpClient client = new HttpClient();
        private bool isload = false;
        public Form1() {
            InitializeComponent();
            getdata();
        }

        private Dictionary<int, string> goback = new Dictionary<int, string> {
            {0,"去程" },
            {1,"返程" },
            {2,"尚未發車" },
            {3,"末班已駛離" },
        };

        private void load() {
            if (!isload) {
                MessageBox.Show("資料未載入完成，請稍後再試");
                return;
            }
            var route = Program.routes.GroupBy(x=>x.Id).ToDictionary(x => x.Key, x => x.First().nameZh);
            var stop = Program.stops.GroupBy(x=>x.Id).ToDictionary(x => x.Key, x => x.First().nameZh);
            DataTable dt = new DataTable();
            dt.Columns.Add("RouteNumber");
            dt.Columns.Add("StopId");
            dt.Columns.Add("EstimateTime");
            dt.Columns.Add("GoBack");
            dt.BeginLoadData();
            foreach (var o in Program.busdata) {
                string time;
                int times = int.Parse(o.EstimateTime);
                switch (times) {
                    case -1:
                        time = "尚未發車";
                        break;
                    case -2:
                        time = "交管不停靠";
                        break;
                    case -3:
                        time = "末班車已過";
                        break;
                    case -4:
                        time = "今日未營運";
                        break;
                    default:
                        if (times < 60) {
                            time = "即將抵達";
                        }
                        else time = $"{times / 60}分";
                        break;
                }
                route.TryGetValue(o.RouteId, out string routes);
                stop.TryGetValue(o.StopId, out string stops);
                goback.TryGetValue(int.Parse(o.GoBack), out string gobacks);
                dt.Rows.Add(routes ?? "無資料", stops ?? "無資料", time, gobacks ?? "未知");
            }
            dt.EndLoadData();
            dataGridView1.DataSource = dt;
            var dv = (DataTable)dataGridView1.DataSource;
            dv.DefaultView.RowFilter = $"RouteNumber LIKE '%{textBox1.Text}%'";
        }

        private void sreach() {
            var dv = (DataTable)dataGridView1.DataSource;
            dv.DefaultView.RowFilter = $"RouteNumber LIKE '%{textBox1.Text}%'";
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
                        isload = true;
                        load();
                    }
                    else {
                        MessageBox.Show($"Error Code: {data.StatusCode} {route.StatusCode}");
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show("Error Reason : " + ex.Message);
                }
                await Task.Delay(10000);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
            sreach();
        }
    }
}
