using GenerateIISLogs.Utility;
using GenerateIISLogs.Website;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GenerateIISLogs
{
    public class IISLog
    {
        private static ManualResetEvent _QueueLock = new ManualResetEvent(false);
        private Random _Random = new Random(RandomSeeds.GetSeed());
        private static Queue<IISLog> _Queue = new Queue<IISLog>();
        private static Timer _Timer = null;

        public DateTime Timestamp { get; private set; }
        public string Date { get; private set; }

        public string Time { get; private set; }

        public string Site { get; private set; }

        public string Method { get; private set; }

        public string Page { get; private set; }

        public string QueryString { get; private set; }

        public int Port { get; private set; }

        public string Username { get; private set; }

        public string ClientHost { get; private set; }

        public string UserAgent { get; private set; }

        public string Referer { get; private set; }

        public int Response { get; private set; }

        public int Subresponse { get; private set; }

        public int ScStatus { get; private set; }

        public int ServerClientBytes { get; private set; }

        public int ClientServerBytes { get; private set; }

        public int TimeTaken { get; private set; }

        public Task WaitTask { get; private set; }

        public int WaitTime { get; private set; }

        public static IISLog Generate(SiteUser siteuser, TimedUri resourceMetric = null)
        {
            var iislog = new IISLog();

            try
            {
                var metric = resourceMetric == null ? siteuser.CurrentMetric : resourceMetric;
                var random = new Random(Environment.TickCount);

                iislog.Timestamp = DateTime.UtcNow;
                iislog.Date = iislog.Timestamp.ToString("yyyy-MM-dd");
                iislog.Time = iislog.Timestamp.ToString("HH:mm:ss");
                iislog.Site = $"10.13.0.{random.Next(1, 10)}";
                iislog.Method = "GET";
                iislog.Page = metric.Path.LocalPath;
                iislog.QueryString = string.IsNullOrEmpty(metric.Path.Query) ? "-" : metric.Path.Query;
                iislog.Port = metric.Path.Port;
                iislog.ClientHost = "-";
                iislog.UserAgent = siteuser.UserAgent.Replace(' ', '+');
                iislog.Referer = siteuser.LastUri == null ? "-" : siteuser.LastUri.AbsoluteUri;
                iislog.Response = metric.HttpCode;
                iislog.Subresponse = random.Next(50);
                iislog.ScStatus = random.Next(25);
                iislog.ServerClientBytes = metric.Bytes;
                iislog.ClientServerBytes = random.Next(512, 1500);
                iislog.TimeTaken = (int)metric.Elapsed.TotalMilliseconds;
                iislog.WaitTask = Task.Delay(iislog.TimeTaken + 1);
            }
            catch (Exception ex)
            {
                C.History($"E -> {ex.Message}");
            }

            return iislog;
        }

        public static IISLog Enqueue(SiteUser siteuser, TimedUri resourceMetric = null)
        {
            var iislog = Generate(siteuser, resourceMetric);

            iislog.WaitTask.Wait(); // Simulates downloading
            new Task(() =>
            {
                _QueueLock.WaitOne();
                _Queue.Enqueue(iislog);
            }).Start();

            return iislog;
        }

        private string LogOutput()
        {
            return $"{Date} {Time} {Site} {Method} {Page} {QueryString} {Port} {ClientHost} {UserAgent} {Referer} {Response} {Subresponse} {ScStatus}";
        }

        public static void Start()
        {
            _Timer = new Timer(WriteAllQueuedEntries, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        private static void WriteAllQueuedEntries(object state)
        {
            _QueueLock.Reset();

            var arrayCopy = _Queue.ToArray();
            _Queue.Clear();

            _QueueLock.Set();

            if (arrayCopy.Length == 0) return;

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var logfilename = $"ex{timestamp}.log";
            using (var logfile = File.AppendText(logfilename))
                foreach (var entry in arrayCopy)
                {
                    if (entry == null) continue;
                    logfile.WriteLine(entry.LogOutput());
                }
            //while (entries.Count > 0)
            //{
            //    var entry = entries.Dequeue();
            //    if (entry == null) continue;
            //    logfile.WriteLine(entry.LogOutput());
            //}
        }
    }
}
