using System;
using System.Linq;
using System.Threading.Tasks;
using FakeDatabase;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;

namespace durable_function_perf_sandbox
{
    public static class DatabaseExtraction
    {
        private static TelemetryConfiguration appInsightsConfig = TelemetryConfiguration.CreateDefault();
        private static TelemetryClient client = new TelemetryClient(appInsightsConfig);
        
        [FunctionName("GetRecordBatches")]
        public static Task<Record[][]> GetRecordBatches([ActivityTrigger] string unused, ILogger log) {
            var db = new DbContext();
            var numRecords = db.Records.Count();
            var batchSize = Convert.ToInt32(Environment.GetEnvironmentVariable("BatchSize"));

            var numberOfBatches = (int) System.Math.Ceiling((double) numRecords / batchSize);
            var result = new Record[numberOfBatches][];

            client.TrackTrace($"Running with batchSize: {batchSize} - expecting to create {numberOfBatches} batches for {numRecords} records");
            client.TrackTrace($"Running with batchSize: {batchSize}");
            for (int i = 0; i < numberOfBatches; i++)
            {   
                result[i] = db.Records.Skip(i * batchSize).Take(batchSize).ToArray();
            }            
            return Task.FromResult(result);
        }
    }
}