using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace bustest {
    internal static class Program {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        
        public static List<bus> busdata = new List<bus>();
        public static string url = "https://tcgbusfs.blob.core.windows.net/blobbus";
        public static List<route> routes = new List<route>();
        public static List<Stop> stops = new List<Stop>();

        public class route {
            public int Id { get; set; }
            public string nameZh { get; set; }
            public string departureZh { get; set; }
            public string destinationZh { get; set; }
        }

        public class Stop {
            public int Id { get; set; }
            public string nameZh { get; set; }
        }

        public class bus {
            public int RouteId { get; set; }
            public int StopId { get; set; }
            public string EstimateTime { get; set; }

            public string GoBack {  get; set; }
        }
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
