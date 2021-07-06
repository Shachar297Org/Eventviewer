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
    }
}
