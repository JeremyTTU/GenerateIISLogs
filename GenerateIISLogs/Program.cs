using GenerateIISLogs.Data;
using GenerateIISLogs.Spider;
using GenerateIISLogs.Utility;
using GenerateIISLogs.Website;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace GenerateIISLogs
{
    class Program
    {
        static void Main(string[] args)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;

            RandomSeeds.Init();

            C.Init();

            ResourceData.Init();

            //WebsiteSpider spider = null;

            //switch (1)
            //{
            //    case 0:
            //        spider = new WebsiteSpider("jrcigars.com", "https://www.jrcigars.com/");
            //        spider.AddExclude("blog");
            //        break;
            //    case 1:
            //        spider = new WebsiteSpider("pillows.com", "https://www.pillows.com/");
            //        spider.AddExclude("blog");
            //        break;
            //    case 2:
            //        spider = new WebsiteSpider("guns.com", "https://www.guns.com/");
            //        spider.AddExclude("register");
            //        spider.AddExclude("news");
            //        break;
            //}

            //spider.LoadDetails();
            //spider.LoadTimings();
            //spider.Spider();


            WebsiteReplica.LoadUrls("www.pillows.com.urls");
            WebsiteReplica.LoadMetrics("www.pillows.com.metrics");

            var siteuser = new SiteUser();
            Console.WriteLine($"IP Address: {siteuser.IpAddress}");
            Console.WriteLine($"User Agent: {siteuser.UserAgent}");

            IISLog.Start();

            var surfers = new List<Task>();

            for (var x = 0; x < 10; x++)
            {
                siteuser.SurfTheInternet();
                surfers.Add(Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith((o) => siteuser.CancelSurfing()));
            }

            Task.WaitAll(surfers.ToArray());

            Console.WriteLine("CODE COMPLETED");
            Console.ReadLine();
        }
    }
}
