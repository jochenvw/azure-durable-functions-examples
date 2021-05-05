using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using FakeDatabase;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;


namespace durable_function_perf_sandbox
{
    public class performance_sandbox
    {
        private static TelemetryConfiguration appInsightsConfig = TelemetryConfiguration.CreateDefault();
        private static TelemetryClient client = new TelemetryClient(appInsightsConfig);

        [FunctionName("performance_sandbox")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            await context.CallActivityAsync("CallSelectIntoInDatabase", null);
            log.LogInformation($"CallSelectIntoInDatabase done !");

            var result = await context.CallActivityAsync<Record[][]>("GetRecordBatches", null);
            log.LogInformation($"GetRecordBatches done ! {result.Length} batches");

            var httpCallTasks = new Task[result.Length];
            for (var i = 0; i < result.Length; i++)
            {
                httpCallTasks[i] = context.CallActivityAsync<string[]>("CallHTTPAPI", result[i]);
            }
            await Task.WhenAll(httpCallTasks);
            log.LogInformation($"HTTP Calls Done!");
        }

        [FunctionName("performance_sandbox_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("performance_sandbox", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}