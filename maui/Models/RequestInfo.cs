using System;
using System.Collections.Generic;
using System.Text;

namespace fiskaltrust.Middleware.Demo.Models
{
    public class RequestInfo
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string HeaderB64 { get; set; }
        public string? BodyB64 { get; set; }


    }
}
