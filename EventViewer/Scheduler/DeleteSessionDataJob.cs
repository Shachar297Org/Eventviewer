using EventViewer.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;

namespace EventViewer.Scheduler
{
    [DisallowConcurrentExecution]
    public class DeleteSessionDataJob : IJob
    {
        private readonly ILogger<DeleteSessionDataJob> _logger;
        private readonly ILiteDBEventsDataService _liteDBService;

        public DeleteSessionDataJob(ILogger<DeleteSessionDataJob> logger, ILiteDBEventsDataService liteDBService)
        {
            _logger = logger;
            _liteDBService = liteDBService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Deleting expired sessions from DB");

            var count = _liteDBService.DeleteExpiredSessions();

            if (count > 0)
            {
                _logger.LogInformation($"Deleted {count} expired sessions from DB");
            }
            
            return Task.CompletedTask;
        }
    }
}
