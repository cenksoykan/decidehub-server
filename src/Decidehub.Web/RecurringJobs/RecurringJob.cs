using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;

namespace Decidehub.Web.RecurringJobs
{
    public abstract class RecurringJob
    {
        public async Task RunWithCulture(PerformContext context, string culture)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            await RunAsync(context);
        }

        protected abstract Task RunAsync(PerformContext context);

        public virtual string CronExpression => "*/5 * * * *";
    }
}