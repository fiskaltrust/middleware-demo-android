using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.Demo.Models
{
    public class RequestInfo
    {
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string HeaderB64 { get; set; } = string.Empty;
        public string? BodyB64 { get; set; }
    }
}
