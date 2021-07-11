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

        public override bool Equals(object obj)
        {
            var arg = obj as JwtToken;
            return Token == arg.Token && Expiration == arg.Expiration;
        }
    }
}
