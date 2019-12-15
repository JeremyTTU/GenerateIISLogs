using GenerateIISLogs.Utility;
using GenerateIISLogs.Website;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateIISLogs.Spider
{
    [Serializable]
    public class WebsiteSpider
    {
        private static object SyncLock = new object();
        private ProgressComponent Progress = new ProgressComponent();
        private HashSet<PageDetails> _Details = new HashSet<PageDetails>();
        private HashSet<TimedUri> _Timings = new HashSet<TimedUri>();

        public string Hostname { get; private set; }

        public Uri BasePath { get; private set; }

        public bool OnlyBase { get; set; }

        public int MaxThreads { get; set; } = 8;

        public HashSet<string> UriExcludes = new HashSet<string>();

        public HashSet<string> UriStartsWithExcludes = new HashSet<string>() { "mailto", "javascript", "tel", "#", "?" };

        public WebsiteSpider(string hostname, string baseUrl)
        {
            Progress.Setup(C.DataContext, "Spider", C.Console);

            Hostname = hostname.ToLower();
            BasePath = new Uri(baseUrl);
        }

        public void AddExclude(string exclude)
        {
            UriExcludes.Add(exclude);
        }

        public void SaveDetails()
        {
            using (var file = File.Create($"{BasePath.Host}.detail"))
                new BinaryFormatter().Serialize(file, _Details);
        }

        public void LoadDetails()
        {
            C.WriteBottomLeft("Loading Details...");
            if (File.Exists($"{BasePath.Host}.detail"))
                using (var file = File.Open($"{BasePath.Host}.detail", FileMode.Open))
                    _Details = (HashSet<PageDetails>)new BinaryFormatter().Deserialize(file);

        }

        public void SaveTimings()
        {
            using (var file = File.Create($"{BasePath.Host}.timing"))
                new BinaryFormatter().Serialize(file, _Timings);
        }

        public void LoadTimings()
        {
            C.WriteBottomLeft("Loading Timings...");
            if (File.Exists($"{BasePath.Host}.timing"))
                using (var file = File.Open($"{BasePath.Host}.timing", FileMode.Open))
                    _Timings = (HashSet<TimedUri>)new BinaryFormatter().Deserialize(file);
        }

        public IEnumerable<PageDetails> Details()
        {
            return _Details;
        }

        public void SaveDetailsTimer(object state)
        {
            C.WriteBottomLeft("Saving...");
            SaveDetails();
        }

        public void Spider()
        {
            _Details.Add(GetResourcesFromPage(BasePath.AbsoluteUri));

            var links = LinksToSpider().ToArray();

            var timer = new Timer(SaveDetailsTimer, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

            Progress.Progress = new ProgressControl("Crawling", links.Length);

            while (links.Length > 0)
            {
                Progress.Progress.SetTotal(links.Length);
                Parallel.ForEach(links, new ParallelOptions { MaxDegreeOfParallelism = MaxThreads }, link =>
                {
                    var pageDetails = GetResourcesFromPage(link);
                    var result = _Details.Add(pageDetails);
                    if (!result)
                        C.History($"DUP -> {link}");
                    //else
                    //    C.History($"{link}");

                    Progress.Progress.IncrementDone();
                });

                SaveDetails();
                links = LinksToSpider().ToArray();
            }
            timer.Dispose();

            Progress.Progress.Enabled = false;

            var tLookup = _Timings.ToLookup(o => o.HostPath);
            var allLinks = _Details.SelectMany(o => o.All).Concat(_Details.Select(o => o.Path.AbsoluteUri)).Select(l => new TimedUri(new Uri(l))).Distinct().ToArray();

            foreach (var link in allLinks)
                _Timings.Add(link);

            Progress.Progress.Reset("Timing", allLinks.Length);

            Parallel.ForEach(allLinks, new ParallelOptions { MaxDegreeOfParallelism = MaxThreads }, link =>
            {
                if (link.HttpCode == 0)
                    link.GetTimed();

                Progress.Progress.IncrementDone();
            });

            SaveTimings();

            Console.WriteLine("Writing output");
            using (var file = File.CreateText($"{BasePath.Host}.urls"))
            {
                foreach (var value in _Details.Where(x => x.IsSpiderable))
                {
                    if (value.Css != null)
                        foreach (var o in value.Css)
                            file.WriteLine(GetExportLine(value.Path, "CSS", o));
                    if (value.Javascript != null)
                        foreach (var o in value.Javascript)
                            file.WriteLine(GetExportLine(value.Path, "JS", o));
                    if (value.Images != null)
                        foreach (var o in value.Images)
                            file.WriteLine(GetExportLine(value.Path, "IMAGE", o));
                    if (value.Links != null)
                        foreach (var o in value.Links)
                            file.WriteLine(GetExportLine(value.Path, "LINK", o));
                    if (value.Forms != null)
                        foreach (var o in value.Forms)
                            file.WriteLine(GetExportLine(value.Path, "FORM", o));
                }
            }

            using (var file = File.CreateText($"{BasePath.Host}.metrics"))
            {
                foreach (var timing in _Timings)
                    file.WriteLine($"{timing.Path.AbsoluteUri}\t{timing.HttpCode}\t{timing.Bytes}\t{timing.Elapsed}");
            }
        }

        private string GetExportLine(Uri path, string type, string link)
        {
            return $"{path}\t{type}\t{link}";
        }


        public IEnumerable<string> LinksToSpider()
        {
            var lookup = _Details.ToLookup(o => o.HostPath);
            return _Details.Where(o => o.Links != null).SelectMany(o => o.Links.Where(l => o.IsSpiderable && IsLocalPage(l) && !lookup.Contains(l.Substring(l.IndexOf('/') + 2)) && !UriExcludes.Any(u => l.Contains(u)))).Distinct();
        }

        public IEnumerable<PageDetails> GetPages(PageDetails originalPage)
        {
            foreach (var link in originalPage.Links.Where(o => IsLocalPage(o)))
                if (!HaveVisitedPage(link))
                    yield return GetResourcesFromPage(link);
        }

        public PageDetails GetResourcesFromPage(string path)
        {

            var pageDetails = new PageDetails();
            pageDetails.SetUri(new Uri(BasePath, path));

            if (pageDetails.Path.Scheme.ToLower() == "http" || pageDetails.Path.Scheme.ToLower() == "https")
            {
                var tries = 0;

                HtmlDocument doc = null;
                var uStop = false;
                var webClient = new WebClient();

                while (!uStop && tries < 3)
                {
                    try
                    {
                        var html = webClient.DownloadString(pageDetails.Path);
                        doc = new HtmlDocument();
                        doc.LoadHtml(html);
                        pageDetails.HttpResponse = 200;
                        uStop = true;
                    }
                    catch (WebException webEx)
                    {
                        var responseCode = new Regex("[0-9]{1,}").Match(webEx.Message);
                        if (responseCode.Success)
                            pageDetails.HttpResponse = int.Parse(responseCode.Value);
                        uStop = true;
                    }
                    catch (Exception ex)
                    {
                        Task.Delay(1000).Wait();
                        tries++;
                    }
                }

                if (pageDetails.HttpResponse == 200 && doc != null && !string.IsNullOrEmpty(doc.Text))
                {
                    try
                    {
                        var allElements = doc.DocumentNode.SelectNodes("//*").ToArray();

                        pageDetails.Css = allElements.Where(o => string.Compare(o.Name, "link", true) == 0 && o.Attributes["rel"] != null).Where(o => o.GetAttributeValue("rel", null).ToLower() == "stylesheet").Select(o => o.GetAttributeValue("href", null)).Where(o => !string.IsNullOrEmpty(o) && !UriStartsWithExcludes.Any(u => o.StartsWith(u, StringComparison.CurrentCultureIgnoreCase)) && !UriExcludes.Any(u => o.Contains(u))).Select(o => MakeAbsoluteUri(pageDetails.Path, o)).Where(z => z != null).Distinct().ToArray();
                        pageDetails.Javascript = allElements.Where(o => string.Compare(o.Name, "script", true) == 0 && o.Attributes["src"] != null).Select(o => o.GetAttributeValue("src", null)).Where(o => !string.IsNullOrEmpty(o) && !UriStartsWithExcludes.Any(u => o.StartsWith(u, StringComparison.CurrentCultureIgnoreCase)) && !UriExcludes.Any(u => o.Contains(u))).Select(o => MakeAbsoluteUri(pageDetails.Path, o)).Where(z => z != null).Distinct().ToArray();
                        pageDetails.Images = allElements.Where(o => string.Compare(o.Name, "img", true) == 0 && o.Attributes["src"] != null).Select(o => o.GetAttributeValue("src", null)).Where(o => !string.IsNullOrEmpty(o) && !UriStartsWithExcludes.Any(u => o.StartsWith(u, StringComparison.CurrentCultureIgnoreCase)) && !UriExcludes.Any(u => o.Contains(u))).Select(o => MakeAbsoluteUri(pageDetails.Path, o)).Where(z => z != null).Distinct().ToArray();
                        pageDetails.Links = allElements.Where(o => string.Compare(o.Name, "a", true) == 0 && o.Attributes["href"] != null).Select(o => o.GetAttributeValue("href", null)).Where(o => !string.IsNullOrEmpty(o) && !UriStartsWithExcludes.Any(u => o.StartsWith(u, StringComparison.CurrentCultureIgnoreCase)) && !UriExcludes.Any(u => o.Contains(u))).Select(o => MakeAbsoluteUri(pageDetails.Path, o)).Where(z => z != null).Distinct().ToArray();
                        pageDetails.Forms = allElements.Where(o => string.Compare(o.Name, "form", true) == 0 && o.Attributes["action"] != null).Select(o => o.GetAttributeValue("action", null)).Where(o => !string.IsNullOrEmpty(o) && !UriStartsWithExcludes.Any(u => o.StartsWith(u, StringComparison.CurrentCultureIgnoreCase)) && !UriExcludes.Any(u => o.Contains(u))).Select(o => MakeAbsoluteUri(pageDetails.Path, o)).Where(z => z != null).Distinct().ToArray();

                        pageDetails.Process();

                        allElements = null;
                    }
                    catch
                    {
                        pageDetails.IsSpiderable = false;
                    }
                }
                else
                    pageDetails.IsSpiderable = false;
            }
            else
                pageDetails.IsSpiderable = false;

            return pageDetails;
        }

        private Uri MakeBase(Uri uri)
        {
            var path = uri.GetLeftPart(UriPartial.Path);
            if (path.IndexOf('?') > 0)
                path = path.Substring(0, path.Length - path.IndexOf('?'));
            return new Uri(path);
        }

        private string MakeAbsoluteUri(Uri path, string url)
        {
            try
            {
                var fuckingPound = url.IndexOf('#');
                if (fuckingPound > -1)
                    url = url.Substring(0, fuckingPound);
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri uri))
                    return (uri.IsAbsoluteUri ? uri : new Uri(path, uri)).AbsoluteUri;
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private bool IsLocalPage(string path)
        {
            var temp = new Uri(path);
            if (temp.Scheme != "http" && temp.Scheme != "https") return false;
            var rHostname = string.Join("", Hostname.Reverse());
            var rPathname = string.Join("", temp.Host.Reverse());
            return rPathname.IndexOf(rHostname + ".", StringComparison.CurrentCultureIgnoreCase) > -1;
        }

        private bool HaveVisitedPage(string path)
        {
            return _Details.Any(o => o.Compare(new Uri(path)));
        }

        static string ReverseDomainName(string domain)
        {
            return string.Join(".", domain.Split('.').Reverse());
        }
    }
}
