using System.Threading.Tasks;
using Decidehub.Core.Interfaces;

namespace Decidehub.Web.RecurringJobs
{
    public class CheckPollCompletionJob : RecurringJob
    {
        private readonly IPollJobService _pollJobService;

        public CheckPollCompletionJob(IPollJobService pollJobService)
        {
            _pollJobService = pollJobService;
        }

        public override string CronExpression => "* * * * *";

        protected override async Task RunAsync()
        {
            await _pollJobService.CheckPollCompletion();
        }
    }
}