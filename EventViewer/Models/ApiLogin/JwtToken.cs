using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Models.ApiLogin
{
    public class JwtToken
    {
        public string Token { get; set; }
        public long Expiration { get; set; }
    }
}
