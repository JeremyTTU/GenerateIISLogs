using GenerateIISLogs.Data;
using GenerateIISLogs.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GenerateIISLogs.Website
{
    public class SiteUser
    {
        private Random _Random = new Random(RandomSeeds.GetSeed());
        private List<string> _UrlCache = new List<string>();
        private CancellationTokenSource _Cancellation = new CancellationTokenSource();
        private Task _SurfTask = null;

        public string IpAddress { get; private set; }

        public string UserAgent { get; private set; }

        public WebsiteReplica Current { get; private set; } = null;

        public Uri CurrentUri => Current == null ? null : Current.Path;

        public TimedUri CurrentMetric => CurrentUri == null ? null : WebsiteReplica.TimingLookupByPath[CurrentUri].FirstOrDefault();

        public WebsiteReplica Last { get; private set; } = null;

        public Uri LastUri => Last == null ? null : Last.Path;

        public TimedUri LastMetric => LastUri == null ? null : WebsiteReplica.TimingLookupByPath[LastUri].FirstOrDefault();

        public string HttpVersion { get; private set; }

        public SiteUser()
        {
            IpAddress = $"{_Random.Next(253) + 1}.{_Random.Next(253) + 1}.{_Random.Next(253) + 1}.{_Random.Next(253) + 1}";
            UserAgent = ResourceData.GetRandomData("UserAgents");
            switch (_Random.Next(3))
            {
                case 0:
                    HttpVersion = "HTTP/1.0";
                    break;
                case 1:
                    HttpVersion = "HTTP/1.1";
                    break;
                case 2:
                    HttpVersion = "HTTP/2.0";
                    break;
            }
        }

        public void SurfTheInternet()
        {
            _SurfTask = new Task(() =>
            {
                var cancelToken = _Cancellation.Token;

                while (!cancelToken.IsCancellationRequested)
                {
                    ClickLink();

                    var stopwatch = new Stopwatch();

                    stopwatch.Start();

                    var resources = Current.PageResources.Select(p => WebsiteReplica.TimingLookupByPath[p].FirstOrDefault()).Where(o => o != null).ToArray();

                    var page = IISLog.Enqueue(this, CurrentMetric);

                    Parallel.ForEach(resources, new ParallelOptions { MaxDegreeOfParallelism = 4 }, resource => IISLog.Enqueue(this, resource));

                    stopwatch.Stop();

                    C.History($"V -> {CurrentUri} {resources.Count()} / {stopwatch.Elapsed}");

                    Task.Delay(_Random.Next(8000, 30000)).Wait();
                }
            });
            _SurfTask.Start();
        }

        private static int MAX_DOWNLOADS = 4;

        private async Task DownloadAsync(TimedUri[] timedUris)
        {
            using (var semaphore = new SemaphoreSlim(MAX_DOWNLOADS))
            {
                var tasks = timedUris.Select((metric) => new Task(() =>
                {
                    semaphore.WaitAsync();
                    try
                    {
                        IISLog.Enqueue(this, metric);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                foreach (var task in tasks)
                    task.Start();

                await Task.WhenAll(tasks.ToArray());
            }
        }

        public void CancelSurfing()
        {
            _Cancellation.Cancel();
        }

        public void ClickLink()
        {
            Last = Current;
            Current = Current == null ? WebsiteReplica.Homepage() : Current.Navigate();
        }

        public dynamic GetResourceTotals()
        {
            var resources = Current.PageResources.Select(p => WebsiteReplica.TimingLookupByPath[p].FirstOrDefault()).Where(o => o != null);

            var totalSize = resources.Where(o => o.HttpCode == 200).Sum(o => o.Bytes);
            var totalElapsed = TimeSpan.FromMilliseconds(resources.Where(o => o.HttpCode == 200).Sum(o => o.Elapsed.TotalMilliseconds));

            return new { TotalResources = resources.Count(), TotalSize = totalSize, TotalElapsed = totalElapsed };
        }


        public void DownloadResources()
        {
            // Main File...
            IISLog.Generate(this);

            foreach (var resourceMetric in this.Current.PageMetrics)
                IISLog.Generate(this, resourceMetric);
        }
    }
}
