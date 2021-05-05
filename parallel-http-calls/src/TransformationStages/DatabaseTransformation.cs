using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace durable_function_perf_sandbox
{
    public static class DatabaseTransformation
    {
        [FunctionName("CallSelectIntoInDatabase")]
        public static async Task CallSelectIntoInDatabase([ActivityTrigger] string unused, ILogger log) {
            // Calls EF Core to execute SQL Statment
            // See: https://docs.microsoft.com/en-us/ef/core/querying/raw-sql
            await Task.Delay(1000);
        }
    }
}