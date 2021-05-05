using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FakeDatabase;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace durable_function_perf_sandbox
{
    public static class APICall
    {
        private static TelemetryConfiguration appInsightsConfig = TelemetryConfiguration.CreateDefault();
        private static TelemetryClient client = new TelemetryClient(appInsightsConfig);

        // Read up on - use httpClient as static
        // https://docs.microsoft.com/en-us/azure/azure-functions/manage-connections
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("CallHTTPAPI")]
        public static async Task<string[]> CallHTTPAPI([ActivityTrigger] Record[] input, ILogger log)
        {
            var tasks = new string[input.Length];
            var apiEndpoint = Environment.GetEnvironmentVariable("ApiURL").ToString();

            for (int i = 0; i < input.Length; i++)
            {
                tasks[i] = await httpClient.GetStringAsync(apiEndpoint);
            }

            client.TrackMetric("API Calls Done", (double) input.Length);
            return tasks;

        }
    }
}