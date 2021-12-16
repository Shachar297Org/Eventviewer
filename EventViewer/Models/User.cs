using EventViewer.Models.ApiLogin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Models
{
    public class User
    {
        public JwtToken AccessToken { get; set; }
        public JwtToken RefreshToken { get; set; }

        public string UserId { get; set; }

        public string Environment { get; set; }
        public bool UsesBasicAuth { get; set; }

        public override bool Equals(object obj)
        {
            var arg = obj as User;
            return AccessToken != null && RefreshToken != null && AccessToken.Equals(arg.AccessToken) 
                && RefreshToken.Equals(arg.RefreshToken) && Environment.Equals(arg.Environment) && UserId.Equals(arg.UserId);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
