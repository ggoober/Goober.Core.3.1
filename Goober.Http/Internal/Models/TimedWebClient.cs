using System;
using System.Net;
using System.Text;

namespace Goober.Http.Internal.Models
{
    internal class TimedWebClient : WebClient
    {
        public int Timeout { get; set; } = 10000;

        public TimedWebClient()
        {
            Encoding = Encoding.UTF8;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            try
            {
                var objWebRequest = base.GetWebRequest(address);
                objWebRequest.Timeout = this.Timeout;
                return objWebRequest;
            }
            catch (Exception ex)
            {
                ex.Data.Add("Url", address);
                ex.Data.Add("Exception.StackTrace", ex.StackTrace);
                ex.Data.Add("Exception.Message", ex.Message);
                throw;
            }
        }
    }
}
