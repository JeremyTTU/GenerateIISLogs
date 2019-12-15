using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace GenerateIISLogs.Utility
{
    public class TimedWebClient : WebClient
    {
        public int Timeout { get; set; }

        public TimedWebClient()
        {
            this.Timeout = 10000;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout = this.Timeout;
            return objWebRequest;
        }
    }
}
