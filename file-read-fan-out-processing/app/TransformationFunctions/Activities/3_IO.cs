using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TransformationFunctions.DTOs;
using System.Net.Http;

namespace TransformationFunctions.Activities
{
    public static class IOFunctions
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("SendToAPI")]
        public static FormatAInstance[] SendToAPI([ActivityTrigger] FormatAInstance[] input, ILogger log)
        {
            var endpoint = System.Environment.GetEnvironmentVariable("APIEndpoint");

            for (int i = 0; i < input.Length; i++)
            {
                var message = new { Message = input[i].ToString() };
                httpClient.PostAsJsonAsync(endpoint, message).Wait();
            }

            log.LogInformation($"API: {input.Length} messages sent to {endpoint}");
            log.LogMetric("API Calls done", input.Length);
            return input;
        }
    }
}
