using EventViewer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace EventViewer.Interfaces
{
    public interface ICommandsApiClient
    {
        Task<List<CommandData>> GetCommands(string environment, string userid, DateTime? from = null, DateTime? to = null, Device? device = null, CancellationToken cancellationToken = default);
    }
}
