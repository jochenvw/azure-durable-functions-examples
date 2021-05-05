using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using TransformationFunctions.DTOs;

namespace TansformationFunctions
{
    public partial class FileTransformer
    {
        [FunctionName("FileTransformer")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            // Read BLOB file into lines
            var lines = await context.CallActivityAsync<InputFormat[]>("ReadInputFileFromBlob", "unused-but-cannotbenull");
            var batches = await context.CallActivityAsync<InputFormat[][]>("GroupLinesInBatches", lines);
            var numberOfBatches = batches.Length;

            var aTasks = new Task<FormatAInstance[]>[numberOfBatches];
            for (var i = 0; i < numberOfBatches; i++)
            {
                aTasks[i] = context.CallActivityAsync<FormatAInstance[]>("ConvertCSVToFormatA", batches[i]);
            }
            await Task.WhenAll(aTasks);

            var apiCalls = new Task<FormatAInstance[]>[numberOfBatches];
            for (var i = 0; i < numberOfBatches; i++)
            {
                apiCalls[i] = context.CallActivityAsync<FormatAInstance[]>("SendToAPI", aTasks[i].Result);
            }
            await Task.WhenAll(apiCalls);

            var bTasks = new Task<FormatBInstance[]>[numberOfBatches];
            for (var i = 0; i < numberOfBatches; i++)
            {
                bTasks[i] = context.CallActivityAsync<FormatBInstance[]>("ConvertFormatAToFormatB", aTasks[i].Result);
            }
            await Task.WhenAll(bTasks);

            var dbWrites = new Task<bool>[numberOfBatches];
            for (var i = 0; i < numberOfBatches; i++)
            {
                dbWrites[i] = context.CallActivityAsync<bool>("WriteToDatabase", bTasks[i].Result);
            }
            await Task.WhenAll(dbWrites);

            for (var i = 0; i < numberOfBatches; i++)
            {
                dbWrites[i] = context.CallActivityAsync<bool>("WriteToDatabaseUsingEF", bTasks[i].Result);
            }
            await Task.WhenAll(dbWrites);

            return "Done!";
        }


        /// <summary>
        /// Trigger function - starts the orchestration
        /// </summary>
        [FunctionName("FileTransformer_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var content = req.Content;
            var instanceId = await starter.StartNewAsync("FileTransformer", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}