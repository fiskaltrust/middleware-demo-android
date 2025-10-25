using System.Collections.Generic;

namespace fiskaltrust.Middleware.Demo
{
    public class POSSystemAPIRequest
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Body { get; set; }
        public string ResponseAction { get; set; }
        public string RequestId { get; set; }
    }
}