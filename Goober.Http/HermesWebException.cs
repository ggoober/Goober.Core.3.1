using System;
using System.Net;

namespace Goober.Http
{
    public class HermesWebException : WebException
    {
        public string BodyAsString { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public HermesWebException(string message, HttpStatusCode code, string body) : base(message)
        {            
            BodyAsString = body;
            HttpStatusCode = code;
        }

        public HermesWebException(string message, HttpStatusCode code, Exception innerException, string body) : base(message, innerException)
        {
            BodyAsString = body;
            HttpStatusCode = code;
        }
    }
}
