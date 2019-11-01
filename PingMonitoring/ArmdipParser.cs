using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace PingMonitoring
{
    class ArmdipParser
    {

        private static Regex regex = new Regex(@"esr=(\d{2,6})");
        public static string Url = "http://127.0.0.1:10101/armdip.html";
        private static HtmlParser parser = new HtmlParser();
        private static readonly char[] chr = new char[] { '/', ' ' };
        public static TrainboardState[] Update()
        {
            TrainboardState ts;
            List<TrainboardState> tsl = new List<TrainboardState>(45);
            string page = GetPage();
            var document = parser.ParseDocument(page);
            foreach (IElement element in document.QuerySelectorAll(".menu_item"))
            {
                ts = new TrainboardState();

                string tmp = element.QuerySelector(".menu_item_a").GetAttribute("href");
                ts.Esr = Convert.ToInt32(regex.Match(tmp).Groups[1].Value);
                ts.Total = Convert.ToByte(element.QuerySelector(".menu_count_ok").TextContent);

                IElement dmg = element.QuerySelector(".menu_count_error");
                if (dmg != null)
                    ts.Damaged = Convert.ToByte(element.QuerySelector(".menu_count_error").TextContent.TrimStart(chr));
                else
                    ts.Damaged = 0;
                tsl.Add(ts);
                Console.WriteLine(ts.Esr + " " + ts.Damaged + "/" + ts.Total);
            }
            return tsl.ToArray();
        }

        private static string GetPage()
        {
            using (WebClient wc = new WebClient())
                return "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=UTF-8\"></head><body>" + wc.DownloadString(Url) + "</body></html>";


        }
    }
    public class TrainboardState
    {
        public int Esr;
        public byte Total, Damaged;
    }
}
