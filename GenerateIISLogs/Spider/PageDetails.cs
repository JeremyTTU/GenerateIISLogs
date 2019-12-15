using GenerateIISLogs.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerateIISLogs.Spider
{
    [Serializable]
    public class PageDetails
    {
        private int _HashCodeCache = 0;
        public bool IsSpiderable = false;

        public Uri Path { get; private set; } = null;
        public string HostPath => Path.Host + Path.PathAndQuery;
        public int HttpResponse { get; set; } = 0;
        public TimeSpan Offset { get; set; } = TimeSpan.Zero;
        public int Bytes { get; set; } = -1;
        public string[] Css { get; set; } = null;
        public string[] Javascript { get; set; } = null;
        public string[] Images { get; set; } = null;
        public string[] Links { get; set; } = null;
        public string[] Forms { get; set; } = null;

        public string[] All => ArrayOrEmpty(Css).Concat(ArrayOrEmpty(Javascript)).Concat(ArrayOrEmpty(Images)).Concat(ArrayOrEmpty(Links)).Where(o => !string.IsNullOrEmpty(o)).ToArray();

        public string[] ArrayOrEmpty(string[] array)
        {
            return array == null ? new[] { string.Empty } : array;
        }

        public void Process()
        {
            if (Css != null)
                foreach (var t in Css)
                    t.ToString();
            if (Javascript != null)
                foreach (var t in Javascript)
                    t.ToString();
            if (Images != null)
                foreach (var t in Images)
                    t.ToString();
            if (Links != null)
                foreach (var t in Links)
                    t.ToString();
            if (Forms != null)
                foreach (var t in Forms)
                    t.ToString();
            IsSpiderable = true;
        }

        public bool Compare(string url)
        {
            return Compare(new Uri(Path, url));
        }
        public bool Compare(Uri uri)
        {
            if (this.Path == null) return false;
            return string.Compare(uri.Host, this.Path.Host, StringComparison.CurrentCultureIgnoreCase) == 0 && string.Compare(uri.PathAndQuery, this.Path.PathAndQuery, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        public override bool Equals(object obj)
        {
            var that = obj as PageDetails;
            if (that == null) return false;
            return string.Compare(this.HostPath, that.HostPath, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        public override int GetHashCode()
        {
            if (_HashCodeCache == 0)
                _HashCodeCache = HostPath.GetHashCode();
            return _HashCodeCache;
        }

        internal void SetUri(Uri path)
        {
            Path = path;
        }

        internal void SetUri(string path)
        {
            Path = new Uri(path);
        }
    }
}
