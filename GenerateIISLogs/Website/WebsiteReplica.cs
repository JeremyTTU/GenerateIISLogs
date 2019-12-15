using GenerateIISLogs.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GenerateIISLogs.Website
{
    public class WebsiteReplica
    {
        private Random _Random = new Random(RandomSeeds.GetSeed());
        private static ProgressComponent Progress = new ProgressComponent();
        public static List<WebsiteReplica> Pages = new List<WebsiteReplica>();
        public static List<TimedUri> Timing = new List<TimedUri>();
        public static ILookup<Uri, WebsiteReplica> PageLookupByPath = null;
        public static ILookup<string, WebsiteReplica> PageLookupByUrl = null;
        public static ILookup<Uri, TimedUri> TimingLookupByPath = null;

        public Uri Path { get; private set; }
        public string Url => Path.AbsoluteUri;
        public string Method { get; private set; } = "GET";
        public List<Uri> Css { get; private set; } = new List<Uri>();
        public List<Uri> Images { get; private set; } = new List<Uri>();
        public List<Uri> Javascript { get; private set; } = new List<Uri>();
        public List<Uri> Links { get; private set; } = new List<Uri>();
        public WebsiteReplica[] PageLinks => Links.Select(l => PageLookupByPath[l].FirstOrDefault()).Where(p => p != null).ToArray();
        public List<Uri> Forms { get; private set; } = new List<Uri>();
        public TimedUri Metric => WebsiteReplica.TimingLookupByPath[Path].FirstOrDefault();
        public Uri[] PageResources => AOE(Css).Concat(AOE(Images)).Concat(AOE(Javascript)).Where(l => l != null).Distinct().ToArray();
        public TimedUri[] PageMetrics => PageResources.Select(o => TimingLookupByPath[o].FirstOrDefault()).Where(t => t != null).ToArray();

        private IEnumerable<Uri> AOE(IEnumerable<Uri> o)
        {
            return o == null || o.Count() == 0 ? new List<Uri>(new Uri[] { null }) : o;
        }

        public WebsiteReplica Navigate()
        {
            WebsiteReplica websiteReplica;
            do
            {
                var link = Links[_Random.Next(Links.Count)];
                websiteReplica = PageLookupByPath[link].FirstOrDefault();
                if (websiteReplica == null)
                    Task.Delay(10).Wait();
            } while (websiteReplica == null);
            return websiteReplica;
        }

        public static WebsiteReplica Homepage()
        {
            return Pages.FirstOrDefault(o => o.Path.AbsolutePath == "/" || o.Path.AbsolutePath == "index.html");
        }

        public static void LoadUrls(string filename)
        {
            Progress.Setup(C.DataContext, "", C.Console);

            CreatePages(filename);
            LoadPages(filename);

            PageLookupByPath = Pages.ToLookup(o => o.Path);
            PageLookupByUrl = Pages.ToLookup(o => o.Url);

            GetStats();
        }

        public static void LoadMetrics(string filename)
        {
            var linesTotal = File.ReadLines(filename).Count();

            Progress.Progress = new ProgressControl("Load Metrics", linesTotal);
            var hashSet = new HashSet<TimedUri>();

            using (var file = File.OpenText(filename))
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine().Split('\t');

                    var tempUri = new Uri(line[0]);
                    var uri = new Uri($"{tempUri.Scheme}://{Pages.First().Path.Host}{tempUri.PathAndQuery}");
                    var timedUri = new TimedUri(uri) { HttpCode = int.Parse(line[1]), Bytes = int.Parse(line[2]), Elapsed = TimeSpan.Parse(line[3]) };
                    hashSet.Add(timedUri);

                    Progress.Progress.IncrementDone();
                }

            Timing = hashSet.ToList();
            TimingLookupByPath = Timing.ToLookup(o => o.Path);
        }

        private static void GetStats()
        {
            C.History($"Replica Pages Loaded: {Pages.Count}");
            C.History($"Replica Links Loaded: {Pages.Sum(o => o.Links.Count)}");
            C.History($"Unique Links Loaded: {Pages.SelectMany(o => o.Links).Distinct().Count()}");
            var linksWithPages = Pages.Sum(o => o.PageLinks.Count());
            C.History($"Links with Pages: {linksWithPages}");
        }

        private static void CreatePages(string filename)
        {
            var linesTotal = File.ReadLines(filename).Count();

            Progress.Progress = new ProgressControl("Create Pages", linesTotal);
            var hashSet = new HashSet<string>();

            using (var file = File.OpenText(filename))
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine().Split('\t');

                    hashSet.Add(line[0]);

                    Progress.Progress.IncrementDone();
                }

            foreach (var page in hashSet)
                Pages.Add(new WebsiteReplica { Path = new Uri(page) });

            Progress.Progress.Enabled = false;
        }

        private static void LoadPages(string filename)
        {
            var linesTotal = File.ReadLines(filename).Count();
            Progress.Progress = new ProgressControl("Load Pages", linesTotal);

            var lookup = Pages.ToLookup(o => o.Url);

            using (var file = File.OpenText(filename))
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine().Split('\t');
                    var page = lookup[line[0]].First();

                    var tempUri = new Uri(line[2]);
                    var uri = new Uri($"{tempUri.Scheme}://{page.Path.Host}{tempUri.PathAndQuery}");

                    switch (line[1])
                    {
                        case "CSS":
                            page.Css.Add(uri);
                            break;
                        case "IMAGE":
                            page.Images.Add(uri);
                            break;
                        case "JS":
                            page.Javascript.Add(uri);
                            break;
                        case "LINK":
                            page.Links.Add(uri);
                            break;
                        case "FORM":
                            page.Links.Add(uri);
                            break;
                    }

                    Progress.Progress.IncrementDone();
                }
            Progress.Progress.Enabled = false;
        }
    }
}
