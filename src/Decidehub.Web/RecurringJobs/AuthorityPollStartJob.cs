using System.Threading.Tasks;
using Decidehub.Core.Interfaces;
using Hangfire.Server;

namespace Decidehub.Web.RecurringJobs
{
    public class AuthorityPollStartJob : RecurringJob
    {
        private readonly IPollJobService _pollJobService;

        public override string CronExpression => "* * * * *";

        public AuthorityPollStartJob(IPollJobService pollJobService)
        {
            _pollJobService = pollJobService;
        }

        protected override async Task RunAsync()
        {
            await _pollJobService.AuthorityPollStart();
        }
    }
}