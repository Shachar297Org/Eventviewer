using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventViewer.Models.ApiLogin
{
  public class AuthOperationData
  {
    public string UserId { get; set; }
    public string TenantId { get; set; }
    public bool MfaRequired { get; set; }
    public bool PasswordResetRequired { get; set; }

    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public string Expiration { get; set; }

    public JwtToken AccessJwt { get; set; }
    public JwtToken RefreshJwt { get; set; }
  }
}
