using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenerateIISLogs.Utility
{
    [Serializable]
    public class TimedUri
    {
        private int _HashCodeCache = 0;

        public Uri Path { get; set; }
        public string HostPath => Path.Host + Path.AbsolutePath;
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
        public int Bytes { get; set; } = 0;
        public int HttpCode { get; set; } = 0;

        public TimedUri(Uri uri)
        {
            Path = uri;
        }

        public void GetTimed()
        {
            var tries = 0;
            var stop = false;

            while (!stop && tries < 3)
            {
                try
                {
                    var size = 0;
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    var webRequest = WebRequest.Create(Path);
                    using (var webResponse = webRequest.GetResponse())
                    using (var responseStream = webResponse.GetResponseStream())
                    {
                        var buffer = new byte[1420];
                        var count = responseStream.Read(buffer, 0, buffer.Length);
                        size += count;
                        while (count > 0)
                        {
                            count = responseStream.Read(buffer, 0, buffer.Length);
                            size += count;
                        }
                    }

                    stopwatch.Stop();

                    Bytes = size;
                    Elapsed = stopwatch.Elapsed;
                    HttpCode = 200;

                    stop = true;
                }
                catch (WebException webEx)
                {
                    var responseCode = new Regex("[0-9]{1,}").Match(webEx.Message);
                    if (responseCode.Success)
                        HttpCode = int.Parse(responseCode.Value);
                    stop = true;
                }
                catch (Exception)
                {
                    Task.Delay(1000).Wait();
                    tries++;
                }
            }
        }

        public bool Compare(string url)
        {
            return Compare(new Uri(Path, url));
        }
        public bool Compare(Uri uri)
        {
            if (this.Path == null) return false;
            return string.Compare(uri.Host, this.Path.Host, StringComparison.CurrentCultureIgnoreCase) == 0 && string.Compare(uri.AbsolutePath, this.Path.AbsolutePath, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        public override bool Equals(object obj)
        {
            var that = obj as TimedUri;
            if (that == null) return false;
            return that.HostPath == this.HostPath;
        }

        public override int GetHashCode()
        {
            if (_HashCodeCache == 0)
                _HashCodeCache = HostPath.GetHashCode();
            return _HashCodeCache;
        }
    }
}
