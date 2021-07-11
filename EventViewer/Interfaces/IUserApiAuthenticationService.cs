using EventViewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventViewer.Interfaces
{
    public interface IUserApiAuthenticationService
    {
        Task<User> GetTokensForUser(User user, string tokenUri);
        Task<User> RefreshTokensForUser(User user, string tokenUri);
    }
}
