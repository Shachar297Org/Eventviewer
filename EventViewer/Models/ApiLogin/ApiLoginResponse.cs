using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventViewer.Models.ApiLogin
{
	public class ApiLoginResponse
	{
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public string UserId { get; set; }
		public string TenantId { get; set; }
		public string Email { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Phone { get; set; }
		public bool EmailVerified { get; set; }
		public bool PhoneVerified { get; set; }
		public bool PasswordResetRequired { get; set; }
		public string MfaRequestToken { get; set; }
		public List<string> GroupNames { get; set; }
		public JwtToken AccessJwt { get; set; }
		public string Locale { get; set; }
		public JwtToken RefreshJwt { get; set; }
		public bool MfaRequired { get; set; }
        public string Environment { get; set; }
    }
}
