using System.Collections.Generic;

namespace fiskaltrust.Middleware.Demo
{
    public class POSSystemAPIRequest
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string? Body { get; set; }
        public string ResponseAction { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
    }
}
